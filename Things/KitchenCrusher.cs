using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things {
    internal class KitchenCrusher : MonoBehaviour {
        public GameObject Crusher;
        System.Random CrusherRandom = new System.Random();
        bool ActivateCrusher;
        bool RetractCrusher;
        public Vector3 CrusherStartPos;
        public Vector3 CrusherEndPos;
        private PlayerControllerB victim;
        public AudioSource CrusherSound;
        public AudioClip SoundToPlay;

        private void OnTriggerEnter(Collider other) {
            if (!other.gameObject.CompareTag("Player")) {
                return;
            }
            victim = other.gameObject.GetComponent<PlayerControllerB>();
            WTOBase.LogToConsole("Player colliding");
            if (CrusherRandom.Next(1, 100) > 45) {
                if(ActivateCrusher == false) {
                    WTOBase.LogToConsole("activate crusher!");    
                    ActivateCrusher = true;
                    CrusherSound.PlayOneShot(SoundToPlay);
                }
            }
        }
        private void OnTriggerExit(Collider other) { 
            if(victim == other.gameObject.GetComponent<PlayerControllerB>()) {
                victim = null;
            }
        }

        private void Start() {
            CrusherStartPos = Crusher.transform.position;
            CrusherEndPos = Crusher.transform.position + (Vector3.Scale(new Vector3(-6, 0, -6), Crusher.transform.right));
        }

        private void SetCrusherRetract() {
            RetractCrusher = true;
        }

        private float LerpDuration = 0.3f;
        private float timeElapsed;

        private void Update() {
            if (ActivateCrusher) {
                if(timeElapsed < LerpDuration) { 
                    Crusher.transform.position = Vector3.Lerp(CrusherStartPos, CrusherEndPos, timeElapsed/LerpDuration);
                    timeElapsed += Time.deltaTime;
                    if(timeElapsed / LerpDuration > 0.75 && !(victim == null)){
                        victim.DamagePlayer(100, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing, 0);
                    }
                    return;
                }
                Invoke("SetCrusherRetract", 3);
                ActivateCrusher = false;
                timeElapsed = 0;
            }
            if (RetractCrusher) {
                Crusher.transform.position = Vector3.Lerp(CrusherEndPos, CrusherStartPos, timeElapsed / LerpDuration);
                timeElapsed += Time.deltaTime;
                return;
            }
        }
    }
}
