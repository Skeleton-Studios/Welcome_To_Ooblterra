using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches;

/* Modified from AlexCodesGames' AdditionalSuits mod,
    * which gives explicit permission on both the repo and in 
    * the plugin.cs file. Thank you!
    */
internal class SuitPatch
{

    private const string SuitPath = WTOBase.RootPath + "CustomSuits/";
    private const string BlackSuitPath = WTOBase.RootPath + "CustomSuits/BlackSuits";
    private static readonly AssetBundle SuitBundle = WTOBase.ItemAssetBundle;

    private const string PosterGameObject = "HangarShip/Plane.001";

    static readonly string[] SuitMaterialNames = [
        "RedSuit.mat",
        "ProtSuit.mat",
        "YellowSuit.mat",
        "GreenSuit.mat",
        "BlueSuit.mat",
        "IndigoSuit.mat",
        "MackSuit.mat"
    ];

    private static bool SuitsLoaded = false;

    public static Material GhostPlayerSuit;

    //PATCHES
    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPrefix]
    private static void StartPatch(ref StartOfRound __instance)
    {
        GhostPlayerSuit = WTOBase.ContextualLoadAsset<Material>(SuitBundle, SuitPath + "GhostPlayerSuit.mat");
        if (WTOBase.WTOCustomSuits.Value)
        {
            LoadSuits(SuitPath);
        }
        /*
        switch (WTOBase.WTOCustomSuits.Value) {
            case SuitStatus.Enable:
                LoadSuits(SuitPath);
                return;
            case SuitStatus.Disable:
                return;
            case SuitStatus.Purchase:
                AddSuitPurchaseNode();
                return;
            case SuitStatus.SleepsSpecial:
                LoadSuits(BlackSuitPath);
                return;
            default:
                return;
        }*/
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPatch(typeof(RoundManager), "GenerateNewLevelClientRpc")]
    [HarmonyPostfix]
    [HarmonyPriority(0)]
    private static void PatchPosters(StartOfRound __instance)
    {
        if (WTOBase.WTOCustomPoster.Value)
        {
            ReplacePoster();
        }
        /*
        switch (WTOBase.WTOCustomPoster.Value) {
            case PosterStatus.ReplaceVanilla:
                ReplacePoster();
                return;
            case PosterStatus.AddAsDecor:
                AddPosterShipDeco();
                return;
            case PosterStatus.Disable:
                return;
            default: 
                return;
        }
        */
    }

    //METHODS
    private static void LoadSuits(string RelevantPath)
    {
        if (SuitsLoaded)
        {
            WTOBase.LogToConsole("SUITS ALREADY LOADED!");
            return;
        }
        UnlockableItem unlockableItem = StartOfRound.Instance.unlockablesList.unlockables.First(x => x.suitMaterial != null);
        //Create new suits based on the materials
        foreach (string MatName in SuitMaterialNames)
        {
            UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
            UnlockableSuit newSuit = new();
            newUnlockableItem.suitMaterial = WTOBase.ContextualLoadAsset<Material>(SuitBundle, RelevantPath + MatName);
            //prepare and set name
            String SuitName = MatName.Substring(0, MatName.Length - 4);
            newUnlockableItem.unlockableName = SuitName;
            //add new item to the listing of tracked unlockable items
            StartOfRound.Instance.unlockablesList.unlockables.Add(newUnlockableItem);
        }
        SuitsLoaded = true;
    }
    private static void AddSuitPurchaseNode()
    {

    }
    private static void ReplacePoster()
    {
        if (GameObject.Find(PosterGameObject) == null)
        {
            return;
        }
        Material[] materials = ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials;
        materials[1] = WTOBase.ContextualLoadAsset<Material>(SuitBundle, SuitPath + "Poster.mat");
        ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials = materials;
    }
    private static void AddPosterShipDeco()
    {

    }
}
