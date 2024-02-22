using System.Linq;
using Unity.Netcode;
using UnityEngine;
using LethalLib.Modules;
using HarmonyLib;
using Welcome_To_Ooblterra.Properties;
using static LethalLib.Modules.Dungeon;
using DunGen.Graph;
using LethalLib.Extras;
using GameNetcodeStuff;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using System;
using System.Collections.Generic;
using UnityEngine.AI;
using LethalLevelLoader;

namespace Welcome_To_Ooblterra.Patches;
internal class FactoryPatch {

    public static EntranceTeleport MainExit;
    public static EntranceTeleport FireExit;
    private static readonly AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
    private const string DungeonPath = WTOBase.RootPath + "CustomDungeon/Data/";
    private const string BehaviorPath = WTOBase.RootPath + "CustomDungeon/Behaviors/";
    private const string SecurityPath = WTOBase.RootPath + "CustomDungeon/Security/";
    private const string DoorPath = WTOBase.RootPath + "CustomDungeon/Doors/";
    public static List<SpawnableMapObject> SecurityList = new();


    //PATCHES 
    [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
    [HarmonyPrefix]
    private static bool WTOSpawnMapObjects(RoundManager __instance) {
        if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            return true;
        }
        if (__instance.currentLevel.spawnableMapObjects.Length == 0) {
            return true;
        }
        System.Random MapHazardRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 587);
        __instance.mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
        RandomMapObject[] AllRandomSpawnList = UnityEngine.Object.FindObjectsOfType<RandomMapObject>();
        for (int MapObjectIndex = 0; MapObjectIndex < __instance.currentLevel.spawnableMapObjects.Length; MapObjectIndex++) {
            List<RandomMapObject> ValidRandomSpawnList = new List<RandomMapObject>();
            int MapObjectsToSpawn = (int)__instance.currentLevel.spawnableMapObjects[MapObjectIndex].numberToSpawn.Evaluate((float)MapHazardRandom.NextDouble());
            if (__instance.increasedMapHazardSpawnRateIndex == MapObjectIndex) {
                MapObjectsToSpawn = Mathf.Min(MapObjectsToSpawn * 2, 150);
            }
            if (MapObjectsToSpawn <= 0) {
                continue;
            }
            for (int NextSpawnIndex = 0; NextSpawnIndex < AllRandomSpawnList.Length; NextSpawnIndex++) {
                if (AllRandomSpawnList[NextSpawnIndex].spawnablePrefabs.Contains(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn)) {
                    ValidRandomSpawnList.Add(AllRandomSpawnList[NextSpawnIndex]);
                }
            }
            for (int i = 0; i < MapObjectsToSpawn; i++) {
                //lol
                if(ValidRandomSpawnList.Count <= 0) {
                    continue;
                }
                RandomMapObject RandomSpawn = ValidRandomSpawnList[MapHazardRandom.Next(0, ValidRandomSpawnList.Count)];
                Vector3 SpawnPos = RandomSpawn.transform.position;
                GameObject NewHazard = UnityEngine.Object.Instantiate(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn, SpawnPos, RandomSpawn.transform.rotation, __instance.mapPropsContainer.transform);
                NewHazard.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                ValidRandomSpawnList.Remove(RandomSpawn);
            }
        }
        return false;
    }

    //METHODS
    public static void Start() {

        ExtendedDungeonFlow OoblDungeonFlow = WTOBase.LoadAsset<ExtendedDungeonFlow>(FactoryBundle, DungeonPath + "OoblLabExtendedDungeonFlow.asset");
        OoblDungeonFlow.manualPlanetNameReferenceList.Clear();
        OoblDungeonFlow.manualPlanetNameReferenceList.Add(new StringWithRarity("523 Ooblterra", 300));
        PatchedContent.RegisterExtendedDungeonFlow(OoblDungeonFlow);

        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(BehaviorPath + "ChargedBattery.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(SecurityPath + "TeslaCoil.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(SecurityPath + "SpikeTrap.prefab"));
        
    }
}
