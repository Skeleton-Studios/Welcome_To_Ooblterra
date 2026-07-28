using GameNetcodeStuff;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Items;

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

        public Item BatteryItem;

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

            if(IsServer)
            {
                SpawnBatteryObjects();
            }
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
            var spawns = (
                from s in FindObjectsOfType<RandomMapObject>()
                where s.spawnablePrefabs.Contains(BatteryItem.spawnPrefab) && Vector3.Distance(transform.position, s.transform.position) > 80f
                select s
            ).ToList();

            Log.Debug($"Viable Battery Spawns: {spawns.Count}");
            if (spawns.Count == 0)
            {
                Log.Error("NO VIABLE SPAWNS FOR BATTERY FOUND!");
                return null;
            }

            System.Random MachineRandom = new(StartOfRound.Instance.randomMapSeed);
            return spawns[MachineRandom.Next(0, spawns.Count)];
        }

        private int CalculateBatteryScrapValue(bool charged)
        {
            // Battery does not get spawned in the same way that other scrap does, so we need to calculate its scrap value here
            // and distribute it to all clients. 
            if (!charged)
            {
                // Non charged battery uses a fixed low number
                // 225 / 8 = 28.125, which is close to the fixed 30 the old code used, but this can be somewhat controlled by client mods that change minValue.
                return BatteryItem.minValue / 8;
            }

            // This calc is copied from LC code, so could need updating if LC changes the calculation method.
            return (int)(RoundManager.Instance.AnomalyRandom.Next(BatteryItem.minValue, BatteryItem.maxValue) * RoundManager.Instance.scrapValueMultiplier);
        }

        private WTOBattery SpawnBattery(bool charged, Transform transform, Transform parent)
        {
            GameObject battery = Instantiate(BatteryItem.spawnPrefab, transform.position, transform.rotation, parent);
            WTOBattery batteryBehaviour = battery.GetComponent<WTOBattery>();
            batteryBehaviour.HasCharge = charged;
            RoundManager.Instance.spawnedSyncedObjects.Add(battery);
            battery.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);

            SetBatteryScrapValueClientRpc(batteryBehaviour, CalculateBatteryScrapValue(charged));

            return batteryBehaviour;
        }

        [ClientRpc]
        void SetBatteryScrapValueClientRpc(NetworkBehaviourReference networkBehaviour, int scrapValue)
        {
            if(networkBehaviour.TryGet(out WTOBattery battery))
            {
                battery.SetScrapValue(scrapValue);
            }
            else
            {
                Log.Error("Failed to get WTOBattery from NetworkBehaviourReference in SetBatteryScrapValueClientRpc. This should never happen.");
            }
        }

        private void SpawnBatteryObjects() 
        {
            // Recepticle transform to insert the battery into
            {
                GameObject BatteryRecepticleTransform = Instantiate(BatteryRecepticleTransformPrefab, BatteryTransform.position, BatteryTransform.rotation, transform);
                RoundManager.Instance.spawnedSyncedObjects.Add(BatteryRecepticleTransform);
                SpawnedBatteryRecepticleTransform = BatteryRecepticleTransform.GetComponent<NetworkObject>();
                SpawnedBatteryRecepticleTransform.Spawn(destroyWithScene: true);
            }

            // Spawn the initial drained battery in the recepticle
            SetInsertedBattery(SpawnBattery(false, BatteryTransform, SpawnedBatteryRecepticleTransform.transform));

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
                    Log.Info("Spawning charged battery at " + spawn.transform.position);
                    SpawnBattery(true, spawn.transform, RoundManager.Instance.mapPropsContainer.transform);
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
                try
                {
                    UpdateInsertedBatteryStateOnClient(battery);
                }
                catch (System.Exception ex)
                {
                    Log.Error("Error in SetInsertedBattery when client is calling: " + ex);
                }
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
        }
    }
}
