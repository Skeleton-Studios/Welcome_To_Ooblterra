using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinChemPoint : NetworkBehaviour {

    [InspectorName("Defaults")]
    public InteractTrigger triggerScript;
    public NetworkObject ChemContainer;
    public BoxCollider triggerCollider;
    public FrankensteinTerminal ConnectedTerminal;

    private bool HasChem = false;
    private float updateInterval;
    private NetworkObject lastObjectAddedToChemPoint;

    private void Awake() {
        ConnectedTerminal = FindObjectOfType<FrankensteinTerminal>();
    }
    private void Update() {
        if (NetworkManager.Singleton == null) {
            return;
        }
        if (ConnectedTerminal == null) {
            WTOBase.LogToConsole("Connected terminal null!");
            ConnectedTerminal = FindObjectOfType<FrankensteinTerminal>();
        }
        if (updateInterval > 1f) {
            updateInterval = 0f;
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null) {
                triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is GrabbableObject;
            }
        } else {
            updateInterval += Time.deltaTime;
        }
    }

    //when interacted, attach the body to the point
    public void AttachChem(PlayerControllerB player) {
        if (ChemContainer.GetComponentsInChildren<GrabbableObject>().Length > 0 || GameNetworkManager.Instance == null || player != GameNetworkManager.Instance.localPlayerController) {
            return;
        }

        Vector3 vector = RoundManager.RandomPointInBounds(triggerCollider.bounds);
        vector.y = triggerCollider.bounds.min.y;
        if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out var hitInfo, 8f, 1048640, QueryTriggerInteraction.Collide)) {
            vector = hitInfo.point;
        }
        vector.y += player.currentlyHeldObjectServer.itemProperties.verticalOffset;
        vector = ChemContainer.transform.InverseTransformPoint(vector);
        //PutObjectOnTableServerRpc(player.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
        lastObjectAddedToChemPoint = player.currentlyHeldObjectServer.GetComponent<NetworkObject>();
        lastObjectAddedToChemPoint.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(false);
        player.DiscardHeldObject(placeObject: true, ChemContainer, vector, matchRotationOfParent: false);

        HasChem = true;
        Debug.Log("Chemical Placed");
        return;
    }
    [ServerRpc]
    public void PutObjectOnTableServerRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToChemPoint)) {
            PutObjectOnTableClientRpc(grabbableObjectNetObject);
        } else {
            Debug.LogError("ServerRpc: Could not find networkobject in the object that was placed on table.");
        }
    }

    [ClientRpc]
    public void PutObjectOnTableClientRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToChemPoint)) {
            lastObjectAddedToChemPoint.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
        } else {
            Debug.LogError("ClientRpc: Could not find networkobject in the object that was placed on table.");
        }
    }
}
