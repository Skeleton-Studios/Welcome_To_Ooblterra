using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Welcome_To_Ooblterra.Patches;
using System.IO;
using System.Reflection;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Configuration;

namespace Welcome_To_Ooblterra{

    /* S/O to Minx, whose tutorial provided a great starting point.
     * Wouldn'tve had any of this code without it, thank you!
     */

    [BepInPlugin(modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {

        public static ConfigFile ConfigFile;
        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.4.0";

        private readonly Harmony WTOHarmony = new Harmony(modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;

        public static AssetBundle LevelAssetBundle;
        public static AssetBundle ItemAssetBundle;
        public static AssetBundle FactoryAssetBundle;
        public static AssetBundle MonsterAssetBundle;

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
            WTOLogSource.LogInfo("Welcome to Ooblterra!");

            WTOHarmony.PatchAll(typeof(WTOBase));
            //WTOHarmony.PatchAll(typeof(WTOConfig));
            WTOHarmony.PatchAll(typeof(MonsterPatch));

            if (/* WTOConfig.WTOCustomSuits.Value*/ true) {
                WTOHarmony.PatchAll(typeof(SuitPatch));
            }

            LogToConsole("BEGIN PRINTING LOADED ASSETS");

            //AllowedState.TryParse(WTOConfig.CustomInteriorEnabled.ToString(), out AllowedState InteriorState);
            if (/*InteriorState != AllowedState.Off*/ true) {
                string FactoryBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custominterior");
                FactoryAssetBundle = AssetBundle.LoadFromFile(FactoryBundlePath);
                WTOHarmony.PatchAll(typeof(FactoryPatch));
                foreach (string AssetNameToPrint in FactoryAssetBundle.GetAllAssetNames()) {
                    Debug.Log("Asset in Item bundle: " + AssetNameToPrint);
                }
            }

            //AllowedState.TryParse(WTOConfig.SpawnScrapStatus.ToString(), out AllowedState ItemState);
            if (/*ItemState != AllowedState.Off*/ true) {
                string ItemBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customitems");
                ItemAssetBundle = AssetBundle.LoadFromFile(ItemBundlePath);
                WTOHarmony.PatchAll(typeof(ItemPatch));
                foreach (string AssetNameToPrint in ItemAssetBundle.GetAllAssetNames()) {
                    Debug.Log("Asset in Item bundle: " + AssetNameToPrint);
                }
            }

            if (/* WTOConfig.OoblterraEnabled.Value*/ true) {
                string LevelBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custommoon");
                LevelAssetBundle = AssetBundle.LoadFromFile(LevelBundlePath);
                WTOHarmony.PatchAll(typeof(MoonPatch));
                foreach (string AssetNameToPrint in LevelAssetBundle.GetAllAssetNames()) {
                    Debug.Log("Asset in Level bundle: " + AssetNameToPrint);
                }
            }

            /*
            AllowedState.TryParse(WTOConfig.SpawnOutdoorEnemyStatus.ToString(), out AllowedState OutdoorMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnIndoorEnemyStatus.ToString(), out AllowedState IndoorMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnAmbientEnemyStatus.ToString(), out AllowedState DaytimeMonsterState);
            AllowedState.TryParse(WTOConfig.SpawnSecurityStatus.ToString(), out AllowedState SecurityState);
            if (OutdoorMonsterState != AllowedState.Off || IndoorMonsterState != AllowedState.Off
                || DaytimeMonsterState != AllowedState.Off || SecurityState != AllowedState.Off
            true) {*/
            string MonsterBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customenemies");
            MonsterAssetBundle = AssetBundle.LoadFromFile(MonsterBundlePath);
            foreach (string AssetNameToPrint in MonsterAssetBundle.GetAllAssetNames()) {
                Debug.Log("Asset in bundle: " + AssetNameToPrint);
            }            
            LogToConsole("END PRINTING LOADED ASSETS");
            ItemPatch.AddCustomItems();
            MonsterPatch.CreateWanderer();
        }
    }
}
