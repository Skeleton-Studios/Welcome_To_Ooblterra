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

        public static void CreateEnemy(string EnemyName, List<SpawnableEnemyWithRarity> EnemyList, int rarity) {

            EnemyType enemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>(EnemyName);
            //TerminalNode terminalNode3 = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>("Assets/CustomMonsters/Wanderer/WandererTerminal.asset");
            /*
            TerminalKeyword terminalKeyword = null;
            if (false) {
                terminalKeyword = new TerminalKeyword();
                terminalKeyword.word = "Wanderer";
                terminalKeyword.isVerb = false;
            }
            */
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
            EnemyList?.Add(new SpawnableEnemyWithRarity { enemyType = enemyType, rarity = rarity });
            //Enemies.RegisterEnemy(enemyType, customEnemy.rarity, customEnemy.levelFlags, customEnemy.spawnType, terminalNode3, terminalKeyword);

        }

        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        public static void SpawnItem(StartOfRound __instance) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                var Monster = UnityEngine.Object.Instantiate(InsideEnemies[3].enemyType.enemyPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                Monster.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("EyeSec spawned...");
            }
        }
    }
}
