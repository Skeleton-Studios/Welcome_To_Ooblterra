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
            WTOBase.LogToConsole("Dungeon Added: " + OoblFacilityDungeon.ToString());

        }

        [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
        [HarmonyPostfix]

        static void FixTeleportDoors() {
            SpawnSyncedObject[] SyncedObjects = GameObject.FindObjectsOfType<SpawnSyncedObject>();
            NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
            bool bFoundEntranceA = false;
            bool bFoundEntranceB = false;
            int iVentsFound = 0;
            foreach (SpawnSyncedObject syncedObject in SyncedObjects) {
                if (syncedObject.spawnPrefab.name == "EntranceTeleportA_EMPTY") {
                    NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportA");
                    bFoundEntranceA = true;
                    syncedObject.spawnPrefab = networkPrefab.Prefab;
                } else if (syncedObject.spawnPrefab.name == "EntranceTeleportB_EMPTY") {
                    NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "EntranceTeleportB");
                    bFoundEntranceB = true;
                    syncedObject.spawnPrefab = networkPrefab.Prefab;
                } else if (syncedObject.spawnPrefab.name == "VentDummy") {
                    NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.Prefabs.First(x => x.Prefab.name == "VentEntrance");
                    iVentsFound++;
                    syncedObject.spawnPrefab = networkPrefab.Prefab;
                }
            }
            if (!bFoundEntranceA && !bFoundEntranceB) {
                return;
            }
        }
    }
}
