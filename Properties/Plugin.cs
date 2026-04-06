using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLevelLoader;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;

namespace Welcome_To_Ooblterra.Properties
{
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

    public enum LogType
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public enum LogSourceType
    {
        Generic,
        Enemy,
        Item,
        Thing,
        Room
    }

    public enum StartupType
    {
        /// <summary>
        /// Normal mod startup
        /// </summary>
        Normal,

        /// <summary>
        /// Render the contour map and then exit the application
        /// </summary>
        ContourMapRender
    }

    [BepInPlugin(modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {

        public class WTOLogger(System.Type Type, LogSourceType SourceType = LogSourceType.Generic)
        {
            public readonly System.Type Type = Type;
            public readonly LogSourceType SourceType = SourceType;

            public void Debug(string text, bool AddFlair = true, bool ForcePrint = false)
            {
                LogToConsole(Type, SourceType, LogType.Debug, text, AddFlair, ForcePrint);
            }

            public void Info(string text, bool AddFlair = true, bool ForcePrint = false)
            {
                LogToConsole(Type, SourceType, LogType.Info, text, AddFlair, ForcePrint);
            }

            public void Warning(string text, bool AddFlair = true, bool ForcePrint = false)
            {
                LogToConsole(Type, SourceType, LogType.Warning, text, AddFlair, ForcePrint);
            }

            public void Error(string text, bool AddFlair = true, bool ForcePrint = false)
            {
                LogToConsole(Type, SourceType, LogType.Error, text, AddFlair, ForcePrint);
            }
        }
        
        private class AssetBundleLoadException(string message) : System.Exception(message)
        {
        }

        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "2.1.0";

        private readonly Harmony WTOHarmony = new(modGUID);
        public static ManualLogSource WTOLogSource;
        public static WTOBase Instance;

        private static AssetBundle ContentAssetBundle;
        private static readonly string[] RootPaths = [
            "Assets/Resources/WelcomeToOoblterra/",
            "Assets/WelcomeToOoblterra/",
            "WelcomeToOoblterra/",
            ""
        ];
        private static string resolvedRootPath = "";

        public static ConfigEntry<bool> WTODebug;
        public static ConfigEntry<bool> WTOCustomSuits;
        public static ConfigEntry<bool> WTOCustomPoster;
        public static ConfigEntry<bool> WTOScalePrice;
        public static ConfigEntry<string> WTOHazardList;
        public static ConfigEntry<string> WTOHazardMoonList;
        public static ConfigEntry<int> WTOFootsteps;
        public static ConfigEntry<int> WTOMusic;
        public static ConfigEntry<TiedToLabEnum> WTOForceHazards;
        public static ConfigEntry<TiedToLabEnum> WTOForceInsideMonsters;
        public static ConfigEntry<TiedToLabEnum> WTOForceOutsideMonsters;
        public static ConfigEntry<TiedToLabEnum> WTOForceDaytimeMonsters;
        public static ConfigEntry<TiedToLabEnum> WTOForceScrap;
        public static ConfigEntry<bool> WTOForceOutsideOnly;
        public static ConfigEntry<int> WTOWeightScale;

        public static ConfigEntry<bool> WTOLogging_Debug;
        public static ConfigEntry<bool> WTOLogging_Info;
        public static ConfigEntry<bool> WTOLogging_Warning;
        public static ConfigEntry<bool> WTOLogging_Error;
        public static ConfigEntry<string> WTOLogging_Filter;

        public static ConfigEntry<bool> WTOTestRoom;
        public static ConfigEntry<StartupType> WTOStartupType;
        public static ConfigEntry<bool> WTOAutoRoute;

        public static ConfigEntry<string> WTOContourMapWritePath;

        private static readonly WTOLogger Log = new (typeof(WTOBase));

        void Awake()
        {
            /*CONFIG STUFF*/
            {
                WTODebug = Config.Bind("1. Debugging", "Print Debug Strings", false, "Whether or not to write WTO's debug print-strings to the log."); //IMPLEMENTED
                WTOLogging_Debug = Config.Bind("1. Debugging", "Log Level Debug Messages", false, "Whether or not to write debug messages to the log. These are the lowest, most spammy logs.");
                WTOLogging_Info = Config.Bind("1. Debugging", "Log Level Info Messages", true, "Whether or not to write info messages to the log. General info logs that shouldn't be printed too often.");
                WTOLogging_Warning = Config.Bind("1. Debugging", "Log Level Warning Messages", true, "Whether or not to write warning messages to the log. Warnings that are potentially errors or misconfigurations.");
                WTOLogging_Error = Config.Bind("1. Debugging", "Log Level Error Messages", true, "Whether or not to write error messages to the log. Errors that represent genuine problems that should not be happening.");
                WTOLogging_Filter = Config.Bind("1. Debugging", "Log Filter", "WTO", "The filter to apply to the log. Only messages that match this RegEx filter will be printed. If empty, all messages will be printed. This is applied to the final log string, including class name.");

                WTOFootsteps = Config.Bind("2. Accessibility", "Footstep Sounds", 100, "Adjust the volume of 523 Ooblterra's custom footstep sound. Binds between 0 and 100."); //IMPLEMENTED 
                WTOMusic = Config.Bind("2. Accessibility", "Music Volume", 100, "Adjust the volume of 523-Ooblterra's custom Time-Of-Day music. Binds between 0 and 100.");

                WTOCustomSuits = Config.Bind("3. Ship Stuff", "Custom Suit Status", true, "Whether or not to add WTO's custom suits."); //IMPLEMENTED
                WTOCustomPoster = Config.Bind("3. Ship Stuff", "Visit Ooblterra Poster Status", true, "Whether or not to add WTO's custom poster."); //IMPLEMENTED

                WTOHazardList = Config.Bind("4. Map Hazards", "Custom Hazard List", "SpikeTrap, TeslaCoil, BabyLurkerEgg, BearTrap", "A list of all of WTO's custom hazards to enable. Affects 523-Ooblterra, and also has influence on the settings below."); //IMPLEMENTED
                /*WTOForceOutsideOnly = Config.Bind("5. Modded Content", "Force Configuration settings on 523 Ooblterra", true, "When true, forces 523 Ooblterra to spawn only the enemies/scrap found in its LLL config settings. This prevents custom monsters/scrap from spawning on the moon, unless manually specified.");*/
                //WTOScalePrice = Config.Bind("5. Scrap", "Scale Scrap By Route Price", false, "Changes the value of Ooblterra's scrap to fit relative to the route price set for 523 Ooblterra. Only affects Ooblterra's custom scrap."); //IMPLEMENTED
                WTOForceHazards = Config.Bind("5. Modpack Controls", "Bind WTO Hazards to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own hazards, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
                WTOForceInsideMonsters = Config.Bind("5. Modpack Controls", "Bind WTO Inside Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own inside enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
                WTOForceOutsideMonsters = Config.Bind("5. Modpack Controls", "Bind WTO Outside Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with 523 Ooblterra's outside enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
                WTOForceDaytimeMonsters = Config.Bind("5. Modpack Controls", "Bind WTO Daytime Enemies to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with 523 Ooblterra's daytime enemies, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
                WTOForceScrap = Config.Bind("5. Modpack Controls", "Bind WTO Scrap to Oobl Lab", TiedToLabEnum.WTOOnly, "Whether the Oobl Lab should always spawn with its own scrap, regardless of moon. See the wiki on Thunderstore for more information."); //IMPLEMENTED
                WTOWeightScale = Config.Bind("5. Modpack Controls", "WTOAppend Weight Scale", 1, "For any setting configured to WTOAppend above, this setting multiplies that thing's weight before appending it to list."); //IMPLEMENTED

                WTOTestRoom = Config.Bind("6. Testing", "Enable Test Room Teleporter", false, "Whether or not to enable the test room teleporter in Ooblterra. This spawns a teleporter outside of the ship and spawns the test room.");
                WTOStartupType = Config.Bind("6. Testing", "Startup Type", StartupType.Normal, "The type of startup to use for the game. Leave this set to Normal. The other modes are for internal use only (e.g. for automated tools)");
                WTOAutoRoute = Config.Bind("6. Testing", "Auto Route", false, "When true, automatically routes to Ooblterra on game start.");

                WTOContourMapWritePath = Config.Bind("7. Contour Map Render", "Contour Map Write Path", "contourmap.exr", "The file path to write the contour map file to when Startup Type is set to ContourMapRender");
            }

            //Load up various things and tell the console we've loaded
            Instance ??= this;

            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            FixLLLLoadingFreeze();

            try
            {
                LoadWelcomeToOoblterra();
            }
            catch (System.Exception ex)
            {
                Log.Error($"Failed to load Welcome To Ooblterra. If you have the time, please report this error on our GitHub repository https://github.com/Skeleton-Studios/Welcome_To_Ooblterra/issues : {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a loading freeze fix to Lethal Level Loader.
        /// This currently happens because LLL caches all scene bundles after the first run, and
        /// then will not attempt to load them on startup from then on.
        /// This causes LLL to never get past the loading phase and simply sit stuck on the Online / LAN screen.
        /// 
        /// There are some issues open on LLL's repository about this:
        /// https://github.com/IAmBatby/LethalLevelLoader/issues/177
        /// https://github.com/IAmBatby/LethalLevelLoader/issues/175
        /// </summary>
        private void FixLLLLoadingFreeze()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(LethalLevelLoader.AssetBundleLoader));

            System.Type AssetBundleLoaderType = assembly.GetType("LethalLevelLoader.AssetBundles.AssetBundleLoader");
            if (AssetBundleLoaderType == null)
            {
                Log.Warning("Could not find LethalLevelLoader.AssetBundles.AssetBundleLoader type");
                return;
            }

            PropertyInfo AllowLoadingProperty = AssetBundleLoaderType.GetProperty("AllowLoading", BindingFlags.Static | BindingFlags.NonPublic);
            if(AllowLoadingProperty == null)
            {
                Log.Warning("Could not find AllowLoading field in AssetBundleLoader");
                return;
            }

            bool AllowLoading = (bool)AllowLoadingProperty.GetValue(null);

            if(AllowLoading)
            {
                // This can't be in the errored state, so we don't need to do anything.
                Log.Info("AllowLoading is already true, so AssetBundleLoader is not in a stuck state. No need to apply fix.");
                return;
            }

            // Next, get the "Instance" field value, which is an instance of the inner AssetBundleLoader class, 
            // and get Instance.AssetBundleInfos List

            PropertyInfo InstanceProperty = AssetBundleLoaderType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if(InstanceProperty == null)
            {
                Log.Warning("Could not find Instance field in AssetBundleLoader");
                return;
            }

            object AssetBundleLoaderInstance = InstanceProperty.GetValue(null);

            FieldInfo AssetBundleInfosField = AssetBundleLoaderType.GetField("AssetBundleInfos", BindingFlags.Instance | BindingFlags.NonPublic);

            if(AssetBundleInfosField == null)
            {
                Log.Warning("Could not find AssetBundleInfos field in AssetBundleLoader");
                return;
            }
            
            // This type is luckily public
            List<LethalLevelLoader.AssetBundles.AssetBundleInfo> AssetBundleInfos = (List<LethalLevelLoader.AssetBundles.AssetBundleInfo>)AssetBundleInfosField.GetValue(AssetBundleLoaderInstance);

            if(AssetBundleInfos.Count == 0)
            {
                // If there's none, then im not sure how we're in this state.
                Log.Info("AssetBundleInfos list is empty, so AssetBundleLoader is not in a stuck state. No need to apply fix.");
                return;
            }

            // Next, get the internal static knownSceneBundles dictionary
            FieldInfo fieldInfo = AssetBundleLoaderType.GetField("knownSceneBundles", BindingFlags.Static | BindingFlags.NonPublic);

            if (fieldInfo == null)
            {
                Log.Warning("Could not find knownSceneBundles field in AssetBundleLoader");
                return;
            }
    
            Dictionary<string, LethalLevelLoader.AssetBundles.LethalBundleManifest> knownSceneBundles = (Dictionary<string, LethalLevelLoader.AssetBundles.LethalBundleManifest>)fieldInfo.GetValue(null);

            bool allSceneBundlesAreKnownAndSkipped = true;
            foreach (var info in AssetBundleInfos)
            {
                if (!knownSceneBundles.TryGetValue(info.AssetBundleFileName, out var bundleManifest))
                {
                    // Does not exist in knownSceneBundles, so this will hit TryLoadBundle path which is OK
                    allSceneBundlesAreKnownAndSkipped = false;
                    break;
                }

                if(bundleManifest.timestamp != File.GetLastWriteTime(info.AssetBundleFilePath).Ticks)
                {
                    // This will trigger TryLoadBundle path which is OK.
                    allSceneBundlesAreKnownAndSkipped = false;
                    break;
                }
            }

            if(!allSceneBundlesAreKnownAndSkipped)
            {
                // TryLoadBundle will have been called
                Log.Info("Not all scene bundles are known and skipped, so AssetBundleLoader is not in a stuck state. No need to apply fix.");
                return;
            }

            // Now, check if all bundles have hasInitialized private member set to true,
            // And also check the activeLoadRequest private member is not null
            FieldInfo hasInitializedField = typeof(LethalLevelLoader.AssetBundles.AssetBundleInfo).GetField("hasInitialized", BindingFlags.Instance | BindingFlags.NonPublic);
            if (hasInitializedField == null) {
                Log.Warning("Could not find hasInitialized field in AssetBundleInfo");
                return;
            }

            FieldInfo activeLoadRequestField = typeof(LethalLevelLoader.AssetBundles.AssetBundleInfo).GetField("activeLoadRequest", BindingFlags.Instance | BindingFlags.NonPublic);
            if (activeLoadRequestField == null) {
                Log.Warning("Could not find activeLoadRequest field in AssetBundleInfo");
                return;
            }

            foreach (var info in AssetBundleInfos) {
                bool hasInitialized = (bool)hasInitializedField.GetValue(info);
                object activeLoadRequest = activeLoadRequestField.GetValue(info);
                if (!hasInitialized || activeLoadRequest != null) {
                    // Not sure what to make of this really - let's presume it's fine
                    return;
                }
            }

            FieldInfo requestedBundleCountField = AssetBundleLoaderType.GetField("requestedBundleCount", BindingFlags.Static | BindingFlags.NonPublic);
            if (requestedBundleCountField == null) {
                Log.Warning("Could not find requestedBundleCount field in AssetBundleLoader");
                return;
            }

            FieldInfo processedBundleCount = AssetBundleLoaderType.GetField("processedBundleCount", BindingFlags.Static | BindingFlags.NonPublic);
            if (processedBundleCount == null) {
                Log.Warning("Could not find processedBundleCount field in AssetBundleLoader");
                return;
            }

            Log.Info("AssetBundleLoader appears to be in a stuck state where all bundles are being skipped due to being known, but they have not been marked as initialized. Attempting to fix by loading bundles directly.");

            requestedBundleCountField.SetValue(AssetBundleLoaderInstance, AssetBundleInfos.Count);
            processedBundleCount.SetValue(AssetBundleLoaderInstance, 0);

            foreach(var info in AssetBundleInfos) {
                info.TryLoadBundle();
            }
        }

        /// <summary>
        /// Primary entrypoint to load the WelcomeToOoblterra mod.
        /// If this runs without throwing, the mod should be loaded successfully.
        /// </summary>
        private void LoadWelcomeToOoblterra()
        {
            Log.Info("Loading - Start!");

            WTOHarmony.PatchAll(typeof(WTOBase));
            WTOHarmony.PatchAll(typeof(FactoryPatch));
            WTOHarmony.PatchAll(typeof(ItemPatch));
            WTOHarmony.PatchAll(typeof(MonsterPatch));
            WTOHarmony.PatchAll(typeof(MoonPatch));
            WTOHarmony.PatchAll(typeof(SuitPatch));
            WTOHarmony.PatchAll(typeof(TerminalPatch));

            Log.Info("Harmony Patches applied! Loading asset bundle...");

            LoadAssetBundle();

            Log.Info("Asset bundle loaded! Reading assets and registering objects...");

            MoonPatch.Start();
            FactoryPatch.Start();
            ItemPatch.Start();
            MonsterPatch.Start();

            Log.Info("Assets read successfully! Applying netcode runtime init...");

            //NetcodeWeaver stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            Log.Info("Runtime init applied! Welcome To Ooblterra successfully loaded");
        }

        /// <summary>
        /// Loads the main asset bundle for WTO.
        /// Since v73, the asset bundle is now just one "content" bundle
        /// instead of several other bundles.
        /// </summary>
        /// <exception cref="FileNotFoundException">If the asset bundle can't be found</exception>
        /// <exception cref="AssetBundleLoadException">If the asset bundle fails to load for some reason</exception>
        private void LoadAssetBundle()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fullPath = Path.Combine(baseDir, "content");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Asset bundle not found at path: {fullPath}");
            }
            ContentAssetBundle = AssetBundle.LoadFromFile(fullPath);

            if (!ContentAssetBundle)
            {
                throw new AssetBundleLoadException($"Failed to load asset bundle from path: {fullPath}");
            }
        }

        private static string ResolveRootPath(string pathToAsset)
        {
            // Need to identify what the root path was when this asset bundle was build. It can change depending on where
            // the files were in unity. First, try some known ones, then do a brute force check.
            foreach (string rootPath in RootPaths) {
                var fullPath = Path.Join(rootPath, pathToAsset).ToLower();
                if (ContentAssetBundle.Contains(fullPath)) {
                    Log.Info($"Resolved asset bundle root path: {rootPath}");
                    return rootPath;
                }
            }

            // Failed to find using known root paths, so do a brute force check 
            // of all asset names in the bundle to see if we can find the target asset and work backwards from there.
            foreach (var name in ContentAssetBundle.GetAllAssetNames()) {
                var lowerName = name.ToLower();
                if (lowerName.EndsWith(pathToAsset.ToLower())) {
                    return name[..^pathToAsset.Length];
                }
            }

            Log.Error($"Could not resolve asset bundle root path using target {pathToAsset}. This will most likely cause asset loading to fail.");
            return "";
        }

        /// <summary>
        /// Loads an asset from the asset bundle, or from disk if in the editor.
        /// In practice, you probably wont be loading this from the unity editor...
        /// </summary>
        /// <typeparam name="T">The asset type to load, derived from UnityEngine.Object</typeparam>
        /// <param name="PathToAsset">The path to the asset within the asset bundle</param>
        /// <param name="LogLoading">Whether to log the loading process</param>
        /// <returns>The loaded asset of type T</returns>
        /// <exception cref="AssetBundleLoadException">Thrown if the asset bundle is not loaded yet, or if the asset fails to load (most likely because it was missing or the wrong type)</exception>
        public static T ContextualLoadAsset <T>(string PathToAsset, bool LogLoading = true) where T : UnityEngine.Object {
            if (Application.platform == RuntimePlatform.WindowsEditor) {
                string PathMinusFileType = PathToAsset.Substring(17);
                PathMinusFileType = PathMinusFileType.Substring(0, PathMinusFileType.LastIndexOf("."));
                if (LogLoading) {
                    Log.Info($"Loading {PathMinusFileType} from resources folder...", ForcePrint: true);
                }
                return Resources.Load<T>(PathMinusFileType);
            } else {
                if(LogLoading) {
                    Log.Info($"Loading {PathToAsset}...", ForcePrint:true);
                }

                if(!ContentAssetBundle)
                {
                    throw new AssetBundleLoadException($"Cannot load {PathToAsset} because ContentAssetBundle is not loaded yet.");
                }

                if(resolvedRootPath == "")
                {
                    resolvedRootPath = ResolveRootPath(PathToAsset);
                }

                PathToAsset = Path.Join(resolvedRootPath, PathToAsset);

                return ContentAssetBundle.LoadAsset<T>(PathToAsset) ?? throw new AssetBundleLoadException($"Failed to load asset at path {PathToAsset}. Asset was null after loading.");
            }
        }

        /// <summary>
        /// Central logging facility that prints logs to the BepInEx logger.
        /// </summary>
        /// <param name="LogClassType">The type of the class that is logging the message</param>
        /// <param name="SourceType">The source type of the log message</param>
        /// <param name="logType">The type of log message (Debug, Info, Warning, Error)</param>
        /// <param name="text">The log message text</param>
        /// <param name="AddFlair">Adds some ====== to the log message</param>
        /// <param name="ForcePrint">Whether to force print the log message regardless of settings. It could be set to off in mod settings but this bypasses that</param>
        private static void LogToConsole(System.Type LogClassType, LogSourceType SourceType, LogType logType, string text, bool AddFlair = true, bool ForcePrint = false) {
            // Handle enable / disable of specific log types, in addition to force print and debug mode
            if (!ForcePrint)
            {
                bool enabled = logType switch
                {
                    LogType.Debug => WTOLogging_Debug.Value,
                    LogType.Info => WTOLogging_Info.Value,
                    LogType.Warning => WTOLogging_Warning.Value,
                    LogType.Error => WTOLogging_Error.Value,
                    _ => true
                };

                if (!WTODebug.Value || !enabled)
                {
                    return;
                }
            }

            // TODO: Use SourceType to filter logs if necessary.

            text = $"[{LogClassType.Name}]: {text}";

            if (AddFlair) { 
                text = $"======={text}=======";
            }

            // Log as appropriate type for debug, info, warning etc.
            switch(logType)
            {
                case LogType.Debug: WTOLogSource.LogDebug(text); break;
                case LogType.Info: WTOLogSource.LogInfo(text); break;
                case LogType.Warning: WTOLogSource.LogWarning(text); break;
                case LogType.Error: WTOLogSource.LogError(text); break;
                default: WTOLogSource.LogMessage(text); break;
            }
        }

        public static List<string> CSVSeperatedStringList(string InputString) {
            return new(InputString.Replace(" ", "").ToLower().Split(','));
        }

        public static ClientRpcSendParams AllClientsButSender(ServerRpcParams rpcParams)
        {
            return new ClientRpcSendParams
            {
                TargetClientIds = [.. from id in NetworkManager.Singleton.ConnectedClientsIds where id != rpcParams.Receive.SenderClientId select id]
            };
        }

        private static void PrintReferences(IEnumerable<Object> objects)
        {
            string HierarchyString(GameObject x)
            {
                string s = x.name;
                while (x.transform.parent != null)
                {
                    x = x.transform.parent.gameObject;
                    s = x.name + "/" + s;
                }
                return s;
            }

            void PrintReference(Object o)
            {
                if (o == null)
                {
                    return;
                }

                if (o is GameObject)
                {
                    GameObject g = (GameObject)o;
                    foreach (Component c in g.GetComponents(typeof(Component)))
                    {
                        PrintReference(c);
                    }
                    for (int i = 0; i < g.transform.childCount; i++)
                    {
                        PrintReference(g.transform.GetChild(i).gameObject);
                    }
                }
                else if (o is MeshFilter)
                {
                    MeshFilter f = (MeshFilter)o;
                    if (f.sharedMesh == null)
                    {
                        return;
                    }
                    var str = HierarchyString(f.gameObject);
                    Log.Info(str + ": " + f.sharedMesh.name);

                }
                else if (o is MeshRenderer)
                {
                    MeshRenderer meshRenderer = (MeshRenderer)o;
                    var str = HierarchyString(meshRenderer.gameObject);
                    for (int i = 0; i < meshRenderer.materials.Length; i++)
                    {
                        Log.Info(str + " - " + i.ToString() + ": " + meshRenderer.materials[i].name);
                    }
                }
                else if (o is AudioSource)
                {
                    AudioSource audioSource = (AudioSource)o;
                    var str = HierarchyString(audioSource.gameObject);
                    if (audioSource.clip != null)
                    {
                        Log.Info(str + ": " + audioSource.clip.name);
                    }
                    if (audioSource.outputAudioMixerGroup != null)
                    {
                        Log.Info(str + ": " + audioSource.outputAudioMixerGroup.name);
                    }
                }
                else
                {
                    var str = o.name;
                    Log.Info(str + ": " + o.GetType().ToString());
                }
            }

            foreach (Object o in objects)
            {
                PrintReference(o);
            }
        }
    }

}
