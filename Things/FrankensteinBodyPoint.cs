using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinBodyPoint : NetworkBehaviour {
    public InteractTrigger triggerScript;
    public bool HasBody;
    public NetworkObject deskObjectsContainer;
    private float updateInterval;
    private BoxCollider triggerCollider;

    //Figure out how to create an interaction point

    //make it specify that it wants a player body, just how the charger wants battery powered items
    private void Update() {
        if (NetworkManager.Singleton == null) {
            return;
        }

        if (updateInterval > 1f) {
            updateInterval = 0f;
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null) {
                triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemName == "Body";
            }
        } else {
            updateInterval += Time.deltaTime;
        }
    }
    //when interacted, attach the body to the point
    private void AttachBody(PlayerControllerB player) {
        if (deskObjectsContainer.GetComponentsInChildren<GrabbableObject>().Length < 1 && GameNetworkManager.Instance != null && player == GameNetworkManager.Instance.localPlayerController) {
            Vector3 vector = RoundManager.RandomPointInBounds(triggerCollider.bounds);
            vector.y = triggerCollider.bounds.min.y;
            if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out var hitInfo, 8f, 1048640, QueryTriggerInteraction.Collide)) {
                vector = hitInfo.point;
            }

            vector.y += player.currentlyHeldObjectServer.itemProperties.verticalOffset;
            vector = deskObjectsContainer.transform.InverseTransformPoint(vector);
            //AddObjectToDeskServerRpc(player.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
            player.DiscardHeldObject(placeObject: true, deskObjectsContainer, vector, matchRotationOfParent: false);
            Debug.Log("discard held object called from deposit items desk");
        }
    }
}
