using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    public class BearTrap : NetworkBehaviour, IHittable {

        public int DamageAmount = 5;

        // Random range for how long it will take for the bear trap to
        // become re-enabled after being hit.
        public float RecoveryTimeRangeMin = 5f;
        public float RecoveryTimeRangeMax = 10f;
        
        public float InitialSpawnTimeRangeMin = 200f;
        public float InitialSpawnTimeRangeMax = 400f;

        public Animator BearTrapAnim;
        public AudioClip CloseSound;

        // Incremented per trap spawned so all the traps don't use
        // the same start of round seed.
        static int BearTrapCount = 0;

        // Server side variables
        private System.Random Random;
        private readonly HashSet<PlayerControllerB> PlayerInRangeList = new();

        private bool Raised
        {
            get => BearTrapAnim.GetBool("IsRaised");
            set => BearTrapAnim.SetBool("IsRaised", value);
        }

        private bool Closed
        {
            get => BearTrapAnim.GetBool("CloseTrap");
            set => BearTrapAnim.SetBool("CloseTrap", value);
        }

        // Client side class for tracking if multiple traps are all trapping a player at once
        // and when they're fully freed from all traps.
        class ClientTrappedEntry(float originalMoveSpeed, float originalJumpForce)
        {
            private float originalMoveSpeed = originalMoveSpeed;
            private float originalJumpForce = originalJumpForce;
            int count = 1;

            public void Increment()
            {
                count++;
            }

            public bool Decrement()
            {
                count--;
                return count == 0;
            }

            public void RestorePlayer(PlayerControllerB player)
            {
                player.movementSpeed = originalMoveSpeed;
                player.jumpForce = originalJumpForce;
            }
        }
        
        // Client side variables
        private Coroutine? TrappedClientDamageCoroutine = null;

        private float TrappedClientDamageTimer = 0.5f;

        // Tracking across multiple bear traps trapping the player at once
        private static ClientTrappedEntry? SharedTrappedClientEntry = null;

        private static readonly WTOBase.WTOLogger Log = new(typeof(BearTrap), LogSourceType.Thing);

        private static float RandomFloatInRange(System.Random random, float minValue, float maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }

        private void Start() 
        {
            // Ensure different seed per trap or they all do the same thing
            Random = new System.Random(StartOfRound.Instance.randomMapSeed + BearTrapCount++);

            if(IsServer)
            {
                MoveToStartingLocation();

                // Start with trap underground and choose a random initial time to come up
                Raised = false;
                Closed = false;

                // Come back up after random time - sets raised to true once finished.
                float recoveryTime = RandomFloatInRange(Random, InitialSpawnTimeRangeMin, InitialSpawnTimeRangeMax);
                StartCoroutine(RaiseTrapCoroutine(recoveryTime));
            }
        }

        private void MoveToStartingLocation()
        {
            float randomYaw = RandomFloatInRange(Random, 0f, 360f);

            // We want the bear trap to sit properly on the ground facing up - so we do 4 traces at its corners to find the average ground height and normal, and then move/rotate the trap accordingly.
            // Presume a size of 1x1 for the trap, and a max trace distance of 5.
            Vector3[] traceStartOffsets = [
                new Vector3(0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(-0.5f, 0, -0.5f)
            ];

            Vector3 averageHitPoint = Vector3.zero;
            Vector3 averageHitNormal = Vector3.zero;
            int count = 0;

            foreach (var offset in traceStartOffsets)
            {
                Vector3 traceStart = transform.position + offset;
                var hits = Physics.RaycastAll(traceStart, Vector3.down, 3f);
                foreach(var hit in hits.OrderBy(h => h.distance))
                {
                    if(hit.collider.gameObject != gameObject)
                    {
                        // Process this as a surface for the trap to sit on
                        averageHitPoint += hit.point;
                        averageHitNormal += hit.normal;
                        count++;
                        break;
                    }
                }
            }

            if(count == 0)
            {
                Log.Warning("Bear Trap couldn't find any surfaces below it to position itself on, falling back to nav mesh position");

                var hit = Utils.GetRandomNavMeshPositionInRadiusExtended(transform.position);
                if(!hit.HasValue)
                {
                    Log.Error("Bear Trap couldn't find any nav mesh position to position itself on either, leaving at original position");
                    return;
                }

                transform.position = hit.Value.position;
                transform.rotation = Quaternion.Euler(0, randomYaw, 0);

                return;
            }

            averageHitPoint /= count;
            averageHitNormal.Normalize();

            // Move the trap to the average hit point, and rotate it to align with the average hit normal.
            transform.position = averageHitPoint;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, averageHitNormal) * Quaternion.Euler(0, randomYaw, 0);
        }

        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID) 
        {
            Debug.Log($"Bear trap was hit on the {(IsServer ? "server" : "client")} with force {force}");

            // Process only on server (re-calls this function)
            if (IsClient)
            {
                OnHitServerRpc();
            }
            else
            {
                ProcessHit();
            }

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnHitServerRpc()
        {
            ProcessHit();
        }

        private void ProcessHit()
        {
            if(!Raised)
            {
                // Exit early if already disabled from being hit
                return;
            }

            // Go back down and hide again
            Raised = false;
            ReleaseAllVictims();

            // Come back up after recovery time - re-sets raised to true once finished.
            float recoveryTime = RandomFloatInRange(Random, RecoveryTimeRangeMin, RecoveryTimeRangeMax);
            StartCoroutine(RaiseTrapCoroutine(recoveryTime));
        }

        public void OnTriggerStay(Collider other) 
        {
            // Using OnTriggerStay to repeatedly check for when the trap becomes re-enabled
            // after the hit timeout expires.

            if(!IsServer)
            {
                return;
            }

            if(!Raised || PlayerInRangeList.Count > 0)
            {
                // Trap is not raised yet, or is already eating a player, so ignore
                return;
            }

            if (other.gameObject.TryGetComponent<PlayerControllerB>(out var PlayerInRange) && PlayerInRangeList.Add(PlayerInRange))
            {
                Log.Debug($"Bear Trap: Adding Player {PlayerInRange} to player in range list...");
                Closed = true;
                OnClientTrappedClientRpc(WTOBase.PlayerIndex(PlayerInRange));
            }
        }

        [ClientRpc]
        private void OnClientTrappedClientRpc(int clientIndex) 
        {
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            // we use -1 in this case to mean 'all players'
            if (clientIndex == -1 || clientIndex == WTOBase.PlayerIndex(localPlayer))
            {
                // Snapshot the original values here in case some other mod 
                // also modified them.
                // Best effort attempt since we can't really know what other mods might
                // be doing to the movement speed.
                TrappedClientDamageTimer = 0.5f; // reset on retrap
                if(SharedTrappedClientEntry == null)
                {
                    // create initial entry for trapped player since one does not exist from
                    // another trap
                    SharedTrappedClientEntry = new ClientTrappedEntry(localPlayer.movementSpeed, localPlayer.jumpForce);
                }
                else
                {
                    // increment existing ref count
                    SharedTrappedClientEntry.Increment();
                }
                localPlayer.movementSpeed = 0.4f;
                localPlayer.jumpForce = 1;

                // Start dealing damage to local trapped player
                if(TrappedClientDamageCoroutine != null)
                {
                    Log.Warning("Client trap damage coroutine was somehow already running on re-trap");
                }
                else
                {
                    TrappedClientDamageCoroutine = StartCoroutine(DamageLocalPlayerCoroutine());
                }
            }
            
            // Always play trap sound on all clients
            GetComponent<AudioSource>().PlayOneShot(CloseSound);
        }

        public void OnTriggerExit(Collider other) 
        {
            if(!IsServer)
            {
                return;
            }

            if(other.gameObject.TryGetComponent<PlayerControllerB>(out var PlayerInRange) && PlayerInRangeList.Remove(PlayerInRange))
            {
                Log.Debug($"Bear Trap: Removing Player {PlayerInRange} from player in range list...");
                Closed = PlayerInRangeList.Count > 0;
                OnClientFreedClientRpc(WTOBase.PlayerIndex(PlayerInRange));
            }
        }

        [ClientRpc]
        private void OnClientFreedClientRpc(int clientIndex) 
        {
            if(TrappedClientDamageCoroutine == null)
            {
                // Local client is not trapped, so ignore this message
                return;
            }

            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            // we use -1 in this case to mean 'all players'
            if (clientIndex == -1 || clientIndex == WTOBase.PlayerIndex(localPlayer))
            {
                // Local client is no longer trapped
                StopCoroutine(TrappedClientDamageCoroutine);
                TrappedClientDamageCoroutine = null;

                if(SharedTrappedClientEntry == null)
                {
                    Log.Error("SharedTrappedClientEntry was null when trying to free player from trap");
                    return;
                }
                if(SharedTrappedClientEntry.Decrement())
                {
                    // This was the last trap freeing the player, so restore original values and clear entry
                    SharedTrappedClientEntry.RestorePlayer(localPlayer);
                    SharedTrappedClientEntry = null;
                }
            }
        }

        private void ReleaseAllVictims()
        {
            // Server only - free all clients from the trap

            // No players trapped anymore
            PlayerInRangeList.Clear();
            Closed = false;

            // Inform the players of this fact so they can reset their movement speed and jump force if needed.
            OnClientFreedClientRpc(-1);
        }

        private IEnumerator DamageLocalPlayerCoroutine()
        {
            while(true)
            {
                // Run on local client - can't damage from server side.
                PlayerControllerB victim = StartOfRound.Instance.localPlayerController;
                AcidWater.DamageOverlappingPlayer(victim, 0.5f, ref TrappedClientDamageTimer, 5, CauseOfDeath.Mauling);
                if (victim.health > 1)
                {
                    victim.movementSpeed = 0.4f;
                    victim.jumpForce = 1;
                }
                else
                {
                    // Restore player movement before death
                    SharedTrappedClientEntry?.RestorePlayer(victim);
                }

                yield return null;
            }
        }

        private IEnumerator RaiseTrapCoroutine(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);

            Raised = true;
            Log.Debug($"Trap raised after {waitTime} seconds");
        }
    }
}
