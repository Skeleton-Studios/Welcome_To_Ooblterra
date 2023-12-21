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

namespace Welcome_To_Ooblterra.Patches {
    internal class FactoryPatch {

        private static AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
        internal static DungeonDef OoblFacilityDungeon;

        public static void Start() {
            DungeonFlow OoblFacilityFlow = FactoryBundle.LoadAsset<DungeonFlow>("Assets/CustomInterior/Data/WTOFlow.asset");


            OoblFacilityDungeon = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
            OoblFacilityDungeon.dungeonFlow = OoblFacilityFlow;
            OoblFacilityDungeon.rarity = 99999;

            Dungeon.AddDungeon(OoblFacilityDungeon, Levels.LevelTypes.ExperimentationLevel);

        }

        [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
        [HarmonyPostfix]
        static void FixTeleportDoors() {
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
                    EntranceTeleport NewEntrance = GameObject.Instantiate(MainEntrance);
                    NewEntrance.GetComponent<NetworkObject>().Spawn();
                    NewEntrance.transform.position = Object.transform.position; 
                    NewEntrance.isEntranceToBuilding = false;
                    break;
                }
            }
                    /*
                    SpawnSyncedObject[] SyncedObjects = GameObject.FindObjectsOfType<SpawnSyncedObject>();
                    NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
                    bool bFoundEntranceA = false;
                    bool bFoundEntranceB = false;
                    int iVentsFound = 0;
                    foreach (SpawnSyncedObject syncedObject in SyncedObjects) {
                        if (syncedObject.spawnPrefab.name == "EntranceTeleportA_EMPTY") {
                            NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.m_Prefabs.Last(x => x.Prefab.name == "EntranceTeleportA");
                            if (networkPrefab == null) {
                                WTOBase.LogToConsole("Failed to find EntranceTeleportA prefab.");
                                return;
                            }
                            WTOBase.LogToConsole("Found and replaced EntranceTeleportA prefab.");
                            bFoundEntranceA = true;
                            syncedObject.spawnPrefab = networkPrefab.Prefab;
                        } else if (syncedObject.spawnPrefab.name == "EntranceTeleportB_EMPTY") {
                            NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.m_Prefabs.First(x => x.Prefab.name == "EntranceTeleportB");
                            bFoundEntranceB = true;
                            syncedObject.spawnPrefab = networkPrefab.Prefab;
                        } else if (syncedObject.spawnPrefab.name == "VentDummy") {
                            NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.m_Prefabs.First(x => x.Prefab.name == "VentEntrance");
                            iVentsFound++;
                            syncedObject.spawnPrefab = networkPrefab.Prefab;
                        }
                    }
                    if (!bFoundEntranceA && !bFoundEntranceB) {
                        return;
                    }
                    */
        }

        [HarmonyPatch(typeof(RoundManager), "Update")]
        [HarmonyPostfix]
        static void CheckDoors() {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
                for (int i = 0; i < array.Length; i++) { 
                    WTOBase.LogToConsole("Entrance Number: "+ i);
                    Debug.Log("Entrance ID = 0?" + (array[i].entranceId != 0).ToString());
                    Debug.Log("Not Is Entrance to Building? " + (!array[i].isEntranceToBuilding).ToString());
                    WTOBase.LogToConsole("Next entrance...");
                }
            }
        }
    }
}
