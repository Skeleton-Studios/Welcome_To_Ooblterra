using HarmonyLib;
using System;
using UnityEngine;
using Unity.AI.Navigation;
using WonderAPI;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {

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

            //Load our level asset object
            MyNewMoon = LevelBundle.LoadAsset<SelectableLevel>("Assets/CustomScene/OoblterraLevel.asset");

            //Set certain variables that we can't set in unity or else shit will break
            MyNewMoon.planetPrefab = __instance.levels[2].planetPrefab;
            MyNewMoon.spawnableOutsideObjects = new SpawnableOutsideObjectWithRarity[] { __instance.levels[0].spawnableOutsideObjects[6] };
            MyNewMoon.levelAmbienceClips = __instance.levels[2].levelAmbienceClips;

            MonsterPatch.SetSecurityObjects(true, MyNewMoon, __instance);
            ItemPatch.SetMoonItemList(false, MyNewMoon, __instance);
            MonsterPatch.SetInsideMonsters(true, MyNewMoon, __instance);
            MonsterPatch.SetOutsideMonsters(true, MyNewMoon, __instance);
            MonsterPatch.SetDaytimeMonsters(true, MyNewMoon, __instance);

            MoonFriendlyName = MyNewMoon.PlanetName;
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
            TerminalEntry.defaultVerb = RouteKeyword;
            Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, ActiveTerminal.moonsCatalogueList.Length + 1); //Resize list of moons displayed 
            ActiveTerminal.moonsCatalogueList[ActiveTerminal.moonsCatalogueList.Length - 1] = MyNewMoon; //Add our moon to that list
            Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry; //Add our terminal entry 
            TerminalEntry.defaultVerb = RouteKeyword; //Set its default verb to "route"


            TerminalNode RouteNode = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/523route.asset");
            RouteNode.terminalOptions[0].noun = ActiveTerminal.terminalNodes.allKeywords[4];
            RouteNode.terminalOptions[0].result = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/523CancelRoute.asset");
            RouteNode.terminalOptions[1].noun = ActiveTerminal.terminalNodes.allKeywords[3];

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
        private static void InitCustomLevel(StartOfRound __instance) {

            if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
                return;
            }
            WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);


            string[] ObjectNamesToDestroy = new string[]{
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Local Volumetric Fog",
                "GroundFog",
                "Sky and Fog Global Volume",
                "SunTexture",
            };

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject ObjToDestroy in allObjects) {
                
                if (ObjToDestroy.name.Contains("Models2VowFactory")) {
                    try {
                        ObjToDestroy.SetActive(false);
                        WTOBase.LogToConsole("Vow factory adjusted.");
                    } catch {
                        WTOBase.LogToConsole("Issue adjusting vow factory");
                    }
                }


                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds"))) {
                    GameObject.Destroy(ObjToDestroy);
                }
                foreach (string UnwantedObjString in ObjectNamesToDestroy) {
                    //If the object has any of the names in the list, it's gotta go
                    if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                        GameObject.Destroy(ObjToDestroy);
                        continue;
                    }
                }
            }
            SunObject = GameObject.Find("SunWithShadows");
            SunAnimObject = GameObject.Find("SunAnimContainer");
            IndirectLight = GameObject.Find("Indirect");

            //Load our custom prefab
            GameObject MyLevelAsset = WTOBase.LevelAssetBundle.LoadAsset("Assets/CustomScene/customlevel.prefab") as GameObject;
            GameObject MyInstantiatedLevel = GameObject.Instantiate(MyLevelAsset);
            WTOBase.LogToConsole("Loaded custom terrain object!");
            try {
                NavMeshSurface NavMesh = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
                //NavMesh.UpdateNavMesh();
            } catch(Exception E) {
                WTOBase.LogToConsole("Failed to rebuild navmesh. Error: " + E);
            }  

            /*
            var prefab = StartOfRound.Instance.levels[6].Enemies.First(spawnableEnemy => spawnableEnemy.enemyType.enemyName == "Spring").enemyType.enemyPrefab;
            var position = new Vector3(0, 0, 0); // up to you
            var enemy = GameObject.Instantiate(prefab, position, Quaternion.identity);
            RoundManager.SpawnEnemyOnServer(new Vector3(0, 0, 0), 0f, -3);
            */

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

        [HarmonyPatch(typeof(TimeOfDay), "PlayTimeMusicDelayed")]
        [HarmonyPrefix]
        private static bool SkipTODMusic() {
            return false;
        }
    }
}

