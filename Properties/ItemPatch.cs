using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;

namespace Welcome_To_Ooblterra.Properties {
    internal class ItemPatch {
        
        private class ItemData {
            private string ItemPath;
            private int Rarity;
            private Item Itemref;
            public ItemData(string path, int rarity) {
                ItemPath = path;
                Rarity = rarity;
            }
            public string GetItemPath(){  return ItemPath; }
            public int GetRarity(){ return Rarity; }
            public void SetItem(Item ItemToSet){ Itemref= ItemToSet; }
            public Item GetItem(){ return Itemref; }

        }
        //This array stores all our custom items
        private static ItemData[] ItemList = new ItemData[] { 
            new ItemData("Assets/CustomItems/AlienCrate.asset", 10),
            new ItemData("Assets/Customitems/FiveSixShovel.asset", 30),
            new ItemData("Assets/CustomItems/HandCrystal.asset", 5),
            new ItemData("Assets/CustomItems/OoblCorpse.asset", 40),
            new ItemData("Assets/CustomItems/StatueSmall.asset", 10),
            new ItemData("Assets/CustomItems/WandCorpse.asset", 40),
            new ItemData("Assets/CustomItems/WandFeed.asset", 10),
        };


        //Add our custom items
        //I know its public I need to reference it in the fucking plugin class!!!! leave me alone!!!!!!!
        public static void AddCustomItems() {
            //Create our custom items
            Item NextItem;
            SpawnableItemWithRarity MoonScrapItem;

            foreach(ItemData MyCustomScrap in ItemList){
                NextItem = WTOBase.ItemAssetBundle.LoadAsset<Item>(MyCustomScrap.GetItemPath());               
                NetworkPrefabs.RegisterNetworkPrefab(NextItem.spawnPrefab);
                //Items.RegisterScrap(NextItem, MyCustomScrap.GetRarity(), Levels.LevelTypes.All);
                MyCustomScrap.SetItem(NextItem);
                MoonScrapItem = new SpawnableItemWithRarity {
                    spawnableItem = NextItem,
                    rarity = MyCustomScrap.GetRarity()
                };
                WTOBase.AddToScrapList(MoonScrapItem);
            }
        }

        //try to spawn the object 
        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPostfix]
        private static void TrySpawnNewItem(StartOfRound __instance) {
            if (Keyboard.current.f8Key.wasPressedThisFrame) {
                //var Crystal = UnityEngine.Object.Instantiate(ItemList[2].GetItem().spawnPrefab, __instance.localPlayerController.gameplayCamera.transform.position, Quaternion.identity);
                //Crystal.GetComponent<NetworkObject>().Spawn();
                WTOBase.LogToConsole("Custom item spawned...");
            }
        }
    }
}