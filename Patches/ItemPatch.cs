using HarmonyLib;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using LethalLib.Modules;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Patches;
internal class ItemPatch {
    public class ItemData {
        private string AssetName;
        private int Rarity;
        private Item Itemref;
        public ItemData(string name, int rarity) {
            AssetName = ItemPath + name;
            Rarity = rarity;
        }
        public string GetItemPath() { return AssetName; }
        public int GetRarity() { return Rarity; }
        public void SetItem(Item ItemToSet) { Itemref = ItemToSet; }
        public Item GetItem() { return Itemref; }

    }

    private const string ItemPath = "Assets/CustomItems/";
    private static List<SpawnableItemWithRarity> MoonScrap = new List<SpawnableItemWithRarity>();

    //This array stores all our custom items
    public static ItemData[] ItemList = new ItemData[] {
        new ItemData("AlienCrate.asset", 30),
        new ItemData("FiveSixShovel.asset", 10),
        new ItemData("HandCrystal.asset", 30),
        new ItemData("OoblCorpse.asset", 5),
        new ItemData("StatueSmall.asset", 40),
        new ItemData("WandCorpse.asset", 5),
        new ItemData("WandFeed.asset", 20),
        new ItemData("SprintTotem.asset", 5),
        new ItemData("CursedTotem.asset", 2),
        new ItemData("Chems.asset", 0)
    };

    //Add our custom items
    public static void Start() {
        //Create our custom items
        Item NextItemToProcess;
        SpawnableItemWithRarity MoonScrapItem;
        foreach(ItemData MyCustomScrap in ItemList){
            //Load item based on its path 
            NextItemToProcess = WTOBase.ItemAssetBundle.LoadAsset<Item>(MyCustomScrap.GetItemPath());
            //Register it with LethalLib
            NetworkPrefabs.RegisterNetworkPrefab(NextItemToProcess.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(NextItemToProcess, MyCustomScrap.GetRarity(), Levels.LevelTypes.None, new string[] { "OoblterraLevel" });
            //Set the item reference in the ItemData class
            MyCustomScrap.SetItem(NextItemToProcess);

            //Add item to internal moonscrap list 
            MoonScrapItem = new SpawnableItemWithRarity {
                spawnableItem = NextItemToProcess,
                rarity = MyCustomScrap.GetRarity()
            };
            MoonScrap.Add(MoonScrapItem);
            Debug.Log("Item Loaded: " + MoonScrapItem.spawnableItem.name);
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
        ItemGrabbableObject.SetScrapValue((int)(RoundManager.Instance.AnomalyRandom.Next(ItemList[internalID].GetItem().minValue, ItemList[internalID].GetItem().maxValue) * RoundManager.Instance.scrapValueMultiplier));

        if (Network.IsHost) {
            ItemNetworkObject.Spawn();
        }
            
        WTOBase.LogToConsole("Custom item spawned...");
    }
}