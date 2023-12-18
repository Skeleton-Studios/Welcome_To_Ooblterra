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

        public static List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableEnemyWithRarity> AdultWandererContainer = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableEnemyWithRarity> InsideEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableEnemyWithRarity> OutdoorEnemies = new List<SpawnableEnemyWithRarity>();
        public static List<SpawnableMapObject> SecurityList = new List<SpawnableMapObject>();
        /* TODO: All these signatures SHOULD be taking the list as a param, 
         * and the SOR Instance should probably be grabbed on awake so it 
         * doesn't need to be passed. 
         */
        public static void SetInsideMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.Enemies = new List<SpawnableEnemyWithRarity>(){ __instance.levels[5].Enemies[4] };
                return;
            }
            //TODO: make this reference the indoor monster list
            foreach (SpawnableEnemyWithRarity monster in InsideEnemies) {
                Moon.Enemies.Add(monster);
            }
            
        }
        public static void SetOutsideMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.OutsideEnemies.Add(__instance.levels[0].OutsideEnemies[0]);
                return;
            }
            //TODO: make this reference the outdoor monster list
            foreach (SpawnableEnemyWithRarity monster in OutdoorEnemies) {
                Moon.OutsideEnemies.Add(monster);
            }
            
        }
        public static void SetDaytimeMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance){
            if (UseDefaultList) {
                Moon.DaytimeEnemies.Add(__instance.levels[0].DaytimeEnemies[1]);
                return;
            }
            foreach (SpawnableEnemyWithRarity monster in DaytimeEnemies) {
                Moon.DaytimeEnemies.Add(monster);
            }
            
        }
        public static void SetSecurityObjects(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.spawnableMapObjects = __instance.levels[2].spawnableMapObjects;
                return;
            }
            //resize the security item array
            Moon.spawnableMapObjects = new SpawnableMapObject[SecurityList.Count];
            for(int i = 0; i < SecurityList.Count; i++) {
                Moon.spawnableMapObjects.SetValue(SecurityList[i], i);
            }
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
                var Monster = UnityEngine.Object.Instantiate(InsideEnemies[0].enemyType.enemyPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                Monster.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("Gallenarma spawned...");
            }
        }


    }
}
