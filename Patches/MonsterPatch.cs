using HarmonyLib;
using LethalLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches;
internal class MonsterPatch {

    public static List<SpawnableEnemyWithRarity> InsideEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> OutsideEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableEnemyWithRarity> AdultWandererContainer = new List<SpawnableEnemyWithRarity>();
    public static List<SpawnableMapObject> SecurityList = new List<SpawnableMapObject>();

    private const string EnemyPathRoot = "Assets/CustomEnemies/";
    private static bool enemiesInList;

    [HarmonyPatch(typeof(QuickMenuManager), "Debug_SetEnemyDropdownOptions")]
    [HarmonyPrefix]
    private static void AddMonstersToDebug(QuickMenuManager __instance) {
        if (enemiesInList) {
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

        enemiesInList = true;
    }


    public static void SetInsideMonsters(SelectableLevel Moon) {
        Moon.Enemies = InsideEnemies;
    }
    public static void SetInsideMonsters(SelectableLevel Moon, List<SpawnableEnemyWithRarity> EnemyList) {
        Moon.Enemies = EnemyList;
    }

    public static void SetOutsideMonsters(SelectableLevel Moon) {
        Moon.OutsideEnemies = OutsideEnemies;
    }
    public static void SetOutsideMonsters(SelectableLevel Moon, List<SpawnableEnemyWithRarity> EnemyList) {
            Moon.OutsideEnemies = EnemyList;
    }

    public static void SetDaytimeMonsters(SelectableLevel Moon) {
        Moon.DaytimeEnemies = DaytimeEnemies;
    }
    public static void SetDaytimeMonsters(SelectableLevel Moon, List<SpawnableEnemyWithRarity> EnemyList) {
            Moon.DaytimeEnemies = EnemyList;
    }

    public static void SetSecurityObjects(SelectableLevel Moon) {
        SpawnableMapObject[] spawnableMapObjects = new SpawnableMapObject[SecurityList.Count];
        Moon.spawnableMapObjects = spawnableMapObjects;
        for (int i = 0; i < SecurityList.Count; i++) {
            Moon.spawnableMapObjects.SetValue(SecurityList[i], i);
        }
    }
    public static void SetSecurityObjects(SelectableLevel Moon, SpawnableMapObject[] Objects) {
        Moon.spawnableMapObjects = Objects;

    }

    public static void CreateEnemy(string EnemyName, List<SpawnableEnemyWithRarity> EnemyList, int rarity, LethalLib.Modules.Enemies.SpawnType SpawnType, string InfoName = null, string KeywordName = null) {
            
        string EnemyFolderName = EnemyName.Remove(EnemyName.Length - 6, 6) + "/";
        TerminalNode EnemyInfo = null;
        TerminalKeyword EnemyKeyword = null;

        EnemyType enemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>(EnemyPathRoot + EnemyFolderName + EnemyName);

        if (InfoName != null) {
            EnemyInfo = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>(EnemyPathRoot + EnemyFolderName + InfoName);
        }
        if (KeywordName != null) {
            EnemyKeyword = WTOBase.MonsterAssetBundle.LoadAsset<TerminalKeyword>(EnemyPathRoot + EnemyFolderName + KeywordName);
            EnemyKeyword.defaultVerb = TerminalPatch.InfoKeyword;
        }

        LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
        LethalLib.Modules.Enemies.RegisterEnemy(enemyType, rarity, LethalLib.Modules.Levels.LevelTypes.None, SpawnType, new string[] { "OoblterraLevel" }, EnemyInfo, EnemyKeyword);
        EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = enemyType, rarity = rarity });
        Debug.Log("Monster Loaded: " + EnemyName.Remove(EnemyName.Length - 6, 6));
    }

    public static void Start() {
        CreateEnemy("Wanderer.asset", DaytimeEnemies, 50, LethalLib.Modules.Enemies.SpawnType.Daytime, "WandererTerminal.asset", "WandererKeyword.asset");
        CreateEnemy("AdultWanderer.asset", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside, "AdultWandererTerminal.asset", "AdultWandererKeyword.asset");
        CreateEnemy("Gallenarma.asset", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default, "GallenTerminal.asset", "GallenKeyword.asset");
        //CreateEnemy("Assets/CustomMonsters/BabyLurker/BabyLurker.asset", InsideEnemies, 10);
        CreateEnemy("EyeSecurity.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "EyeSecTerminal.asset", "EyeSecKeyword.asset");
        CreateEnemy("Lurker.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "LurkerTerminal.asset", "LurkerKeyword.asset");
    }
}
