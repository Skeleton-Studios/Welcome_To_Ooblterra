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

namespace Welcome_To_Ooblterra{

    /* S/O to Minx, whose tutorial provided a great starting point.
     * Wouldn'tve had any of this code without it, thank you!
     */

    [BepInPlugin (modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {

        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.3.0";
        public static bool SuitsLoaded;

        //Bundle Paths
        string LevelBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custommoon");
        string ItemBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customitems");
        //string MonsterBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customenemies");
        public static AssetBundle LevelAssetBundle;
        public static AssetBundle ItemAssetBundle;
        //public static AssetBundle MonsterAssetBundle;

        private readonly Harmony WTOHarmony = new Harmony (modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;
        
        private static List<SpawnableItemWithRarity> MoonScrap;

        public static void LogToConsole(string text) {
            text = "=======" + text + "=======";
            Debug.Log (text);
        }

        public static void AddToScrapList(SpawnableItemWithRarity Item) { 
            MoonScrap.AddItem(Item);
            WTOBase.LogToConsole(Item.ToString());
        }
        public static List<SpawnableItemWithRarity> GetScrapList() { return MoonScrap; }
        void Awake() {
            //Load up various things and tell the console we've loaded
            if (Instance == null) {
                Instance = this;
            }
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource (modGUID);
            WTOLogSource.LogInfo("Welcome to Ooblterra!");

            WTOHarmony.PatchAll(typeof(WTOBase));
            WTOHarmony.PatchAll(typeof(ItemPatch));
            WTOHarmony.PatchAll(typeof(MoonPatch));
            WTOHarmony.PatchAll(typeof(SuitPatch));

            //Loads the assetbundle and tells us everything in it
            LevelAssetBundle = AssetBundle.LoadFromFile (LevelBundlePath);
            ItemAssetBundle = AssetBundle.LoadFromFile(ItemBundlePath);
            //MonsterItemBundle = AssetBundle.LoadFromFile(MonsterBundlePath);

            LogToConsole("BEGIN PRINTING LOADED ASSETS");
            foreach (string AssetNameToPrint in LevelAssetBundle.GetAllAssetNames()) {
                Debug.Log("Asset in Level bundle: " + AssetNameToPrint);
            }
            foreach (string AssetNameToPrint in ItemAssetBundle.GetAllAssetNames()) {
                Debug.Log("Asset in Item bundle: " + AssetNameToPrint);
            }
            /*
            foreach (string AssetNameToPrint in MonsterAssetBundle.GetAllAssetNames()) {
                Debug.Log("Asset in bundle: " + AssetNameToPrint);
            }
            */
            LogToConsole("END PRINTING LOADED ASSETS");
            ItemPatch.AddCustomItems();
            SuitsLoaded = false;
        }
    }
}
