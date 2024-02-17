using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Items;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinChemPoint : NetworkBehaviour {

    [InspectorName("Defaults")]
    public NetworkObject parentTo;
    public Collider placeableBounds;
    public InteractTrigger triggerScript;

    public bool hasChemical { get; private set; }
    private int HeldChemicalColorIndex;
    private Chemical HeldChemical;

    private void Update() {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) {
            return;
        }
        if (HeldChemical != null && hasChemical && HeldChemical.isHeld) {
            HeldChemical = null;
            SetChemStateServerRpc(false, -1);
            return;
        }
        if (hasChemical) {
            triggerScript.interactable = false;
            triggerScript.disabledHoverTip = "[Chemical Placed]";
            return;
        }
        triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is Chemical;
        triggerScript.disabledHoverTip = "[No Chemicals in Hand]";

    }

    public Chemical.ChemColor GetCurrentChemicalColor() {
        return (Chemical.ChemColor)HeldChemicalColorIndex;
    }
    public void ClearChemical() {
        HeldChemical.EmptyBeaker();
    }
    public void PlaceObject(PlayerControllerB playerWhoTriggered) {
        if (!playerWhoTriggered.isHoldingObject || !(playerWhoTriggered.currentlyHeldObjectServer != null)) {
            return;
        }
        Debug.Log("Placing object in storage");
        Vector3 vector = itemPlacementPosition(playerWhoTriggered.gameplayCamera.transform, playerWhoTriggered.currentlyHeldObjectServer);
        if(vector == Vector3.zero) {
            return;
        }
        if (parentTo != null) {
            vector = parentTo.transform.InverseTransformPoint(vector);
        }
        HeldChemical = (Chemical)playerWhoTriggered.currentlyHeldObjectServer;
        playerWhoTriggered.DiscardHeldObject(placeObject: true, parentTo, vector, matchRotationOfParent: false);
        Debug.Log("discard held object called from placeobject");
        SetChemStateServerRpc(true, (int)HeldChemical.GetCurrentColor());
    }
    private Vector3 itemPlacementPosition(Transform gameplayCamera, GrabbableObject heldObject) {
        if (!Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out var hitInfo, 7f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) {
            return Vector3.zero;
        }
        if (placeableBounds.bounds.Contains(hitInfo.point)) {
            return hitInfo.point + Vector3.up * heldObject.itemProperties.verticalOffset;
        }
        return placeableBounds.ClosestPoint(hitInfo.point);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetChemStateServerRpc(bool NewState, int NextChemIndex) { 
        SetChemStateClientRpc(NewState, NextChemIndex);
    }
    [ClientRpc]
    private void SetChemStateClientRpc(bool NewState, int NextChemIndex) {
        SetChemState(NewState, NextChemIndex);
    }
    private void SetChemState(bool NewState, int NextChemIndex) {
        hasChemical = NewState;
        HeldChemicalColorIndex = NextChemIndex;
    }
}
