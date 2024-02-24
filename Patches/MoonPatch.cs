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
using LethalLevelLoader;

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
            "Sky and Fog Global Volume",
            "Local Volumetric Fog",
            "SunTexture"
        };
    private static bool LevelLoaded;
    private static bool LevelStartHasBeenRun = false;
    private const string MoonPath = WTOBase.RootPath + "CustomMoon/";

    private static FootstepSurface CachedGrassSurface = null;

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
        MoveNavNodesToNewPositions();
        ManageFootsteps();
        
        LevelStartHasBeenRun = true;
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPostfix]
    public static void DestroyLevel(StartOfRound __instance) {
        StartOfRound.Instance.footstepSurfaces[4].clips = CachedGrassSurface?.clips;
        StartOfRound.Instance.footstepSurfaces[4].hitSurfaceSFX = CachedGrassSurface?.hitSurfaceSFX;
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            DestroyOoblterraPrefab();
            LevelStartHasBeenRun = false;
        }
    }

    [HarmonyPatch(typeof(TimeOfDay), "MoveGlobalTime")]
    [HarmonyPrefix]
    public static void ChangeGlobalTimeMultiplier(TimeOfDay __instance) {
        //It makes no sense that this is working but it is. lol?
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            __instance.globalTimeSpeedMultiplier = __instance.currentLevel.DaySpeedMultiplier;
            __instance.currentLevel.DaySpeedMultiplier = 1f;
            return;
        }
        __instance.globalTimeSpeedMultiplier = 1f;
    }

    [HarmonyPatch(typeof(StartOfRound), "PassTimeToNextDay")]
    [HarmonyPrefix]
    public static bool SettleTimeIssue(StartOfRound __instance) {
        
        
        WTOBase.LogToConsole($"BEGIN PRINT PRE BASE FUNCTION VALUES:");
        Debug.Log($"GLOBAL TIME AT END OF DAY: {TimeOfDay.Instance.globalTimeAtEndOfDay}");
        Debug.Log($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");
        Debug.Log($"TOTAL TIME: {TimeOfDay.Instance.totalTime}");
        Debug.Log($"TIME UNTIL DEADLINE: {TimeOfDay.Instance.timeUntilDeadline}");
        Debug.Log($"DAYS: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}");
        WTOBase.LogToConsole($"END PRINT PRE BASE FUNCTION VALUES:");
        
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            //TimeOfDay.Instance.globalTimeAtEndOfDay *= 0;
            //TimeOfDay.Instance.globalTime *= 0;
            
            WTOBase.LogToConsole($"BEGIN PRINT POST MODIFICATION VALUES:");
            Debug.Log($"GLOBAL TIME AT END OF DAY: {TimeOfDay.Instance.globalTimeAtEndOfDay}");
            Debug.Log($"GLOBAL TIME: {TimeOfDay.Instance.globalTime}");
            Debug.Log($"TOTAL TIME: {TimeOfDay.Instance.totalTime}");
            Debug.Log($"TIME UNTIL DEADLINE: {TimeOfDay.Instance.timeUntilDeadline}");
            Debug.Log($"DAYS: {(int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime)}");
            WTOBase.LogToConsole($"END PRINT POST MODIFICATION VALUES:");
            
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
        ExtendedLevel Ooblterra = WTOBase.ContextualLoadAsset<ExtendedLevel>(LevelBundle, MoonPatch.MoonPath + "OoblterraExtendedLevel.asset");
        MoonFriendlyName = Ooblterra.selectableLevel.PlanetName;
        Debug.Log($"Ooblterra Found: {Ooblterra != null}");
        PatchedContent.RegisterExtendedLevel(Ooblterra);
    }
    private static void DestroyOoblterraPrefab() {
        if (LevelLoaded) { 
            GameObject.Destroy(LevelPrefab);
        }
        LevelLoaded = false;
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
        if (CachedGrassSurface == null) {
            CachedGrassSurface = StartOfRound.Instance.footstepSurfaces[4];
        }
        for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++) {
            FootstepSurface NextSurface = StartOfRound.Instance.footstepSurfaces[i];
            if (NextSurface.surfaceTag == "Grass") {
                WTOBase.LogToConsole($"Grass surface index: {i}");
                NextSurface.clips = new AudioClip[] {
                    WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP01.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP02.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP03.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP04.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP05.wav")
                };
                NextSurface.hitSurfaceSFX = WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLE_Fall.wav");
            }
        }
    }
}

