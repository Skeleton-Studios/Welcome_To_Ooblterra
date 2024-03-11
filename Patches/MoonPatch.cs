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
    private static bool OoblterraHasBeenInitialized = false;
    private const string MoonPath = WTOBase.RootPath + "CustomMoon/";

    private static FootstepSurface GrassSurfaceRef;
    private static AudioClip[] OoblFootstepClips;
    private static AudioClip OoblHitSFX;
    private static AudioClip[] GrassFootstepClips;
    private static AudioClip GrassHitSFX;
    private static AudioClip[] CachedTODMusic;

    //PATCHES

    //Destroy the necessary actors and set our scene
    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix]
    private static void InitCustomLevel(StartOfRound __instance) {
        NetworkManager NetworkStatus = GameObject.FindObjectOfType<NetworkManager>();
        if(NetworkStatus.IsHost && !GameNetworkManager.Instance.gameHasStarted) {
            return;
        }
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            OoblterraHasBeenInitialized = false;
            return;
        }
        WTOBase.LogToConsole("Has level start been run? " + OoblterraHasBeenInitialized);
        if (OoblterraHasBeenInitialized) {
            return;
        }
        WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);
        MoveNavNodesToNewPositions();
        GrassSurfaceRef.clips = OoblFootstepClips;
        GrassSurfaceRef.hitSurfaceSFX = OoblHitSFX;
        OoblterraHasBeenInitialized = true;
    }

    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPostfix]
    public static void DestroyLevel(StartOfRound __instance) {
        GrassSurfaceRef.clips = GrassFootstepClips;
        GrassSurfaceRef.hitSurfaceSFX = GrassHitSFX;
    }

    [HarmonyPatch(typeof(TimeOfDay), "Start")]
    [HarmonyPrefix]
    public static void ChangeGlobalTimeMultiplier(TimeOfDay __instance) {
        /*
        if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
            __instance.globalTimeSpeedMultiplier = __instance.currentLevel.DaySpeedMultiplier;
            __instance.currentLevel.DaySpeedMultiplier = 1f;
            return; 
        }
        __instance.globalTimeSpeedMultiplier = 1f;
        */
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
            TimeOfDay.Instance.globalTimeAtEndOfDay *= 0;
            TimeOfDay.Instance.globalTime *= 0;
            
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

    [HarmonyPatch(typeof(TimeOfDay), "Start")]
    [HarmonyPrefix]
    private static void ManageTODMusic(TimeOfDay __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            if(CachedTODMusic != null) {
                __instance.timeOfDayCues = CachedTODMusic;
            }
            return;
        }
        __instance.timeOfDayCues = new AudioClip[4]{
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_StartOfDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_MidDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_LateDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_Night.ogg")
        };
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

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void SetFootstepSounds(StartOfRound __instance) {
        const string FootstepPath = MoonPath + "Sound/Footsteps/";
        GrassSurfaceRef = StartOfRound.Instance.footstepSurfaces[4];

        GrassFootstepClips = StartOfRound.Instance.footstepSurfaces[4].clips;
        GrassHitSFX = StartOfRound.Instance.footstepSurfaces[4].hitSurfaceSFX;
        
        OoblFootstepClips = new AudioClip[] {
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP01.wav"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP02.wav"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP03.wav"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP04.wav"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP05.wav")
        };
        OoblHitSFX = WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLE_Fall.wav");
    }

    //METHODS
    public static void Start() {
        ExtendedLevel Ooblterra = WTOBase.ContextualLoadAsset<ExtendedLevel>(LevelBundle, MoonPatch.MoonPath + "OoblterraExtendedLevel.asset");
        MoonFriendlyName = Ooblterra.selectableLevel.PlanetName;
        Debug.Log($"Ooblterra Found: {Ooblterra != null}");
        PatchedContent.RegisterExtendedLevel(Ooblterra);
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
}

