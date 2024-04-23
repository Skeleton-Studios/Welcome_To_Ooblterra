using HarmonyLib;
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

    //PATCHES
    [HarmonyPatch(typeof(QuickMenuManager), "Debug_SetEnemyDropdownOptions")]
    [HarmonyPrefix]
    private static void AddMonstersToDebug(QuickMenuManager __instance) {
        if (EnemiesInList) {
            return;
        }
        var testLevel = __instance.testAllEnemiesLevel;
        var firstEnemy = testLevel.Enemies.FirstOrDefault(); //Grab all of the test enemies 
        if (firstEnemy == null) { //check to see if the list of enemies actually exists
            WTOBase.WTOLogSource.LogMessage("Failed to get first enemy for debug list!");
            return;
        }
            
        var enemies = testLevel.Enemies;
        var outsideEnemies = testLevel.OutsideEnemies;
        var daytimeEnemies = testLevel.DaytimeEnemies;

        enemies.Clear();
        foreach(SpawnableEnemyWithRarity InsideEnemy in InsideEnemies) { 
            if (!enemies.Contains(InsideEnemy)) {
                enemies.Add(new SpawnableEnemyWithRarity {
                    enemyType = InsideEnemy.enemyType,
                    rarity = InsideEnemy.rarity
                });
                WTOBase.WTOLogSource.LogMessage("Added " + InsideEnemy.enemyType.name + "To debug list");
            }
        }

        daytimeEnemies.Clear();
        foreach (SpawnableEnemyWithRarity DaytimeEnemy in DaytimeEnemies) {
            if (!daytimeEnemies.Contains(DaytimeEnemy)) {
                daytimeEnemies.Add(new SpawnableEnemyWithRarity {
                    enemyType = DaytimeEnemy.enemyType,
                    rarity = DaytimeEnemy.rarity
                });
                WTOBase.WTOLogSource.LogMessage("Added " + DaytimeEnemy.enemyType.name + "To debug list");
            }
        }

        outsideEnemies.Clear();
        outsideEnemies.Add(new SpawnableEnemyWithRarity {
            enemyType = AdultWandererContainer[0].enemyType,
            rarity = AdultWandererContainer[0].rarity
        });
        WTOBase.WTOLogSource.LogMessage("Added " + AdultWandererContainer[0].enemyType.name + "To debug list");

        EnemiesInList = true;
    }

    [HarmonyPatch(typeof(EnemyAI), "SetEnemyStunned")]
    [HarmonyPostfix]
    private static void SetOwnershipToStunningPlayer(EnemyAI __instance) { 
        if(__instance is not WTOEnemy || __instance.stunnedByPlayer == null) {
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

//METHODS 
public static void Start() {
        CreateEnemy("Wanderer.asset", DaytimeEnemies, 50, LethalLib.Modules.Enemies.SpawnType.Daytime, "WandererTerminal.asset", "WandererKeyword.asset");
        CreateEnemy("AdultWanderer.asset", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside, "AdultWandererTerminal.asset", "AdultWandererKeyword.asset");
        CreateEnemy("Gallenarma.asset", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default, "GallenTerminal.asset", "GallenKeyword.asset");
        //CreateEnemy("Assets/CustomMonsters/BabyLurker/BabyLurker.asset", InsideEnemies, 10);
        CreateEnemy("EyeSecurity.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "EyeSecTerminal.asset", "EyeSecKeyword.asset");
        CreateEnemy("Lurker.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "LurkerTerminal.asset", "LurkerKeyword.asset");
        CreateEnemy("OoblGhost.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "OoblGhostTerminal.asset", "OoblGhostKeyword.asset");
        CreateEnemy("Enforcer.asset", InsideEnemies, 10, LethalLib.Modules.Enemies.SpawnType.Default, "EnforcerTerminal.asset", "EnforcerKeyword.asset");
        CreateEnemy("BabyLurker.asset", InsideEnemies, 10, LethalLib.Modules.Enemies.SpawnType.Default, "BabyLurkerTerminal.asset", "BabyLurkerKeyword.asset");
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
    WTOBase.WTOLogSource.LogMessage("Monster Loaded: " + EnemyName.Remove(EnemyName.Length - 6, 6));
}
}
