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
    public AudioSource ShelfSFX;
    System.Random ShelfRandom;

    public void Start() {
        List<SpawnableItemWithRarity> ScrapPool = StartOfRound.Instance.currentLevel.spawnableScrap;
        ShelfRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        foreach (Transform SpawnLocation in ScrapSpawnPoints) {
            //get a random object from the scrap pool
            if (base.IsServer) {
                SpawnableItemWithRarity NextScrap = ScrapPool[ShelfRandom.Next(0, ScrapPool.Count)];
                //Instantiate it at our current scrap spawn point 
                GameObject SpawnedScrap = Instantiate(NextScrap.spawnableItem.spawnPrefab, SpawnLocation.transform.position, SpawnLocation.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
                //set its scrap value THIS PROBABLY ISNT SYNCED PROPERLY 
                GrabbableObject ImRunningOutOfScrapNames = SpawnedScrap.GetComponent<GrabbableObject>();
                int ScrapValue = ShelfRandom.Next(ImRunningOutOfScrapNames.itemProperties.minValue, ImRunningOutOfScrapNames.itemProperties.maxValue);
                ScrapValue = (int)Math.Round(ScrapValue * 0.4);
                //ImRunningOutOfScrapNames.SetScrapValue(ScrapValue);

                NetworkObject ScrapNetworkObject = SpawnedScrap.GetComponent<NetworkObject>();
                //Spawn it
                ScrapNetworkObject.Spawn(destroyWithScene: true);

                RoundManager.Instance.spawnedSyncedObjects.Add(SpawnedScrap);
                SetScrapValueServerRpc(ScrapNetworkObject, ScrapValue);
            }
        }
    }

    public void OpenShelf() {
        ShelfOpener.SetTrigger("Open");
        ShelfSFX.Play();
    }

    [ServerRpc]
    public void SetScrapValueServerRpc(NetworkObjectReference ScrapToSet, int ScrapValue) {
        SetScrapValueClientRpc(ScrapToSet,ScrapValue);
    }
    [ClientRpc]
    public void SetScrapValueClientRpc(NetworkObjectReference ScrapToSet, int ScrapValue) {
        ScrapToSet.TryGet(out var ScrapNetworkobject);
        GrabbableObject NextScrap = ScrapNetworkobject.GetComponent<GrabbableObject>();
        if (NextScrap != null) {
            NextScrap.SetScrapValue(ScrapValue);
        }
    }
}

