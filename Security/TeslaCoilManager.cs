using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Welcome_To_Ooblterra.Enemies;
using static Welcome_To_Ooblterra.Things.TeslaCoil;

namespace Welcome_To_Ooblterra.Things
{
    /// <summary>
    /// Single manager instance that handles all tesla coils in the level.
    /// Keeps track of what things are in range of tesla coils and applies 
    /// the tesla coil effects.
    /// Multiple things need to be taken into account:
    /// - Objects can be in range of multiple tesla coils at once.
    /// - Tesla coils can be enabled and disabled.
    /// </summary>
    internal class TeslaCoilManager : NetworkBehaviour
    {
        public static TeslaCoilManager Instance => _instance ?? throw new InvalidOperationException(
            "TeslaCoilManager has not been initialised. The manager is created via a StartOfRound.Awake Harmony postfix."
        );

        private static TeslaCoilManager _instance;

        private Dictionary<TeslaCoil, CoilInRangeState> TeslaCoils = new();

        private CoilInRangeState mergedState = new();

        // when local player goes out of range of *all* tesla coils, these items are what get re-enabled.
        private HashSet<GrabbableObject> disabledLocalPlayerItems = new();

        // Grabbable objects that were dropped on the ground and disabled.
        // Only seen by the server.
        // Once they go out of range (i.e. stop appearing in mergedState.grabbableObjectsInRange) they
        // get removed from here.
        private HashSet<GrabbableObject> disabledGrabbableObjects = new();

        private static readonly WTOBase.WTOLogger Log = new(typeof(TeslaCoilManager), LogSourceType.Room);

        private void Awake()
        {
            _instance = this;
        }

        public void AddTeslaCoil(TeslaCoil coil, CoilInRangeState state)
        {
            TeslaCoils[coil] = state;
        }

        public void RemoveTeslaCoil(TeslaCoil coil)
        {
            TeslaCoils.Remove(coil);
        }

        private void CleanBehaviourSet<T>(HashSet<T> objects) where T : UnityEngine.Object
        {
            objects.RemoveWhere(obj => !obj || obj switch
            {
                GrabbableObject grabbable => grabbable.deactivated,
                EyeSecAI eyeSec => eyeSec.isEnemyDead,
                _ => false
            });
        }

        private void CleanBehaviourSets()
        {
            CleanBehaviourSet(disabledLocalPlayerItems);
            CleanBehaviourSet(disabledGrabbableObjects);

            foreach(var coilState in TeslaCoils.Values)
            {
                CleanBehaviourSet(coilState.grabbableObjectsInRange);
                CleanBehaviourSet(coilState.eyeSecInRange);
            }

            // Don't need to clean mergedState as it gets re-set by UpdateMergedState() - don't do unnecessary work.
        }

        private CoilInRangeState UpdateMergedState()
        {
            mergedState.Clear();

            foreach (var coilState in TeslaCoils.Values)
            {
                if (coilState.isEnabled)
                {
                    mergedState.localPlayerInRange |= coilState.localPlayerInRange;
                    mergedState.grabbableObjectsInRange.UnionWith(coilState.grabbableObjectsInRange);
                    mergedState.eyeSecInRange.UnionWith(coilState.eyeSecInRange);
                }
            }

            return mergedState;
        }

        private void Update()
        {
            if(StartOfRound.Instance.inShipPhase)
            {
                // No tesla coils on the ship, so no need to do unnecessary work.
                return;
            }

            // Clean out anything that is no longer valid (destroyed, deactivated, etc) from the sets we are tracking.
            CleanBehaviourSets();

            CoilInRangeState state = UpdateMergedState();

            UpdateGrabbableObjects(state);
            UpdateEyeSec(state);
            UpdateLocalPlayer(state);
        }

        private void UpdateGrabbableObjects(CoilInRangeState state)
        {
            if (!IsServer) 
            {
                return;
            }

            // Do not process items held by the player here, let the client holding
            // the item be the one to process it.
            // If they drop it on the ground though, it will come back through here once a tesla coil 
            // collier picks it up.

            var itemsInRangeNotHeldByPlayer = (
                from item in state.grabbableObjectsInRange
                where item.playerHeldBy == null
                select item
            ).ToHashSet();

            // On server only, pick up any grabbable objects in range of the coil that are
            // in our list of things to disable and disable them.
            foreach (GrabbableObject grabbableObject in itemsInRangeNotHeldByPlayer)
            {
                if(ItemNeedsDisable(grabbableObject))
                {
                    Log.Info($"Disabling item {grabbableObject.name} in influence of coil");
                    uint sendFlags = SetItemEnabled(grabbableObject, -1, false);
                    disabledGrabbableObjects.Add(grabbableObject);
                    SetItemEnabledClientRpc(grabbableObject, -1, false, sendFlags, new ClientRpcParams
                    {
                        // Only send to other clients, do not double call SetItemEnabled on here.
                        Send = WTOBase.AllClientsButHost()
                    });
                }
            }

            var itemsToPossiblyEnable = (
                from item in disabledGrabbableObjects
                where !itemsInRangeNotHeldByPlayer.Contains(item)
                select item
            ).ToList();

            foreach (GrabbableObject grabbableObject in itemsToPossiblyEnable)
            {
                Log.Info($"Re-enabling item {grabbableObject.name} no longer under influence of coil");
                disabledGrabbableObjects.Remove(grabbableObject);

                // If a player has picked up this item, so don't re-enable it.
                // Otherwise we would get a quick enable -> disable as it swaps from
                // being enabled here to being disabled again by the player holding it
                // being in the tesla coil influence.
                if (grabbableObject.playerHeldBy == null)
                {
                    // Not held, so re-enable as we disabled it.
                    uint sendFlags = SetItemEnabled(grabbableObject, -1, true);
                    SetItemEnabledClientRpc(grabbableObject, -1, true, sendFlags, new ClientRpcParams 
                    {
                        // Only send to other clients, do not double call SetItemEnabled on here.
                        Send = WTOBase.AllClientsButHost()
                    });
                }
            }
        }

        private void UpdateEyeSec(CoilInRangeState state)
        {
            foreach (EyeSecAI eyeSec in EyeSecAI.EyeSecList.Values)
            {
                eyeSec.BuffedByTeslaCoil = state.eyeSecInRange.Contains(eyeSec);
            }
        }

        private HashSet<GrabbableObject> GetPlayerItems(PlayerControllerB player)
        {
            HashSet<GrabbableObject> items = new();

            if(player == null)
            {
                // Extremely unlikely in practice, but let's avoid a null reference
                // just in case.
                return items;
            }

            foreach (GrabbableObject item in player.ItemSlots)
            {
                if (item != null)
                {
                    items.Add(item);
                }
            }

            if (player.ItemOnlySlot != null)
            {
                items.Add(player.ItemOnlySlot);
            }

            return items;
        }

        private void UpdateLocalPlayer(CoilInRangeState state)
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            int playerId = WTOBase.PlayerIndex(localPlayer);

            if (!state.localPlayerInRange)
            {
                // Local player no longer under influence of any tesla coil, re-enable any items that were
                // forcefully disabled when they were in range.
                // Note: Server does not check this, so it's fully client side.
                foreach (GrabbableObject item in disabledLocalPlayerItems)
                {
                    uint sendFlags = SetItemEnabled(item, playerId, true);
                    SetItemEnabledServerRpc(item, playerId, true, sendFlags);
                }

                // Drain disabled items now that they are re-enabled.
                disabledLocalPlayerItems.Clear();
                return;
            }

            // Player in influence of at least one coil - disable any items as applicable.
            HashSet<GrabbableObject> currentPlayerItems = GetPlayerItems(localPlayer);

            foreach (GrabbableObject item in currentPlayerItems)
            {
                if (ItemNeedsDisable(item))
                {
                    // disable on this client first
                    uint sendFlags = SetItemEnabled(item, playerId, false);
                    // mark it as an item that was disabled so it can be re-enabled later when the player leaves the influence of the tesla coil.
                    disabledLocalPlayerItems.Add(item);
                    // disable for other clients (almost all the items sync their state to other clients, so we need to also do this)
                    SetItemEnabledServerRpc(item, playerId, false, sendFlags);
                }
            }

            var itemsPlayerIsNoLongerHolding = (
                from item in disabledLocalPlayerItems
                where !currentPlayerItems.Contains(item)
                select item
            ).ToList();

            foreach (GrabbableObject item in itemsPlayerIsNoLongerHolding)
            {
                // Player dropped this item so we are not tracking it to re-enable anymore.
                disabledLocalPlayerItems.Remove(item);
            }
        }

        private bool ItemIsEnabled(GrabbableObject item)
        {
            return item switch
            {
                WalkieTalkie walkie => walkie.isBeingUsed,
                FlashlightItem flashlight => flashlight.isBeingUsed,
                BoomboxItem boombox => boombox.isPlayingMusic || boombox.isBeingUsed,
                RadarBoosterItem radarBooster => radarBooster.radarEnabled,
                _ => false
            };
        }

        private bool ItemNeedsDisable(GrabbableObject item)
        {
            if(ItemIsEnabled(item))
            {
                return true;
            }

            return item switch
            {
                PatcherTool patcher => patcher.isBeingUsed,
                // Shotgun: only the actively held (non-pocketed) gun, once per stay in range.
                // Re-enable does nothing for it; tracking just prevents repeat fire attempts.
                ShotgunItem shotgun => shotgun.isHeld && !shotgun.isPocketed && !shotgun.safetyOn &&
                    !disabledLocalPlayerItems.Contains(shotgun),
                _ => false
            };
        }

        const int SEND_FLAG_CLIENT_WAS_USING_WALKIE = 1 << 0;

        /// <summary>
        /// Chance that an actively held, safety-off shotgun fires when entering coil influence.
        /// Still tracked afterward so we do not re-roll every frame.
        /// </summary>
        const float SHOTGUN_FIRE_CHANCE = 0.4f;

        private uint SetItemEnabled(GrabbableObject item, int playerId, bool enabled, uint sendFlags = 0)
        {
            // Only relevant for some items to prevent double enable/disable calls.
            // LC does not handle a double call to enable as a no-op, so we need to
            // do that check outself..
            bool enabledStateChanged = ItemIsEnabled(item) != enabled;

            switch (item)
            {
                case WalkieTalkie walkie:
                    if(enabledStateChanged)
                    {
                        walkie.SwitchWalkieTalkieOn(enabled);

                        if (!enabled)
                        {
                            // Manage playing the walkie die sound both on the client that is disabling the walkie, 
                            // and over the network on other clients that receive the network disable command.
                            // A sendFlags is used as a return value from the client who owns the walkie to tell other clients
                            // if they were talking or not when the walkie was disabled.
                            bool isOnOwningPlayer = WTOBase.PlayerIndex(StartOfRound.Instance.localPlayerController) == playerId;
                            bool isOwningPlayerTalking = isOnOwningPlayer && walkie.clientIsHoldingAndSpeakingIntoThis;
                            if (((sendFlags & SEND_FLAG_CLIENT_WAS_USING_WALKIE) != 0 && playerId != -1) || isOwningPlayerTalking)
                            {
                                walkie.BroadcastSFXFromWalkieTalkie(walkie.playerDieOnWalkieTalkieSFX, playerId);
                            }

                            // If the player was talking into the walkie, send the walkie die sound
                            // to the other clients.
                            if (isOwningPlayerTalking)
                            {
                                return SEND_FLAG_CLIENT_WAS_USING_WALKIE;
                            }
                        }
                    }
                    break;

                case FlashlightItem flashlight:
                    if (enabledStateChanged)
                    {
                        flashlight.SwitchFlashlight(enabled);
                    }
                    break;

                case BoomboxItem boombox:
                    if(enabledStateChanged)
                    {
                        boombox.StartMusic(enabled);  
                    }
                    break;

                case RadarBoosterItem radarBooster:
                    // LC turns radar boosters off when pocketed; do not fight that on coil exit.
                    if (enabled && radarBooster.isPocketed)
                    {
                        break;
                    }
                    if(enabledStateChanged)
                    {
                        radarBooster.EnableRadarBooster(enabled);
                    }
                    break;

                case ShotgunItem shotgun:
                    // Held-only (not pocketed / not ground). Chance roll; tracking in
                    // disabledLocalPlayerItems prevents re-rolls while still in range.
                    if (!enabled && shotgun.isHeld && !shotgun.isPocketed &&
                        UnityEngine.Random.value < SHOTGUN_FIRE_CHANCE)
                    {
                        shotgun.ItemActivate(true);
                    }
                    break;

                case PatcherTool patcher:
                    // Just turn it off, but no action to turn back on (needs a new target)
                    // This is already a no-op in LC if called twice.
                    if (!enabled)
                    {
                        patcher.DisablePatcherGun();
                    }
                    break;
            }

            return 0;
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetItemEnabledServerRpc(NetworkBehaviourReference item, int playerId, bool enabled, uint sendFlags, ServerRpcParams serverParams = default)
        {
            SetItemEnabledClientRpc(item, playerId, enabled, sendFlags, new ClientRpcParams()
            {
                // No need to send back to the client that just sent this..
                Send = WTOBase.AllClientsButSender(serverParams)
            });
        }

        [ClientRpc]
        private void SetItemEnabledClientRpc(NetworkBehaviourReference itemRef, int playerId, bool enabled, uint sendFlags, ClientRpcParams clientRpcParams = default)
        {
            if (itemRef.TryGet(out GrabbableObject item) && (enabled || ItemNeedsDisable(item)))
            {
                SetItemEnabled(item, playerId, enabled, sendFlags);
            }
        }
    }
}
