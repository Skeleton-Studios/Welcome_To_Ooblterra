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
    private static readonly AssetBundle ItemBundle = WTOBase.ItemAssetBundle;
    private const string ItemPath = WTOBase.RootPath + "CustomItems/";

    //This array stores all our custom items
    public static ItemData[] ItemList = new ItemData[] {
        new ItemData("AlienCrate.asset", 30),
        new ItemData("FiveSixShovel.asset", 10),
        new ItemData("HandCrystal.asset", 30),
        new ItemData("OoblCorpse.asset", 5),
        new ItemData("StatueSmall.asset", 40),
        new ItemData("WandCorpse.asset", 5),
        new ItemData("WandFeed.asset", 20),
        new ItemData("SprintTotem.asset", 25),
        new ItemData("CursedTotem.asset", 20),
        new ItemData("Chems.asset", 0),
        new ItemData("Battery.asset", 0)
    };

    //PATCHES
    [HarmonyPatch(typeof(RoundManager), "SetLockedDoors")]
    [HarmonyPrefix]
    private static void ReplaceKeys(RoundManager __instance) {
        if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            return;
        }

        GameObject KeyObject = GameObject.Instantiate(WTOBase.ContextualLoadAsset<GameObject>(ItemBundle, ItemPath + "OoblKey.prefab"), __instance.mapPropsContainer.transform);
        __instance.keyPrefab = KeyObject;
    }

    //METHODS
    public static void Start() {
        //Create our custom items
        Item NextItemToProcess;
        foreach(ItemData MyCustomScrap in ItemList){
            //Load item based on its path 
            NextItemToProcess = WTOBase.ContextualLoadAsset<Item>(ItemBundle, MyCustomScrap.GetItemPath());
            //Register it with LethalLib
            NetworkPrefabs.RegisterNetworkPrefab(NextItemToProcess.spawnPrefab);
            //if it aint broke... (but technically I should get rid of the reference to ooblterra because I dont want to have all the scrap listed twice)
            LethalLib.Modules.Items.RegisterScrap(NextItemToProcess, MyCustomScrap.GetRarity(), Levels.LevelTypes.None);
            //Set the item reference in the ItemData class
            MyCustomScrap.SetItem(NextItemToProcess);
            //Debug.Log("Item Loaded: " + NextItemToProcess.name);
        }
        NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(ItemBundle, ItemPath + "OoblKey.prefab"));
    }       
}