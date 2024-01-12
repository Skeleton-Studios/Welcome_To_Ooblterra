using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class AcidWater : MonoBehaviour {

    public int DamageAmount;
    private float TimeSincePlayerDamaged = 0f;

    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (!other.gameObject.CompareTag("Player")) {
            return;
        }
        if ((TimeSincePlayerDamaged < 0.5f)) {
            TimeSincePlayerDamaged += Time.deltaTime;
            return;
        }
        if (victim != null) {
            TimeSincePlayerDamaged = 0f;
            victim.DamagePlayer(DamageAmount, hasDamageSFX: true, callRPC: true, CauseOfDeath.Drowning);
            WTOBase.LogToConsole("New health amount: " + victim.health);
        }
            
    }
}
