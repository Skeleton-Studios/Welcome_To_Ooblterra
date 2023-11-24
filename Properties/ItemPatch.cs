using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Welcome_To_Ooblterra.Properties {
    internal class ItemPatch {
        private static GameObject HandCrystalItem;

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        //try to add our custom items
        private static void AddCustomItems(StartOfRound __instance) {
            //Create our custom items
            HandCrystalItem = WTOBase.MyAssets.LoadAsset<GameObject>("Assets/CustomItems/handcrystal.prefab");
            //LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(HandCrystalItem.spawnPrefab);
            /*
            try { 
                foreach (string assetname in WTOBase.GetModelListKeys()) {
                    Item item = WTOBase.MyAssets.LoadAsset<Item>("Assets/CustomItems" + assetname + ".fbx");
                    item.name = assetname;
                    item.twoHanded = false;
                    NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
                    Items.RegisterScrap(item, WTOBase.GetModelListValue(assetname), LevelTypes.All);
                    WTOBase.PrintToConsole("Custom items loaded!");
                }
            } catch {
                WTOBase.PrintToConsole("Error adding custom assets!");
            }
         */
        }

        //try to spawn the object 
        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        private static void TrySpawnNewItem(StartOfRound __instance) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                var Crystal = UnityEngine.Object.Instantiate(HandCrystalItem, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                //Crystal.GetComponent<NetworkObject>().Spawn();
                WTOBase.PrintToConsole("Custom item spawned...");
            }
        }
    }
}
