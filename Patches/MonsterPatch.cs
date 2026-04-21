using HarmonyLib;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches
{
    internal class MonsterPatch {
        private static readonly WTOBase.WTOLogger Log = new(typeof(MonsterPatch), LogSourceType.Generic);

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SetEnemyStunned))]
        [HarmonyPostfix]
        private static void SetOwnershipToStunningPlayer(EnemyAI __instance) { 
            if(__instance is not WTOEnemy || __instance.stunnedByPlayer == null){
                return;
            }
            Log.Info($"Enemy: {__instance.GetType()} STUNNED BY: {__instance.stunnedByPlayer}; Switching ownership...");
            __instance.ChangeOwnershipOfEnemy(__instance.stunnedByPlayer.actualClientId);
        }

        [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SetClientCalculatingAI))]
        [HarmonyPrefix]
        private static bool PreventGhostAgentEnable(EnemyAI __instance, bool enable) {
            // Fix for OoblGhostAI re-enabling the nav agent each frame.
            // This would cause a huge number of errors to be printed in the console.
            // The ghost does not even use the nav agent anyway
            // The base code for this simply calls
            // isClientCalculatingAI = enable
            // navAgent.enabled = enable.
            if (__instance is OoblGhostAI) {
                __instance.isClientCalculatingAI = enable;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UseSignalTranslatorClientRpc))]
        [HarmonyPostfix]
        private static void TellAllGhostsOfSignalTransmission() {
            OoblGhostAI[] Ghosts = GameObject.FindObjectsOfType<OoblGhostAI>();
            foreach(OoblGhostAI Ghost in Ghosts) {
                Ghost.EvalulateSignalTranslatorUse();
            }
        }
    }
}
