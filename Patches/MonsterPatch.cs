using HarmonyLib;
using LethalLib.Modules;
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

        public static void CreateEnemy(string EnemyName, List<SpawnableEnemyWithRarity> EnemyList, int rarity, LethalLib.Modules.Enemies.SpawnType SpawnType, bool Wanderer = false) {
            EnemyType enemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>(EnemyName);
            TerminalNode terminalNode3 = null;
            TerminalKeyword terminalKeyword = null;
            if (Wanderer) { 
                terminalNode3 = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>("Assets/CustomMonsters/Wanderer/WandererTerminal.asset");            
                terminalKeyword = WTOBase.MonsterAssetBundle.LoadAsset<TerminalKeyword>("Assets/CustomMonsters/Wanderer/WandererKeyword.asset");
            }
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(enemyType, rarity, Levels.LevelTypes.OoblterraLevel, SpawnType, terminalNode3, terminalKeyword);
            EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = enemyType, rarity = rarity });
        }

        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        public static void SpawnItem(StartOfRound __instance) {
            
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                /*
                var Monster = UnityEngine.Object.Instantiate(InsideEnemies[2].enemyType.enemyPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                Monster.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("EyeSec spawned...");
                */
            }
            
        }
        public static void Start() {
            CreateEnemy("Assets/CustomMonsters/Wanderer/Wanderer.asset", DaytimeEnemies, 100, LethalLib.Modules.Enemies.SpawnType.Daytime, true);
            CreateEnemy("Assets/CustomMonsters/AdultWanderer/AdultWanderer.asset", AdultWandererContainer, 0, LethalLib.Modules.Enemies.SpawnType.Outside);
            CreateEnemy("Assets/CustomMonsters/Gallenarma/Gallenarma.asset", InsideEnemies, 30, LethalLib.Modules.Enemies.SpawnType.Default);
            //CreateEnemy("Assets/CustomMonsters/BabyLurker/BabyLurker.asset", InsideEnemies, 10);
            CreateEnemy("Assets/CustomMonsters/EyeSecurity/EyeSecurity.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default);
            CreateEnemy("Assets/CustomMonsters/Lurker/Lurker.asset", InsideEnemies, 20, LethalLib.Modules.Enemies.SpawnType.Default);
        }
    }
}
