﻿using GameNetcodeStuff;
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
    public NetworkObject parentTo;
    public Collider placeableBounds;
    public InteractTrigger triggerScript;

    private bool hasChemical;
    private Chemical HeldChemical;
    private float checkHoverTipInterval;

    private void Update() {
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null) {
            if (hasChemical && HeldChemical.heldByPlayerOnServer) {
                hasChemical = false;
                HeldChemical = null;
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
        hasChemical = true;
    }

    private Vector3 itemPlacementPosition(Transform gameplayCamera, GrabbableObject heldObject) {
        if (Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out var hitInfo, 7f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) {
            if (placeableBounds.bounds.Contains(hitInfo.point)) {
                return hitInfo.point + Vector3.up * heldObject.itemProperties.verticalOffset;
            }
            return placeableBounds.ClosestPoint(hitInfo.point);
        }
        return Vector3.zero;
    }

    public override void __initializeVariables() {
        base.__initializeVariables();
    }

    public override string __getTypeName() {
        return "PlaceableObjectsSurface";
    }
}
