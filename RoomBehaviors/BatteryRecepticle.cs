using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    public class BatteryRecepticle : NetworkBehaviour 
    {

        [InspectorName("Defaults")]
        public InteractTrigger triggerScript;
        // Position that the battery will go
        public Transform BatteryTransform;
        // Position to spawn the scrap shelf at
        public Transform ScrapShelfTransform;

        public BoxCollider BatteryHitbox;

        private WTOBattery? InsertedBattery = null;

        public Animator MachineAnimator;
        public AudioSource Noisemaker;
        public AudioClip FacilityPowerUp;
        public AudioSource Pistons;
        public AudioSource MachineAmbience;
        public MeshRenderer[] WallLights;
        public Material WallLightMat;
        public Color LightColor;
        public Light CenterLight;

        // Prefab used to spawn the charged battery somewhere around the place.
        public GameObject ChargedBatteryPrefab;
        // Prefab used to spawn the drained battery in the recepticle.
        public GameObject DrainedBatteryPrefab;

        // Prefab used to spawn the scrap shelf
        public GameObject ScrapShelfPrefab;
        private ScrapShelf? SpawnedScrapShelf = null;

        // LethalCompany needs an object to parent parent the battery to when it's inserted by the player, 
        // and this object needs a NetworkObject.
        // We can't use the root of this machine for this, as we want to parent and match the parent rotation.
        public GameObject BatteryRecepticleTransformPrefab;
        private NetworkObject? SpawnedBatteryRecepticleTransform = null;

        public Material FrontConsoleMaterial;
        public Material SideConsoleMaterial;
        public MeshRenderer MachineMesh;
        private WideDoorway[] Doorways;

        private bool sentBatteryIsHeldMessage = false;

        private static readonly WTOBase.WTOLogger Log = new(typeof(BatteryRecepticle), LogSourceType.Room);

        public void Start() 
        {
            Doorways = FindObjectsByType<WideDoorway>(FindObjectsSortMode.None);
            CenterLight.intensity = 0;
            foreach(MeshRenderer WallLight in WallLights) 
            {
                WallLight.sharedMaterial = WallLightMat;
            }
            foreach (LightComponent NextLight in GameObject.FindObjectsOfType<LightComponent>().Where(x => x.SetColorByDistance == true))
            {
                NextLight.SetColorRelative(this.transform.position);
            }

            SpawnBatteryObjects();
        }

        private void Update() 
        {
            // Only need to run this on the client
            if(GameNetworkManager.Instance == null || !IsClient || GameNetworkManager.Instance.localPlayerController == null)
            {
                return;
            }

            WallLightMat.SetColor("_EmissiveColor", LightColor);

            UpdateClientBatteryState();
        }

        private void UpdateClientBatteryState()
        {
            if (InsertedBattery == null)
            {
                BatteryHitbox.enabled = true;
                triggerScript.enabled = true;
                if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer is WTOBattery)
                {
                    // Player is currently holding a battery
                    triggerScript.interactable = true;
                    triggerScript.hoverTip = "Insert Battery : [E]";
                    return;
                }
                // Player is not holding a battery. Could be some other shit they have or nothing at all.
                triggerScript.interactable = false;
                triggerScript.disabledHoverTip = "[Requires Battery]";
                return;
            }

            BatteryHitbox.enabled = false;
            triggerScript.interactable = false;
            triggerScript.disabledHoverTip = "";

            if (InsertedBattery.isHeld && !sentBatteryIsHeldMessage)
            {
                // Battery has been picked up by this client, so we set the
                // inserted battery to null.
                sentBatteryIsHeldMessage = true;
                SetInsertedBattery(null);
            }
        }

        private RandomMapObject? FindChargedBatterySpawn()
        {
            List<RandomMapObject> AllRandomSpawnList = new();
            List<RandomMapObject> ViableSpawnlist = new();
            AllRandomSpawnList.AddRange(FindObjectsOfType<RandomMapObject>());
            float MinSpawnRange = 80f;
            foreach (RandomMapObject BatterySpawn in AllRandomSpawnList.Where(x => x.spawnablePrefabs.Contains(ChargedBatteryPrefab)))
            {
                float SpawnPointDistance = Vector3.Distance(this.transform.position, BatterySpawn.transform.position);
                Log.Debug($"BATTERY DISTANCE: {SpawnPointDistance}");
                if (SpawnPointDistance > MinSpawnRange)
                {
                    ViableSpawnlist.Add(BatterySpawn);
                }
            }
            Log.Debug($"Viable Battery Spawns: {ViableSpawnlist.Count}");
            if (ViableSpawnlist.Count == 0)
            {
                Log.Error("NO VIABLE SPAWNS FOR BATTERY FOUND!");
                return null;
            }
            System.Random MachineRandom = new();
            return ViableSpawnlist[MachineRandom.Next(0, ViableSpawnlist.Count)];
        }

        private void SpawnBatteryObjects() 
        {
            if (!IsServer) 
            {
                return;
            }

            // Recepticle transform to insert the battery into
            {
                GameObject BatteryRecepticleTransform = Instantiate(BatteryRecepticleTransformPrefab, BatteryTransform.position, BatteryTransform.rotation, transform);
                RoundManager.Instance.spawnedSyncedObjects.Add(BatteryRecepticleTransform);
                SpawnedBatteryRecepticleTransform = BatteryRecepticleTransform.GetComponent<NetworkObject>();
                SpawnedBatteryRecepticleTransform.Spawn(destroyWithScene: true);
            }

            // Spawn the initial drained battery in the recepticle
            {
                GameObject DrainedBattery = Instantiate(DrainedBatteryPrefab, BatteryTransform.position, BatteryTransform.rotation, SpawnedBatteryRecepticleTransform.transform);
                RoundManager.Instance.spawnedSyncedObjects.Add(DrainedBattery);
                DrainedBattery.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                SetInsertedBattery(DrainedBattery.GetComponent<WTOBattery>());
            }

            // Spawn the scrap shelf
            {
                GameObject ScrapShelf = Instantiate(ScrapShelfPrefab, ScrapShelfTransform.position, ScrapShelfTransform.rotation, RoundManager.Instance.mapPropsContainer.transform);
                RoundManager.Instance.spawnedSyncedObjects.Add(ScrapShelf);
                SpawnedScrapShelf = ScrapShelf.GetComponent<ScrapShelf>();
                ScrapShelf.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
            }

            // Spawn a charged battery somewhere in the level for the player to find and insert.
            {
                RandomMapObject? spawn = FindChargedBatterySpawn();
                if (spawn != null)
                {
                    GameObject ChargedBattery = Instantiate(ChargedBatteryPrefab, spawn.transform.position, spawn.transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
                    RoundManager.Instance.spawnedSyncedObjects.Add(ChargedBattery);
                    ChargedBattery.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                }
            }

            // Ensure the client has these references
            SyncSpawnedObjectsClientRpc(SpawnedBatteryRecepticleTransform, SpawnedScrapShelf.GetComponent<NetworkObject>());
        }

        public void TryInsertOrRemoveBattery(PlayerControllerB playerWhoTriggered) 
        {
            if(!IsClient)
            {
                Log.Warning("TryInsertOrRemoveBattery must only be called on the client");
                return;
            }

            Log.Info("Player triggered battery recepticle " + gameObject.GetInstanceID());

            if (InsertedBattery != null && !InsertedBattery.HasCharge) {
                playerWhoTriggered.GrabObjectServerRpc(InsertedBattery.NetworkObject);
                SetInsertedBattery(null);
                return;
            }

            if (!playerWhoTriggered.isHoldingObject || !(playerWhoTriggered.currentlyHeldObjectServer != null)) 
            {
                return;
            }

            if(!playerWhoTriggered.currentlyHeldObjectServer.TryGetComponent<WTOBattery>(out var batteryInHand)) 
            {
                // Not holding a battery somehow
                return;
            }

            Log.Info("Placing battery in recepticle");
            if(SpawnedBatteryRecepticleTransform == null)
            {
                Log.Error("SpawnedBatteryRecepticleTransform is null in TryInsertOrRemoveBattery. This should never happen.");
                return;
            }
            playerWhoTriggered.DiscardHeldObject(placeObject: true, SpawnedBatteryRecepticleTransform);
            SetInsertedBattery(batteryInHand);
        }

        [ClientRpc]
        private void SyncSpawnedObjectsClientRpc(NetworkObjectReference batteryRecepticleTransformRef, NetworkObjectReference scrapShelfRef)
        {
            // client needs to know about these objects so it can refer to them correctly.

            if (batteryRecepticleTransformRef.TryGet(out var batteryRecepticleTransform))
            {
                SpawnedBatteryRecepticleTransform = batteryRecepticleTransform.GetComponent<NetworkObject>();
            }
            else
            { 
                Log.Error("Failed to get NetworkObject from SpawnedBatteryRecepticleTransform in SyncSpawnedObjectsClientRpc. This should never happen.");
            }

            if (scrapShelfRef.TryGet(out var scrapShelf))
            {
                SpawnedScrapShelf = scrapShelf.GetComponent<ScrapShelf>();
            }
            else
            {
                Log.Error("Failed to get ScrapShelf from SpawnedScrapShelf in SyncSpawnedObjectsClientRpc. This should never happen.");
            }

        }

        [ServerRpc(RequireOwnership = false)]
        private void InsertBatteryServerRpc(NetworkObjectReference batteryNetObject, bool isNull, ServerRpcParams rpcParams = default) 
        {
            InsertBatteryClientRpc(batteryNetObject, isNull, new ClientRpcParams()
            {
                Send = WTOBase.AllClientsButSender(rpcParams)
            });
        }

        [ClientRpc]
        private void InsertBatteryClientRpc(NetworkObjectReference batteryNetObject, bool isNull, ClientRpcParams rpcParams = default) 
        {
            if(isNull)
            {
                // can't send a null NetworkObjectReference, so we send a bool to indicate that the battery should be cleared instead.
                UpdateInsertedBatteryStateOnClient(null);
                return;
            }

            if (!batteryNetObject.TryGet(out var networkObject))
            {
                Log.Error("Failed to get NetworkObject from NetworkObjectReference in InsertBatteryClientRpc. This should never happen.");
                return;
            }

            if (!networkObject.TryGetComponent<WTOBattery>(out var battery))
            {
                Log.Error("NetworkObject passed to InsertBatteryClientRpc did not have a WTOBattery component. This should never happen.");
                return;
            }
               
            UpdateInsertedBatteryStateOnClient(battery);
        }

        private void UpdateInsertedBatteryStateOnClient(WTOBattery? battery)
        {
            InsertedBattery = battery;

            if(InsertedBattery != null)
            {
                sentBatteryIsHeldMessage = false;
                InsertedBattery.EnablePhysics(false);

                if(InsertedBattery.HasCharge)
                {
                    InsertedBattery.grabbable = false;
                    TurnOnPower();
                }
                else
                {
                    InsertedBattery.grabbable = true;
                    InsertedBattery.GetComponent<BoxCollider>().enabled = true;
                }
            }
        }

        /// <summary>
        /// Main entrypoint to setting the inserted battery state.
        /// Can be called from server or client and it will do the right thing.
        /// </summary>
        /// <param name="battery"></param>
        private void SetInsertedBattery(WTOBattery? battery)
        {
            // Can't send a null object reference, so we send this object's network object instead, and a boolean
            // to indicate that the battery should be cleared.
            NetworkObject referenceToSend = battery == null ? GetComponent<NetworkObject>() : battery.GetComponent<NetworkObject>();
            if (IsClient)
            {
                // If client is calling, then set immediately and use server to broadcast to all
                UpdateInsertedBatteryStateOnClient(battery);
                InsertBatteryServerRpc(referenceToSend, battery == null);
            }
            else
            {
                // If server is calling, then broadcast to clients
                InsertBatteryClientRpc(referenceToSend, battery == null);
            }
        }

        private void TurnOnPower() 
        {
            if (GameNetworkManager.Instance.localPlayerController.isInsideFactory) 
            { 
                Noisemaker.PlayOneShot(FacilityPowerUp);
            }
            SpawnedScrapShelf.OpenShelf();
            MachineAmbience.Play();
            Pistons.Play();
            LightComponent[] LightsInLevel = FindObjectsOfType<LightComponent>();
            foreach (LightComponent light in LightsInLevel) 
            {
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
            ManageEnemies();
            foreach(WideDoorway NextDoorway in Doorways) 
            {
                NextDoorway.RaiseDoor();
            }
        }

        private void ManageEnemies() 
        {
            EyeSecAI.BuffedByMachineOn = true;
            if(OoblGhostAI.GhostList.Count < 1) 
            {
                RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -1000, 0), 0, 1, MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Oobl Ghost").enemyType);
            }
            RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -1000, 0), 0, 1, MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Oobl Ghost").enemyType);
        }

    }
}
