using DunGen;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition.Attributes;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class BatteryRecepticle : NetworkBehaviour {

    [InspectorName("Defaults")]
    public NetworkObject parentTo;
    public NetworkObject BatteryNetObj;
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
    public GameObject BatteryPrefab;

    public Material FrontConsoleMaterial;
    public Material SideConsoleMaterial;
    public MeshRenderer MachineMesh;
    private System.Random MachineRandom;
    private WideDoorway[] Doorways;

    public void Start() {
        scrapShelf = FindFirstObjectByType<ScrapShelf>();
        FindObjectsByType<WideDoorway>(FindObjectsSortMode.None);
        WTOBattery[] BatteryList = FindObjectsOfType<WTOBattery>();
        InsertedBattery = BatteryList.First(x => x.HasCharge == false);
        RecepticleHasBattery = true;
        CenterLight.intensity = 0;
        foreach(MeshRenderer WallLight in WallLights) {
            WallLight.sharedMaterial = WallLightMat;
        }
        MachineRandom = new();
        SpawnBatteryAtFurthestPoint();
        foreach (LightComponent NextLight in GameObject.FindObjectsOfType<LightComponent>().Where(x => x.SetColorByDistance == true)) {
            NextLight.SetColorRelative(this.transform.position);
        }
        Doorways = FindObjectsByType<WideDoorway>(FindObjectsSortMode.None);
    }
    private void Update() {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) {
            return;
        }
        WallLightMat.SetColor("_EmissiveColor", LightColor);
        if (RecepticleHasBattery) {
            BatteryHitbox.enabled = false;
            triggerScript.interactable = false;
            triggerScript.disabledHoverTip = "";
            if (InsertedBattery != null && InsertedBattery.isHeld) {
                InsertedBattery = null;
                RecepticleHasBattery = false;
                return;
            }
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

    private void SpawnBatteryAtFurthestPoint() {
        if (!base.IsServer) {
            return;
        }
        List<RandomMapObject> AllRandomSpawnList = new();
        List<RandomMapObject> ViableSpawnlist = new();
        AllRandomSpawnList.AddRange(FindObjectsOfType<RandomMapObject>());
        float MinSpawnRange = 80f;
        foreach (RandomMapObject BatterySpawn in AllRandomSpawnList.Where(x => x.spawnablePrefabs.Contains(BatteryPrefab))) {
            float SpawnPointDistance = Vector3.Distance(this.transform.position, BatterySpawn.transform.position);
            WTOBase.LogToConsole($"BATTERY DISTANCE: {SpawnPointDistance }");
            if (SpawnPointDistance > MinSpawnRange) {
                ViableSpawnlist.Add(BatterySpawn);
            }
        }
        WTOBase.LogToConsole($"Viable Battery Spawns: {ViableSpawnlist.Count}");
        RandomMapObject ChosenSpawn = ViableSpawnlist[MachineRandom.Next(0, ViableSpawnlist.Count)];
        GameObject NewHazard = Instantiate(BatteryPrefab, ChosenSpawn.transform.position, ChosenSpawn.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
        NewHazard.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
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
        WTOBase.LogToConsole("Placing battery in recepticle");
        Vector3 vector = BatteryTransform.position;
        if (parentTo != null) {
            vector = parentTo.transform.InverseTransformPoint(vector);
        }
        InsertedBattery = (WTOBattery)playerWhoTriggered.currentlyHeldObjectServer;
        
        RecepticleHasBattery = true;
        playerWhoTriggered.DiscardHeldObject(placeObject: true, parentTo, vector);
        InsertedBattery.transform.rotation = BatteryTransform.rotation;
        InsertBatteryServerRpc(InsertedBattery.gameObject.GetComponent<NetworkObject>());
        WTOBase.LogToConsole("discard held object called from placeobject");
        if (InsertedBattery.HasCharge) {
            TurnOnPowerServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void InsertBatteryServerRpc(NetworkObjectReference grabbableObjectNetObject) {
        InsertBatteryClientRpc(grabbableObjectNetObject);
    }
    [ClientRpc]
    public void InsertBatteryClientRpc(NetworkObjectReference grabbableObjectNetObject) {
        InsertBattery(grabbableObjectNetObject);
    }
    public void InsertBattery(NetworkObjectReference grabbableObjectNetObject) {
        if (grabbableObjectNetObject.TryGet(out BatteryNetObj)) {
            BatteryNetObj.gameObject.GetComponentInChildren<GrabbableObject>().EnablePhysics(enable: false);
            InsertedBattery = BatteryNetObj.GetComponentInChildren<WTOBattery>();
        } else {
            WTOBase.WTOLogSource.LogError("BATTERY COULD NOT BE CONVERTED.");
        }
        BatteryNetObj.transform.rotation = BatteryTransform.rotation;
        RecepticleHasBattery = true;
        if (InsertedBattery.HasCharge) {
            InsertedBattery.grabbable = false;
        } else {
            InsertedBattery.grabbable = true;
            InsertedBattery.GetComponent<BoxCollider>().enabled = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TurnOnPowerServerRpc() {
        TurnOnPowerClientRpc();
    }
    [ClientRpc]
    public void TurnOnPowerClientRpc() {
        TurnOnPower();
    } 
    public void TurnOnPower() {
        if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) { 
            Noisemaker.PlayOneShot(FacilityPowerUp);
        }
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
        BatteryNetObj.gameObject.GetComponentInChildren<GrabbableObject>().grabbable = false;
        ManageEnemies();
        foreach(WideDoorway NextDoorway in Doorways) {
            NextDoorway.RaiseDoor();
        }
        
    }

    private void ManageEnemies() {
        EyeSecAI.BuffedByMachineOn = true;
        if(OoblGhostAI.GhostList.Count < 1) {
            RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -1000, 0), 0, 1, MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Oobl Ghost").enemyType);
        }
        RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -1000, 0), 0, 1, MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Oobl Ghost").enemyType);
    }

}
