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
public class BatteryRecepticle : NetworkBehaviour {

    [InspectorName("Defaults")]
    public NetworkObject parentTo;
    public InteractTrigger triggerScript;
    public Transform BatteryTransform;

    private WTOBattery InsertedBattery;
    private bool RecepticleHasBattery;

    public Animator MachineAnimator;
    public AudioSource Noisemaker;
    public AudioClip FacilityPowerUp;

    private ScrapShelf scrapShelf;


    public void Start() {
        scrapShelf = FindFirstObjectByType<ScrapShelf>();
    }

    private void Update() {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) {
            return;
        }
        if (RecepticleHasBattery) {
            if (InsertedBattery.HasCharge) {
                WTOBase.LogToConsole("Disabling Battery Recepticle Script!");
                triggerScript.enabled = false;
                return;
            }
            if (GameNetworkManager.Instance.localPlayerController.twoHanded || GameNetworkManager.Instance.localPlayerController.FirstEmptyItemSlot() == -1) {
                triggerScript.interactable = false;
                triggerScript.disabledHoverTip = "[Hands Full]";
                return;
            }
            triggerScript.interactable = true;
            triggerScript.hoverTip = "Take Battery";
            return;
        } else {
            if(GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is WTOBattery) {
                triggerScript.interactable = true;
                triggerScript.hoverTip = "Insert Battery";
                return;
            }
            triggerScript.interactable = false;
            triggerScript.disabledHoverTip = "[Requires Battery]";
        }


    }

    public void TryInsertOrRemoveBattery(PlayerControllerB playerWhoTriggered) {
        if (RecepticleHasBattery && !InsertedBattery.HasCharge) {
            playerWhoTriggered.GrabObjectServerRpc(InsertedBattery.NetworkObject);
            RecepticleHasBattery = false;
            return;
        }
        if (!playerWhoTriggered.isHoldingObject || !(playerWhoTriggered.currentlyHeldObjectServer != null)) {
            return;
        }
        Debug.Log("Placing battery in recepticle");
        Vector3 vector = BatteryTransform.position;
        if (parentTo != null) {
            vector = parentTo.transform.InverseTransformPoint(vector);
        }
        InsertedBattery = (WTOBattery)playerWhoTriggered.currentlyHeldObjectServer;
        playerWhoTriggered.DiscardHeldObject(placeObject: true, parentTo, vector);
        Debug.Log("discard held object called from placeobject");
        if(InsertedBattery.HasCharge) {
            TurnOnPowerServerRpc();
        }
    }

    [ServerRpc]
    public void TurnOnPowerServerRpc() {
        TurnOnPowerClientRpc();
    }

    [ClientRpc]
    public void TurnOnPowerClientRpc() {
        TurnOnPower();
    }
    
    public void TurnOnPower() {
        Noisemaker.PlayOneShot(FacilityPowerUp);
        scrapShelf.OpenShelf();
        LightComponent[] LightsInLevel = FindObjectsOfType<LightComponent>();
        foreach (LightComponent light in LightsInLevel) {
            light.SetLightColor();
            light.SetLightBrightness(300);
        }
        MachineAnimator.SetTrigger("PowerOn");
    }
}
