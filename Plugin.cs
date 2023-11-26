using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Welcome_To_Ooblterra.Patches;
using System.IO;
using System.Reflection;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace Welcome_To_Ooblterra{

    /* S/O to Minx, whose tutorial provided a great starting point.
     * Wouldn'tve had any of this code without it, thank you!
     */

    [BepInPlugin (modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {

        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.3.0";
        private const string BundleName = "customlevel";
        public static bool SuitsLoaded;

        string pathToBundle = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), BundleName);
        private readonly Harmony WTOHarmony = new Harmony (modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;
        public static AssetBundle MyAssets;

        public static void LogToConsole(string text) {
            text = "=======" + text + "=======";
            Debug.Log (text);
        }

        void Awake() {
            //Load up various things and tell the console we've loaded
            if (Instance == null) {
                Instance = this;
            }
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource (modGUID);
            WTOLogSource.LogInfo("Welcome to Ooblterra!");
            WTOHarmony.PatchAll(typeof(WTOBase));
            WTOHarmony.PatchAll(typeof(MoonPatch));
            WTOHarmony.PatchAll(typeof(ItemPatch));
            WTOHarmony.PatchAll(typeof(SuitPatch));

            //Loads the assetbundle and tells us everything in it
            LogToConsole("Bundle Path: " + pathToBundle);
            MyAssets = AssetBundle.LoadFromFile (pathToBundle);

            LogToConsole("BEGIN PRINTING LOADED ASSETS");
            foreach (string AssetNameToPrint in MyAssets.GetAllAssetNames ()) {
                Debug.Log("Asset in bundle: " + AssetNameToPrint);
            }
            LogToConsole("END PRINTING LOADED ASSETS");
            ItemPatch.AddCustomItems();
            SuitsLoaded = false;
        }
    }
}
