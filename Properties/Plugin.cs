using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Welcome_To_Ooblterra.Patches;
using System.IO;
using System.Reflection;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Welcome_To_Ooblterra.Properties;

/* S/O to Minx, whose tutorial provided a great starting point.
    * Wouldn'tve had any of this code without it, thank you!
    */

[HideInInspector]
public enum SuitStatus {
    Enable,
    Purchase,
    Disable,
    SleepsSpecial
}

public enum PosterStatus {
    ReplaceVanilla,
    AddAsDecor,
    Disable
}

public enum FootstepEnum { 
    Enable,
    Quiet,
    Disable
}

public enum TiedToLabEnum {
    WTOOnly,
    AppendWTO,
    UseMoonDefault
}

[BepInPlugin(modGUID, modName, modVersion)]
public class WTOBase : BaseUnityPlugin {

    //static Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    private const string modGUID = "SkullCrusher.WTO";
    private const string modName = "Welcome To Ooblterra";
    private const string modVersion = "1.1.0";

    private readonly Harmony WTOHarmony = new Harmony(modGUID);
    public static ManualLogSource WTOLogSource;
    public static WTOBase Instance;

    public static AssetBundle LevelAssetBundle;
    public static AssetBundle ItemAssetBundle;
    public static AssetBundle FactoryAssetBundle;
    public static AssetBundle MonsterAssetBundle;
    public const string RootPath = "Assets/Resources/WelcomeToOoblterra/";

    
    public static ConfigEntry<bool> WTODebug;
    public static ConfigEntry<bool> WTOCustomSuits;
    public static ConfigEntry<bool> WTOCustomPoster;
    public static ConfigEntry<bool> WTOScalePrice;
    public static ConfigEntry<string> WTOHazardList;
    public static ConfigEntry<string> WTOHazardMoonList;
    public static ConfigEntry<int> WTOFootsteps;
    public static ConfigEntry<TiedToLabEnum> WTOForceHazards;
    public static ConfigEntry<TiedToLabEnum> WTOForceInsideMonsters;
    public static ConfigEntry<TiedToLabEnum> WTOForceOutsideMonsters;
    public static ConfigEntry<TiedToLabEnum> WTOForceDaytimeMonsters;
    public static ConfigEntry<TiedToLabEnum> WTOForceScrap;
    public static ConfigEntry<bool> WTOForceOutsideOnly;
    public static ConfigEntry<int> WTOWeightScale;

    [HarmonyPatch(typeof(StartOfRound), "Update")]
    [HarmonyPostfix]
    public static void DebugHelper(StartOfRound __instance) {
        if (Keyboard.current.f8Key.wasPressedThisFrame) {
            foreach(SpawnableMapObject Hazard in __instance.currentLevel.spawnableMapObjects) {
                LogToConsole($"{Hazard.prefabToSpawn.name}");
            }
            foreach (SpawnableOutsideObjectWithRarity OutsideObject in __instance.currentLevel.spawnableOutsideObjects) {
                LogToConsole($"{OutsideObject.spawnableObject.prefabToSpawn.name}");
            }
        }
        if (Keyboard.current.f9Key.wasPressedThisFrame) {
            StartOfRound.Instance.ChangeLevelServerRpc(13, FindObjectOfType<Terminal>().groupCredits);
        }
    }

    void Awake() {
        /*CONFIG STUFF*/{
            WTODebug = Config.Bind("1. Debugging", "Print Debug Strings", false, "Whether or not to write WTO's debug print-strings to the log."); //IMPLEMENTED
            WTOFootsteps = Config.Bind("2. Accessibility", "Footstep Sounds", 100, "Adjust the volume of 523 Ooblterra's custom footstep sound. Binds between 0 and 100."); //IMPLEMENTED
            WTOCustomSuits = Config.Bind("3. Ship Stuff", "Custom Suit Status", true, "Whether or not to add WTO's custom suits."); //IMPLEMENTED
            WTOCustomPoster = Config.Bind("3. Ship Stuff", "Visit Ooblterra Poster Status",  true, "Whether or not to add WTO's custom poster."); //IMPLEMENTED
            WTOHazardList = Config.Bind("4. Map Hazards", "Custom Hazard List", "SpikeTrap, TeslaCoil, BabyLurkerEgg, BearTrap", "A list of all of WTO's custom hazards to enable. Affects 523-Ooblterra, and also has influence on the settings below."); //IMPLEMENTED
            WTOForceOutsideOnly = Config.Bind("5. Modded Content", "Force Configuration settings on 523 Ooblterra", true, "When true, forces 523 Ooblterra to spawn only the enemies/scrap found in its LLL config settings. This prevents custom monsters/scrap from spawning on the moon, unless manually specified.");
            WTOScalePrice = Config.Bind("6. Scrap", "Scale Scrap By Route Price", false, "Changes the value of Ooblterra's scrap to fit relative to the route price set for 523 Ooblterra. Only affects Ooblterra's custom scrap."); //IMPLEMENTED
            WTOForceHazards = Config.Bind("7. Modpack Controls", "Bind WTO Hazards to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own hazards, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
            WTOForceInsideMonsters = Config.Bind("7. Modpack Controls", "Bind WTO Inside Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own inside enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
            WTOForceOutsideMonsters = Config.Bind("7. Modpack Controls", "Bind WTO Outside Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with 523 Ooblterra's outside enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
            WTOForceDaytimeMonsters = Config.Bind("7. Modpack Controls", "Bind WTO Daytime Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with 523 Ooblterra's daytime enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
            WTOForceScrap = Config.Bind("7. Modpack Controls", "Bind WTO Scrap to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own scrap, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
            WTOWeightScale = Config.Bind("7. Modpack Controls", "WTOAppend Weight Scale", 1, "For any setting configured to WTOAppend above, this setting multiplies that thing's weight before appending it to list."); //IMPLEMENTED


        }

        //Load up various things and tell the console we've loaded
        if (Instance == null) {
            Instance = this;
        }

        WTOLogSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);

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
    public static T ContextualLoadAsset<T>(AssetBundle Bundle, string PathToAsset, bool LogLoading = true) where T : UnityEngine.Object {
        if (Application.platform == RuntimePlatform.WindowsEditor) {
            string PathMinusFileType = PathToAsset.Substring(17);
            PathMinusFileType = PathMinusFileType.Substring(0, PathMinusFileType.LastIndexOf("."));
            if (LogLoading) { 
                LogToConsole($"Loading {PathMinusFileType} from resources folder...", ForcePrint: true);
            }
            return Resources.Load<T>(PathMinusFileType);
        } else {
            //Some postprocessing on this text to make the readout a little cleaner
            int LengthOfAssetName = PathToAsset.Length - PathToAsset.LastIndexOf("/");
            string CleanAssetName = PathToAsset.Substring(PathToAsset.LastIndexOf("/"), LengthOfAssetName);
            if(LogLoading) { 
                LogToConsole($"Loading {CleanAssetName} from {Bundle.name}...", ForcePrint:true);
            }
            return Bundle.LoadAsset<T>(PathToAsset);
        }
    }
    public static void LogToConsole(string text, bool AddFlair = true, bool ForcePrint = false) {
        if (AddFlair) { 
            text = "=======" + text + "=======";
        }
        if (WTODebug.Value || ForcePrint) {
            WTOLogSource.LogMessage(text);
        }
    }
    public static List<string> CSVSeperatedStringList(string InputString) {
        List<string> MyStringArray = new();
        InputString = InputString.Replace(" ", "");
        InputString = InputString.ToLower();
        MyStringArray = InputString.Split(',').ToList<string>();
        return MyStringArray;
    }
}

