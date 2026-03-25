using GameNetcodeStuff;
using HarmonyLib;
using LethalLevelLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches
{
    internal class MoonPatch {

        public static string MoonFriendlyName;

        public static Animator OoblFogAnimator;

        public const string MoonPath = "CustomMoon/";

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

        private static readonly WTOBase.WTOLogger Log = new(typeof(MoonPatch), LogSourceType.Generic);

        //PATCHES
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SceneManager_OnLoadComplete1))]
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
                    WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP01.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP02.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP03.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP04.wav"),
                    WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLESTEP05.wav")
                };
                OoblHitSFX = WTOBase.ContextualLoadAsset<AudioClip>(FootstepPath + "TENTACLE_Fall.wav");
            }
            //MUSIC CACHING AND ARRAY CREATION
            if (CachedTODMusic == null) { 
                CachedTODMusic = TimeOfDay.Instance.timeOfDayCues;
                CachedAmbientMusic = SoundManager.Instance.DaytimeMusic;
            }
            OoblTODMusic ??= new AudioClip[] {
                WTOBase.ContextualLoadAsset<AudioClip>(MoonPath + "Oobl_StartOfDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(MoonPath + "Oobl_MidDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(MoonPath + "Oobl_LateDay.ogg"),
                WTOBase.ContextualLoadAsset<AudioClip>(MoonPath + "Oobl_Night.ogg")
            };
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
            ReplaceStoryLogIDs();       
        }

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnShipLandedMiscEvents))]
        [HarmonyPostfix]
        private static void SetFogTies(StartOfRound __instance) { 
            if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
                return; 
            }
            OoblFogAnimator = GameObject.Find("OoblFog").gameObject.GetComponent<Animator>();
            Log.Debug($"Fog animator found : {OoblFogAnimator != null}");
            if (TimeOfDay.Instance.sunAnimator == OoblFogAnimator){
                Log.Debug($"Sun Animator IS fog animator, supposedly");
                return;
            }
            TimeOfDay.Instance.sunAnimator = OoblFogAnimator;
            Log.Debug($"Is Sun Animator Fog Animator? {TimeOfDay.Instance.sunAnimator == OoblFogAnimator}");
            TimeOfDay.Instance.playDelayedMusicCoroutine = null;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetInsideLightingDimness))]
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

        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayFootstepSound))]
        [HarmonyPrefix]
        private static void PatchFootstepSound(PlayerControllerB __instance) {
            if(StartOfRound.Instance.currentLevel.PlanetName != MoonFriendlyName || __instance.currentFootstepSurfaceIndex != 4) {
                __instance.movementAudio.volume = 0.447f;
                return;
            }
            float BoundFootstepSound = Mathf.Clamp((float)WTOBase.WTOFootsteps.Value, 0, 100);
            __instance.movementAudio.volume = ((BoundFootstepSound / 100f) * 0.447f);
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnOutsideHazards))]
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

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Start))]
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

        [HarmonyPatch(typeof(StartMatchLever), nameof(StartMatchLever.Start))]
        [HarmonyPrefix]
        private static void AutoRouteToOoblterra(StartMatchLever __instance) {
            // applied to StartMatchLevel since this will appear when the ship loads in.
            // if we are set to autoroute, change the level to ooblterra.
            if (WTOBase.WTOAutoRoute.Value && StartOfRound.Instance.IsServer)
            {
                StartOfRound.Instance.StartCoroutine(RouteToOoblterraOnceLLLIsLoaded(__instance));
            }
        }

        private static Func<bool> GetIsLLLReadyFunc()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(LethalLevelLoader.AssetBundleLoader));

            System.Type NetworkBundleManagerType = assembly.GetType("LethalLevelLoader.NetworkBundleManager");

            if (NetworkBundleManagerType == null)
            {
                Log.Warning("Could not find LethalLevelLoader.NetworkBundleManager type");
                return () => true;
            }

            PropertyInfo InstanceProperty = NetworkBundleManagerType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (InstanceProperty == null)
            {
                Log.Warning("Could not find Instance field in NetworkBundleManager");
                return () => true;
            }

            object NetworkBundleManagerInstance = InstanceProperty.GetValue(null);

            FieldInfo AllowedToLoadLevelField = NetworkBundleManagerType.GetField("allowedToLoadLevel", BindingFlags.Instance | BindingFlags.NonPublic);

            if (AllowedToLoadLevelField == null)
            {
                Log.Warning("Could not find allowedToLoadLevel field in NetworkBundleManager");
                return () => true;
            }

            NetworkVariable<bool> allowedToLoadLevel = AllowedToLoadLevelField.GetValue(NetworkBundleManagerInstance) as NetworkVariable<bool>;

            return () =>
            {
                try
                {
                    return allowedToLoadLevel.Value;
                }
                catch (Exception ex)
                {
                    Log.Warning($"Exception while trying to get allowedToLoadLevel value: {ex}");
                    return true;
                }
            };
        }

        private static IEnumerator RouteToOoblterraOnceLLLIsLoaded(StartMatchLever startMatchLever)
        {
            Log.Info("Waiting for LLL to finish loading before routing to Ooblterra...");
            yield return new WaitUntil(GetIsLLLReadyFunc());

            Log.Info("LLL finished loading, routing to Ooblterra...");
            StartOfRound.Instance.ChangeLevel(OoblterraExtendedLevel.SelectableLevel.levelID);
            StartOfRound.Instance.ArriveAtLevel();

            Log.Info("Pulling lever to start match...");
            startMatchLever.leverHasBeenPulled = true;
            startMatchLever.leverAnimatorObject.SetBool("pullLever", true);
            startMatchLever.triggerScript.interactable = false;
            startMatchLever.PullLever();
        }

        //METHODS
        public static void Start() {
            OoblterraExtendedLevel = WTOBase.ContextualLoadAsset<ExtendedLevel>(MoonPath + "OoblterraExtendedLevel.asset");
            MoonFriendlyName = OoblterraExtendedLevel.SelectableLevel.PlanetName;
            Log.Info($"Ooblterra Found: {OoblterraExtendedLevel != null}");
            PatchedContent.RegisterExtendedLevel(OoblterraExtendedLevel);
            CachedSpawnableMapObjects = OoblterraExtendedLevel.SelectableLevel.spawnableMapObjects;
            foreach (SpawnableItemWithRarity Hazard in MoonPatch.OoblterraExtendedLevel.SelectableLevel.spawnableScrap) {
                Log.Info($"{Hazard.spawnableItem.name}"); 
            }
        }

        private static void MoveNavNodesToNewPositions() {
            //Get a list of all outside navigation nodes
            GameObject[] NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

            //Build a list of all our Oobltera nodes
            List<GameObject> CustomNodes = new();
            IEnumerable<GameObject> allObjects = GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.name == "OoblOutsideNode");
            foreach (GameObject Object in allObjects) {
                    CustomNodes.Add(Object);
            }
            Log.Info("Outside nav points: " + CustomNodes.Count().ToString());

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

}
