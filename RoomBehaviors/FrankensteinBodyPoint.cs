using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinBodyPoint : NetworkBehaviour {
    
    [InspectorName("Defaults")]
    public InteractTrigger triggerScript;
    public NetworkObject TableBodyContainer;
    public BoxCollider triggerCollider;

    private float updateInterval;
    private NetworkObject lastObjectAddedToTable;

    [HideInInspector]
    public GrabbableObject BodyGO;
    public RagdollGrabbableObject PlayerRagdoll;
    public Transform RespawnPos;
    public bool HasBody;

    //Figure out how to create an interaction point


    private void Update() {
        if (NetworkManager.Singleton == null) {
            return;
        }
        if (updateInterval > 1f) {
            updateInterval = 0f;
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null) {
                triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is RagdollGrabbableObject;
            }
        } else {
            updateInterval += Time.deltaTime;
        }
    }

    //when interacted, attach the body to the point
    public void AttachBody(PlayerControllerB player) {
        if (TableBodyContainer.GetComponentsInChildren<GrabbableObject>().Length > 0 || GameNetworkManager.Instance == null || player != GameNetworkManager.Instance.localPlayerController) {
            return;
        }
        BodyGO = player.currentlyHeldObjectServer;
        try {
            PlayerRagdoll = BodyGO as RagdollGrabbableObject;
        } catch {
            WTOBase.LogToConsole("Body is not a Ragdoll?!?");
        }
        Vector3 vector = RoundManager.RandomPointInBounds(triggerCollider.bounds);
        vector.y = triggerCollider.bounds.min.y;
        if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out var hitInfo, 8f, 1048640, QueryTriggerInteraction.Collide)) {
            vector = hitInfo.point;
        }
        vector.y += player.currentlyHeldObjectServer.itemProperties.verticalOffset;
        vector = TableBodyContainer.transform.InverseTransformPoint(vector);
        if(PlayerRagdoll != null) { 
            WTOBase.LogToConsole($"Player Body ID is {PlayerRagdoll.bodyID.Value}");
        }
        PutObjectOnTableServerRpc(player.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
        player.DiscardHeldObject(placeObject: true, TableBodyContainer, vector, matchRotationOfParent: false);
        HasBody = true;
        Debug.Log("Body placed on frankenstein point");
    }
    [ServerRpc]
    public void PutObjectOnTableServerRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToTable)) {
                PutObjectOnTableClientRpc(grabbableObjectNetObject);
        } else {
            Debug.LogError("ServerRpc: Could not find networkobject in the object that was placed on table.");
        }
    }

    [ClientRpc]
    public void PutObjectOnTableClientRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToTable)) {
            lastObjectAddedToTable.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
        } else {
            Debug.LogError("ClientRpc: Could not find networkobject in the object that was placed on table.");
        }
    }
}
