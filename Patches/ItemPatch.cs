using HarmonyLib;
using UnityEngine;
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
            Item NextItemToProcess;
            SpawnableItemWithRarity MoonScrapItem;
            foreach(ItemData MyCustomScrap in ItemList){
                //Load item based on its path 
                NextItemToProcess = WTOBase.ItemAssetBundle.LoadAsset<Item>(MyCustomScrap.GetItemPath());
                //Register it with LethalLib
                NetworkPrefabs.RegisterNetworkPrefab(NextItemToProcess.spawnPrefab);
                Items.RegisterScrap(NextItemToProcess, MyCustomScrap.GetRarity(), Levels.LevelTypes.None, new string[] { "OoblterraLevel" });
                //Set the item reference in the ItemData class
                MyCustomScrap.SetItem(NextItemToProcess);

                //Add item to internal moonscrap list 
                MoonScrapItem = new SpawnableItemWithRarity {
                    spawnableItem = NextItemToProcess,
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
        public static void SpawnItem(Vector3 location, int internalID) {
            NetworkManager Network = GameObject.FindObjectOfType<NetworkManager>();
            GameObject SpawnedItem = Object.Instantiate(ItemList[internalID].GetItem().spawnPrefab, location, Quaternion.identity);
            GrabbableObject ItemGrabbableObject = SpawnedItem.GetComponent<GrabbableObject>();
            NetworkObject ItemNetworkObject = SpawnedItem.GetComponent<NetworkObject>();

            //this needs to be synced with local clients
            ItemGrabbableObject.scrapValue = (int)(RoundManager.Instance.AnomalyRandom.Next(ItemList[5].GetItem().minValue, ItemList[5].GetItem().maxValue) * RoundManager.Instance.scrapValueMultiplier);

            if (Network.IsHost) {
                ItemNetworkObject.Spawn();
            }
            
            WTOBase.LogToConsole("Custom item spawned...");
        }
    }
}