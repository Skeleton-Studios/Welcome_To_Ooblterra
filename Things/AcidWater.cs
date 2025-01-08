using GameNetcodeStuff;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class AcidWater : MonoBehaviour {

    public int UserSetDamageAmount;
    private float TimeSincePlayerDamaged = 0f;

    private static readonly WTOBase.WTOLogger Log = new(typeof(AcidWater), LogSourceType.Thing);

    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (!other.gameObject.CompareTag("Player")) {
            return;
        }
        DamageOverlappingPlayer(victim, 0.5f, ref TimeSincePlayerDamaged, UserSetDamageAmount);
    }

    public static void DamageOverlappingPlayer(PlayerControllerB victim, float TotalDamageTime, ref float TimeSinceDamageTaken, int DamageAmount) {
        if ((TimeSinceDamageTaken < TotalDamageTime)) {
            TimeSinceDamageTaken += Time.deltaTime;
            return;
        }
        if (victim != null) {
            TimeSinceDamageTaken = 0f;
            victim.DamagePlayer(DamageAmount, hasDamageSFX: true, callRPC: true, CauseOfDeath.Drowning);
            Log.Debug("New health amount: " + victim.health);
        }
    }
}
