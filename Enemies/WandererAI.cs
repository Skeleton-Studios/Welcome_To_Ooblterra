using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Welcome_To_Ooblterra.Enemies
{
    public class WandererAI : WTOEnemy 
    {
        // If trying to roam but stuck for this many seconds, go to investigate for a bit and re-try at a different point.
        static readonly float ROAM_TIME_MOVEMENT_EXPIRY_S = 5f;

        //STATES
        private class Investigate : BehaviorState 
        {
            public Investigate() 
            {
                RandomRange = new Vector2(12, 17);
            }

            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                WandererAI ai = WandererList[enemyIndex];
                ai.ReachedNextPoint = false;
                ai.agent.speed = 0f;
                ai.investigateEnd = Time.time + MyRandomInt;
                Log.Debug("Investigating for: " + MyRandomInt + "s");
                creatureAnimator.speed = 1f;
                creatureAnimator.SetBool("Investigating", true);
            }

            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                WandererAI ai = WandererList[enemyIndex];

                if(ai.investigateEnd - Time.time < 0.2) 
                {
                    creatureAnimator.SetBool("Investigating", false);
                }

                PlayerControllerB? targetPlayer = ai.GetClosestPlayer(requireLineOfSight: true);
                if(targetPlayer)
                {
                    ai.targetPlayer = targetPlayer;
                    ai.shouldLookAtPlayer = Vector3.Distance(ai.transform.position, ai.targetPlayer.transform.position) < 4;
                }
                else
                {
                    ai.targetPlayer = null;
                    ai.shouldLookAtPlayer = false;
                }
            }

            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                creatureAnimator.SetBool("Investigating", false);
                WandererAI ai = WandererList[enemyIndex];
                ai.ReachedNextPoint = false;
                ai.targetPlayer = null;
                ai.shouldLookAtPlayer = false;
            }

            public override List<StateTransition> transitions { get; set; } =
            [
                new DoneInvestigating(),
                new InDanger()
            ];
        }

        private class Roam : BehaviorState 
        {
            // bool Wandering;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                WandererAI ai = WandererList[enemyIndex];
                ai.agent.speed = 7f;
                creatureAnimator.SetBool("Moving", true);
                if (!ai.RoamPlanet.inProgress)
                { 
                    ai.StartSearch(ai.transform.position, ai.RoamPlanet);
                }

                ai.roamLastPositionTime = Time.time;
                ai.roamLastPosition = ai.transform.position;
            }

            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator)
            {
                WandererAI ai = WandererList[enemyIndex];
                if (!WandererList[enemyIndex].RoamPlanet.inProgress) 
                {
                    ai.StartSearch(ai.transform.position, ai.RoamPlanet);
                }

                // Determine if the wanderer is able to move while roaming.
                // If it can't move, then it's stuck and RoamStuck will take the transition
                // and re-position it on the nav mesh
                if(Vector3.SqrMagnitude(ai.transform.position - ai.roamLastPosition) > 1.0f)
                {
                    ai.roamLastPositionTime = Time.time;
                }
            }

            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                creatureAnimator.SetBool("Moving", false);
            }

            public override List<StateTransition> transitions { get; set; } =
            [
                new RoamStuck(),
                new FoundNextPoint(),
                new InDanger()
            ];
        }

        private class RoamUnstuck : BehaviorState 
        {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator)
            {
                // Try to find a position on the nav mesh to go to
                // Copies the same structure that masked player enemy teleport does:
                // Only on owner, disable agent, set position, enable agent.
                WandererAI ai = WandererList[enemyIndex];
                if(ai.IsOwner)
                {
                    // Commented out because the wanderer could get blocked by some dynamic object then suddenly teleport..
                    // this is not really what we want here.
                    // instead, we allow this state to server as a refresh of the Roam state in the hopes it fixes itself.
                    /*
                    NavMeshHit? newPos = Utils.GetRandomNavMeshPositionInRadiusExtended(ai.transform.position);
                    if(newPos.HasValue)
                    {
                        Log.Warning("Unsticking wanderer and teleporting to " + newPos.Value.position);
                        ai.agent.enabled = false;
                        ai.transform.position = newPos.Value.position;
                        ai.agent.enabled = true;
                    }
                    else
                    {
                        Log.Error("Wanderer could not find a spot on nav mesh to move to after getting stuck");
                    }*/
                }
            }

            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator)
            {

            }

            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {

            }

            public override List<StateTransition> transitions { get; set; } =
            [
                new GoStraightToRoam(),
            ];
        }

        private class Flee : BehaviorState 
        {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                creatureAnimator.speed = 2f;
                creatureAnimator.SetBool("Moving", true);

                WandererAI ai = WandererList[enemyIndex];
                // Used to periodically update the flee.
                // The flee start position is used in cases where a closest player can't be found.
                ai.fleeStartTime = Time.time;
                ai.fleeLastCheck = ai.fleeStartTime;
                ai.fleeStartPosition = ai.transform.position;
                UpdateFlee(ai);
            }

            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                WandererAI ai = WandererList[enemyIndex];
                ai.agent.speed = 10f;

                // No need to re-calculate this every single frame, just run it every second or so, the target position is
                // not likely to update that much.
                float now = Time.time;
                if(now - ai.fleeLastCheck > 1.0f)
                {
                    UpdateFlee(ai);
                    ai.fleeLastCheck = now;
                }                
            }

            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                creatureAnimator.SetBool("Moving", false);
                creatureAnimator.speed = 1f;
            }

            private void UpdateFlee(WandererAI ai)
            {
                PlayerControllerB? nearestPlayer = ai.NearestPlayer(ai.RegisteredThreats);

                // Either run from the player, or just run to a fixed node somewhere away from where we started our flee.
                Vector3 fleeSourcePosition = nearestPlayer?.transform.position ?? ai.fleeStartPosition;

                Transform? farthestNode = ai.ChooseFarthestNodeFromPosition(fleeSourcePosition);
                if(!farthestNode)
                {
                    Log.Error("Wanderer could not find anywhere to flee to");
                    return;
                }

                ai.SetDestinationToPosition(farthestNode.position);
            }

            public override List<StateTransition> transitions { get; set; } = 
            [
                new NoLongerInDanger()
            ];
        }

        private class Stunned : BehaviorState 
        {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                WandererAI ai = WandererList[enemyIndex];
                creatureAnimator.SetBool("Stunned", value: true);
                ai.agent.speed = 0f;
                if(ai.stunnedByPlayer != null)
                {
                    ai.AddThreat(ai.stunnedByPlayer);
                }
            }

            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                // Nothing to do here, just wait for the stun to wear off, which is detected by the InDanger transition.
            }
            
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) 
            {
                creatureAnimator.SetBool("Stunned", value: false);
                WandererList[enemyIndex].agent.speed = 10f;
            }

            public override List<StateTransition> transitions { get; set; } =
            [
                new StunOver()
            ];
        }
        
        //TRANSITIONS
        private class DoneInvestigating : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                WandererAI ai = WandererList[enemyIndex];
                return ai.investigateEnd - Time.time <= 0;
            }

            public override BehaviorState NextState() 
            {
                return new Roam();
            }
        }

        private class FoundNextPoint : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                return WandererList[enemyIndex].ReachedNextPoint;
            }

            public override BehaviorState NextState() 
            {
                return new Investigate();
            }
        }

        private class RoamStuck : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                // Trying to roam, but sitting in place for too long
                return Time.time - WandererList[enemyIndex].roamLastPositionTime > ROAM_TIME_MOVEMENT_EXPIRY_S;
            }

            public override BehaviorState NextState() 
            {
                return new RoamUnstuck();
            }
        }

        private class GoStraightToRoam : StateTransition
        {
            public override bool CanTransitionBeTaken() 
            {
                return true;
            }

            public override BehaviorState NextState() 
            {
                return new Roam();
            }
        }

        private class InDanger : StateTransition
        {
            public override bool CanTransitionBeTaken() 
            {
                WandererAI ai = WandererList[enemyIndex];
                PlayerControllerB? nearestPlayer = ai.NearestPlayer(ai.RegisteredThreats);
                if(nearestPlayer == null)
                {
                    // No threats means not in danger
                    return false;
                }

                // Flee if a threat is nearby
                return Vector3.Distance(ai.transform.position, nearestPlayer.transform.position) < 5;
            }

            public override BehaviorState NextState() 
            {
                return new Flee();
            }
        }

        private class StunOver : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                WandererAI ai = WandererList[enemyIndex];
                return ai.stunNormalizedTimer <= 0;
            }

            public override BehaviorState NextState() 
            {
                return new Flee();
            }
        }

        private class NoLongerInDanger : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                WandererAI ai = WandererList[enemyIndex];

                // Run away for at least 5 seconds
                if(Time.time - ai.fleeStartTime < 5.0f)
                {
                    return false;
                }

                PlayerControllerB? nearestPlayer = ai.NearestPlayer(ai.RegisteredThreats);
                if(nearestPlayer == null)
                {
                    // No threats means not in danger
                    return true;
                }

                // 1 extra metre away to avoid oscilations 
                return Vector3.Distance(ai.transform.position, nearestPlayer.transform.position) >= 6;
            }

            public override BehaviorState NextState() 
            {
                return new Roam();
            }
        }

        private class HitByStunGun : StateTransition 
        {
            public override bool CanTransitionBeTaken() 
            {
                WandererAI ai = WandererList[enemyIndex];
                return ai.stunNormalizedTimer > 0 && ai.ActiveState is not Stunned;
            }

            public override BehaviorState NextState() 
            {
                return new Stunned();
            }
        }

        private readonly List<PlayerControllerB> RegisteredThreats = new();
        // use by investigate behaviour
        private float investigateEnd = 0;

        public Transform WandererHead;
        private bool shouldLookAtPlayer = false;
        private float HeadTurnTime;
        private Quaternion? headLookRotation = null;
        private Quaternion headDefaultRotation = Quaternion.identity;
        public static Dictionary<int, WandererAI> WandererList = new();
        private static int WandererID;
        private readonly AISearchRoutine RoamPlanet = new();
        private bool ReachedNextPoint = false;
        public Item WandererCorpse;
        public EnemyType AdultWandererEnemyType;

        // used by roam behaviour
        private Vector3 roamLastPosition;
        private float roamLastPositionTime = 0;

        // used by flee behaviour
        private Vector3 fleeStartPosition = Vector3.zero;
        private float fleeLastCheck = 0;
        private float fleeStartTime = 0;

        private bool killed = false;

        private static readonly WTOBase.WTOLogger Log = new(typeof(WandererAI), LogSourceType.Enemy);

        public override void Start() 
        {
            MyValidState = PlayerState.Outside;
            InitialState = new Investigate();
            PrintDebugs = false;
            WandererID++;
            WTOEnemyID = WandererID;

            Log.Info($"Adding Wanderer {this} #{WandererID}");
            WandererList.Add(WandererID, this);
            if (!agent.isOnNavMesh) {
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
            stunNormalizedTimer = -1;
            GlobalTransitions.Add(new HitByStunGun());
            headDefaultRotation = WandererHead.transform.localRotation;
            base.Start();
        }

        public void LateUpdate() 
        {
            MaybeLookAtPlayer();
        }

        private void MaybeLookAtPlayer()
        {
            if(!shouldLookAtPlayer || targetPlayer == null)
            {
                if(HeadTurnTime < 0.2f && headLookRotation.HasValue)
                {
                    HeadTurnTime = Mathf.Min(HeadTurnTime + Time.deltaTime, 0.2f);
                    WandererHead.localRotation = Quaternion.Slerp(headLookRotation.Value, headDefaultRotation, HeadTurnTime / 0.2f);

                    if(HeadTurnTime < 0.2f)
                    {
                        return;
                    }

                    // allow headLookRotation to be nulled on completion
                }

                headLookRotation = null;
                return;
            }

            // Begin at the current rotation
            headLookRotation ??= WandererHead.localRotation;

            headLookRotation = Quaternion.Slerp(headLookRotation.Value, GetLocalLookRotation(targetPlayer), Time.deltaTime * 10f);
            WandererHead.localRotation = headLookRotation.Value;
            HeadTurnTime = 0;
        }

        private Quaternion GetLocalLookRotation(PlayerControllerB player)
        {
            // Target to look at in local wanderer head space
            Vector3 lookDirection = player.cameraContainerTransform.position - WandererHead.position;

            // The default direction that the wanderer looks, which in the editor is -Z (bind pose forward direction)
            // This ends up being a check against the right vector..?
            Vector3 wandererForward = WandererHead.parent.rotation * (headDefaultRotation * Vector3.right);

            Vector2 lookDirection2D = new(lookDirection.x, lookDirection.z);
            Vector2 wandererForward2D = new(wandererForward.x, wandererForward.z);
            if(Vector2.Angle(lookDirection2D, wandererForward2D) > 45f)
            {
                // Don't turn the head too much.
                // because we set a scale on the root wanderer skeleton, it results in the
                // head scaling weirdly as the wanderer looks around with its head, so
                // we just limit the amount it can move its head.
                // The full and correct fix here is to bake the scale into the skeleton...
                return headDefaultRotation;
            }

            // lookDirection negated due to -Z
            Quaternion worldLookDirection = Quaternion.LookRotation(-lookDirection.normalized, transform.up);
            // The needed rotation seems to be 90 degrees out for some reason..
            // The wanderer also needs to look down a bit, since the eye on its head naturally looks up a bit.
            // It results in it looking like the wanderer is looking above the player's head without the additional
            // Z rotation added too.
            Quaternion baseWandererHeadRotation = Quaternion.Euler(0, 90, -25);

            // Convert to local space using wanderer head parent's rotation
            return (Quaternion.Inverse(WandererHead.parent.rotation) * worldLookDirection * baseWandererHeadRotation).normalized;
        }

        private void AddThreat(PlayerControllerB threat)
        {
            if (threat != null && !RegisteredThreats.Contains(threat))
            {
                RegisteredThreats.Add(threat);
            }
        }

        private PlayerControllerB? NearestPlayer(List<PlayerControllerB> List) 
        {
            float distance = Mathf.Infinity;

            if (List.Count == 0)
            {
                return null;
            }

            PlayerControllerB? nearestPlayer = null;
            foreach (PlayerControllerB threat in List) 
            {
                if(threat == null)
                {
                    // Sanity check since nulls were previously added to this list
                    continue;
                }

                float enemydistance = Vector3.Distance(threat.transform.position, transform.position);
                if (enemydistance < distance) 
                {
                    distance = enemydistance;
                    nearestPlayer = threat;
                }
            }

            return nearestPlayer;
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) 
        {
            base.HitEnemy(force, playerWhoHit, playHitSFX);

            if(killed)
            {
                // Basic sanity check, nothing further needs to be done here
                // once the enemy is dead.
                return;
            }

            int killerId = -1;

            // This can be null, and we need to handle it properly.
            if(playerWhoHit != null)
            {
                killerId = (int)playerWhoHit.playerClientId;
                AddThreat(playerWhoHit);
            }
            
            creatureAnimator.SetTrigger("Hit");
            enemyHP -= force;
            Log.Debug($"NEW WANDERER HEALTH: {enemyHP}");

            if (enemyHP <= 0) 
            {
                DieServerRPC(killerId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void DieServerRPC(int KillerID) 
        {
            if(killed)
            {
                // Already died, don't duplicate spawning of corpse or adult.
                return;
            }
            killed = true;

            // Only spawn the adult if it was a player who did the killing.
            if(KillerID != -1)
            {
                SpawnParent(KillerID);
            }

            Log.Debug("Wanderer dying!");
            SpawnCorpse();
            KillEnemyOnOwnerClient();
        }

        /// <summary>
        /// Spawns an adult wanderer targeting the player that killed the wanderer.
        /// </summary>
        /// <param name="killerId">integer ID of the player that killed the wanderer. Must be >= 0</param>
        private void SpawnParent(int killerId)
        {
            Log.Info($"Spawning Adult Wanderer targeting player ID {killerId}");
            PlayerControllerB Killer = StartOfRound.Instance.allPlayerScripts[killerId];

            Vector3 AdultSpawnPos = Killer.transform.position - Vector3.Scale(new Vector3(-5, 0, -5), Killer.transform.forward * -1);
            Quaternion AdultSpawnRot = new(0, Quaternion.LookRotation(Killer.transform.position - AdultSpawnPos).y, 0, 1);

            GameObject AdultWanderer = Instantiate(AdultWandererEnemyType.enemyPrefab, AdultSpawnPos, AdultSpawnRot);
            AdultWanderer.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
            
            AdultWandererAI adultWandererAI = AdultWanderer.gameObject.GetComponent<AdultWandererAI>();
            adultWandererAI.SetTargetServerRpc(killerId);
            
            // Propagate to base lethal company the same way SpawnRandomDaytimeEnemy() does.
            RoundManager.Instance.SpawnedEnemies.Add(adultWandererAI);
            adultWandererAI.enemyType.numberSpawned++;
            adultWandererAI.enemyType.hasSpawnedAtLeastOne = true;
        }

        /// <summary>
        /// Spawns the wanderer's corpse slightly above the wanderer's position.
        /// </summary>
        private void SpawnCorpse()
        {
            //Spawn Wanderer Corpse
            NetworkObject SpawnedItem = Instantiate(WandererCorpse.spawnPrefab, transform.position + new Vector3(0, 5, 0), Quaternion.identity).GetComponent<NetworkObject>();
            int WandererCorpseValue = (int)(RoundManager.Instance.AnomalyRandom.Next(WandererCorpse.minValue, WandererCorpse.maxValue) * RoundManager.Instance.scrapValueMultiplier);
            SpawnedItem.GetComponent<GrabbableObject>().SetScrapValue(WandererCorpseValue);
            SpawnedItem.Spawn();
            SetCorpseValueClientRpc(SpawnedItem, WandererCorpseValue);
        }

        public override void ReachedNodeInSearch() 
        {
            base.ReachedNodeInSearch();
            ReachedNextPoint = true;
        }

        [ClientRpc]
        public void SetCorpseValueClientRpc(NetworkObjectReference Corpse, int CorpseValue) 
        {
            if(Corpse.TryGet(out var ScrapNetworkobject))
            {
                ScrapNetworkobject.GetComponent<GrabbableObject>().SetScrapValue(CorpseValue);
            }
            else
            {
                Log.Error($"Failed to set scrap value for corpse {Corpse}");
            }
        }
    }
}
