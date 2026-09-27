using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    public class ScrapShelf : NetworkBehaviour 
    {

        public Transform[] ScrapSpawnPoints;
        public Animator ShelfOpener;
        public AudioSource ShelfSFX;

        public void Start() 
        {
            if(!IsServer)
            {
                return;
            }

            List<SpawnableItemWithRarity> RandomScrapTypes = StartOfRound.Instance.currentLevel.spawnableScrap;
            List<SpawnableItemWithRarity> OneHanded = new();
            List<SpawnableItemWithRarity> TwoHanded = new();
            foreach(SpawnableItemWithRarity spawnableItem in RandomScrapTypes) 
            {
                // Skip 0 rarity, as these would never be spawned by the real selected loot tables.
                // If we allow 0, then LunarConfig will end up causing default scrap to be spawned for
                // Ooblterra, since it gets added to the loot table with rarity 0 instead of being 
                // excluded entirely.
                // The mechanism for this is a bit complex:
                // - LunarConfig will stamp all items with dawn_lib:lunar_config
                // - DawnLib treats items with this tag as "overridable" and registers them all.
                // - The spawn list contains a bunch of 0 rarity items, which are filtered out by the
                //   normal spawn paths, but the ScrapShelf previously used the entire loot table and
                //   just grabbed random items, causing 0 rarity scrap to get spawned.
                if(spawnableItem.rarity > 0)
                {
                    (spawnableItem.spawnableItem.twoHanded ? TwoHanded : OneHanded).Add(spawnableItem);
                }
            }

            if (OneHanded.Count == 0 && TwoHanded.Count == 0)
            {
                return;
            }

            System.Random ShelfRandom = new (StartOfRound.Instance.randomMapSeed);
            foreach (Transform SpawnLocation in ScrapSpawnPoints)
            {
                bool preferTwoHanded = TwoHanded.Count > 0 && (OneHanded.Count == 0 || ShelfRandom.Next(0, 100) < 80);
                List<SpawnableItemWithRarity> pool = preferTwoHanded ? TwoHanded : OneHanded;
                SpawnableItemWithRarity ScrapToSpawn = pool[ShelfRandom.Next(0, pool.Count)];
                //Instantiate it at our current scrap spawn point 
                GameObject SpawnedScrap = Instantiate(ScrapToSpawn.spawnableItem.spawnPrefab, SpawnLocation.transform.position, SpawnLocation.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
                //set its scrap value 
                GrabbableObject ScrapGrabbableObject = SpawnedScrap.GetComponent<GrabbableObject>();
                int ScrapValue = ShelfRandom.Next(ScrapGrabbableObject.itemProperties.minValue, ScrapGrabbableObject.itemProperties.maxValue);
                ScrapValue = (int)Math.Round(ScrapValue * 0.4);
                //Spawn it
                NetworkObject ScrapNetworkObject = SpawnedScrap.GetComponent<NetworkObject>();
                ScrapNetworkObject.Spawn(destroyWithScene: true);
                RoundManager.Instance.spawnedSyncedObjects.Add(SpawnedScrap);
                SetScrapValueClientRpc(ScrapNetworkObject, ScrapValue);
            }
        }

        public void OpenShelf() 
        {
            ShelfOpener.SetTrigger("Open");
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) 
            {
                ShelfSFX.Play();
            }
        }

        [ClientRpc]
        public void SetScrapValueClientRpc(NetworkObjectReference ScrapToSet, int ScrapValue) 
        {
            ScrapToSet.TryGet(out var ScrapNetworkobject);
            GrabbableObject NextScrap = ScrapNetworkobject.GetComponent<GrabbableObject>();
            NextScrap?.SetScrapValue(ScrapValue);
        }
    }
}
