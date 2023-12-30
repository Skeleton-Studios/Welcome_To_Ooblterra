using HarmonyLib;
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {

    internal class MoonPatch {

        public static string MoonFriendlyName;
        public static SelectableLevel MyNewMoon;
        
        private static readonly AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
        private static UnityEngine.Object LevelPrefab = null;
        private static readonly string[] ObjectNamesToDestroy = new string[]{
                "CompletedVowTerrain",
                "tree",
                "Tree",
                "Rock",
                "StaticLightingSky",
                //"ForestAmbience",
                "Sky and Fog Global Volume",
                "Local Volumetric Fog",
                "SunTexture"
            };
        private static bool LevelLoaded;

        //PATCHES

        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        private static void FuckThePlanet(StartOfRound __instance) {
            if (__instance.currentLevel.PlanetName != MoonFriendlyName) {
                DestroyOoblterraPrefab();
            }
        }

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        [HarmonyPriority(0)]
        private static void AddMoonToList(StartOfRound __instance) {
            //Load our level asset object
            MyNewMoon = LevelBundle.LoadAsset<SelectableLevel>("Assets/CustomScene/OoblterraLevel.asset");
            MoonFriendlyName = MyNewMoon.PlanetName;
            SetMoonVariables(MyNewMoon, __instance);
            AddToMoons(MyNewMoon, __instance);
        }

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void InitCustomLevel(StartOfRound __instance) {
            DestroyOoblterraPrefab();
            if (__instance.currentLevel.PlanetName != MoonFriendlyName /*|| !GameNetworkManager.Instance.gameHasStarted*/) {
                return;
            }
            WTOBase.LogToConsole("Loading into level " + MoonFriendlyName);
            DestroyVowObjects();
            //Load our custom prefab
            LevelPrefab = GameObject.Instantiate(WTOBase.LevelAssetBundle.LoadAsset("Assets/CustomScene/customlevel.prefab"));
            WTOBase.LogToConsole("Loaded custom terrain object!");
            MoveDoors();
            ManageCustomSun();
            MoveNavNodesToNewPositions();
            ManageFootsteps();
            LevelLoaded = true;
        }

        [HarmonyPatch(typeof(StartOfRound), "ShipHasLeft")]
        [HarmonyPostfix]
        public static void DestroyLevel(StartOfRound __instance) {
            if (__instance.currentLevel.PlanetName == MoonFriendlyName) {
                DestroyOoblterraPrefab();
            }
        }
        
        [HarmonyPatch(typeof(TimeOfDay), "PlayTimeMusicDelayed")]
        [HarmonyPrefix]
        private static bool SkipTODMusic() {
            return false;
        }

        //METHODS
        private static void SetMoonVariables(SelectableLevel Moon, StartOfRound Instance) {
            Moon.planetPrefab = Instance.levels[2].planetPrefab;
            Moon.spawnableOutsideObjects = new SpawnableOutsideObjectWithRarity[0];
            Moon.levelAmbienceClips = Instance.levels[2].levelAmbienceClips;

            MonsterPatch.SetSecurityObjects(Moon, Instance.levels[5].spawnableMapObjects);
            ItemPatch.SetMoonItemList(Moon);
            //MonsterPatch.SetInsideMonsters(MyNewMoon);
            //MonsterPatch.SetOutsideMonsters(MyNewMoon, new List<SpawnableEnemyWithRarity>() {} );
            //MonsterPatch.SetDaytimeMonsters(MyNewMoon);

        }
        //Following two methods taken from MoonAPI, thanks Bizzlemip
        public static T[] ResizeArray<T>(T[] oldArray, int newSize) {
            T[] array = new T[newSize];
            oldArray.CopyTo(array, 0);
            return array;
        }
        private static int AddToMoons(SelectableLevel Moon, StartOfRound Instance) {
            Instance.levels = ResizeArray(Instance.levels, Instance.levels.Length + 1);
            int num = -1;
            for (int i = 0; i < Instance.levels.Length; i++) {
                if (Instance.levels[i] == null) {
                    num = i;
                    break;
                }
            }
            if (num == -1) {
                throw new NullReferenceException("No null value found in StartOfRound.levels");
            }
            Instance.levels[num] = Moon;
            foreach (SelectableLevel level in Instance.levels) {
                WTOBase.LogToConsole(level.name);
            }
            return num;
        }
        private static void DestroyOoblterraPrefab() {
            GameObject.Destroy(LevelPrefab);
            LevelLoaded = false;
        }
        private static void MoveNavNodesToNewPositions() {
            //Get a list of all outside navigation nodes
            GameObject[] NavNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");

            //Build a list of all our Oobltera nodes
            List<GameObject> CustomNodes = new List<GameObject>();
            IEnumerable<GameObject> allObjects = GameObject.FindObjectsOfType<GameObject>().Where(obj => obj.name == "OoblOutsideNode");
            foreach (GameObject Object in allObjects) {
                    CustomNodes.Add(Object);
            }
            WTOBase.LogToConsole("Outside nav points: " + allObjects.Count().ToString());

            //Put outside nav nodes at the location of our ooblterra nodes. Destroy any extraneous ones
            for (int i = 0; i < NavNodes.Count(); i++) {
                if (CustomNodes.Count() > i) {
                    NavNodes[i].transform.position = CustomNodes[i].transform.position;
                } else {
                    GameObject.Destroy(NavNodes[i]);
                }
            }
        }
        private static void ManageCustomSun() {
            //Ooblterra has no sun so we're getting rid of it
            GameObject SunObject = GameObject.Find("SunWithShadows");
            GameObject SunAnimObject = GameObject.Find("SunAnimContainer");
            GameObject IndirectLight = GameObject.Find("Indirect");
            SunAnimObject.GetComponent<animatedSun>().directLight = GameObject.Find("OoblSun").GetComponent<Light>();
            SunAnimObject.GetComponent<animatedSun>().indirectLight = GameObject.Find("OoblIndirect").GetComponent<Light>();
            GameObject.Destroy(SunObject);
            GameObject.Destroy(IndirectLight);
        }
        private static void ManageFootsteps() {
            foreach (FootstepSurface surfaces in StartOfRound.Instance.footstepSurfaces) {
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
            //I have no fucking clue why this works for the foliage too but fuck it I guess
            IEnumerable<GameObject> allObjects = GameObject.FindObjectsOfType<GameObject>().Where(obj => ObjectNamesToDestroy.Any(obj.name.Contains));
            foreach (GameObject ObjToDestroy in allObjects) {
                GameObject.Destroy(ObjToDestroy);
                continue;
            }
            GameObject Factory = GameObject.Find("Models2VowFactory");
            Factory.SetActive(false);
            /*
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
            */
        }
        private static void MoveDoors() {
            //The prefab contains an object called TeleportSnapLocation that we move the primary door to
            GameObject Entrance = GameObject.Find("EntranceTeleportA");
            GameObject SnapLoc = GameObject.Find("TeleportSnapLocation");
            Entrance.transform.position = SnapLoc.transform.position;
            GameObject FireExit = GameObject.Find("EntranceTeleportB");
            GameObject FireExitSnapLoc = GameObject.Find("FireExitSnapLocation");
            FireExit.transform.position = FireExitSnapLoc.transform.position;
        }
    }
}

