using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things;
public class ScrapShelf : NetworkBehaviour {

    public Transform[] ScrapSpawnPoints;
    public Animator ShelfOpener;
    System.Random ShelfRandom;

    public void Start() {
        List<SpawnableItemWithRarity> ScrapPool = StartOfRound.Instance.currentLevel.spawnableScrap;
        ShelfRandom = new System.Random();
        foreach (Transform SpawnLocation in ScrapSpawnPoints) {
            //get a random object from the scrap pool
            SpawnableItemWithRarity NextScrap = ScrapPool[ShelfRandom.Next(0, ScrapPool.Count)];
            //Instantiate it at our current scrap spawn point 
            GameObject SpawnedScrap = Instantiate(NextScrap.spawnableItem.spawnPrefab, SpawnLocation.transform.position, SpawnLocation.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
            //set its scrap value THIS PROBABLY ISNT SYNCED PROPERLY 
            GrabbableObject ImRunningOutOfScrapNames = SpawnedScrap.GetComponent<GrabbableObject>();
            int ScrapValue = ShelfRandom.Next(ImRunningOutOfScrapNames.itemProperties.minValue, ImRunningOutOfScrapNames.itemProperties.maxValue);
            ScrapValue = (int)Math.Round(ScrapValue * 0.4);
            ImRunningOutOfScrapNames.SetScrapValue(ScrapValue);

            //Spawn it
            SpawnedScrap.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            RoundManager.Instance.spawnedSyncedObjects.Add(SpawnedScrap);
        }
    }

    public void OpenShelf() {
        ShelfOpener.SetTrigger("Open");
    }

}

