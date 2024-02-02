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

namespace Welcome_To_Ooblterra.Patches;
internal class FactoryPatch {

    public static EntranceTeleport MainExit;
    public static EntranceTeleport FireExit;
    private static readonly AssetBundle FactoryBundle = WTOBase.FactoryAssetBundle;
    private static NetworkManager networkManagerRef;
    private const string DungeonPath = "Assets/CustomDungeon/Data/";
    private const string BehaviorPath = "Assets/CustomDungeon/Behaviors/";
    private const string SecurityPath = "Assets/CustomDungeon/Security/";
    private const string DoorPath = "Assets/CustomDungeon/Doors/";
    public static List<SpawnableMapObject> SecurityList = new List<SpawnableMapObject>();


    //PATCHES 
    [HarmonyPatch (typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void GetNetworkManager(StartOfRound __instance) {
        networkManagerRef = __instance.NetworkManager;
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
        UnityEngine.Object[] AllTeleports = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
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

    //I have no actual fucking clue why this is the case, but I have to do this in order to get the frankenstein stuff to spawn properly
    //This is pretty much a verbatim copy of the original function, just with some logs thrown in for sake of debugging
    [HarmonyPatch(typeof(RoundManager), "SpawnSyncedProps")]
    [HarmonyPrefix]
    private static bool WTOSpawnSyncedProps(RoundManager __instance) {
        

        __instance.spawnedSyncedObjects.Clear();
        SpawnSyncedObject[] array = UnityEngine.Object.FindObjectsOfType<SpawnSyncedObject>();
        if (array == null) {
            return true;
        }
        //WTOBase.LogToConsole("BEGIN CHECKING PROPS");
        foreach (SpawnSyncedObject obj in array) {
            //Debug.Log($"Synced object: {obj}");
            //Debug.Log($"Synced object prefab: {obj.spawnPrefab}");
        }
        //WTOBase.LogToConsole("END CHECKING PROPS");

        __instance.mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
        //Debug.Log($"Found map props container?: {__instance.mapPropsContainer != null}");
        
        //Debug.Log($"Spawning synced props on server. Length: {array.Length}");
        for (int i = 0; i < array.Length; i++) {
            GameObject gameObject = null;
            try { 
                gameObject = UnityEngine.Object.Instantiate(array[i].spawnPrefab, array[i].transform.position, array[i].transform.rotation, __instance.mapPropsContainer.transform);
            } catch {
                //WTOBase.LogToConsole($"Instantiation of {array[i].spawnPrefab} failed!");
            }
            if (gameObject != null) {
                try {
                    gameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                    __instance.spawnedSyncedObjects.Add(gameObject);
                } catch {
                    //WTOBase.LogToConsole($"Instantiation of {array[i].spawnPrefab} passed, but does not have a network object!");
                }
            }
        }
        return false;
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
        GameObject.Find("ActualIndirect").GetComponent<Light>().enabled = !TimeOfDay.Instance.insideLighting;
    }

    //METHODS
    public static void Start() {
        //Create the Dungeon and load it 
        DungeonDef OoblFacilityDungeon = FactoryBundle.LoadAsset<DungeonDef>(DungeonPath + "WTODungeonDef.asset");
        OoblFacilityDungeon.name = "Oobl Laboratory";
        OoblFacilityDungeon.rarity = 99999;
        AddDungeon(OoblFacilityDungeon, Levels.LevelTypes.None, new string[] { "OoblterraLevel" });
        Debug.Log("Dungeon Loaded: " + OoblFacilityDungeon.name);
        
        //Register the frankenstein point, will need to add the machine room here
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(BehaviorPath + "FrankensteinPoint.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(BehaviorPath + "FrankensteinWorkbench.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(BehaviorPath + "BatteryRecepticle.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(BehaviorPath + "ScrapShelf.prefab"));

        //Register the door 
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(DoorPath + "OoblDoor.prefab"));

        //Register the custom security
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(SecurityPath + "TeslaCoil.prefab"));
        NetworkPrefabs.RegisterNetworkPrefab(FactoryBundle.LoadAsset<GameObject>(SecurityPath + "SpikeTrap.prefab"));
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

    public static void SetSecurityObjects(SelectableLevel Moon) {
        SpawnableMapObject[] SpawnableMapObjects = new SpawnableMapObject[SecurityList.Count];
        Moon.spawnableMapObjects = SpawnableMapObjects;
        for (int i = 0; i < SecurityList.Count; i++) {
            Moon.spawnableMapObjects.SetValue(SecurityList[i], i);
        }
    }
    public static void SetSecurityObjects(SelectableLevel Moon, SpawnableMapObject[] Objects) {
        Moon.spawnableMapObjects = Objects;

    }
}
