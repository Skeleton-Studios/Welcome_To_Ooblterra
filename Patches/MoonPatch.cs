using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using LethalLevelLoader;
using GameNetcodeStuff;

namespace Welcome_To_Ooblterra.Patches;

internal class MoonPatch {

    public static string MoonFriendlyName;

    public static Animator OoblFogAnimator;

    public static readonly AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
    public const string MoonPath = WTOBase.RootPath + "CustomMoon/";

    private static FootstepSurface GrassSurfaceRef;
    
    private static AudioClip[] OoblFootstepClips;
    private static AudioClip OoblHitSFX;
    private static AudioClip[] GrassFootstepClips;
    private static AudioClip GrassHitSFX;

    private static AudioClip[] CachedTODMusic;
    private static AudioClip[] CachedAmbientMusic;
    private static AudioClip[] OoblTODMusic;

    private static SpawnableMapObject[] CachedSpawnableMapObjects;
    public static ExtendedLevel OoblterraExtendedLevel;


    //PATCHES
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
            OoblFootstepClips = [
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP01.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP02.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP03.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP04.wav"),
                WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLESTEP05.wav")
            ];
            OoblHitSFX = WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, FootstepPath + "TENTACLE_Fall.wav");
        }
        //MUSIC CACHING AND ARRAY CREATION
        if (CachedTODMusic == null) { 
            CachedTODMusic = TimeOfDay.Instance.timeOfDayCues;
            CachedAmbientMusic = SoundManager.Instance.DaytimeMusic;
        }
        OoblTODMusic ??= [
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_StartOfDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_MidDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_LateDay.ogg"),
            WTOBase.ContextualLoadAsset<AudioClip>(LevelBundle, MoonPath + "Oobl_Night.ogg")
        ];
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
        SoundManager.Instance.DaytimeMusic = [];
        ReplaceStoryLogIDs();       
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
        TimeOfDay.Instance.playDelayedMusicCoroutine = null;
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

    [HarmonyPatch(typeof(PlayerControllerB), "PlayFootstepSound")]
    [HarmonyPrefix]
    private static void PatchFootstepSound(PlayerControllerB __instance) {
        if(StartOfRound.Instance.currentLevel.PlanetName != MoonFriendlyName || __instance.currentFootstepSurfaceIndex != 4) {
            __instance.movementAudio.volume = 0.447f;
            return;
        }
        float BoundFootstepSound = Mathf.Clamp((float)WTOBase.WTOFootsteps.Value, 0, 100);
        __instance.movementAudio.volume = ((BoundFootstepSound / 100f) * 0.447f);
    }

    [HarmonyPatch(typeof(RoundManager), "SpawnOutsideHazards")]
    [HarmonyPrefix]
    private static bool WTOSpawnOutsideObjects(RoundManager __instance) {
        if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
            return true;
        }
        if (!WTOBase.CSVSeperatedStringList(WTOBase.WTOHazardList.Value).Contains("beartrap")) {
            __instance.currentLevel.spawnableMapObjects = null;
            return false;
        }
        __instance.currentLevel.spawnableMapObjects = CachedSpawnableMapObjects;
        return true;
    }

    [HarmonyPatch(typeof(TimeOfDay), "Start")]
    [HarmonyPrefix]
    private static bool AdjustTODMusic(TimeOfDay __instance) {
        if (RoundManager.Instance.currentLevel.PlanetName != MoonFriendlyName) {
            __instance.TimeOfDayMusic.volume = 1f;
            return true;
        }
        float MusicPercentage = Mathf.Clamp((float)WTOBase.WTOFootsteps.Value, 0, 100);
        __instance.TimeOfDayMusic.volume = MusicPercentage / 100f;
        return true;
    }

    /*[HarmonyPatch(typeof(GrabbableObject), "Start")]
    [HarmonyPostfix]
    private static void SetScrapValueWTO(GrabbableObject __instance) {
        if(RoundManager.Instance.currentLevel.PlanetName != MoonFriendlyName || !WTOBase.WTOScalePrice.Value) {
            return;
        }
        int FinalScrapValue = __instance.scrapValue;
        int RoutePrice = PatchedContent.ExtendedLevels.First(x => x.SelectableLevel.PlanetName == MoonFriendlyName).RoutePrice;
        WTOBase.LogToConsole($" Current Ooblterra route price: {RoutePrice}");
        float ValueScale = RoutePrice / 1700;
        float ClampedValue = Mathf.Clamp(FinalScrapValue * ValueScale, FinalScrapValue * 0.1f, FinalScrapValue * 2);
        FinalScrapValue = Mathf.RoundToInt(ClampedValue);
        __instance.SetScrapValue(FinalScrapValue);
    }*/

    //METHODS
    public static void Start() {
        OoblterraExtendedLevel = WTOBase.ContextualLoadAsset<ExtendedLevel>(LevelBundle, MoonPath + "OoblterraExtendedLevel.asset");
        MoonFriendlyName = OoblterraExtendedLevel.SelectableLevel.PlanetName;
        WTOBase.LogToConsole($"Ooblterra Found: {OoblterraExtendedLevel != null}");
        PatchedContent.RegisterExtendedLevel(OoblterraExtendedLevel);
        CachedSpawnableMapObjects = OoblterraExtendedLevel.SelectableLevel.spawnableMapObjects;
        foreach (SpawnableItemWithRarity Hazard in MoonPatch.OoblterraExtendedLevel.SelectableLevel.spawnableScrap) {
            WTOBase.LogToConsole($"{Hazard.spawnableItem.name}"); 
        }
    }
    private static void MoveNavNodesToNewPositions() {
        //Get a list of all outside navigation nodes
        GameObject[] NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

        //Build a list of all our Oobltera nodes
        List<GameObject> CustomNodes = [];
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
    private static void ReplaceStoryLogIDs() {
        StoryLog[] LevelStoryLogs = GameObject.FindObjectsOfType<StoryLog>();
        foreach(StoryLog LevelStoryLog in LevelStoryLogs) {
            if (TerminalPatch.LogDictionary.TryGetValue(LevelStoryLog.storyLogID, out int ResultValue)){
                LevelStoryLog.storyLogID = ResultValue;
            } 
        }
    }

}

