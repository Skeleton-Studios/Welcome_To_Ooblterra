using HarmonyLib;
using LethalLevelLoader;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches
{
    internal class MonsterPatch {

        public static List<SpawnableEnemyWithRarity> InsideEnemies = new();
        public static List<SpawnableEnemyWithRarity> OutsideEnemies = new();
        public static List<SpawnableEnemyWithRarity> DaytimeEnemies = new();
        public static List<SpawnableEnemyWithRarity> AdultWandererContainer = new();

        private const string EnemyPath = "CustomEnemies/";
        public const bool ShouldDebugEnemies = true;

        private static readonly Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToInsideSpawnLists = new();
        private static readonly Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToOutsideSpawnLists = new();
        private static readonly Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToDaytimeSpawnLists = new();

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

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.AssignRandomEnemyToVent))]
        [HarmonyPrefix]
        private static void SetInsideEnemiesWTO(RoundManager __instance) {
            string PlanetName = __instance.currentLevel.PlanetName;
            if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow) {
                if (MoonsToInsideSpawnLists.TryGetValue(PlanetName, out List<SpawnableEnemyWithRarity> ResultEnemyList)) {
                    __instance.currentLevel.Enemies = ResultEnemyList;
                }
                return;
            }
            SetMonsterStuff(WTOBase.WTOForceInsideMonsters.Value, ref __instance.currentLevel.Enemies, MoonsToInsideSpawnLists[MoonPatch.MoonFriendlyName]);
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnRandomOutsideEnemy))]
        [HarmonyPrefix]
        private static void SetOutsideEnemiesWTO(RoundManager __instance, float timeUpToCurrentHour) {
                string PlanetName = __instance.currentLevel.PlanetName;
                if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow)
                {
                    if (MoonsToOutsideSpawnLists.TryGetValue(PlanetName, out List<SpawnableEnemyWithRarity> OutsideEnemyList))
                    {
                        __instance.currentLevel.OutsideEnemies = OutsideEnemyList;
                    }
                    return;
                }
                SetMonsterStuff(WTOBase.WTOForceOutsideMonsters.Value, ref __instance.currentLevel.OutsideEnemies, MoonsToOutsideSpawnLists[MoonPatch.MoonFriendlyName]);
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnRandomDaytimeEnemy))]
        [HarmonyPrefix]
        private static void SetDaytimeEnemiesWTO(RoundManager __instance) {
            string PlanetName = __instance.currentLevel.PlanetName;
            if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow) {
                if (MoonsToDaytimeSpawnLists.TryGetValue(PlanetName, out List<SpawnableEnemyWithRarity> DaytimeEnemyList)) {
                    __instance.currentLevel.OutsideEnemies = DaytimeEnemyList;
                }
                return;
            }
            SetMonsterStuff(WTOBase.WTOForceDaytimeMonsters.Value, ref __instance.currentLevel.DaytimeEnemies, MoonsToDaytimeSpawnLists[MoonPatch.MoonFriendlyName]);
        }


        //METHODS 
        public static void Start() {
            CreateEnemy("Wanderer.asset", DaytimeEnemies, 50, LethalLib.Modules.Enemies.SpawnType.Daytime, "WandererTerminal.asset", "WandererKeyword.asset");
            CreateEnemy("AdultWanderer.asset", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside, "AdultWandererTerminal.asset", "AdultWandererKeyword.asset");
            CreateEnemy("Gallenarma.asset", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default, "GallenTerminal.asset", "GallenKeyword.asset");
            CreateEnemy("EyeSecurity.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "EyeSecTerminal.asset", "EyeSecKeyword.asset");
            //CreateEnemy("Lurker.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "LurkerTerminal.asset", "LurkerKeyword.asset");
            CreateEnemy("OoblGhost.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "OoblGhostTerminal.asset", "OoblGhostKeyword.asset");
            CreateEnemy("Enforcer.asset", InsideEnemies, 10, LethalLib.Modules.Enemies.SpawnType.Default, "EnforcerTerminal.asset", "EnforcerKeyword.asset");
            CreateEnemy("BabyLurker.asset", InsideEnemies, 10, LethalLib.Modules.Enemies.SpawnType.Default, "BabyLurkerTerminal.asset", "BabyLurkerKeyword.asset");
            CreateEnemy("GhostPlayer.asset", OutsideEnemies, 10, LethalLib.Modules.Enemies.SpawnType.Outside);
            if (!MoonsToInsideSpawnLists.ContainsKey(MoonPatch.MoonFriendlyName)) {
                MoonsToInsideSpawnLists.Add(MoonPatch.MoonFriendlyName, MoonPatch.OoblterraExtendedLevel.SelectableLevel.Enemies);
            }
            if (!MoonsToOutsideSpawnLists.ContainsKey(MoonPatch.MoonFriendlyName)) {
                MoonsToOutsideSpawnLists.Add(MoonPatch.MoonFriendlyName, MoonPatch.OoblterraExtendedLevel.SelectableLevel.OutsideEnemies);
            }
            if (!MoonsToDaytimeSpawnLists.ContainsKey(MoonPatch.MoonFriendlyName)) {
                MoonsToDaytimeSpawnLists.Add(MoonPatch.MoonFriendlyName, MoonPatch.OoblterraExtendedLevel.SelectableLevel.DaytimeEnemies);
            }
        }

        public static void CreateEnemy(string EnemyName, List<SpawnableEnemyWithRarity> EnemyList, int rarity, LethalLib.Modules.Enemies.SpawnType SpawnType, string InfoName = null, string KeywordName = null) {
            string EnemyFolderName = EnemyName.Remove(EnemyName.Length - 6, 6) + "/";
            TerminalNode EnemyInfo = null;
            TerminalKeyword EnemyKeyword = null;

            EnemyType EnemyType = WTOBase.ContextualLoadAsset<EnemyType>(EnemyPath + EnemyFolderName + EnemyName);
            EnemyType.enemyPrefab.GetComponent<EnemyAI>().debugEnemyAI = false;

            if (InfoName != null) {
                EnemyInfo = WTOBase.ContextualLoadAsset<TerminalNode>(EnemyPath + EnemyFolderName + InfoName);
            }
            if (KeywordName != null) {
                EnemyKeyword = WTOBase.ContextualLoadAsset<TerminalKeyword>(EnemyPath + EnemyFolderName + KeywordName);
            }

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EnemyType.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(EnemyType, rarity, LethalLib.Modules.Levels.LevelTypes.None, SpawnType, /*new string[] { "OoblterraLevel" },*/ EnemyInfo, EnemyKeyword);
            EnemyList?.Add(new SpawnableEnemyWithRarity(EnemyType, rarity));
            Log.Info("Monster Loaded: " + EnemyName.Remove(EnemyName.Length - 6, 6));
        }

        private static void SetMonsterStuff(TiedToLabEnum TiedToLabState, ref List<SpawnableEnemyWithRarity> CurrentMoonEnemyList, List<SpawnableEnemyWithRarity> OoblterraEnemyList) {
            List<SpawnableEnemyWithRarity> WeightedOoblterraEnemies = new();
            foreach(SpawnableEnemyWithRarity Enemy in OoblterraEnemyList) {
                WeightedOoblterraEnemies.Add(new SpawnableEnemyWithRarity(Enemy.enemyType, Enemy.rarity * WTOBase.WTOWeightScale.Value));
            }
            switch (TiedToLabState) {
                case TiedToLabEnum.WTOOnly:
                    CurrentMoonEnemyList = OoblterraEnemyList;
                    break;
                case TiedToLabEnum.AppendWTO:
                    CurrentMoonEnemyList.AddRange(WeightedOoblterraEnemies);
                    break;
            }
        }
    }
}
