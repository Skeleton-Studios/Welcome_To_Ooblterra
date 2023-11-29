using HarmonyLib;
using UnityEngine;


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
            ReplaceRoom("Assets/CustomInterior/CustomStartRoom.prefab", "ManorStartRoom", "CustomElevator");
           
        }


        private static void ReplaceRoom(string PathToNewRoom, string BaseMeshName, string NewMeshName) {
            //Create an instance of the new mesh 
            GameObject ObjectMesh = WTOBase.FactoryAssetBundle.LoadAsset<GameObject>("PathToNewRoom");
            GameObject.Instantiate(ObjectMesh);
            GameObject ReplacedMesh = GameObject.Find(BaseMeshName);
            ReplacedMesh.GetComponent<MeshFilter>().mesh = GameObject.Find(NewMeshName).GetComponent<MeshFilter>().mesh;
            ReplacedMesh.GetComponent<MeshRenderer>().materials = GameObject.Find(NewMeshName).GetComponent<MeshRenderer>().materials;
            ReplacedMesh.GetComponent<MeshCollider>().sharedMesh = GameObject.Find(NewMeshName).GetComponent<MeshFilter>().mesh;

            GameObject.Destroy(ObjectMesh);
        }
    }
}
