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

namespace Welcome_To_Ooblterra.Patches;
internal class FactoryPatch {

    public static EntranceTeleport MainExit;
    public static EntranceTeleport FireExit;
    private static readonly AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
    private static NetworkManager networkManagerRef;
    private const string DungeonPath = "Assets/CustomDungeon/Data/";

    //PATCHES 
    [HarmonyPatch (typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void GetNetworkManager(StartOfRound __instance) {
        networkManagerRef = __instance.NetworkManager;
    }

    [HarmonyPatch(typeof(RoundManager), "SpawnScrapInLevel")]
    [HarmonyPrefix]
    private static void ScrapValueAdjuster(RoundManager __instance) {
        if(__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            __instance.scrapValueMultiplier = 0.4f;
            return;
        }
        __instance.scrapValueMultiplier = 1f;
    }

    [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
    [HarmonyPostfix]
    private static void CreateCorrespondingExits(RoundManager __instance) {
        if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            return;
        }

        //Grab all the entranceteleports and use the IDs to determine which is the main and fire entrance 
        /* NOTE:
            * The best way to make this function work for March is probably to set id 0 as normal, but then set subsequent IDs to instantiate 
            * a new list item. Make the FireEntrance variable a list that can have either 1 thing added to it or 3, and then run the CreateExit
            * on a loop iterating over that list
            */
        EntranceTeleport MainEntrance = null;
        EntranceTeleport FireEntrance = null;
        EntranceTeleport[] ExistingEntranceArray = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
        WTOBase.LogToConsole($"Found {ExistingEntranceArray.Length} fire exits to iterate over...");
        for (int i = 0; i < ExistingEntranceArray.Length; i++) {
            if (ExistingEntranceArray[i].entranceId == 0) {
                MainEntrance = ExistingEntranceArray[i];
            } else {
                FireEntrance = ExistingEntranceArray[i];
            }
        }

        MainExit = CreateExit("SpawnEntranceTrigger", MainEntrance);
        FireExit = CreateExit("SpawnEntranceBTrigger", FireEntrance);

        //Fire exits are rotated wrong on Ooblterra for some reason 
        FireEntrance.transform.Rotate(0, -90, 0);
        FireExit.transform.Rotate(0, -90, 0);
        Object[] AllTeleports = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
        foreach(EntranceTeleport Teleport in AllTeleports){ 
            Teleport.FindExitPoint();
            WTOBase.LogToConsole($"Entrance #{System.Array.IndexOf(AllTeleports, Teleport)} exitPoint: {Teleport.exitPoint}");
        }
        ReplaceVents();
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPrefix]
    private static void DestroyExitsOnLeave(StartOfRound __instance) {
        DestroyExit(MainExit);
        DestroyExit(FireExit);
    }

    [HarmonyPatch(typeof(EntranceTeleport), "TeleportPlayer")]
    [HarmonyPrefix]
    private static void CheckTeleport(EntranceTeleport __instance) {
        WTOBase.LogToConsole("Attempting Teleport");
        if (RoundManager.Instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
            return;
        }
        WTOBase.LogToConsole("Player Location: " + GameNetworkManager.Instance.localPlayerController.transform.position);
        //WTOBase.LogToConsole("Exit Location: " + __instance.exitPoint.position);
        TimeOfDay.Instance.insideLighting = __instance.isEntranceToBuilding;
    }

    //METHODS
    public static void Start() {
        //Create the Dungeon and load it 
        DungeonFlow OoblFacilityFlow = FactoryBundle.LoadAsset<DungeonFlow>(DungeonPath + "WTOFlow.asset");
        DungeonDef OoblFacilityDungeon = ScriptableObject.CreateInstance<LethalLib.Extras.DungeonDef>();
        OoblFacilityDungeon.name = "Oobl Laboratory";
        OoblFacilityDungeon.dungeonFlow = OoblFacilityFlow;
        OoblFacilityDungeon.rarity = 99999;
        AddDungeon(OoblFacilityDungeon, Levels.LevelTypes.None, new string[] { "OoblterraLevel" });
        Debug.Log("Dungeon Loaded: " + OoblFacilityDungeon.name);
    }
    private static void DestroyExit(EntranceTeleport ExitToDestroy) {
        if (ExitToDestroy == null) {
            return;
        } 
        if (networkManagerRef.IsHost) {
            ExitToDestroy.GetComponent<NetworkObject>().Despawn();
        }
        GameObject.Destroy(ExitToDestroy);
    }
    private static void ReplaceVents() {
        int VentsFound = 0;
        SpawnSyncedObject[] SyncedObjects = GameObject.FindObjectsOfType<SpawnSyncedObject>();
        NetworkPrefab networkPrefab = networkManagerRef.NetworkConfig.Prefabs.m_Prefabs.First(x => x.Prefab.name == "VentEntrance");
        if (networkPrefab == null) {
            WTOBase.LogToConsole("LC Vent Prefab not found!!");
            return;
        }
        foreach (SpawnSyncedObject ventSpawnPoint in SyncedObjects.Where(objectToTest => objectToTest.spawnPrefab.name == "VentDummy")) {
            VentsFound++;
            GameObject.Instantiate(networkPrefab.Prefab);
            networkPrefab.Prefab.transform.position = ventSpawnPoint.transform.position;
            networkPrefab.Prefab.transform.rotation = ventSpawnPoint.transform.rotation;
        }
        WTOBase.LogToConsole("Vents Found: " + VentsFound);
    }
    private static EntranceTeleport CreateExit(string SpawnLocationName, EntranceTeleport ExistingEntrance) {
        //Grab a list of every game object
        GameObject[] PossibleEntrances = GameObject.FindObjectsOfType<GameObject>();

        //Create a copy of the entrance that already exists 
        EntranceTeleport NewExit = GameObject.Instantiate(ExistingEntrance);
        if (networkManagerRef.IsHost) {
            NewExit.GetComponent<NetworkObject>().Spawn();
        }
        //Iterate through the list and find an object with the spawntrigger dummy's name 
        /* NOTE: 
            * I was about to make it so that this uses a GameObject.Find instead of a foreach, but actually with slight
            * modification it may be possible to adapt this function to allow The Laboratory to work properly with March's 3 fire exits. 
            */
        foreach (GameObject NewExitSpawnPoint in PossibleEntrances.Where(Obj => Obj.name.Contains(SpawnLocationName))) {              
            //Set the exit's transform to match the spawn point we found 
            NewExit.transform.position = NewExitSpawnPoint.transform.position;
            NewExit.transform.rotation = NewExitSpawnPoint.transform.rotation;

            //IDK why the fuck I need to do this but for some reason I need to remove the teleport player delegate and add a new one :) 
            InteractTrigger ExitInteractor = NewExit.GetComponent<InteractTrigger>();
            ExitInteractor.onInteract.RemoveAllListeners();
            ExitInteractor.onInteract.AddListener(delegate { NewExit.TeleportPlayer(); });

            NewExit.isEntranceToBuilding = false;
            //For march we will probably need to destroy or flag this Spawn Point so it isn't used again when we call the function again
            return NewExit;
        }
        WTOBase.LogToConsole("Exit Spawn not found!!!!");
        return null;
    }
}
