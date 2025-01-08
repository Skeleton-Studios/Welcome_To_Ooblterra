using GameNetcodeStuff;
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

    private static readonly WTOBase.WTOLogger Log = new(typeof(FrankensteinBodyPoint), LogSourceType.Room);

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

    public void AttachBody(PlayerControllerB player) {
        if (TableBodyContainer.GetComponentsInChildren<GrabbableObject>().Length > 0 || GameNetworkManager.Instance == null || player != GameNetworkManager.Instance.localPlayerController) {
            return;
        }
        
        Vector3 vector = RoundManager.RandomPointInBounds(triggerCollider.bounds);
        vector.y = triggerCollider.bounds.min.y;
        if (Physics.Raycast(new Ray(vector + Vector3.up * 3f, Vector3.down), out var hitInfo, 8f, 1048640, QueryTriggerInteraction.Collide)) {
            vector = hitInfo.point;
        }
        vector.y += player.currentlyHeldObjectServer.itemProperties.verticalOffset;
        vector = TableBodyContainer.transform.InverseTransformPoint(vector);

        PutObjectOnTableServerRpc(player.currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>());
        player.DiscardHeldObject(placeObject: true, TableBodyContainer, vector, matchRotationOfParent: false);

        Log.Info("Body placed on frankenstein point");
    }

    [ServerRpc(RequireOwnership = false)]
    public void PutObjectOnTableServerRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToTable)) {
                PutObjectOnTableClientRpc(grabbableObjectNetObject);
        } else {
            Log.Error("ServerRpc: Could not find networkobject in the object that was placed on table.");
        }
    }
    [ClientRpc]
    public void PutObjectOnTableClientRpc(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out lastObjectAddedToTable)) {
            lastObjectAddedToTable.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
            BodyGO = lastObjectAddedToTable.gameObject.GetComponentInChildren<GrabbableObject>();
            try {
                PlayerRagdoll = BodyGO as RagdollGrabbableObject;
            } catch {
                Log.Error("Body is not a Ragdoll?!?");
            }
            HasBody = true;
            if (PlayerRagdoll != null) {
                Log.Debug($"Player Body ID is {PlayerRagdoll.bodyID.Value}");
            }
        } else {
            Log.Error("ClientRpc: Could not find networkobject in the object that was placed on table.");
        }
    }
}
