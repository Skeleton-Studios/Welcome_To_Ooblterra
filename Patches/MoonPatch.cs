using HarmonyLib;
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;
using System.Runtime.CompilerServices;
using DunGen.Adapters;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Welcome_To_Ooblterra.Patches;

internal class MoonPatch {

    public static string MoonFriendlyName;
    public static SelectableLevel MyNewMoon;

    public static Animator OoblFogAnimator;

    private static readonly AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
    private static UnityEngine.Object LevelPrefab = null;
    private static readonly string[] ObjectNamesToDestroy = new string[]{
            "CompletedVowTerrain",
            "tree",
            "Tree",
            "Rock",
            "StaticLightingSky",
            //"ForestAmbience",
            "Sky and Fog Global Volume",
            "Local Volumetric Fog",
            "SunTexture"
        };
    private static bool LevelLoaded;
    private static bool LevelStartHasBeenRun = false;
    private const string MoonPath = "Assets/WelcomeToOoblterra/CustomMoon/";

    //PATCHES
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPrefix]
    [HarmonyPriority(0)]
    private static void FuckThePlanet(StartOfRound __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            DestroyOoblterraPrefab();
        }
    }

    //Defining the custom moon for the API
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPrefix]
    [HarmonyPriority(0)]
    private static void AddMoonToList(StartOfRound __instance) {
        //SetMoonVariables(MyNewMoon, __instance);
        //AddToMoons(MyNewMoon, __instance);
        LevelStartHasBeenRun = false;
    }

    //Destroy the necessary actors and set our scene
    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    private static void InitCustomLevel(StartOfRound __instance) {
        NetworkManager NetworkStatus = GameObject.FindObjectOfType<NetworkManager>();
        if(NetworkStatus.IsHost && !GameNetworkManager.Instance.gameHasStarted) {
            return;
        }
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            DestroyOoblterraPrefab();
            LevelStartHasBeenRun = false;
            return;
        }
        WTOBase.LogToConsole("Has level start been run? " + LevelStartHasBeenRun);
        if (LevelStartHasBeenRun) {
            return;
        }
        WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);
        //DestroyVowObjects();
        //Load our custom prefab
        /*
        LevelPrefab = GameObject.Instantiate(WTOBase.LevelAssetBundle.LoadAsset(MoonPath + "customlevel.prefab"));
        LevelLoaded = true;
        WTOBase.LogToConsole("Loaded custom terrain object!");
        */

        //MoveDoors();
        //ManageCustomSun();
        MoveNavNodesToNewPositions();

        //HandleInsideNavigation();
        ManageFootsteps();
        
        LevelStartHasBeenRun = true;
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPostfix]
    public static void DestroyLevel(StartOfRound __instance) {
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            DestroyOoblterraPrefab();
            LevelStartHasBeenRun = false;
        }
    }

    /*
    [HarmonyPatch(typeof(StartOfRound), "ChangeLevel")]
    [HarmonyPrefix]
    public static bool ProtectChangeLevel(StartOfRound __instance, int levelID) {
        if(levelID >= 0 && levelID < __instance.levels.Length) {
            return true;
        }
        Debug.Log($"level id: {0} (OVERRIDEN)");
        Debug.Log("Changing level");
        __instance.currentLevel = __instance.levels[0];
        __instance.currentLevelID = 0;
        TimeOfDay.Instance.currentLevel = __instance.currentLevel;
        RoundManager.Instance.currentLevel = __instance.levels[0];
        SoundManager.Instance.ResetSoundType();
        return false;
    }
    */

    [HarmonyPatch(typeof(TimeOfDay), "MoveGlobalTime")]
    [HarmonyPrefix]
    public static void ChangeGlobalTimeMultiplier(TimeOfDay __instance) {
        //Steal the time speed from the scriptablelevel asset, assign it here, and set it to 1 there
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            __instance.globalTimeSpeedMultiplier = 0.7f;
            return;
        }
        __instance.globalTimeSpeedMultiplier = 1f;
    }

    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    [HarmonyPrefix]
    public static bool SettleTimeIssue(StartOfRound __instance) {
        
        /* 
         * WTOBase.LogToConsole($"BEGIN PRINT PRE BASE FUNCTION VALUES:");
        Debug.Log($"GLOBAL TIME AT END OF DAY: {TimeOfDay.Instance.globalTimeAtEndOfDay}");
        Debug.Log($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");
        Debug.Log($"TOTAL TIME: {TimeOfDay.Instance.totalTime}");
        Debug.Log($"TIME UNTIL DEADLINE: {TimeOfDay.Instance.timeUntilDeadline}");
        Debug.Log($"DAYS: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}");
        WTOBase.LogToConsole($"END PRINT PRE BASE FUNCTION VALUES:");
        */
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            TimeOfDay.Instance.globalTimeAtEndOfDay *= 0;
            TimeOfDay.Instance.globalTime *= 0;
            /*
            WTOBase.LogToConsole($"BEGIN PRINT POST MODIFICATION VALUES:");
            Debug.Log($"GLOBAL TIME AT END OF DAY: {TimeOfDay.Instance.globalTimeAtEndOfDay}");
            Debug.Log($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");
            Debug.Log($"TOTAL TIME: {TimeOfDay.Instance.totalTime}");
            Debug.Log($"TIME UNTIL DEADLINE: {TimeOfDay.Instance.timeUntilDeadline}");
            Debug.Log($"DAYS: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}");
            WTOBase.LogToConsole($"END PRINT POST MODIFICATION VALUES:");
            */
            return true;
        }
        return true;
    }

    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    [HarmonyPostfix]
    public static void SettleTimeIssue2(StartOfRound __instance) {
        WTOBase.LogToConsole($"BEGIN PRINT POST BASE FUNCTION VALUES:");
        Debug.Log($"GLOBAL TIME AT END OF DAY: {TimeOfDay.Instance.globalTimeAtEndOfDay}");
        Debug.Log($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");
        Debug.Log($"TOTAL TIME: {TimeOfDay.Instance.totalTime}");
        Debug.Log($"TIME UNTIL DEADLINE: {TimeOfDay.Instance.timeUntilDeadline}");
        Debug.Log($"DAYS: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}");
        WTOBase.LogToConsole($"END PRINT POST BASE FUNCTION VALUES:");
    }


    [HarmonyPatch(typeof(TimeOfDay), "PlayTimeMusicDelayed")]
    [HarmonyPrefix]
    private static bool SkipTODMusic() {
        return false;
    }

    [HarmonyPatch(typeof(StartOfRound), "OnShipLandedMiscEvents")]
    [HarmonyPostfix]
    private static void SetFogTies(StartOfRound __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            return;
        }
        OoblFogAnimator = GameObject.Find("OoblFog").gameObject.GetComponent<Animator>();
        WTOBase.LogToConsole($"Fog animator found : {OoblFogAnimator != null}");
        if (TimeOfDay.Instance.sunAnimator == OoblFogAnimator){
            WTOBase.LogToConsole($"Sun Animator IS fog animator, supposedly");
            return;
        }
        TimeOfDay.Instance.sunAnimator = OoblFogAnimator;
        WTOBase.LogToConsole($"Is Sun Animator Fog Animator? {TimeOfDay.Instance.sunAnimator == OoblFogAnimator}");
    }

    [HarmonyPatch(typeof(TimeOfDay), "SetInsideLightingDimness")]
    [HarmonyPrefix]
    private static void SpoofLightValues(TimeOfDay __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            return;
        }
        Light Direct = GameObject.Find("ActualSun").GetComponent<Light>();
        Light Indirect = GameObject.Find("ActualIndirect").GetComponent<Light>();
        TimeOfDay timeOfDay = GameObject.FindObjectOfType<TimeOfDay>();
        timeOfDay.sunIndirect = Indirect;
        timeOfDay.sunDirect = Direct;
    }

    //METHODS
    public static void Start() {
        //Load our level asset object
        //MyNewMoon = LevelBundle.LoadAsset<SelectableLevel>(MoonPath + "OoblterraLevel.asset");
        MoonFriendlyName = "523 Ooblterra";
        //Debug.Log(MoonFriendlyName + " Level Object found: " + (MyNewMoon != null).ToString());
    }
    private static void SetMoonVariables(SelectableLevel Moon, StartOfRound Instance) {
        //Moon.spawnableOutsideObjects = new SpawnableOutsideObjectWithRarity[0];
        //Moon.levelAmbienceClips = Instance.levels[2].levelAmbienceClips;

        //FactoryPatch.SetSecurityObjects(Moon, Instance.levels[5].spawnableMapObjects);
        //ItemPatch.SetMoonItemList(Moon);
        //MonsterPatch.SetInsideMonsters(MyNewMoon);
        //MonsterPatch.SetOutsideMonsters(MyNewMoon, new List<SpawnableEnemyWithRarity>() {} );
        //MonsterPatch.SetDaytimeMonsters(MyNewMoon);

    }
    //Following two methods taken from MoonAPI, thanks Bizzlemip
    public static T[] ResizeArray<T>(T[] oldArray, int newSize) {
        T[] array = new T[newSize];
        oldArray.CopyTo(array, 0);
        return array;
    }
    private static int AddToMoons(SelectableLevel Moon, StartOfRound Instance) {
        Instance.levels = ResizeArray(Instance.levels, Instance.levels.Length + 1);
        int num = -1;
        for (int i = 0; i < Instance.levels.Length; i++) {
            if (Instance.levels[i] == null) {
                num = i;
                break;
            }
        }
        if (num == -1) {
            throw new NullReferenceException("No null value found in StartOfRound.levels");
        }
        Instance.levels[num] = Moon;
        foreach (SelectableLevel level in Instance.levels) {
            WTOBase.LogToConsole(level.name);
        }
        return num;
    }
    private static void DestroyOoblterraPrefab() {
        if (LevelLoaded) { 
            GameObject.Destroy(LevelPrefab);
        }
        LevelLoaded = false;
    }
    private static void HandleInsideNavigation() {
        UnityNavMeshAdapter NavMeshAdapter = GameObject.FindObjectOfType<UnityNavMeshAdapter>();
        if (NavMeshAdapter != null) {
            WTOBase.LogToConsole("Found Navmesh adapter! Setting bake mode to FullDungeonBake...");
        }
        NavMeshAdapter.BakeMode = UnityNavMeshAdapter.RuntimeNavMeshBakeMode.FullDungeonBake;
        NavMeshAdapter.AddNavMeshLinksBetweenRooms = true;
    }
    private static void MoveNavNodesToNewPositions() {
        //Get a list of all outside navigation nodes
        GameObject[] NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

        //Build a list of all our Oobltera nodes
        List<GameObject> CustomNodes = new List<GameObject>();
        IEnumerable<GameObject> allObjects = GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.name == "OoblOutsideNode");
        foreach (GameObject Object in allObjects) {
                CustomNodes.Add(Object);
        }
        WTOBase.LogToConsole("Outside nav points: " + allObjects.Count().ToString());

        //Put outside nav nodes at the location of our ooblterra nodes. Destroy any extraneous ones
        for (int i = 0; i < NavNodes.Count(); i++) {
            if (CustomNodes.Count() > i) {
                NavNodes[i].transform.position = CustomNodes[i].transform.position;
            } else {
                GameObject.Destroy(NavNodes[i]);
            }
        }
    }
    private static void ManageFootsteps() {
        const string FootstepPath = MoonPath + "Sound/Footsteps/";
        foreach (FootstepSurface surfaces in StartOfRound.Instance.footstepSurfaces) {
            if (surfaces.surfaceTag == "Grass") {
                surfaces.clips = new AudioClip[] {
                    LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP01.wav"),
                    LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP02.wav"),
                    LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP03.wav"),
                    LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP04.wav"),
                    LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP05.wav")
                };
                surfaces.hitSurfaceSFX = LevelBundle.LoadAsset<AudioClip>(FootstepPath + "TENTACLE_Fall.wav");
            }
        }
    }
    private static void DestroyVowObjects() {
        //I have no fucking clue why this works for the foliage too but fuck it I guess
        IEnumerable<GameObject> allObjects = GameObject.FindObjectsOfType<GameObject>().Where(obj => ObjectNamesToDestroy.Any(obj.name.Contains));
        foreach (GameObject ObjToDestroy in allObjects) {
            GameObject.Destroy(ObjToDestroy);
            continue;
        }
        GameObject Factory = GameObject.Find("Models2VowFactory");
        Factory.SetActive(false);
        /*
        foreach (GameObject ObjToDestroy in allObjects) {
            if (ObjToDestroy.name.Contains("Models2VowFactory")) {
                ObjToDestroy.SetActive(false);
                WTOBase.LogToConsole("Vow factory adjusted.");
            }
                
            //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
            if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds"))) {
                GameObject.Destroy(ObjToDestroy);
            }
            foreach (string UnwantedObjString in ObjectNamesToDestroy) {
                    
                if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                    GameObject.Destroy(ObjToDestroy);
                    continue;
                }
            }
        }
        */
    }
    private static void MoveDoors() {
        //The prefab contains an object called TeleportSnapLocation that we move the primary door to
        GameObject Entrance = GameObject.Find("EntranceTeleportA");
        GameObject SnapLoc = GameObject.Find("TeleportSnapLocation");
        Entrance.transform.position = SnapLoc.transform.position;
        GameObject FireExit = GameObject.Find("EntranceTeleportB");
        GameObject FireExitSnapLoc = GameObject.Find("FireExitSnapLocation");
        FireExit.transform.position = FireExitSnapLoc.transform.position;
    }
}

