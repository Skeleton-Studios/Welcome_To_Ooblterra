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
                (spawnableItem.spawnableItem.twoHanded ? TwoHanded : OneHanded).Add(spawnableItem);
            }

            System.Random ShelfRandom = new (StartOfRound.Instance.randomMapSeed);
            foreach (Transform SpawnLocation in ScrapSpawnPoints)
            {
                //get a random object from the scrap pool
                SpawnableItemWithRarity ScrapToSpawn = ShelfRandom.Next(0, 100) < 80 ?
                    TwoHanded[ShelfRandom.Next(0, TwoHanded.Count)] :
                    OneHanded[ShelfRandom.Next(0, OneHanded.Count)];
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
