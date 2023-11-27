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

        private static string MoonFriendlyName;
        private static GameObject SunObject;
        private static GameObject SunAnimObject;
        private static GameObject IndirectLight;
        private static AssetBundle LevelBundle = WTOBase.LevelAssetBundle;

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AddMoonToList(StartOfRound __instance) {
            //Create new moon based on vow
            MyNewMoon = LevelBundle.LoadAsset<SelectableLevel>("Assets/CustomScene/OoblterraLevel.asset"); ;
            MoonFriendlyName = MyNewMoon.PlanetName;

            MyNewMoon.spawnableScrap = __instance.levels[2].spawnableScrap;
            try {
                foreach (SpawnableItemWithRarity item in WTOBase.GetScrapList()) {
                    MyNewMoon.spawnableScrap.AddItem<SpawnableItemWithRarity>(item);
                }
                foreach (SpawnableItemWithRarity addeditem in MyNewMoon.spawnableScrap) {
                    WTOBase.LogToConsole(addeditem.ToString());
                }
            } catch {
                WTOBase.LogToConsole("Failed!");
            }
            //Add moon to API
            Core.AddMoon(MyNewMoon);
            return true;
        }

        //Add the custom moon to the terminal
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AddMoonToTerminal(StartOfRound __instance) {
            
            Terminal ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>(); //Terminal object reference 
            TerminalKeyword RouteKeyword = ActiveTerminal.terminalNodes.allKeywords[26];
            TerminalKeyword InfoKeyword = ActiveTerminal.terminalNodes.allKeywords[6];

            TerminalKeyword TerminalEntry = LevelBundle.LoadAsset<TerminalKeyword>("Assets/CustomScene/523-Ooblterra.asset"); //get our bundle's Terminal Keyword
            int MoonListLength = ActiveTerminal.moonsCatalogueList.Length; //Resize list of moons displayed when you type in the keyword
            Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, MoonListLength + 1);
            ActiveTerminal.moonsCatalogueList[MoonListLength] = MyNewMoon; //Add our moon to that list
            Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry; //Add our terminal entry 
            TerminalEntry.defaultVerb = RouteKeyword; //Set its default verb to "route"

            TerminalNode RouteNode = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/523route.asset"); //get our bundle's route node

            //Resize our RouteKeyword array and put our new route confirmation into it
            Array.Resize<CompatibleNoun>(ref RouteKeyword.compatibleNouns, RouteKeyword.compatibleNouns.Length + 1);
            RouteKeyword.compatibleNouns[RouteKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = TerminalEntry,
                result = RouteNode
            };
            //Resize InfoKeyword array and put our new info into it
            Array.Resize<CompatibleNoun>(ref InfoKeyword.compatibleNouns, InfoKeyword.compatibleNouns.Length + 1);
            InfoKeyword.compatibleNouns[InfoKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = TerminalEntry,
                result = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/OoblterraInfo.asset")
            };
        }

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void CustomLevelInit(StartOfRound __instance) {
            if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
                return;
            }
            WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);
            string[] ObjectsToDestroy = new string[] {
                "CompletedVowTerrain",
                "tree",
                "Tree",
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
            GameObject MyLevelAsset = WTOBase.LevelAssetBundle.LoadAsset("Assets/CustomScene/customlevel.prefab") as GameObject;
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

