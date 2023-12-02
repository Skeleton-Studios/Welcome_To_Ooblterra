namespace Welcome_To_Ooblterra {
    internal class MonsterPatch {
        /* TODO: All these signatures SHOULD be taking the list as a param, 
         * and the SOR Instance should probably be grabbed on awake so it 
         * doesn't need to be passed. 
         */
        public static void SetInsideMonsters(bool UseDefaultList, SelectableLevel Moon, StartOfRound __instance) {
            if (UseDefaultList) {
                Moon.Enemies = __instance.levels[5].Enemies;
                return;
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
            /*TODO: make this reference the ambient enemy list
            foreach (SpawnableItemWithRarity item in ItemPatch.MoonScrap) {
                Moon.spawnableScrap.Add(item);
            }
            */
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



    }
}
