using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Welcome_To_Ooblterra.Patches;
using System.IO;
using System.Reflection;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Configuration;
using Unity.Netcode;
using System;
using System.Reflection.Emit;
using UnityEngine.InputSystem;
using GameNetcodeStuff;

namespace Welcome_To_Ooblterra.Properties {

    /* S/O to Minx, whose tutorial provided a great starting point.
     * Wouldn'tve had any of this code without it, thank you!
     */

    [BepInPlugin(modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {

        public static ConfigFile ConfigFile;
        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.7.5";

        private readonly Harmony WTOHarmony = new Harmony(modGUID);
        internal ManualLogSource WTOLogSource;
        public static WTOBase Instance;

        public static AssetBundle LevelAssetBundle;
        public static AssetBundle ItemAssetBundle;
        public static AssetBundle FactoryAssetBundle;
        public static AssetBundle MonsterAssetBundle;

        public static bool DoInteractCheck = false;
        public static int InteractNumber = 0;
        public static void LogToConsole(string text) {
            text = "=======" + text + "=======";
            Debug.Log (text);
        }

        public enum AllowedState { 
            Off = 0,
            CustomLevelOnly = 1,
            AllLevels = 2
        }

        void Awake() {

            //Load up various things and tell the console we've loaded
            if (Instance == null) {
                Instance = this;
            }

            //ConfigFile = Instance.Config ;
            //ConfigFile.Save();
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            const string appendix = "";
            WTOLogSource.LogInfo("Welcome to Ooblterra! " + appendix);

            WTOHarmony.PatchAll(typeof(WTOBase));
            //WTOHarmony.PatchAll(typeof(WTOConfig));
            WTOHarmony.PatchAll(typeof(FactoryPatch));
            WTOHarmony.PatchAll(typeof(ItemPatch));
            WTOHarmony.PatchAll(typeof(MonsterPatch));
            WTOHarmony.PatchAll(typeof(MoonPatch));
            WTOHarmony.PatchAll(typeof(SuitPatch));
            WTOHarmony.PatchAll(typeof(TerminalPatch));
            
            if (/* WTOConfig.WTOCustomSuits.Value*/ true) {
                
            }
            LogToConsole("BEGIN PRINTING LOADED ASSETS");
            //AllowedState.TryParse(WTOConfig.CustomInteriorEnabled.ToString(), out AllowedState InteriorState);
            if (/*InteriorState != AllowedState.Off*/ true) {
                string FactoryBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customdungeon");             
                FactoryAssetBundle = AssetBundle.LoadFromFile(FactoryBundlePath);
            }

            //AllowedState.TryParse(WTOConfig.SpawnScrapStatus.ToString(), out AllowedState ItemState);
            if (/*ItemState != AllowedState.Off*/ true) {
                string ItemBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customitems");
                ItemAssetBundle = AssetBundle.LoadFromFile(ItemBundlePath);
            }

            if (/* WTOConfig.OoblterraEnabled.Value*/ true) {
                string LevelBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custommoon");
                LevelAssetBundle = AssetBundle.LoadFromFile(LevelBundlePath);
            }

            /*
            AllowedState.TryParse(WTOConfig.SpawnOutdoorEnemyStatus.ToString(), out AllowedState OutdoorMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnIndoorEnemyStatus.ToString(), out AllowedState IndoorMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnAmbientEnemyStatus.ToString(), out AllowedState DaytimeMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnSecurityStatus.ToString(), out AllowedState SecurityState);
            if (OutdoorMonsterState != AllowedState.Off || IndoorMonsterState != AllowedState.Off
                || DaytimeMonsterState != AllowedState.Off || SecurityState != AllowedState.Off
            true) {
            */
            string MonsterBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customenemies");
            MonsterAssetBundle = AssetBundle.LoadFromFile(MonsterBundlePath);

            FactoryPatch.Start();
            ItemPatch.Start();
            MonsterPatch.Start();
            MoonPatch.Start();
            SuitPatch.Start();
            TerminalPatch.Start();
            

            //NetcodeWeaver stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types) {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods) {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0) {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        public static void DebugHelper(StartOfRound __instance) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                /*
                DoInteractCheck = !DoInteractCheck;
                LogToConsole($"PRINTING INTERACT INFORMATION? {DoInteractCheck}");
                
                UnityEngine.Object[] NumberOfEntrances = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
                LogToConsole($"Number of entrances on map: {NumberOfEntrances.Length}");
                for (int i = 0; i < NumberOfEntrances.Length; i++) {
                    ((EntranceTeleport)NumberOfEntrances[i]).FindExitPoint();
                    LogToConsole($"Entrance #{i} exitPoint: {((EntranceTeleport)NumberOfEntrances[i]).exitPoint}");
                }
                
                bool flag = TimeOfDay.Instance.sunAnimator == MoonPatch.OoblFogAnimator;
                WTOBase.LogToConsole($"Is fog animator correct? {flag}");
                if (!flag) {
                    TimeOfDay.Instance.sunAnimator = MoonPatch.OoblFogAnimator;
                }
                
                SprayPaintItem[] SprayPaints = GameObject.FindObjectsOfType<SprayPaintItem>();
                foreach(SprayPaintItem sprayPaint in SprayPaints) {
                    sprayPaint.debugSprayPaint = true;
                }

                
                WTOBase.LogToConsole("BEGIN PRINTING LIST OF ENTRANCES");
                EntranceTeleport[] array = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>(includeInactive: true);
                foreach (EntranceTeleport entrance in array) {
                    Debug.Log(entrance);
                }
                WTOBase.LogToConsole("END PRINTING LIST OF ENTRANCES");
                
                var Monster = UnityEngine.Object.Instantiate(InsideEnemies[2].enemyType.enemyPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                Monster.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("EyeSec spawned...");
                */
            }
        }
    }
}
