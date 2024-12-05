using HarmonyLib;
using LethalLevelLoader;
using LethalLib.Modules;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace Welcome_To_Ooblterra.Patches;

internal class ItemPatch
{


    private static AudioClip CachedDiscoBallMusic;
    private static AudioClip OoblterraDiscoMusic;
    public class ItemData(string name, int rarity)
    {
        private readonly string AssetName = ItemPath + name;
        private readonly int Rarity = rarity;
        private Item Itemref;

        public string GetItemPath() { return AssetName; }
        public int GetRarity() { return Rarity; }
        public void SetItem(Item ItemToSet) { Itemref = ItemToSet; }
        public Item GetItem() { return Itemref; }

    }
    private static readonly AssetBundle ItemBundle = WTOBase.ItemAssetBundle;
    private const string ItemPath = WTOBase.RootPath + "CustomItems/";

    private static readonly Dictionary<string, List<SpawnableItemWithRarity>> MoonsToItemLists = [];

    //This array stores all our custom items
    public static ItemData[] ItemList = [
        new("AlienCrate.asset", 30),
        new("FiveSixShovel.asset", 10),
        new("HandCrystal.asset", 30),
        new("OoblCorpse.asset", 5),
        new("StatueSmall.asset", 40),
        new("WandCorpse.asset", 5),
        new("WandFeed.asset", 20),
        new("SprintTotem.asset", 25),
        new("CursedTotem.asset", 20),
        new("Chems.asset", 0),
        new("Battery.asset", 0)
    ];

    //PATCHES
    [HarmonyPatch(typeof(RoundManager), "SetLockedDoors")]
    [HarmonyPrefix]
    private static void ReplaceKeys(RoundManager __instance)
    {
        if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName)
        {
            return;
        }

        GameObject KeyObject = GameObject.Instantiate(WTOBase.ContextualLoadAsset<GameObject>(ItemBundle, ItemPath + "OoblKey.prefab"), __instance.mapPropsContainer.transform);
        __instance.keyPrefab = KeyObject;
    }

    [HarmonyPatch(typeof(CozyLights), "SetAudio")]
    [HarmonyPrefix]
    private static bool ReplaceDiscoBall(CozyLights __instance)
    {
        if (StartOfRound.Instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName)
        {
            //set the disco ball music back to the default
            if (__instance.turnOnAudio != null)
            {
                __instance.turnOnAudio.clip = CachedDiscoBallMusic;
            }
            return true;
        }
        if (__instance.turnOnAudio != null)
        {
            __instance.turnOnAudio.clip = OoblterraDiscoMusic;
        }
        return true;
    }

    [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
    [HarmonyPrefix]
    private static void SetItemsWTO(RoundManager __instance)
    {
        string PlanetName = __instance.currentLevel.PlanetName;
        if (DungeonManager.CurrentExtendedDungeonFlow != FactoryPatch.OoblDungeonFlow)
        {
            if (MoonsToItemLists.TryGetValue(PlanetName, out List<SpawnableItemWithRarity> ResultItemList))
            {
                __instance.currentLevel.spawnableScrap = ResultItemList;
            }
            return;
        }
        SetItemStuff(WTOBase.WTOForceScrap.Value, ref __instance.currentLevel.spawnableScrap, MoonsToItemLists[MoonPatch.MoonFriendlyName]);
    }

    //METHODS
    public static void Start()
    {
        //Create our custom items
        Item NextItemToProcess;
        foreach (ItemData MyCustomScrap in ItemList)
        {
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
        if (!MoonsToItemLists.ContainsKey(MoonPatch.MoonFriendlyName))
        {
            MoonsToItemLists.Add(MoonPatch.MoonFriendlyName, MoonPatch.OoblterraExtendedLevel.SelectableLevel.spawnableScrap);
        }
    }

    private static void SetItemStuff(TiedToLabEnum TiedToLabState, ref List<SpawnableItemWithRarity> CurrentMoonItemList, List<SpawnableItemWithRarity> OoblterraItemList)
    {
        List<SpawnableItemWithRarity> WeightedOoblterraItems = [];
        foreach (SpawnableItemWithRarity Item in OoblterraItemList)
        {
            WeightedOoblterraItems.Add(new SpawnableItemWithRarity { spawnableItem = Item.spawnableItem, rarity = Item.rarity * WTOBase.WTOWeightScale.Value });
        }
        switch (TiedToLabState)
        {
            case TiedToLabEnum.WTOOnly:
                CurrentMoonItemList = OoblterraItemList;
                break;
            case TiedToLabEnum.AppendWTO:
                CurrentMoonItemList.AddRange(WeightedOoblterraItems);
                break;
        }
    }
}