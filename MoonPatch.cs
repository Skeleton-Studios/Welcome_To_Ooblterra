using HarmonyLib;
using System;
using WonderAPI;
using UnityEngine;



namespace Welcome_To_Ooblterra.Patches {

    /* First two methods are modified from Bizzlemip's LC_Construct mod.
     * GET PERMISSION FOR THIS JAMES
     */

    internal class MoonPatch {

        //Identifiers for the Moon
        private static SelectableLevel MyNewMoon;
        private const string LevelAssetPath = "Assets/CustomScene/customlevel.prefab";

        private const string MoonFriendlyName = "Ooblterra";
        private const string MoonDescription = "POPULATION: Entirely wiped out.\nCONDITIONS: Hazardous. Deadly air, mostly clear skies. Always nighttime. The ground is alive.\nFAUNA: The unique conditions of Ooblterra allow for lots of strange beings.";
        private const string MoonRiskLevel = "A";
        private const string MoonConfirmation = "Are you certain you want to go to Ooblterra?\n\nPlease CONFIRM or DENY.\n\n";
        private const string MoonTravelText = "Routing to Ooblterra... Please be patient, travel to new planets may take a while.";

        private static GameObject SunObject;
        private static GameObject SunAnimObject;
        private static GameObject IndirectLight;

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AddMoonToList(StartOfRound __instance) {
            SelectableLevel vow = __instance.GetComponent<StartOfRound>().levels[2];
            //Create new moon based on vow
            MyNewMoon = vow;

            //override relevant properties
            MyNewMoon.PlanetName = MoonFriendlyName;
            MyNewMoon.name = MoonFriendlyName;
            MyNewMoon.LevelDescription = MoonDescription;
            MyNewMoon.riskLevel = MoonRiskLevel;
            MyNewMoon.timeToArrive = 3f;
            MyNewMoon.planetHasTime = true;

            //__instance.levels.Resize(ref __instance.levels, __instance.levels.Length + 1);
            //AddToArray(MyNewMoon);

            //Add moon to API
            Core.AddMoon(MyNewMoon);
            return true;
        }

        //Add the custom moon to the terminal
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AddMoonToTerminal(StartOfRound __instance) {

            GameObject TerminalScriptRef = GameObject.Find("TerminalScript");
            Terminal ActiveTerminal = TerminalScriptRef.GetComponent<Terminal>();
            TerminalKeyword TerminalEntry = new TerminalKeyword();
            TerminalEntry.name = MoonFriendlyName;
            TerminalEntry.word = MoonFriendlyName.ToLower();
            int MoonListLength = ActiveTerminal.moonsCatalogueList.Length;
            Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, MoonListLength + 1);
            ActiveTerminal.moonsCatalogueList[MoonListLength] = Core.GetMoons()[MoonFriendlyName];
            Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry;
            TerminalNode terminalNode = new TerminalNode();
            terminalNode.name = MoonFriendlyName;
            terminalNode.itemCost = 0;
            terminalNode.displayText = MoonTravelText;
            terminalNode.buyRerouteToMoon = Core.ModdedIds[MoonFriendlyName];
            terminalNode.clearPreviousText = true;
            terminalNode.buyUnlockable = false;
            CompatibleNoun compatibleNoun = new CompatibleNoun();
            compatibleNoun.noun = ActiveTerminal.terminalNodes.allKeywords[3];
            compatibleNoun.result = terminalNode;
            TerminalNode terminalNode2 = new TerminalNode();
            terminalNode2.name = MoonFriendlyName;
            terminalNode2.buyItemIndex = -1;
            terminalNode2.clearPreviousText = true;
            terminalNode2.buyUnlockable = false;
            terminalNode2.displayText = MoonConfirmation;
            terminalNode2.isConfirmationNode = false;
            terminalNode2.itemCost = 0;
            terminalNode2.overrideOptions = true;
            terminalNode2.maxCharactersToType = 15;
            terminalNode2.terminalOptions = new CompatibleNoun[]
            {
                compatibleNoun,
                ActiveTerminal.terminalNodes.allKeywords[26].compatibleNouns[0].result.terminalOptions[1]
            };
            CompatibleNoun compatibleNoun2 = new CompatibleNoun();
            compatibleNoun2.noun = TerminalEntry;
            compatibleNoun2.result = terminalNode2;
            TerminalEntry.defaultVerb = ActiveTerminal.terminalNodes.allKeywords[26];
            Array.Resize<CompatibleNoun>(ref ActiveTerminal.terminalNodes.allKeywords[26].compatibleNouns, ActiveTerminal.terminalNodes.allKeywords[26].compatibleNouns.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[26].compatibleNouns[ActiveTerminal.terminalNodes.allKeywords[26].compatibleNouns.Length - 1] = compatibleNoun2;
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry;
        }

        /* This by all accounts should work.
            * I suspect that the terminal doesn't like being prefixed for some reason. 
            * I'll ask about it later.
         
        [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        private static bool RandomWeatherForCustomMoon(Terminal __instance) {
            int MoonListLength = __instance.moonsCatalogueList.Length;
            //List of weather types we want to choose from 
            Dictionary<int, LevelWeatherType> WeatherTypesToChoose = new Dictionary<int, LevelWeatherType>() { 
                { 0, LevelWeatherType.None }, { 1, LevelWeatherType.Rainy },
                { 2, LevelWeatherType.Stormy }, { 3, LevelWeatherType.Foggy },
                { 4, LevelWeatherType.Eclipsed }, { 5, LevelWeatherType.Flooded }
            };
            System.Random numbergen = new System.Random();
            //MyNewMoon.currentWeather = WeatherTypesToChoose[numbergen.Next(0, 6)]; 
            MyNewMoon.currentWeather = LevelWeatherType.None;
            __instance.moonsCatalogueList[MoonListLength] = Core.GetMoons()[MoonFriendlyName];
            return true;
        }
        */

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void CustomLevelInit(StartOfRound __instance) {
            if (__instance.currentLevel.name != MoonFriendlyName) {
                return;
            }
            WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);
            string[] ObjectsToDestroy = new string[] {
                "CompletedVowTerrain",
                "tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Local Volumetric Fog",
                "GroundFog",
                "Sky and Fog Global Volume",
                "SunTexture"
            };
            
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            
            foreach (GameObject ObjToDestroy in allObjects) {
                foreach (string UnwantedObjString in ObjectsToDestroy) {
                    //If the object has any of the names in the list, it's gotta go
                    if (ObjToDestroy.name.Contains(UnwantedObjString)){
                        GameObject.Destroy(ObjToDestroy);
                    }
                }
                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage")){ 
                    GameObject.Destroy(ObjToDestroy);
                }
            }

            SunObject = GameObject.Find("SunWithShadows");
            SunAnimObject = GameObject.Find("SunAnimContainer");
            IndirectLight = GameObject.Find("Indirect");

            //Load our custom prefab
            GameObject MyLevelAsset = WTOBase.MyAssets.LoadAsset(LevelAssetPath) as GameObject;
            GameObject MyInstantiatedLevel = GameObject.Instantiate(MyLevelAsset);
            WTOBase.LogToConsole("Loaded custom terrain object!");

            //The prefab contains an object called TeleportSnapLocation that we move the primary door to
            GameObject Entrance = GameObject.Find("EntranceTeleportA");
            GameObject SnapLoc = GameObject.Find("TeleportSnapLocation");
            Entrance.transform.position = SnapLoc.transform.position;
            GameObject FireExit = GameObject.Find("EntranceTeleportB");
            GameObject FireExitSnapLoc = GameObject.Find("FireExitSnapLocation");
            FireExit.transform.position = FireExitSnapLoc.transform.position;

            //Testing some sun stuff
            SunAnimObject.GetComponent<animatedSun>().directLight = GameObject.Find("OoblSun").GetComponent<Light>();
            SunAnimObject.GetComponent<animatedSun>().indirectLight = GameObject.Find("OoblIndirect").GetComponent<Light>();
            GameObject.Destroy(SunObject);
            GameObject.Destroy(IndirectLight); 
        }
    }
}

