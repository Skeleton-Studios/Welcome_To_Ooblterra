using HarmonyLib;
using System;
using UnityEngine;
using Unity.AI.Navigation;
using System.Linq;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {

    internal class MoonPatch {

        public static IDictionary<string, SelectableLevel> Moons = new Dictionary<string, SelectableLevel>();

        //Identifiers for the Moon
        public static SelectableLevel MyNewMoon;
        public static IDictionary<string, int> ModdedIds = new Dictionary<string, int>();
        //public IDictionary<string, SelectableLevel> mlevels;


        public static string MoonFriendlyName;
        private static GameObject SunObject;
        private static GameObject SunAnimObject;
        private static GameObject IndirectLight;
        private static AssetBundle LevelBundle = WTOBase.LevelAssetBundle;

        //Following two methods taken from MoonAPI, thanks Bizzlemip
        public static T[] ResizeArray<T>(T[] oldArray, int newSize) {
            T[] array = new T[newSize];
            oldArray.CopyTo(array, 0);
            return array;
        }

        private static int AddToMoons(StartOfRound SOR, SelectableLevel Moon) {
            int num = -1;
            for (int i = 0; i < SOR.levels.Length; i++) {
                if (SOR.levels[i] == null) {
                    num = i;
                    break;
                }
            }
            if (num == -1) {
                throw new NullReferenceException("No null value found in StartOfRound.levels");
            }
            SOR.levels[num] = Moon;
            return num;
        }

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        private static void AddMoonToList(StartOfRound __instance) {

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
            MonsterPatch.SetDaytimeMonsters(false, MyNewMoon, __instance);

            MoonFriendlyName = MyNewMoon.PlanetName;
            Moons[MyNewMoon.name] = MyNewMoon;
            TerminalPatch.AddMoonToList();
            //new level array should be big enough to fit our modded moons in it
            __instance.levels = ResizeArray<SelectableLevel>(__instance.levels, __instance.levels.Length + Moons.Count<KeyValuePair<string, SelectableLevel>>());

            //Add our modded moons to the array and assign them an ID in the IDs array
            foreach (KeyValuePair<string, SelectableLevel> keyValuePair in Moons) {
                int num = AddToMoons(__instance, keyValuePair.Value);
                keyValuePair.Value.levelID = num;
                ModdedIds[keyValuePair.Key] = num;
            }
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
                "Sky and Fog Global Volume",
                "SunTexture"
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

                foreach (NavMeshSurface Nav in GameObject.FindObjectsOfType<NavMeshSurface>()) {
                    Nav.RemoveData();
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
            GameObject.Instantiate(WTOBase.LevelAssetBundle.LoadAsset("Assets/CustomScene/customlevel.prefab"));
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

            MoveNavNodesToNewPositions();
        }

        [HarmonyPatch(typeof(TimeOfDay), "PlayTimeMusicDelayed")]
        [HarmonyPrefix]
        private static bool SkipTODMusic() {
            return false;
        }

        private static void MoveNavNodesToNewPositions() {
            //Get a list of all outside navigation nodes
            GameObject[] NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

            //Get a list of all our Oobltera nodes
            List<GameObject> CustomNodes = new List<GameObject>();
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject Object in allObjects) {
                if (Object.name == "OoblOutsideNode") {
                    CustomNodes.Add(Object);
                }
            }
            WTOBase.LogToConsole("Outside nav points: " + allObjects.Count().ToString());
            //For each of the outside navigation nodes, move its position to the corresponding custom node. If the custom node list is
            //exhausted, destroy the outside navigation node.
            for (int i = 0; i < NavNodes.Count(); i++) {
                if (CustomNodes.Count() > i) {
                    NavNodes[i].transform.position = CustomNodes[i].transform.position;
                } else {
                    GameObject.Destroy(NavNodes[i]);
                }
            }
            NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            WTOBase.LogToConsole("Moved nav points: " + NavNodes.Count().ToString());
        }
    }
}

