using System.Linq;
using Unity.Netcode;
using UnityEngine;
using LethalLib.Modules;
using HarmonyLib;
using Welcome_To_Ooblterra.Properties;
using static LethalLib.Modules.Dungeon;
using DunGen.Graph;
using LethalLib.Extras;


namespace Welcome_To_Ooblterra.Patches {
    internal class FactoryPatch {

        public static EntranceTeleport NewEntrance;
        public static EntranceTeleport NewFireExit;
        private static readonly AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
        
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
            NetworkManager NetworkStatus = GameObject.FindObjectOfType<NetworkManager>();

            EntranceTeleport[] ExistingEntranceArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            EntranceTeleport OldMainEntrance = null;
            EntranceTeleport OldFireExit = null;
            for (int i = 0; i < ExistingEntranceArray.Length; i++) {
                if (ExistingEntranceArray[i].entranceId == 0) {
                    OldMainEntrance = ExistingEntranceArray[i];
                } else {
                    OldFireExit = ExistingEntranceArray[i];
                }
            }
            OldFireExit.transform.Rotate(0, -90, 0);
            GameObject[] PossibleEntrances = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject EntranceSpawnPoint in PossibleEntrances.Where(Obj => Obj.name.Contains("SpawnEntranceTrigger"))) {
                NewEntrance = GameObject.Instantiate(OldMainEntrance);
                if (NetworkStatus.IsHost) {
                    NewEntrance.GetComponent<NetworkObject>().Spawn();
                }
                NewEntrance.transform.position = EntranceSpawnPoint.transform.position;
                NewEntrance.transform.rotation = EntranceSpawnPoint.transform.rotation;
                NewEntrance.exitPoint = OldMainEntrance.transform;
                NewEntrance.isEntranceToBuilding = false;
            }
            foreach (GameObject FireExitSpawnPoint in PossibleEntrances.Where(Obj => Obj.name.Contains("SpawnEntranceBTrigger"))) {
                OldFireExit = GameObject.Instantiate(OldFireExit);
                if (NetworkStatus.IsHost) {
                    FactoryPatch.NewFireExit.GetComponent<NetworkObject>().Spawn();
                }
                FactoryPatch.NewFireExit.transform.position = FireExitSpawnPoint.transform.position;
                FactoryPatch.NewFireExit.transform.rotation = FireExitSpawnPoint.transform.rotation;
                FactoryPatch.NewFireExit.transform.Rotate(0, -90, 0);
                FactoryPatch.NewFireExit.exitPoint = OldFireExit.transform;
                FactoryPatch.NewFireExit.isEntranceToBuilding = false;
            }
              
            SpawnSyncedObject[] SyncedObjects = GameObject.FindObjectsOfType<SpawnSyncedObject>();
            NetworkManager networkManager = GameObject.FindObjectOfType<NetworkManager>();
            int VentsFound = 0;

            foreach (SpawnSyncedObject ventSpawnPoint in SyncedObjects) {
                if (ventSpawnPoint.spawnPrefab.name == "VentDummy") {
                    NetworkPrefab networkPrefab = networkManager.NetworkConfig.Prefabs.m_Prefabs.First(NetworkPrefab => NetworkPrefab.Prefab.name == "VentEntrance");
                    if (networkPrefab == null) {
                        WTOBase.LogToConsole("LC Vent Prefab not found!!");
                        return;
                    }
                    VentsFound++;
                    GameObject.Instantiate(networkPrefab.Prefab);
                    networkPrefab.Prefab.transform.position = ventSpawnPoint.transform.position;
                    networkPrefab.Prefab.transform.rotation = ventSpawnPoint.transform.rotation;
                }
            }
            WTOBase.LogToConsole("Vents Found: " + VentsFound);
        }

        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        [HarmonyPostfix]
        public static void DestroyDoors(StartOfRound __instance) {
            NetworkManager Network = GameObject.FindObjectOfType<NetworkManager>();
            EntranceTeleport[] Entrances = new EntranceTeleport[] { NewEntrance, NewFireExit };
            foreach (EntranceTeleport Entrance in Entrances) {
                if (Entrance == null) {
                    continue;
                }
                if (Network.IsHost) {
                    Entrance.GetComponent<NetworkObject>().Despawn();
                }
                GameObject.Destroy(Entrance);
            }
        }

        [HarmonyPatch(typeof(EntranceTeleport), "TeleportPlayer")]
        [HarmonyPrefix]
        public static void CheckTeleport(EntranceTeleport __instance) {
            if(RoundManager.Instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
                return;
            }
            WTOBase.LogToConsole("Attepting Teleport");
            WTOBase.LogToConsole("Player Location: " + GameNetworkManager.Instance.localPlayerController.transform.position);
            WTOBase.LogToConsole("Exit Location: " + __instance.exitPoint.position);
            TimeOfDay.Instance.insideLighting = __instance.isEntranceToBuilding;
        }

        public static void Start() {
            DungeonFlow OoblFacilityFlow = FactoryBundle.LoadAsset<DungeonFlow>("Assets/CustomInterior/Data/WTOFlow.asset");
            DungeonDef OoblFacilityDungeon = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
            OoblFacilityDungeon.dungeonFlow = OoblFacilityFlow;
            OoblFacilityDungeon.rarity = 99999;
            AddDungeon(OoblFacilityDungeon, Levels.LevelTypes.None, new string[] { "OoblterraLevel" });
        }

    }
}
