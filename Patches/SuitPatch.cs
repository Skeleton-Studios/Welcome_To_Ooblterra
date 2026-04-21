using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches
{
    /* Modified from AlexCodesGames' AdditionalSuits mod,
        * which gives explicit permission on both the repo and in 
        * the plugin.cs file. Thank you!
        */
    internal class SuitPatch {

        // TODO: Move the suits to their own asset bundle so they can be loaded properly without disturbing LLL.
        private const string SuitPath = "CustomSuits/";

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

        private static readonly WTOBase.WTOLogger Log = new(typeof(SuitPatch), LogSourceType.Generic);

        //PATCHES
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
        [HarmonyPrefix]
        private static void StartPatch(ref StartOfRound __instance) {
            GhostPlayerSuit = WTOBase.ContextualLoadAsset<Material>(SuitPath + "GhostPlayerSuit.mat");
            if (WTOBase.WTOCustomSuits.Value) {
                LoadSuits(SuitPath);
            }
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
        [HarmonyPostfix]
        [HarmonyPriority(0)]
        private static void PatchPosters() {
            if (WTOBase.WTOCustomPoster.Value){
                ReplacePoster();
            }
        }

        private static void LoadSuits(string RelevantPath) {
            if (SuitsLoaded) {
                Log.Warning("SUITS ALREADY LOADED!");
                return;
            }
            UnlockableItem unlockableItem = StartOfRound.Instance.unlockablesList.unlockables.First(x => x.suitMaterial != null);
            //Create new suits based on the materials
            foreach (string MatName in SuitMaterialNames) {
                UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
                UnlockableSuit newSuit = new();
                newUnlockableItem.suitMaterial = WTOBase.ContextualLoadAsset<Material>(RelevantPath + MatName);
                //prepare and set name
                String SuitName = MatName.Substring(0, MatName.Length - 4);
                newUnlockableItem.unlockableName = SuitName;
                //add new item to the listing of tracked unlockable items
                StartOfRound.Instance.unlockablesList.unlockables.Add(newUnlockableItem);
            }
            SuitsLoaded = true;
        }

        private static void ReplacePoster() {
            if (GameObject.Find(PosterGameObject) == null) {
                return;
            }
            Material[] materials = ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials;
            materials[1] = WTOBase.ContextualLoadAsset<Material>(SuitPath + "Poster.mat");
            ((Renderer)GameObject.Find(PosterGameObject).GetComponent<MeshRenderer>()).materials = materials;
        }
    }
}
