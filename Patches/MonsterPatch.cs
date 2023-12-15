using LethalLib.Modules;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {
    
    internal class MonsterPatch {

        private static List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();
        /* TODO: All these signatures SHOULD be taking the list as a param, 
         * and the SOR Instance should probably be grabbed on awake so it 
         * doesn't need to be passed. 
         */
        public static void SetInsideMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.Enemies = new List<SpawnableEnemyWithRarity>(){ __instance.levels[5].Enemies[4] };
                //return;
            }
            /*TODO: make this reference the indoor monster list
            foreach (SpawnableItemWithRarity item in ItemPatch.MoonScrap) {
                Moon.spawnableScrap.Add(item);
            }
            */
        }
        public static void SetOutsideMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.OutsideEnemies.Add(__instance.levels[0].OutsideEnemies[0]);
                return;
            }
            /*TODO: make this reference the outdoor monster list
            foreach (SpawnableItemWithRarity item in ItemPatch.MoonScrap) {
                Moon.spawnableScrap.Add(item);
            }
            */
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
            /*TODO: make this reference the security item list
            foreach (SpawnableItemWithRarity item in ItemPatch.MoonScrap) {
                Moon.spawnableScrap.Add(item);
            }
            */
        }

        public static void CreateWanderer() {

            EnemyType enemyType = WTOBase.MonsterAssetBundle.LoadAsset<EnemyType>("Assets/CustomMonsters/Wanderer.asset");
            TerminalNode terminalNode3 = WTOBase.MonsterAssetBundle.LoadAsset<TerminalNode>("Assets/CustomMonsters/WandererTerminal.asset");
            /*
            TerminalKeyword terminalKeyword = null;
            if (false) {
                terminalKeyword = new TerminalKeyword();
                terminalKeyword.word = "Wanderer";
                terminalKeyword.isVerb = false;
            }
            */
            NetworkPrefabs.RegisterNetworkPrefab(enemyType.enemyPrefab);
            DaytimeEnemies.Add(new SpawnableEnemyWithRarity { enemyType = enemyType, rarity = 100 });
            //Enemies.RegisterEnemy(enemyType, customEnemy.rarity, customEnemy.levelFlags, customEnemy.spawnType, terminalNode3, terminalKeyword);

        }

    }
}
