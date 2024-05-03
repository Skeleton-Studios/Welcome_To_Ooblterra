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
    private const string MoonPath = WTOBase.RootPath + "CustomMoon/";

    private static FootstepSurface GrassSurfaceRef;
    
    private static AudioClip[] OoblFootstepClips;
    private static AudioClip OoblHitSFX;
    private static AudioClip[] GrassFootstepClips;
    private static AudioClip GrassHitSFX;

    private static AudioClip[] CachedTODMusic;
    private static AudioClip[] CachedAmbientMusic;
    private static AudioClip[] OoblTODMusic; 

    //PATCHES
    /*
    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void CacheFootstepSounds(StartOfRound __instance) {
        const string FootstepPath = MoonPath + "Sound/Footsteps/";
        GrassSurfaceRef = StartOfRound.Instance.footstepSurfaces[4];
        if(GrassFootstepClips == null) {
            GrassFootstepClips = StartOfRound.Instance.footstepSurfaces[4].clips;
            GrassHitSFX = StartOfRound.Instance.footstepSurfaces[4].hitSurfaceSFX;
        }
        if (OoblFootstepClips == null) {
            OoblFootstepClips = new AudioClip[] {
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP01.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP02.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP03.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP04.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP05.wav")
            };
            OoblHitSFX = WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLE_Fall.wav");
        }
    }
    */

    [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
    [HarmonyPostfix] 
    private static void ManageNav(StartOfRound __instance) {
        //FOOTSTEP CACHING AND ARRAY CREATION
        const string FootstepPath = MoonPath + "Sound/Footsteps/";
        GrassSurfaceRef = StartOfRound.Instance.footstepSurfaces[4];
        if (GrassFootstepClips == null) {
            GrassFootstepClips = StartOfRound.Instance.footstepSurfaces[4].clips;
            GrassHitSFX = StartOfRound.Instance.footstepSurfaces[4].hitSurfaceSFX;
        }
        if (OoblFootstepClips == null) {
            OoblFootstepClips = new AudioClip[] {
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP01.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP02.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP03.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP04.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP05.wav")
            };
            OoblHitSFX = WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLE_Fall.wav");
        }
        //MUSIC CACHING AND ARRAY CREATION
        if (CachedTODMusic == null) { 
            CachedTODMusic = TimeOfDay.Instance.timeOfDayCues;
            CachedAmbientMusic = SoundManager.Instance.DaytimeMusic;
        }
        if (OoblTODMusic == null) {
            OoblTODMusic = new AudioClip[4]{
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_StartOfDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_MidDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_LateDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_Night.ogg")
            };
        }
        //ASSIGNMENT
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            TimeOfDay.Instance.timeOfDayCues = CachedTODMusic; 
            SoundManager.Instance.DaytimeMusic = CachedAmbientMusic;
            GrassSurfaceRef.clips = GrassFootstepClips;
            GrassSurfaceRef.hitSurfaceSFX = GrassHitSFX;
            return;
        }
        MoveNavNodesToNewPositions();
        GrassSurfaceRef.clips = OoblFootstepClips;
        GrassSurfaceRef.hitSurfaceSFX = OoblHitSFX;
        TimeOfDay.Instance.timeOfDayCues = OoblTODMusic;
        SoundManager.Instance.DaytimeMusic = new AudioClip[0];
    }
    /*
    [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
    [HarmonyPostfix]
    public static void DestroyLevel(StartOfRound __instance) {
        GrassSurfaceRef.clips = GrassFootstepClips;
        GrassSurfaceRef.hitSurfaceSFX = GrassHitSFX;
        TimeOfDay.Instance.timeOfDayCues = CachedTODMusic;
    }

    [HarmonyPatch(typeof(TimeOfDay), "MoveGlobalTime")]
    [HarmonyPrefix]
    private static void ManageTODMusic(TimeOfDay __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            if(CachedTODMusic != null && __instance.timeOfDayCues != CachedTODMusic) {
                WTOBase.LogToConsole($"Setting TOD music to cached music (Cached Music value: {CachedTODMusic[0].name})");
                __instance.timeOfDayCues = CachedTODMusic;
                SoundManager.Instance.DaytimeMusic = CachedAmbientMusic;
            }
            return;
        }
        if (CachedTODMusic != null) {
            WTOBase.LogToConsole($"Putting TOD cues in cache (current value: {__instance.timeOfDayCues[0].name})");
            CachedTODMusic = __instance.timeOfDayCues;
            CachedAmbientMusic = SoundManager.Instance.DaytimeMusic;
        }
        __instance.timeOfDayCues = OoblTODMusic;
        SoundManager.Instance.DaytimeMusic = new AudioClip[0];
    }
    */
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
        MoonFriendlyName = Ooblterra.SelectableLevel.PlanetName;
        WTOBase.LogToConsole($"Ooblterra Found: {Ooblterra != null}");
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
        WTOBase.LogToConsole("Outside nav points: " + CustomNodes.Count().ToString());

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

