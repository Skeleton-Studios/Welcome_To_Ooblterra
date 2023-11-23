using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using Welcome_To_Ooblterra.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Welcome_To_Ooblterra{
    [BepInPlugin (modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {
        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.1.1";
        private const string BundledAssetPath = "assets/scenes/customlevel.prefab";
        private const string BundleName = "customlevel";

        private readonly Harmony WTOHarmony = new Harmony (modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;
        public static AssetBundle MyLevel;

        public static string GetBundledAssetPath() { return BundledAssetPath; }

        public static void PrintToConsole(string text) {
            text = "=======" + text + "=======";
            Debug.Log (text);
        }

        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource (modGUID);
            WTOLogSource.LogInfo ("Welcome to Ooblterra!");
            WTOHarmony.PatchAll (typeof (WTOBase));
            WTOHarmony.PatchAll (typeof (MoonPatch));
            try {
                string pathToFile = Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), BundleName);
                PrintToConsole("Bundle Path: " + pathToFile);
                MyLevel = AssetBundle.LoadFromFile (pathToFile);
                PrintToConsole("BEGIN PRINTING LOADED ASSETS");
                foreach (string AssetNameToPrint in MyLevel.GetAllAssetNames ()) {
                    WTOBase.PrintToConsole("Asset in bundle: " + AssetNameToPrint);
                }
                PrintToConsole("END PRINTING LOADED ASSETS");

                GameObject MyLevelAsset = MyLevel.LoadAsset (BundledAssetPath) as GameObject;
            } catch {
                PrintToConsole("Asset already loaded, skipping...");
            }
        }

    }

}
