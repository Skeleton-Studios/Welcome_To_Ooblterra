using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition.Attributes;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class BatteryRecepticle : NetworkBehaviour {

    [InspectorName("Defaults")]
    public NetworkObject parentTo;
    public InteractTrigger triggerScript;
    public Transform BatteryTransform;
    public BoxCollider BatteryHitbox;

    private WTOBattery InsertedBattery;
    private bool RecepticleHasBattery;

    public Animator MachineAnimator;
    public AudioSource Noisemaker;
    public AudioClip FacilityPowerUp;
    public AudioSource Pistons;
    public AudioSource MachineAmbience;
    public MeshRenderer[] WallLights;
    public Material WallLightMat;
    public Color LightColor;
    public Light CenterLight;
    private ScrapShelf scrapShelf;

    public Material FrontConsoleMaterial;
    public Material SideConsoleMaterial;
    public MeshRenderer MachineMesh;

    public void Start() {
        scrapShelf = FindFirstObjectByType<ScrapShelf>();
        WTOBattery[] BatteryList = FindObjectsOfType<WTOBattery>();
        InsertedBattery = BatteryList.First(x => x.HasCharge == false);
        RecepticleHasBattery = true;
        CenterLight.intensity = 0;
        foreach(MeshRenderer WallLight in WallLights) {
            WallLight.sharedMaterial = WallLightMat;
        }
    }
    private void Update() {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) {
            return;
        }
        WallLightMat.SetColor("_EmissiveColor", LightColor);
        if (RecepticleHasBattery) {
            /*
            InsertedBattery.EnablePhysics(false);
            InsertedBattery.transform.position = StartOfRound.Instance.propsContainer.InverseTransformPoint(parentTo.transform.InverseTransformPoint(BatteryTransform.position));
            InsertedBattery.transform.rotation = BatteryTransform.rotation;
            */
            BatteryHitbox.enabled = false;
            triggerScript.interactable = false;
            triggerScript.disabledHoverTip = "";
            if (InsertedBattery != null && InsertedBattery.isHeld) {
                InsertedBattery = null;
                RecepticleHasBattery = false;
                return;
            }
            /*
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
            triggerScript.enabled = false;
            triggerScript.hoverTip = "Take Battery";
            return;
            */
        } else {
            BatteryHitbox.enabled = true;
            triggerScript.enabled = true;
            if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is WTOBattery) {
                triggerScript.interactable = true;
                triggerScript.hoverTip = "Insert Battery : [E]";
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
        RecepticleHasBattery = true;
        playerWhoTriggered.DiscardHeldObject(placeObject: true, parentTo, vector);
        InsertedBattery.transform.rotation = BatteryTransform.rotation;
        Debug.Log("discard held object called from placeobject");
        if(InsertedBattery.HasCharge) {
            InsertedBattery.grabbable = false;
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
        MachineAmbience.Play();
        Pistons.Play();
        LightComponent[] LightsInLevel = FindObjectsOfType<LightComponent>();
        foreach (LightComponent light in LightsInLevel) {
            light.SetLightColor();
            light.SetLightBrightness(150);
        }
        Material[] NewMachineMaterials = MachineMesh.materials;
        NewMachineMaterials[2] = SideConsoleMaterial;
        NewMachineMaterials[11] = FrontConsoleMaterial;
        MachineMesh.materials = NewMachineMaterials;
        MachineAnimator.SetTrigger("PowerOn");
        StartRoomLight StartRoomLights = FindObjectOfType<StartRoomLight>();
        StartRoomLights.SetCentralRoomWhite();
    }
}
