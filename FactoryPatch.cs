using HarmonyLib;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using LethalLib.Modules;
using DunGen;
using Dungeon = LethalLib.Modules.Dungeon;

namespace Welcome_To_Ooblterra {
    internal class FactoryPatch {

        /* doesn't seem to do anything
        private static TileSet MyTileSet = WTOBase.FactoryAssetBundle.LoadAsset<TileSet>("Assets/CustomInterior/OoblterraMaze.asset");

        [HarmonyPatch(typeof(RoundManager), "Awake")]
        [HarmonyPostfix]
        private static void AddTilesToDunGen(){
            var runtimeDungeon = GameObject.FindObjectOfType<RuntimeDungeon>();
            var generator = runtimeDungeon.Generator;
            generator.TileInjectionMethods += InjectTiles;
        }
        private static void InjectTiles(RandomStream randomStream, ref List<InjectedTile> tilesToInject) {
            bool isOnMainPath = false;
            float pathDepth = 0.1f;
            float branchDepth = 1.0f;
            var tile = new InjectedTile(MyTileSet, isOnMainPath, pathDepth, branchDepth);
            tilesToInject.Add(tile);
        }
        */

        [HarmonyPatch(typeof(RoundManager), "GenerateNewFloor")]
        [HarmonyPostfix]
        private static void CustomFactoryReplacements(RoundManager __instance) {
            if (__instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName) {
                return;
            }
            ReplaceRoom("Assets/CustomInterior/OoblStartRoom.prefab", "ManorStartRoom", "CustomElevator");
        }

        private static void ReplaceRoom(string PathToNewRoom, string BaseMeshName, string NewMeshName) {
            //Create an instance of the new mesh 
            GameObject ObjectMesh = WTOBase.FactoryAssetBundle.LoadAsset<GameObject>(PathToNewRoom);
            GameObject ReplacedMesh = GameObject.Find(BaseMeshName);

            foreach(MeshRenderer item in GameObject.FindObjectsOfType<MeshRenderer>()) {
                if (item.transform.parent.name == "StartRoomMeshes" || item.transform.parent.name == "RandomProps" || item.transform.parent.name == "ManorStartRoom") {
                    item.enabled = false;
                }
                if (item.name.Contains("Shelf") || item.name.Contains("Books")){
                    item.enabled = false;
                }
            }

            foreach (Light light in GameObject.FindObjectsOfType<Light>()) {
                if(light.name == "PoweredLightTypeB" || light.name == "PoweredLightTypeB (1)") {
                    GameObject.Destroy(light.gameObject);
                }
            }

            GameObject.Find("ManorStartRoom").GetComponent<MeshRenderer>().enabled = false;
            GameObject MainRoom = GameObject.Instantiate(ObjectMesh, ReplacedMesh.transform.position, Quaternion.identity);
            MainRoom.transform.Rotate(0, 90, 0);
            /*
            ReplacedMesh.GetComponent<MeshFilter>().mesh = GameObject.Find(NewMeshName).GetComponent<MeshFilter>().mesh;
            ReplacedMesh.GetComponent<MeshRenderer>().materials = GameObject.Find(NewMeshName).GetComponent<MeshRenderer>().materials;
            ReplacedMesh.GetComponent<MeshCollider>().sharedMesh = GameObject.Find(NewMeshName).GetComponent<MeshFilter>().mesh;

            GameObject.Destroy(ObjectMesh);
            */
        }
    }
}
