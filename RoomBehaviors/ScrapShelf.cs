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
        List<SpawnableItemWithRarity> RandomScrapTypes = StartOfRound.Instance.currentLevel.spawnableScrap;
        ShelfRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        foreach (Transform SpawnLocation in ScrapSpawnPoints) {
            //get a random object from the scrap pool
            if (base.IsServer) {
                SpawnableItemWithRarity ScrapToSpawn = RandomScrapTypes[ShelfRandom.Next(0, RandomScrapTypes.Count)];
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
                SetScrapValueServerRpc(ScrapNetworkObject, ScrapValue);
            }
        }
    }

    public void OpenShelf() {
        ShelfOpener.SetTrigger("Open");
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) {
            ShelfSFX.Play();
        }
        
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

