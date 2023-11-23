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
using LethalLib.Modules;
using static LethalLib.Modules.Levels;

namespace Welcome_To_Ooblterra{
    [BepInPlugin (modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {
        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.1.1";
        private const string LevelAssetPath = "assets/CustomScene/levelprefab.prefab";
        private static Dictionary<string, int> modelAssets = new Dictionary<string, int> ();
        private const string BundleName = "customlevel";

        private readonly Harmony WTOHarmony = new Harmony (modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;
        public static AssetBundle MyAssets;

        public static string GetBundledAssetPath() { return LevelAssetPath; }
        public static Dictionary<string, int>.KeyCollection GetModelListKeys() { return modelAssets.Keys; }
        public static int GetModelListValue(string key) { return modelAssets[key]; }

        public static void PrintToConsole(string text) {
            text = "=======" + text + "=======";
            Debug.Log (text);
        }

        void Awake() {
            //Load up various things and tell the console we've loaded
            if (Instance == null) {
                Instance = this;
            }
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource (modGUID);
            WTOLogSource.LogInfo ("Welcome to Ooblterra!");
            WTOHarmony.PatchAll (typeof (WTOBase));
            WTOHarmony.PatchAll (typeof (MoonPatch));

            //Attempt to load assetbundle
            try {
                string pathToFile = Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location), BundleName);
                PrintToConsole("Bundle Path: " + pathToFile);
                MyAssets = AssetBundle.LoadFromFile (pathToFile);
                PrintToConsole("BEGIN PRINTING LOADED ASSETS");
                foreach (string AssetNameToPrint in MyAssets.GetAllAssetNames ()) {
                    Debug.Log("Asset in bundle: " + AssetNameToPrint);
                }
                PrintToConsole("END PRINTING LOADED ASSETS");
                GameObject MyLevelAsset = MyAssets.LoadAsset(LevelAssetPath) as GameObject;
            } catch {
                PrintToConsole("Asset already loaded, skipping...");
            }

            //A list of item names and their rarities
            modelAssets.Add("handcrystal", 5);
            modelAssets.Add("crate", 10);
            modelAssets.Add("waterbottle", 5);
        }

    }

}
