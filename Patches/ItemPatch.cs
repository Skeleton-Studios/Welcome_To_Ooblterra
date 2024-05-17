using HarmonyLib;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using LethalLib.Modules;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;
using LethalLevelLoader;

namespace Welcome_To_Ooblterra.Patches;

internal class ItemPatch {


    private static AudioClip CachedDiscoBallMusic;
    private static AudioClip OoblterraDiscoMusic;
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

    private static Dictionary<string, List<SpawnableItemWithRarity>> MoonsToItemLists;

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

    [HarmonyPatch(typeof(CozyLights), "SetAudio")]
    [HarmonyPrefix]
    private static bool ReplaceDiscoBall(CozyLights __instance) {
        if (StartOfRound.Instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            //set the disco ball music back to the default
            if(__instance.turnOnAudio != null) {
                __instance.turnOnAudio.clip = CachedDiscoBallMusic;
            }
            return true;
        }
        if (__instance.turnOnAudio != null) {
            __instance.turnOnAudio.clip = OoblterraDiscoMusic;
        }
        return true;
    }

    [HarmonyPatch(typeof(StartOfRound), "StartGame")]
    [HarmonyPostfix]
    private static void SetItemsWTO(StartOfRound __instance) {
        string PlanetName = __instance.currentLevel.PlanetName;
        if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow) {
            if (MoonsToItemLists.TryGetValue(PlanetName, out List<SpawnableItemWithRarity> ResultItemList)) {
                __instance.currentLevel.spawnableScrap = ResultItemList;
            }
            return;
        }
        if (!MoonsToItemLists.ContainsKey(PlanetName)) {
            MoonsToItemLists.Add(PlanetName, __instance.currentLevel.spawnableScrap);
        }
        SetItemStuff(WTOBase.WTOForceScrap.Value, ref __instance.currentLevel.spawnableScrap, MoonPatch.OoblterraExtendedLevel.SelectableLevel.spawnableScrap);
    }

    //METHODS
    public static void Start() {
        //Create our custom items
        Item NextItemToProcess;
        foreach (ItemData MyCustomScrap in ItemList) {
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
        CachedDiscoBallMusic = WTOBase.ContextualLoadAsset<AudioClip>(ItemBundle, ItemPath + "Boombox6QuestionMark.ogg", false);
        OoblterraDiscoMusic = WTOBase.ContextualLoadAsset<AudioClip>(ItemBundle, ItemPath + "ooblboombox.ogg", false);
    }

    private static void SetItemStuff(TiedToLabEnum TiedToLabState, ref List<SpawnableItemWithRarity> CurrentMoonItemList, List<SpawnableItemWithRarity> OoblterraItemList) {
        List<SpawnableItemWithRarity> WeightedOoblterraItems = new();
        foreach (SpawnableItemWithRarity Item in OoblterraItemList) {
            WeightedOoblterraItems.Add(new SpawnableItemWithRarity { spawnableItem = Item.spawnableItem, rarity = Item.rarity * WTOBase.WTOWeightScale.Value });
        }
        switch (TiedToLabState) {
            case TiedToLabEnum.WTOOnly:
                CurrentMoonItemList = OoblterraItemList;
                break;
            case TiedToLabEnum.AppendWTO:
                CurrentMoonItemList.AddRange(WeightedOoblterraItems);
                break;
        }
    }
}