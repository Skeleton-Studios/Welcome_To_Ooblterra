using HarmonyLib;
using LethalLib;
using System.Collections.Generic;
using System.Linq;
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

    private const string EnemyPathRoot = "Assets/CustomEnemies/";
    private static bool EnemiesInList;
    public const bool ShouldDebugEnemies = true;

    [HarmonyPatch(typeof(QuickMenuManager), "Debug_SetEnemyDropdownOptions")]
    [HarmonyPrefix]
    private static void AddMonstersToDebug(QuickMenuManager __instance) {
        if (EnemiesInList) {
            return;
        }
        var testLevel = __instance.testAllEnemiesLevel;
        var firstEnemy = testLevel.Enemies.FirstOrDefault(); //Grab all of the test enemies 
        if (firstEnemy == null) { //check to see if the list of enemies actually exists
            Debug.Log("Failed to get first enemy for debug list!");
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
                Debug.Log("Added " + InsideEnemy.enemyType.name + "To debug list");
            }
        }

        daytimeEnemies.Clear();
        foreach (SpawnableEnemyWithRarity DaytimeEnemy in DaytimeEnemies) {
            if (!daytimeEnemies.Contains(DaytimeEnemy)) {
                daytimeEnemies.Add(new SpawnableEnemyWithRarity {
                    enemyType = DaytimeEnemy.enemyType,
                    rarity = DaytimeEnemy.rarity
                });
                Debug.Log("Added " + DaytimeEnemy.enemyType.name + "To debug list");
            }
        }

        outsideEnemies.Clear();
        outsideEnemies.Add(new SpawnableEnemyWithRarity {
            enemyType = AdultWandererContainer[0].enemyType,
            rarity = AdultWandererContainer[0].rarity
        });
        Debug.Log("Added " + AdultWandererContainer[0].enemyType.name + "To debug list");

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
    public static void CreateEnemy(string EnemyName, List<SpawnableEnemyWithRarity> EnemyList, int rarity, LethalLib.Modules.Enemies.SpawnType SpawnType, string InfoName = null, string KeywordName = null) {
            
        string EnemyFolderName = EnemyName.Remove(EnemyName.Length - 6, 6) + "/";
        TerminalNode EnemyInfo = null;
        TerminalKeyword EnemyKeyword = null;

        EnemyType EnemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>(EnemyPathRoot + EnemyFolderName + EnemyName);
        EnemyType.enemyPrefab.GetComponent<EnemyAI>().debugEnemyAI = false;

        if (InfoName != null) {
            EnemyInfo = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>(EnemyPathRoot + EnemyFolderName + InfoName);
        }
        if (KeywordName != null) {
            EnemyKeyword = WTOBase.MonsterAssetBundle.LoadAsset<TerminalKeyword>(EnemyPathRoot + EnemyFolderName + KeywordName);
            EnemyKeyword.defaultVerb = TerminalPatch.InfoKeyword;
        }

        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(EnemyType.enemyPrefab);
        LethalLib.Modules.Enemies.RegisterEnemy(EnemyType, rarity, LethalLib.Modules.Levels.LevelTypes.None, SpawnType, /*new string[] { "OoblterraLevel" },*/ EnemyInfo, EnemyKeyword);
        EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = EnemyType, rarity = rarity });
        Debug.Log("Monster Loaded: " + EnemyName.Remove(EnemyName.Length - 6, 6));
    }
    public static void Start() {
        CreateEnemy("Wanderer.asset", DaytimeEnemies, 50, LethalLib.Modules.Enemies.SpawnType.Daytime, "WandererTerminal.asset", "WandererKeyword.asset");
        CreateEnemy("AdultWanderer.asset", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside, "AdultWandererTerminal.asset", "AdultWandererKeyword.asset");
        CreateEnemy("Gallenarma.asset", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default, "GallenTerminal.asset", "GallenKeyword.asset");
        //CreateEnemy("Assets/CustomMonsters/BabyLurker/BabyLurker.asset", InsideEnemies, 10);
        CreateEnemy("EyeSecurity.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "EyeSecTerminal.asset", "EyeSecKeyword.asset");
        CreateEnemy("Lurker.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "LurkerTerminal.asset", "LurkerKeyword.asset");
        //CreateEnemy("OoblGhost.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "OoblGhostTerminal.asset", "OoblGhostKeyword.asset");
    }
}
