using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches;
    
/* Modified from AlexCodesGames' AdditionalSuits mod,
    * which gives explicit permission on both the repo and in 
    * the plugin.cs file. Thank you!
    */
internal class SuitPatch {

    private const string SuitPath = WTOBase.RootPath + "CustomSuits/";
    private const string PosterGameObject = "HangarShip/Plane.001";
    static string[] SuitMaterialPaths = new string[] {
        SuitPath + "RedSuit.mat",
        SuitPath + "ProtSuit.mat",
        SuitPath + "YellowSuit.mat",
        SuitPath + "GreenSuit.mat",
        SuitPath + "BlueSuit.mat",
        SuitPath + "IndigoSuit.mat",
        SuitPath + "MackSuit.mat"
    };
    private static bool SuitsLoaded = false;

    public static Material GhostPlayerSuit;

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPrefix]
    private static void StartPatch(ref StartOfRound __instance) {
        GhostPlayerSuit = WTOBase.ItemAssetBundle.LoadAsset<Material>(SuitPath + "GhostPlayerSuit.mat");
        if (SuitsLoaded) {
            return; 
        }
        /*
        IEnumerable<UnlockableItem> Unlockables = (IEnumerable<UnlockableItem>)__instance.unlockablesList;
        UnlockableItem Suit = Unlockables.First(x => x.suitMaterial != null);
        WTOBase.LogToConsole("Suit found: " + Suit.unlockableName);
        */
        //Get all unlockables
        for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++) {
            UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];
            WTOBase.LogToConsole("Processing unlockable {index=" + i + ", name=" + unlockableItem.unlockableName + "}");

            //This will skip executing of the remaining code UNLESS the object we've found is a suit
            if (unlockableItem.suitMaterial == null || !unlockableItem.alreadyUnlocked) {
                continue;
            }
            //Create new suits based on the materials
            foreach (string PathToMaterial in SuitMaterialPaths) {
                UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
                UnlockableSuit newSuit = new UnlockableSuit();
                newUnlockableItem.suitMaterial = WTOBase.ItemAssetBundle.LoadAsset<Material>(PathToMaterial);
                //prepare and set name
                String SuitName = PathToMaterial.Substring(0, PathToMaterial.Length - 4);
                SuitName = SuitName.Substring(SuitPath.Length);
                newUnlockableItem.unlockableName = SuitName;
                //add new item to the listing of tracked unlockable items
                __instance.unlockablesList.unlockables.Add(newUnlockableItem);
            }
            SuitsLoaded = true;
            break;
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPatch(typeof(RoundManager), "GenerateNewLevelClientRpc")]
    [HarmonyPostfix]
    [HarmonyPriority(0)]
    private static void PatchPosters(StartOfRound __instance) {
        Material[] materials = ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials;
        materials[1] = WTOBase.ItemAssetBundle.LoadAsset<Material>(SuitPath + "Poster.mat");
        ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials = materials;
    }
}
