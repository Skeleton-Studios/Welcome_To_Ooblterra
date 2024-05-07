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
using Welcome_To_Ooblterra.Things;
using Welcome_To_Ooblterra.Security;
using LethalLevelLoader;

namespace Welcome_To_Ooblterra.Properties;

/* S/O to Minx, whose tutorial provided a great starting point.
    * Wouldn'tve had any of this code without it, thank you!
    */

[BepInPlugin(modGUID, modName, modVersion)]
public class WTOBase : BaseUnityPlugin {

    public static ConfigFile ConfigFile;
    static Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    private const string modGUID = "SkullCrusher.WTO";
    private const string modName = "Welcome To Ooblterra";
    private const string modVersion = "1.0";

    private readonly Harmony WTOHarmony = new Harmony(modGUID);
    public static ManualLogSource WTOLogSource;
    public static WTOBase Instance;

    public static AssetBundle LevelAssetBundle;
    public static AssetBundle ItemAssetBundle;
    public static AssetBundle FactoryAssetBundle;
    public static AssetBundle MonsterAssetBundle;
    public const string RootPath = "Assets/Resources/WelcomeToOoblterra/";

    //public static bool LightsOn = true;
    //public static bool SwitchState = true;
    //public static GameObject LabPrefab;
    //public static FrankensteinTerminal LabTerminal;
    //public static bool DoInteractCheck = false;
    //public static int InteractNumber = 0;

    [HarmonyPatch(typeof(StartOfRound), "Update")]
    [HarmonyPostfix]
    public static void DebugHelper(StartOfRound __instance) {
        if (Keyboard.current.f8Key.wasPressedThisFrame) {
            WTOBase.LogToConsole("BEGIN PRINT TERMINAL LOGS");
            for (int i = 0; i < FindObjectOfType<Terminal>().logEntryFiles.Count; i++) {
                TerminalNode NextNode = FindObjectOfType<Terminal>().logEntryFiles[i];
                WTOBase.LogToConsole($"LOG ID: {i} ", false);
                WTOBase.LogToConsole($"LOG NAME: {NextNode.name} ", false);
                WTOBase.LogToConsole($"LOG CONTENTS: {NextNode.displayText}", false);
            }
            WTOBase.LogToConsole("END PRINT TERMINAL LOGS");
        }
        if (Keyboard.current.f9Key.wasPressedThisFrame) {
            WTOBase.LogToConsole($"BEGIN PRINT KEYWORD LIST");
            foreach (TerminalKeyword NextKeyword in FindObjectOfType<Terminal>().terminalNodes.allKeywords) {
                WTOBase.LogToConsole($"keyword NAME: {NextKeyword.name} WORD: {NextKeyword.word} verb: {NextKeyword.defaultVerb}");
            }
        }
    }

    void Awake() {

        //Load up various things and tell the console we've loaded
        if (Instance == null) {
            Instance = this;
        }

        WTOLogSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        WTOLogSource.LogInfo($"Welcome to Ooblterra! VERSION {version}");

        WTOHarmony.PatchAll(typeof(WTOBase));
        WTOHarmony.PatchAll(typeof(FactoryPatch));
        WTOHarmony.PatchAll(typeof(ItemPatch));
        WTOHarmony.PatchAll(typeof(MonsterPatch));
        WTOHarmony.PatchAll(typeof(MoonPatch));
        WTOHarmony.PatchAll(typeof(SuitPatch));
        WTOHarmony.PatchAll(typeof(TerminalPatch));
            
        LogToConsole("BEGIN PRINTING LOADED ASSETS");
        string FactoryBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customdungeon");             
        FactoryAssetBundle = AssetBundle.LoadFromFile(FactoryBundlePath);
        string ItemBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customitems");
        ItemAssetBundle = AssetBundle.LoadFromFile(ItemBundlePath);
        string LevelBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "custommoon");
        LevelAssetBundle = AssetBundle.LoadFromFile(LevelBundlePath);
        string MonsterBundlePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "customenemies");
        MonsterAssetBundle = AssetBundle.LoadFromFile(MonsterBundlePath);

        FactoryPatch.Start();
        ItemPatch.Start();
        MonsterPatch.Start();
        MoonPatch.Start();

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
    public static T ContextualLoadAsset<T>(AssetBundle Bundle, string PathToAsset) where T : UnityEngine.Object {
        if (Application.platform == RuntimePlatform.WindowsEditor) {
            string PathMinusFileType = PathToAsset.Substring(17);
            PathMinusFileType = PathMinusFileType.Substring(0, PathMinusFileType.LastIndexOf("."));
            LogToConsole($"Loading {PathMinusFileType} from resources folder...");
            return Resources.Load<T>(PathMinusFileType);
        } else {
            //Some postprocessing on this text to make the readout a little cleaner
            int LengthOfAssetName = PathToAsset.Length - PathToAsset.LastIndexOf("/");
            string CleanAssetName = PathToAsset.Substring(PathToAsset.LastIndexOf("/"), LengthOfAssetName);
            LogToConsole($"Loading {CleanAssetName} from {Bundle.name}...");
            return Bundle.LoadAsset<T>(PathToAsset);
        }
    }
    public static void LogToConsole(string text, bool AddFlair = true) {
        if (AddFlair) { 
            text = "=======" + text + "=======";
        }
        WTOLogSource.LogMessage(text);
    }
    public enum AllowedState {
        Off = 0,
        CustomLevelOnly = 1,
        AllLevels = 2
    }
}

