using HarmonyLib;
using System;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {
    
    /* Modified from AlexCodesGames' AdditionalSuits mod,
     * which gives explicit permission on both the repo and in 
     * the plugin.cs file. Thank you!
     */


    internal class SuitPatch {

        static string[] SuitMaterialPaths = new string[] {
            "Assets/CustomSuits/ProtSuit.mat",
            "Assets/CustomSuits/MackSuit.mat"
        };

        public static bool SuitsLoaded = false;

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPrefix]
        private static void StartPatch(ref StartOfRound __instance) {
            if (SuitsLoaded) {
                return; 
            }
            //Get all unlockables
            for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++) {

                UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];
                WTOBase.LogToConsole("Processing unlockable {index=" + i + ", name=" + unlockableItem.unlockableName + "}");

                //This will skip executing of the remaining code UNLESS the object we've found is a suit
                if (unlockableItem.suitMaterial == null || !unlockableItem.alreadyUnlocked) {
                    continue;
                }

                //Create new suits based on the materials
                foreach (string SuitPath in SuitMaterialPaths) {

                    UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
                    newUnlockableItem.suitMaterial = WTOBase.ItemAssetBundle.LoadAsset<Material>(SuitPath);

                    //prepare and set name
                    String SuitName = SuitPath.Substring(19,8);
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
        private static void PatchPosters(StartOfRound __instance) {
            Material[] materials = ((Renderer)GameObject.Find("HangarShip/Plane.001").GetComponent<MeshRenderer>()).materials;
            materials[1] = WTOBase.ItemAssetBundle.LoadAsset<Material>("Assets/CustomSuits/Poster.mat");
            ((Renderer)GameObject.Find("HangarShip/Plane.001").GetComponent<MeshRenderer>()).materials = materials;
        }
    }
}
