using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra {
    
    /* Heavily modified from AlexCodesGames' AdditionalSuits mod,
     * which gives explicit permission on both the repo and in 
     * the plugin.cs file. Thank you!
     */

    internal class SuitPatch {

        static string[] suitMaterials = new string[] {
            "Assets/CustomSuits/ProtSuit.mat",
            "Assets/CustomSuits/MackSuit.mat"
        };

        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPrefix]
        private static void StartPatch(ref StartOfRound __instance) {
            if (WTOBase.SuitsLoaded) {
                WTOBase.LogToConsole("Step 0");
                return; 
            }
            WTOBase.LogToConsole("Step 1");
            for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++) {

                UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];
                WTOBase.LogToConsole("Processing unlockable {index=" + i + ", name=" + unlockableItem.unlockableName + "}");

                if (unlockableItem.suitMaterial == null || !unlockableItem.alreadyUnlocked) {
                    continue;
                }

                foreach (string SuitPath in suitMaterials) {

                    UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
                    newUnlockableItem.suitMaterial = WTOBase.MyAssets.LoadAsset<Material>(SuitPath);

                    //prepare and set name
                    String SuitName = SuitPath.Substring(19,8);
                    newUnlockableItem.unlockableName = SuitName;

                    //add new item to the listing of tracked unlockable items
                    __instance.unlockablesList.unlockables.Add(newUnlockableItem);
                }
                WTOBase.SuitsLoaded = true;
                break;
            }
        }
    }
}
