using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    public class ScrapShelf : NetworkBehaviour 
    {

        public Transform[] ScrapSpawnPoints;
        public Animator ShelfOpener;
        public AudioSource ShelfSFX;

        private void Start() 
        {
            if(!IsServer)
            { 
                return;
            }

            List<SpawnableItemWithRarity> RandomScrapTypes = StartOfRound.Instance.currentLevel.spawnableScrap;
            List<SpawnableItemWithRarity> OneHanded = [];
            List<SpawnableItemWithRarity> TwoHanded = [];
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

            int[] oneHandedWeights = [.. OneHanded.Select(item => item.rarity)];
            int[] twoHandedWeights = [.. TwoHanded.Select(item => item.rarity)];

            System.Random random = RoundManager.Instance.AnomalyRandom;

            foreach (Transform SpawnLocation in ScrapSpawnPoints)
            {
                // Aim to spawn two handed scap more often as it's usually more valuable.
                bool preferTwoHanded = TwoHanded.Count > 0 && (OneHanded.Count == 0 || random.Next(0, 100) < 80);
                SpawnableItemWithRarity ScrapToSpawn = preferTwoHanded ? 
                    TwoHanded[RoundManager.Instance.GetRandomWeightedIndex(twoHandedWeights, random)] :
                    OneHanded[RoundManager.Instance.GetRandomWeightedIndex(oneHandedWeights, random)];

                GameObject SpawnedScrap = Instantiate(ScrapToSpawn.spawnableItem.spawnPrefab, SpawnLocation.transform.position, SpawnLocation.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);

                GrabbableObject ScrapGrabbableObject = SpawnedScrap.GetComponent<GrabbableObject>();
                // Scrap value is calculated in the same way as vanilla.
                int ScrapValue = (int)(random.Next(ScrapGrabbableObject.itemProperties.minValue, ScrapGrabbableObject.itemProperties.maxValue) * RoundManager.Instance.scrapValueMultiplier);

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
