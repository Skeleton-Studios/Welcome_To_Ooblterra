using Unity.Netcode;
using UnityEngine;
using HarmonyLib;
using Welcome_To_Ooblterra.Properties;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using System.Collections.Generic;
using LethalLevelLoader;

namespace Welcome_To_Ooblterra.Patches
{
    internal class FactoryPatch {

        private const string DungeonPath = "CustomDungeon/Data/";
        private const string BehaviorPath = "CustomDungeon/Behaviors/";
        private const string SecurityPath = "CustomDungeon/Security/";
        private const string DoorPath = "CustomDungeon/Doors/";
        private const string EmptyPrefabsPath = "CustomDungeon/Rooms/EMPTY.PREFABS/";
        public static List<SpawnableMapObject> SecurityList = new();
        public static ExtendedDungeonFlow OoblDungeonFlow;

        private static readonly WTOBase.WTOLogger Log = new(typeof(FactoryPatch), LogSourceType.Generic);

        //PATCHES 
        [HarmonyPatch(typeof(RoundManager), "SpawnMapObjects")]
        [HarmonyPrefix]
        private static bool WTOSpawnMapObjects(RoundManager __instance) {
            if (WTOBase.WTOForceHazards.Value == TiedToLabEnum.UseMoonDefault && __instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
                return true;
            
            }
            if (DungeonManager.CurrentExtendedDungeonFlow != OoblDungeonFlow) {
                return true;
            }
            if (__instance.currentLevel.spawnableMapObjects == null || __instance.currentLevel.spawnableMapObjects.Length == 0) {
                return true;
            }
            System.Random MapHazardRandom = new(StartOfRound.Instance.randomMapSeed + 587);
            __instance.mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
            RandomMapObject[] AllRandomSpawnList = Object.FindObjectsOfType<RandomMapObject>();
            List<string> ValidHazardList = WTOBase.CSVSeperatedStringList(WTOBase.WTOHazardList.Value);
            if(WTOBase.WTOForceHazards.Value != TiedToLabEnum.WTOOnly) {
                foreach(SpawnableMapObject MapObj in __instance.currentLevel.spawnableMapObjects) {
                    ValidHazardList.Add(MapObj.prefabToSpawn.name.ToLower());
                }
            }
            for (int MapObjectIndex = 0; MapObjectIndex < __instance.currentLevel.spawnableMapObjects.Length; MapObjectIndex++) {
                //Check if the map object is allowed to spawn :)
                if (!ValidHazardList.Contains(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn.name.ToLower())){
                    Log.Error($"Object {__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn.name.ToLower()} not found in valid spawn list!");
                    continue;
                }
                List<RandomMapObject> ValidRandomSpawnList = new();
                int MapObjectsToSpawn = (int)__instance.currentLevel.spawnableMapObjects[MapObjectIndex].numberToSpawn.Evaluate((float)MapHazardRandom.NextDouble());
                WTOBase.WTOLogSource.LogInfo($"Attempting to spawn {__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn}; Quantity: {MapObjectsToSpawn}");
                if (__instance.increasedMapHazardSpawnRateIndex == MapObjectIndex) {
                    MapObjectsToSpawn = Mathf.Min(MapObjectsToSpawn * 2, 150);
                }
                if (MapObjectsToSpawn <= 0) {
                    continue;
                }
                for (int NextSpawnIndex = 0; NextSpawnIndex < AllRandomSpawnList.Length; NextSpawnIndex++) {
                    string List = "";
                    foreach (GameObject SpawnablePrefab in AllRandomSpawnList[NextSpawnIndex].spawnablePrefabs) {
                        if (Equals(SpawnablePrefab, __instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn)) {

                            ValidRandomSpawnList.Add(AllRandomSpawnList[NextSpawnIndex]);
                        }
                        List += SpawnablePrefab+ ", ";
                    }
                    //WTOBase.WTOLogSource.LogInfo($"Spawn point {AllRandomSpawnList[NextSpawnIndex].name} contains: {List}");
                

                }
                //WTOBase.WTOLogSource.LogInfo($"Valid Spawns Found: {ValidRandomSpawnList.Count}");
                for (int i = 0; i < MapObjectsToSpawn; i++) {
                    //lol
                    if(ValidRandomSpawnList.Count <= 0) {
                        WTOBase.WTOLogSource.LogInfo($"Objects will not spawn; no valid random spots found!");
                        break;
                    }
                    RandomMapObject RandomSpawn = ValidRandomSpawnList[MapHazardRandom.Next(0, ValidRandomSpawnList.Count)];
                    Vector3 SpawnPos = RandomSpawn.transform.position;
                    GameObject NewHazard = Object.Instantiate(__instance.currentLevel.spawnableMapObjects[MapObjectIndex].prefabToSpawn, SpawnPos, RandomSpawn.transform.rotation, __instance.mapPropsContainer.transform);
                    NewHazard.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                    WTOBase.WTOLogSource.LogInfo($"Spawned new {NewHazard}"); 
                    ValidRandomSpawnList.Remove(RandomSpawn);
                }
            }
            return false;
        }

        //METHODS
        public static void Start() {

            OoblDungeonFlow = WTOBase.ContextualLoadAsset<ExtendedDungeonFlow>(DungeonPath + "OoblLabExtendedDungeonFlow.asset");
            //OoblDungeonFlow.manualPlanetNameReferenceList.Clear();
            //OoblDungeonFlow.manualPlanetNameReferenceList.Add(new StringWithRarity("523 Ooblterra", 300));
            PatchedContent.RegisterExtendedDungeonFlow(OoblDungeonFlow);
        

            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(BehaviorPath + "ChargedBattery.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(BehaviorPath + "BatteryRecepticleTransform.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(SecurityPath + "TeslaCoil.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(SecurityPath + "SpikeTrap.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(SecurityPath + "BabyLurkerEgg.prefab"));

            // Without adding these as network objects and registering them here, this causes an exception in the LC code:
            // [Info   : Unity Log] Exception! Unable to sync spawned objects on host; System.NullReferenceException: Object reference not set to an instance of an object
            // at (wrapper dynamic-method) RoundManager.DMD<RoundManager::SpawnSyncedProps>(RoundManager)
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "EntranceTeleportA.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "EntranceTeleportB.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "Landmine.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "SpikeRoofTrapHazard.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "Turret.prefab"));
            NetworkPrefabs.RegisterNetworkPrefab(WTOBase.ContextualLoadAsset<GameObject>(EmptyPrefabsPath + "VentEntrance.prefab"));
        
        }
    }
}
