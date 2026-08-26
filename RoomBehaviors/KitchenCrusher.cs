using GameNetcodeStuff;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    internal class KitchenCrusher : NetworkBehaviour 
    {
#pragma warning disable 0649 // Assigned in Unity Editor
        public AudioSource CrusherSound;
        public AudioClip SoundToPlay;
        public AudioClip ClickSound;
        public GameObject Crusher;
#pragma warning restore 0649
        private Vector3 CrusherStartPos;
        private Vector3 CrusherEndPos;

        private PlayerControllerB victim;
        System.Random CrusherRandom;
        private NetworkVariable<bool> ActivateCrusher = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private readonly float LerpDuration = 0.3f;
        private float timeElapsed = 0.0f;

        private bool clientInsideCrusher = false;
        private bool finalDamageApplied = false;

        private static readonly WTOBase.WTOLogger Log = new(typeof(KitchenCrusher), LogSourceType.Room);

        private void OnTriggerEnter(Collider other) 
        {
            // Server processes crusher active or not
            // when it sees a client touch it.
            if(IsServer && !ActivateCrusher.Value) 
            {
                if (other.TryGetComponent<PlayerControllerB>(out _))
                {
                    if (CrusherRandom.Next(1, 100) > 45)
                    {
                        ActivateCrusher.Value = true;
                        PlayCrusherActiveSoundClientRpc();
                    }
                    else
                    {
                        PlayCrusherClickSoundClientRpc();
                    }
                }
            }

            if(IsClient && other.TryGetComponent<PlayerControllerB>(out var player) && player == StartOfRound.Instance.localPlayerController)
            {
                clientInsideCrusher = true;
            }
        }

        private void OnTriggerExit(Collider other) 
        {
            if(IsClient && other.TryGetComponent<PlayerControllerB>(out var player) && player == StartOfRound.Instance.localPlayerController)
            {
                clientInsideCrusher = false;
            }
        }

        [ClientRpc]
        private void PlayCrusherActiveSoundClientRpc()
        {
            CrusherSound.PlayOneShot(SoundToPlay);
        }

        [ClientRpc]
        private void PlayCrusherClickSoundClientRpc()
        {
            CrusherSound.PlayOneShot(ClickSound);
        }

        private void Start() 
        {
            CrusherRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
            CrusherStartPos = Crusher.transform.position;
            CrusherEndPos = Crusher.transform.position + (Vector3.Scale(new Vector3(-6, 0, -6), Crusher.transform.right));
        }

        private void Update() 
        {
            if (ActivateCrusher.Value) 
            {
                if(timeElapsed < LerpDuration) 
                {
                    float progress = timeElapsed / LerpDuration;
                    Crusher.transform.position = Vector3.Lerp(CrusherStartPos, CrusherEndPos, progress);
                    timeElapsed += Time.deltaTime;

                    if(progress > 0.75)
                    {
                        DamageLocalPlayerIfInsideCrusher();
                    }
                }
                else
                {
                    // Ensure it always hits the end point exactly, in case of any floating point inaccuracies.
                    Crusher.transform.position = CrusherEndPos;

                    // Final damage instance at 100% if we missed it in the above lerp (due to low fps that could skip over the 75% threshold).
                    if (!finalDamageApplied)
                    {
                        finalDamageApplied = true;
                        DamageLocalPlayerIfInsideCrusher();
                    }
                }
            }
        }

        private void DamageLocalPlayerIfInsideCrusher() 
        {
            if (IsClient && clientInsideCrusher)
            {
                // Damage local client if inside the crusher. This will always fire at least once, even at low fps.
                StartOfRound.Instance.localPlayerController.DamagePlayer(100, causeOfDeath: CauseOfDeath.Crushing);
            }
        }
    }
}
