using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DunGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using static LethalLib.Modules.Levels;
using UnityEngine;
using LethalLib.Modules;
using HarmonyLib;
using Dungeon = LethalLib.Modules.Dungeon;
using Welcome_To_Ooblterra.Properties;
using System.Xml.Linq;
using static LethalLib.Modules.Dungeon;
using DunGen.Graph;
using System.Collections.Generic;
using static UnityEngine.Rendering.HighDefinition.ScalableSettingLevelParameter;
using LethalLib.Extras;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace Welcome_To_Ooblterra.Patches {
    internal class FactoryPatch {

        public static EntranceTeleport NewEntrance;
        private static AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
        internal static DungeonDef OoblFacilityDungeon;

        public static void Start() {
            DungeonFlow OoblFacilityFlow = FactoryBundle.LoadAsset<DungeonFlow>("Assets/CustomInterior/Data/WTOFlow.asset");


            OoblFacilityDungeon = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
            OoblFacilityDungeon.dungeonFlow = OoblFacilityFlow;
            OoblFacilityDungeon.rarity = 99999;
            Dungeon.AddDungeon(OoblFacilityDungeon, Levels.LevelTypes.OoblterraLevel);
        }


        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        private static void ScrapValueAdjuster(RoundManager __instance) {
            if(__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
                __instance.scrapValueMultiplier = 1.0f;
                return;
            }
            __instance.scrapValueMultiplier = 1.5f;
        }

        [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
        [HarmonyPostfix]
        static void FixTeleportDoors(RoundManager __instance) {
            if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
                return;
            }
            NetworkManager Network = GameObject.FindObjectOfType<NetworkManager>();

            EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            EntranceTeleport MainEntrance = null;
            for (int i = 0; i < array.Length; i++) {
                if (array[i].entranceId == 0) {
                    MainEntrance = array[i];
                    break;
                }
            }
            GameObject[] PossibleObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject Object in PossibleObjects) {
                if (Object.name.Contains("SpawnEntranceTrigger")) {
                    NewEntrance = GameObject.Instantiate(MainEntrance);
                    if (Network.IsHost) {
                        NewEntrance.GetComponent<NetworkObject>().Spawn();
                    }
                    NewEntrance.transform.position = Object.transform.position;
                    NewEntrance.transform.rotation = Object.transform.rotation;
                    NewEntrance.isEntranceToBuilding = false;
                    break;
                }
            }

              
            SpawnSyncedObject[] SyncedObjects = GameObject.FindObjectsOfType<SpawnSyncedObject>();
            NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
            int iVentsFound = 0;

            foreach (SpawnSyncedObject syncedObject in SyncedObjects) {
                if (syncedObject.spawnPrefab.name == "VentDummy") {
                    NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.m_Prefabs.First(x => x.Prefab.name == "VentEntrance");
                    if (networkPrefab == null) {
                        WTOBase.LogToConsole("LC Vent Prefab not found!!");
                        return;
                    }
                    //WTOBase.LogToConsole("LC Vent Prefab: " + networkPrefab.Prefab.ToString());
                    iVentsFound++;
                    //syncedObject.spawnPrefab = networkPrefab.Prefab;
                    GameObject.Instantiate(networkPrefab.Prefab);
                    //networkPrefab.Prefab.GetComponent<NetworkObject>().Spawn();
                    networkPrefab.Prefab.transform.position = syncedObject.transform.position;
                    networkPrefab.Prefab.transform.rotation = syncedObject.transform.rotation;
                }
            }
            WTOBase.LogToConsole("Vents Found: " + iVentsFound);
        }
    }
}
