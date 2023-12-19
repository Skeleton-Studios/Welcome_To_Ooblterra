using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using LethalLib.Modules;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Patches {
    internal class ItemPatch {

        private static List<SpawnableItemWithRarity> MoonScrap = new List<SpawnableItemWithRarity>();
        public class ItemData {
            private string ItemPath;
            private int Rarity;
            private Item Itemref;
            public ItemData(string path, int rarity) {
                ItemPath = path;
                Rarity = rarity;
            }
            public string GetItemPath(){  return ItemPath; }
            public int GetRarity(){ return Rarity; }
            public void SetItem(Item ItemToSet){ Itemref= ItemToSet; }
            public Item GetItem(){ return Itemref; }

        }
        //This array stores all our custom items
        public static ItemData[] ItemList = new ItemData[] { 
            new ItemData("Assets/CustomItems/AlienCrate.asset", 30),
            new ItemData("Assets/Customitems/FiveSixShovel.asset", 10),
            new ItemData("Assets/CustomItems/HandCrystal.asset", 30),
            new ItemData("Assets/CustomItems/OoblCorpse.asset", 5),
            new ItemData("Assets/CustomItems/StatueSmall.asset", 40),
            new ItemData("Assets/CustomItems/WandCorpse.asset", 5),
            new ItemData("Assets/CustomItems/WandFeed.asset", 20),
        };

        //Add our custom items
        public static void AddCustomItems() {
            WTOBase.LogToConsole("Adding custom items...");
            //Create our custom items
            Item NextItem;
            SpawnableItemWithRarity MoonScrapItem;

            foreach(ItemData MyCustomScrap in ItemList){
                Debug.Log("Adding " + MyCustomScrap.ToString());
                NextItem = WTOBase.ItemAssetBundle.LoadAsset<Item>(MyCustomScrap.GetItemPath());               
                NetworkPrefabs.RegisterNetworkPrefab(NextItem.spawnPrefab);
                MyCustomScrap.SetItem(NextItem);
                Items.RegisterScrap(NextItem, MyCustomScrap.GetRarity(), Levels.LevelTypes.All);
                
                MoonScrapItem = new SpawnableItemWithRarity {
                    spawnableItem = NextItem,
                    rarity = MyCustomScrap.GetRarity()
                };
                MoonScrap.Add(MoonScrapItem);
            }
        }

        public static void SetMoonItemList(SelectableLevel Moon) {
            Moon.spawnableScrap = MoonScrap;
        }
        public static void SetMoonItemList(SelectableLevel Moon, List<SpawnableItemWithRarity> ItemList) {
                Moon.spawnableScrap = ItemList;
        }

        public static void SpawnItem(Vector3 location) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                var Crystal = UnityEngine.Object.Instantiate(ItemList[5].GetItem().spawnPrefab, location, Quaternion.identity);
                Crystal.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("Custom item spawned...");
            }
        }

        [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
        [HarmonyPrefix]
        private static bool SetItemSpawnPoints(){
            /*Notably, if the first item in the source array is say, a TableTopSpawn,
             * This will mean items can only spawn on tabletops, and there tends to be only like 
             * 2 of those. Will probably cause issues
             * TODO: Ensure that the spawn we grab is a GeneralItemSpawn or even make it so we can
             * specify the spawn type for each item 
            */
            RandomScrapSpawn[] source = Object.FindObjectsOfType<RandomScrapSpawn>();
            foreach (SpawnableItemWithRarity item in MoonScrap) {
                item.spawnableItem.spawnPositionTypes[0] = source[0].spawnableItems;
            }
            return true;
        }

    }
}