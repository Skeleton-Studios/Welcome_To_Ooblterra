using HarmonyLib;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {
    internal class MonsterPatch {

        public static List<SpawnableEnemyWithRarity> InsideEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableEnemyWithRarity> OutsideEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableMapObject> SecurityList = new List<SpawnableMapObject>();
        public static List<SpawnableEnemyWithRarity> AdultWandererContainer = new List<SpawnableEnemyWithRarity>();

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
            //this is insanely hacky 
            EnemyType enemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>("Assets/CustomMonsters/" + EnemyName +"/" + EnemyName + ".asset");
            TerminalNode EnemyInfo = null;
            TerminalKeyword EnemyKeyword = null;
            if (InfoName != null) {
                EnemyInfo = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>("Assets/CustomMonsters/" + EnemyName + "/" + InfoName + ".asset");
            }
            if (KeywordName != null) {
                EnemyKeyword = WTOBase.MonsterAssetBundle.LoadAsset<TerminalKeyword>("Assets/CustomMonsters/" + EnemyName + "/" + KeywordName + ".asset");
            }
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(enemyType, rarity, LethalLib.Modules.Levels.LevelTypes.None, SpawnType, new string[] { "OoblterraLevel" }, EnemyInfo, EnemyKeyword);
            EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = enemyType, rarity = rarity });
        }

        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        public static void SpawnItem(StartOfRound __instance) {
            
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                WTOBase.LogToConsole("BEGIN PRINTING LIST OF ENTRANCES");
                EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true);
                foreach(EntranceTeleport entrance in array){
                    Debug.Log(entrance);
                }
                WTOBase.LogToConsole("END PRINTING LIST OF ENTRANCES");
                /*
                var Monster = UnityEngine.Object.Instantiate(InsideEnemies[2].enemyType.enemyPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                Monster.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("EyeSec spawned...");
                */
            }
            
        }
        public static void Start() {
            CreateEnemy("Wanderer", DaytimeEnemies, 50, LethalLib.Modules.Enemies.SpawnType.Daytime, "WandererTerminal", "WandererKeyword");
            CreateEnemy("AdultWanderer", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside, "AdultWandererTerminal", "AdultWandererKeyword");
            CreateEnemy("Gallenarma", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default, "GallenTerminal", "GallenKeyword");
            //CreateEnemy("Assets/CustomMonsters/BabyLurker/BabyLurker.asset", InsideEnemies, 10);
            CreateEnemy("EyeSecurity", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "EyeSecTerminal", "EyeSecKeyword");
            CreateEnemy("Lurker", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default, "LurkerTerminal", "LurkerKeyword");
        }
    }
}
