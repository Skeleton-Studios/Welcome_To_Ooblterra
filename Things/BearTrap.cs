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

        // Client side variables
        private PlayerControllerB? TrappedPlayer = null;
        private Coroutine? DamageLocalTrappedPlayerCoroutine = null;
        private static float TrappedPlayerOriginalSpeed = 0.0f;
        private static float TrappedPlayerOriginalJumpForce = 0.0f;
        private static int TrappedPlayerCount = 0; // Used to handle multiple traps hitting the same player at once - only restore speed/jump force when the last trap is released.


        private static readonly WTOBase.WTOLogger Log = new(typeof(BearTrap), LogSourceType.Thing);

        private static float RandomFloatInRange(System.Random random, float minValue, float maxValue)
        {
            return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
        }

        private void Start() 
        {
            // Ensure different seed per trap or they all come up at the same time.
            Random = new System.Random(StartOfRound.Instance.randomMapSeed + BearTrapCount++);

            // Start with trap underground and choose a random initial time to come up
            Raised = false;
            Closed = false;

            if(IsServer)
            {
                MoveToStartingLocation();

                // Come back up after random time - sets raised to true once finished.
                float recoveryTime = RandomFloatInRange(Random, InitialSpawnTimeRangeMin, InitialSpawnTimeRangeMax);
                StartCoroutine(RaiseTrapCoroutine(recoveryTime));
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            OnPlayerFreed();
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
                }
                else
                {
                    transform.position = hit.Value.position;
                    transform.rotation = Quaternion.Euler(0, randomYaw, 0);
                }
            }
            else
            {
                averageHitPoint /= count;
                averageHitNormal.Normalize();

                // Move the trap to the average hit point, and rotate it to align with the average hit normal.
                transform.position = averageHitPoint;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, averageHitNormal) * Quaternion.Euler(0, randomYaw, 0);
            }

            // Ensure all clients are synced.
            // No need for a full NetworkTransform - we only pick a position once.
            MoveToStartingLocationClientRpc(transform.position, transform.rotation);
        }

        [ClientRpc]
        private void MoveToStartingLocationClientRpc(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
        {
            if(!Raised)
            {
                // Exit early if already disabled from being hit
                return true;
            }

            if(!IsServer)
            {
                // Go through server
                OnHitServerRpc();
            }
            else
            {
                OnHitServer();
            }

            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        void OnHitServerRpc()
        {
            OnHitServer();
        }

        void OnHitServer()
        {
            if(!Raised)
            {
                // Exit early if already disabled from being hit
                return;
            }

            // Set the server's Raised state as soon as possible in case we get multiple
            // RPC calls from each client calling Hit() (I am not sure how the LC processes this, but
            // better safe than sorry since most things like this tend to be processed on clients).
            Raised = false;

            // Come back up after recovery time - re-sets raised to true once finished.
            // Handle on server to ensure all clients use the same recovery time.
            float recoveryTime = RandomFloatInRange(Random, RecoveryTimeRangeMin, RecoveryTimeRangeMax);
            StartCoroutine(RaiseTrapCoroutine(recoveryTime));

            SetRaisedClientRpc(false);
        }

        [ClientRpc]
        void SetRaisedClientRpc(bool raised) 
        {
            if(!raised)
            {
                // Free player if we have someone trapped
                OnPlayerFreed();
            }
            Raised = raised;
        }

        public void OnTriggerStay(Collider other)
        {
            if(!IsClient)
            {
                return; // only process on client side
            }

            if(!Raised)
            {
                return; // only trap players when the trap is raised
            }

            if (other.gameObject.TryGetComponent<PlayerControllerB>(out var trappedPlayer) && TrappedPlayer == null)
            {
                int playerIndex = WTOBase.PlayerIndex(trappedPlayer);
                Log.Debug($"Bear Trap: Trapped player {playerIndex}");

                // Apply local trap effects.
                OnPlayerTrapped(trappedPlayer);
            }
        }

        public void OnTriggerExit(Collider other) 
        {
            if(!IsClient)
            {
                return; // only process on client side
            }

            if (other.gameObject.TryGetComponent<PlayerControllerB>(out var trappedPlayer) && TrappedPlayer == trappedPlayer)
            {
                int playerIndex = WTOBase.PlayerIndex(trappedPlayer);
                Log.Debug($"Bear Trap: Freed player {playerIndex}");

                // Apply local trap release effects
                OnPlayerFreed();
            }
        }

        private IEnumerator RaiseTrapCoroutine(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);

            Log.Debug($"Trap raised after {waitTime} seconds");
            SetRaisedClientRpc(true);
        }

        private IEnumerator DamageLocalPlayerCoroutine()
        {
            float trappedClientDamageTimer = 0.5f;
            // Run on local client - can't damage from server side.
            PlayerControllerB victim = StartOfRound.Instance.localPlayerController;

            // Basic safety checks to ensure we don't somehow damage the player
            // in weird situations.
            while(!victim.isPlayerDead && TrappedPlayer == victim)
            {
                AcidWater.DamageOverlappingPlayer(victim, 0.5f, ref trappedClientDamageTimer, 5, CauseOfDeath.Mauling);
                if (victim.health > 1)
                {
                    victim.movementSpeed = 0.4f;
                    victim.jumpForce = 1;
                }
                else
                {
                    // Restore player movement before death
                    victim.movementSpeed = TrappedPlayerOriginalSpeed;
                    victim.jumpForce = TrappedPlayerOriginalJumpForce;
                }

                yield return null;
            }

            // Player so the coroutine can exit.
            // Leave the trapped state as trapped though with this dead player body.
            // if the body moves off of the trap then the trap will free them and a different
            // player can be trapped.
        }

        private void OnPlayerTrapped(PlayerControllerB trappedPlayer)
        {
            // Plays client side trap effects.
            TrappedPlayer = trappedPlayer;
            Closed = true;
            GetComponent<AudioSource>().PlayOneShot(CloseSound);

            // Store if we trapped our local player and modify their
            // movement speed and jump force.
            // Don't touch the effects on other players.
            if(TrappedPlayer == StartOfRound.Instance.localPlayerController)
            {
                if(TrappedPlayerCount == 0)
                {
                    // Store original speed/jump force before modifying them, but only for the first trap that hits the player if multiple traps hit at once.
                    TrappedPlayerOriginalSpeed = trappedPlayer.movementSpeed;
                    TrappedPlayerOriginalJumpForce = trappedPlayer.jumpForce;
                }

                // can have multiple traps running the damage routine at once.
                DamageLocalTrappedPlayerCoroutine = StartCoroutine(DamageLocalPlayerCoroutine());
                TrappedPlayerCount++;
            }
        }

        private void OnPlayerFreed()
        {
            // Revert trap effects on the local player, if they are who we trapped.
            
            // Guard against StartOfRound being destroyed before this trap during scene teardown.
            if(StartOfRound.Instance != null && TrappedPlayer == StartOfRound.Instance.localPlayerController)
            {
                // Stop local running damage coroutine.
                if(DamageLocalTrappedPlayerCoroutine != null)
                {
                    StopCoroutine(DamageLocalTrappedPlayerCoroutine);
                    DamageLocalTrappedPlayerCoroutine = null;
                }

                TrappedPlayerCount--;
                if(TrappedPlayerCount == 0)
                {
                    // Restore original speed/jump force when the last trap is released if multiple traps hit at once.
                    TrappedPlayer.movementSpeed = TrappedPlayerOriginalSpeed;
                    TrappedPlayer.jumpForce = TrappedPlayerOriginalJumpForce;
                }
            }

            // Clear local trap effects.
            TrappedPlayer = null;
            Closed = false;
        }
    }
}
