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
    private const string modVersion = "0.8";

    private readonly Harmony WTOHarmony = new Harmony(modGUID);
    internal ManualLogSource WTOLogSource;
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
            /*
            SpikeTrap[] TeslaCoils = FindObjectsOfType<SpikeTrap>();
            foreach(SpikeTrap coil in TeslaCoils) {
                coil.RecieveToggleSpikes(SwitchState);
            }
            SwitchState = !SwitchState;
            
            ("Hotkey override triggered to start visuals...");
            LabTerminal = FindObjectOfType<FrankensteinTerminal>();
            LabTerminal.StartSceneServerRpc(100);

            
            SprayPaintItem[] SprayPaints = GameObject.FindObjectsOfType<SprayPaintItem>();
            foreach (SprayPaintItem sprayPaint in SprayPaints) {
                sprayPaint.debugSprayPaint = true;
            }

            
            LightsOn = !LightsOn;
            WTOBase.LogToConsole($"SETTING OUTDOOR LIGHTS TO: {(LightsOn ? "ON" : "OFF")}");
            //GameObject.Find("ActualSun").GetComponent<Light>().enabled = LightsOn;
            GameObject.Find("ActualIndirect").GetComponent<Light>().enabled = LightsOn;
            
            
            LabTerminal = FindObjectOfType<FrankensteinTerminal>();
            WTOBase.LogToConsole($"REVIVING PLAYER at {LabTerminal}");
            LabTerminal.ReviveDeadPlayerServerRpc();
            
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
        if (Keyboard.current.f9Key.wasPressedThisFrame) {
            StartOfRound.Instance.ChangeLevelServerRpc(9, 60);
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
    public static T LoadAsset<T>(AssetBundle Bundle, string PathToAsset) where T : UnityEngine.Object {
        if (Application.platform == RuntimePlatform.WindowsEditor) {
            string PathMinusFileType = PathToAsset.Substring(17);
            PathMinusFileType = PathMinusFileType.Substring(0, PathMinusFileType.LastIndexOf("."));
            Debug.Log($"Loading {PathMinusFileType} from resources folder...");
            return Resources.Load<T>(PathMinusFileType);
        } else {
            Debug.Log($"Loading {PathToAsset} from {Bundle}...");
            return Bundle.LoadAsset<T>(PathToAsset);
        }
    }
    public static void LogToConsole(string text) {
        text = "=======" + text + "=======";
        Debug.Log(text);
    }
    public enum AllowedState {
        Off = 0,
        CustomLevelOnly = 1,
        AllLevels = 2
    }
}

