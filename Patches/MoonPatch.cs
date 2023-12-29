using HarmonyLib;
using System;
using UnityEngine;
using Unity.AI.Navigation;
using System.Linq;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;
using UnityEngine.Rendering.HighDefinition;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Patches {

    internal class MoonPatch {

        public static string MoonFriendlyName;
        public static IDictionary<string, SelectableLevel> ModdedMoonList = new Dictionary<string, SelectableLevel>();
        public static IDictionary<string, int> ModdedIds = new Dictionary<string, int>();
        public static SelectableLevel MyNewMoon;

        public static UnityEngine.Object LevelPrefab = null;

        //Identifiers for the Moon


        //public IDictionary<string, SelectableLevel> mlevels;

 

        private static GameObject SunObject;
        private static GameObject SunAnimObject;
        private static GameObject IndirectLight;
        private static AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
        private static string[] ObjectNamesToDestroy = new string[]{
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                "ForestAmbience",
                "Sky and Fog Global Volume",
                "Local Volumetric Fog",
                "SunTexture"
            };

        private static bool LevelLoaded;

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
            foreach(SelectableLevel level in SOR.levels) {
                WTOBase.LogToConsole(level.name);
            }
            return num;
        }


        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        private static void AddMoonToList(StartOfRound __instance) {
            if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
                DestroyOoblterraPrefab();
            }
            //Load our level asset object
            MyNewMoon = LevelBundle.LoadAsset<SelectableLevel>("Assets/CustomScene/OoblterraLevel.asset");

            //Set certain variables that we can't set in unity or else shit will break
            MyNewMoon.planetPrefab = __instance.levels[2].planetPrefab;
            MyNewMoon.spawnableOutsideObjects = new SpawnableOutsideObjectWithRarity[] {  };
            MyNewMoon.levelAmbienceClips = __instance.levels[2].levelAmbienceClips;

            MonsterPatch.SetSecurityObjects(MyNewMoon, __instance.levels[5].spawnableMapObjects);
            ItemPatch.SetMoonItemList(MyNewMoon);
            //MonsterPatch.SetInsideMonsters(MyNewMoon);
            //MonsterPatch.SetOutsideMonsters(MyNewMoon, new List<SpawnableEnemyWithRarity>() {} );
            //MonsterPatch.SetDaytimeMonsters(MyNewMoon);

            MoonFriendlyName = MyNewMoon.PlanetName;
            ModdedMoonList[MyNewMoon.name] = MyNewMoon;
            TerminalPatch.AddMoonToList();
            //new level array should be big enough to fit our modded moons in it
            __instance.levels = ResizeArray<SelectableLevel>(__instance.levels, __instance.levels.Length + ModdedMoonList.Count<KeyValuePair<string, SelectableLevel>>());

            //Add our modded moons to the array and assign them an ID in the IDs array
            foreach (KeyValuePair<string, SelectableLevel> keyValuePair in ModdedMoonList) {
                int num = AddToMoons(__instance, keyValuePair.Value);
                keyValuePair.Value.levelID = num;
                ModdedIds[keyValuePair.Key] = num;
            }
        }

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void InitCustomLevel(StartOfRound __instance) {
            if (__instance.currentLevel.PlanetName != MoonFriendlyName /*|| !GameNetworkManager.Instance.gameHasStarted*/) {
                return;
            }
            DestroyOoblterraPrefab();
            WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);






            //Load our custom prefab
            if (!LevelLoaded) { 
                LevelPrefab = GameObject.Instantiate(WTOBase.LevelAssetBundle.LoadAsset("Assets/CustomScene/customlevel.prefab"));
                WTOBase.LogToConsole("Loaded custom terrain object!");
                LevelLoaded = true;
            }
            //The prefab contains an object called TeleportSnapLocation that we move the primary door to
            GameObject Entrance = GameObject.Find("EntranceTeleportA");
            GameObject SnapLoc = GameObject.Find("TeleportSnapLocation");
            Entrance.transform.position = SnapLoc.transform.position;
            GameObject FireExit = GameObject.Find("EntranceTeleportB");
            GameObject FireExitSnapLoc = GameObject.Find("FireExitSnapLocation");
            FireExit.transform.position = FireExitSnapLoc.transform.position;



            ManageCustomSun();
            MoveNavNodesToNewPositions();
            ManageFootsteps(__instance);
            
            //Footsteps

            LevelLoaded = true;
        }

        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        [HarmonyPostfix]
        public static void DestroyLevel(StartOfRound __instance, bool ShouldDestroy = true) {
            if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
                DestroyOoblterraPrefab();
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), "PlayTimeMusicDelayed")]
        [HarmonyPrefix]
        private static bool SkipTODMusic() {
            return false;
        }


        private static void DestroyOoblterraPrefab() {
            GameObject.Destroy(LevelPrefab);
            LevelLoaded = false;
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
        private static void ManageCustomSun() {
            //Ooblterra has no sun so we're getting rid of it
            SunObject = GameObject.Find("SunWithShadows");
            SunAnimObject = GameObject.Find("SunAnimContainer");
            IndirectLight = GameObject.Find("Indirect");
            SunAnimObject.GetComponent<animatedSun>().directLight = GameObject.Find("OoblSun").GetComponent<Light>();
            SunAnimObject.GetComponent<animatedSun>().indirectLight = GameObject.Find("OoblIndirect").GetComponent<Light>();
            GameObject.Destroy(SunObject);
            GameObject.Destroy(IndirectLight);
        }
        private static void ManageFootsteps(StartOfRound __instance) {
            foreach (FootstepSurface surfaces in __instance.footstepSurfaces) {
                if (surfaces.surfaceTag == "Grass") {
                    surfaces.clips = new AudioClip[] {
                        LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLESTEP01.wav"),
                        LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLESTEP02.wav"),
                        LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLESTEP03.wav"),
                        LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLESTEP04.wav"),
                        LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLESTEP05.wav")
                    };
                    surfaces.hitSurfaceSFX = LevelBundle.LoadAsset<AudioClip>("Assets/CustomScene/Sound/Footsteps/TENTACLE_Fall.wav");
                }
            }
        }

        private static void DestroyVowObjects() {
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            //If the object has any of the names in the list, it's gotta go
            /*foreach (GameObject ObjToDestroy in allObjects.Where(obj => ObjectNamesToDestroy.Contains<string>(obj.name) || 
                (obj.name.Contains("Plane") && 
                    (obj.transform.parent.gameObject.name.Contains("Foliage") || obj.transform.parent.gameObject.name.Contains("Mounds"))
                                                                                                                                        ))){
                GameObject.Destroy(ObjToDestroy);
                continue;
            }*/

            foreach (GameObject ObjToDestroy in allObjects) {
                if (ObjToDestroy.name.Contains("Models2VowFactory")) {
                    ObjToDestroy.SetActive(false);
                    WTOBase.LogToConsole("Vow factory adjusted.");
                }

                //If the object's named Plane and its parent is Foliage, it's also gotta go. This gets rid of the grass
                if (ObjToDestroy.name.Contains("Plane") && (ObjToDestroy.transform.parent.gameObject.name.Contains("Foliage") || ObjToDestroy.transform.parent.gameObject.name.Contains("Mounds"))) {
                    GameObject.Destroy(ObjToDestroy);
                }
                foreach (string UnwantedObjString in ObjectNamesToDestroy) {
                    
                    if (ObjToDestroy.name.Contains(UnwantedObjString)) {
                        GameObject.Destroy(ObjToDestroy);
                        continue;
                    }
                }
            }
        }
    }
}

