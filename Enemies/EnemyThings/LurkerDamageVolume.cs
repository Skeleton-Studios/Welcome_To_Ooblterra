using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings {
    internal class LurkerDamageVolume : MonoBehaviour {
        #pragma warning disable 0649 // Assigned in Unity Editor
        public BabyLurkerAI OwningLurker;
        private bool CanDamagePlayer;
        private float TimeTillDamage = 3f;
        private void Update() {
            if(TimeTillDamage <= 0f) {
                CanDamagePlayer = true;
            } else {
                TimeTillDamage -= Time.deltaTime; 
            }
        }
        private void OnTriggerStay(Collider other) {
            PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
            if (!other.gameObject.CompareTag("Player") || !CanDamagePlayer) {
                return;
            }
            victim.DamagePlayer(15, hasDamageSFX: true, callRPC: true, CauseOfDeath.Drowning);
            OwningLurker.KillEnemyClientRpc(true);
        }
    }
}
