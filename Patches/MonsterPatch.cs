using HarmonyLib;
using LethalLevelLoader;
using LethalLib;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches;
internal class MonsterPatch {

    public static List<SpawnableEnemyWithRarity> InsideEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> OutsideEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> AdultWandererContainer = new List<SpawnableEnemyWithRarity>();

    private static readonly AssetBundle EnemyBundle = WTOBase.MonsterAssetBundle;
    private const string EnemyPath = WTOBase.RootPath + "CustomEnemies/";
    private static bool EnemiesInList; 
    public const bool ShouldDebugEnemies = true;

    private static Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToInsideSpawnLists = new();
    private static Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToOutsideSpawnLists = new();
    private static Dictionary<string, List<SpawnableEnemyWithRarity>> MoonsToDaytimeSpawnLists = new();

    /*
    [HarmonyPatch(typeof(QuickMenuManager), "Debug_SetEnemyDropdownOptions")]
    [HarmonyPrefix]
    private static void AddMonstersToDebug(QuickMenuManager __instance) {
        if (EnemiesInList) {
            return;
        }
        var testLevel = __instance.testAllEnemiesLevel;
        var firstEnemy = testLevel.Enemies.FirstOrDefault(); //Grab all of the test enemies 
        if (firstEnemy == null) { //check to see if the list of enemies actually exists
            WTOBase.LogToConsole("Failed to get first enemy for debug list!");
            return;
        }
            
        var enemies = testLevel.Enemies;
        var outsideEnemies = testLevel.OutsideEnemies;
        var daytimeEnemies = testLevel.DaytimeEnemies;

        enemies.Clear();
        foreach(SpawnableEnemyWithRarity InsideEnemy in InsideEnemies) { 
            if (!enemies.Contains(InsideEnemy)){
                enemies.Add(new SpawnableEnemyWithRarity {
                    enemyType = InsideEnemy.enemyType,
                    rarity = InsideEnemy.rarity
                });
                WTOBase.LogToConsole("Added " + InsideEnemy.enemyType.name + "To debug list");
            }
        } 
         
        daytimeEnemies.Clear(); 
        foreach (SpawnableEnemyWithRarity DaytimeEnemy in DaytimeEnemies) {
            if (!daytimeEnemies.Contains(DaytimeEnemy)){
                daytimeEnemies.Add(new SpawnableEnemyWithRarity {
                    enemyType = DaytimeEnemy.enemyType,
                    rarity = DaytimeEnemy.rarity
                });
                WTOBase.LogToConsole("Added " + DaytimeEnemy.enemyType.name + "To debug list");
            }
        }

        outsideEnemies.Clear();
        outsideEnemies.Add(new SpawnableEnemyWithRarity {
            enemyType = AdultWandererContainer[0].enemyType,
            rarity = AdultWandererContainer[0].rarity
        });
        WTOBase.LogToConsole("Added " + AdultWandererContainer[0].enemyType.name + "To debug list");

        EnemiesInList = true;
    }
    */

    [HarmonyPatch(typeof(EnemyAI), "SetEnemyStunned")]
    [HarmonyPostfix]
    private static void SetOwnershipToStunningPlayer(EnemyAI __instance) { 
        if(__instance is not WTOEnemy || __instance.stunnedByPlayer == null){
            return;
        }
        WTOBase.LogToConsole($"Enemy: {__instance.GetType()} STUNNED BY: {__instance.stunnedByPlayer}; Switching ownership...");
        __instance.ChangeOwnershipOfEnemy(__instance.stunnedByPlayer.actualClientId);
    }

    [HarmonyPatch(typeof(HUDManager), "UseSignalTranslatorClientRpc")]
    [HarmonyPostfix]
    private static void TellAllGhostsOfSignalTransmission() {
        OoblGhostAI[] Ghosts = GameObject.FindObjectsOfType<OoblGhostAI>();
        foreach(OoblGhostAI Ghost in Ghosts) {
            Ghost.EvalulateSignalTranslatorUse();
        }
    }

    [HarmonyPatch(typeof(RoundManager), "AssignRandomEnemyToVent")]
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
    [HarmonyPatch(typeof(RoundManager), "SpawnRandomOutsideEnemy")]
    [HarmonyPrefix]
    private static void SetOutsideEnemiesWTO(RoundManager __instance) {
        string PlanetName = __instance.currentLevel.PlanetName;
        if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow) {
            if (MoonsToOutsideSpawnLists.TryGetValue(PlanetName, out List<SpawnableEnemyWithRarity> OutsideEnemyList)) {
                __instance.currentLevel.OutsideEnemies = OutsideEnemyList;
            }
            return;
        }
        SetMonsterStuff(WTOBase.WTOForceOutsideMonsters.Value, ref __instance.currentLevel.OutsideEnemies, MoonsToOutsideSpawnLists[MoonPatch.MoonFriendlyName]);
    }
    [HarmonyPatch(typeof(RoundManager), "SpawnRandomDaytimeEnemy")]
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

    EnemyType EnemyType = WTOBase.ContextualLoadAsset<EnemyType>(EnemyBundle, EnemyPath + EnemyFolderName + EnemyName);
    EnemyType.enemyPrefab.GetComponent<EnemyAI>().debugEnemyAI = false;

    if (InfoName != null) {
        EnemyInfo = WTOBase.ContextualLoadAsset<TerminalNode>(EnemyBundle, EnemyPath + EnemyFolderName + InfoName);
    }
    if (KeywordName != null) {
        EnemyKeyword = WTOBase.ContextualLoadAsset<TerminalKeyword>(EnemyBundle, EnemyPath + EnemyFolderName + KeywordName);
    }

    LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EnemyType.enemyPrefab);
    LethalLib.Modules.Enemies.RegisterEnemy(EnemyType, rarity, LethalLib.Modules.Levels.LevelTypes.None, SpawnType, /*new string[] { "OoblterraLevel" },*/ EnemyInfo, EnemyKeyword);
    EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = EnemyType, rarity = rarity });
    WTOBase.LogToConsole("Monster Loaded: " + EnemyName.Remove(EnemyName.Length - 6, 6));
}

    private static void SetMonsterStuff(TiedToLabEnum TiedToLabState, ref List<SpawnableEnemyWithRarity> CurrentMoonEnemyList, List<SpawnableEnemyWithRarity> OoblterraEnemyList) {
        List<SpawnableEnemyWithRarity> WeightedOoblterraEnemies = new();
        foreach(SpawnableEnemyWithRarity Enemy in OoblterraEnemyList) {
            WeightedOoblterraEnemies.Add(new SpawnableEnemyWithRarity { enemyType = Enemy.enemyType, rarity = Enemy.rarity * WTOBase.WTOWeightScale.Value });
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
