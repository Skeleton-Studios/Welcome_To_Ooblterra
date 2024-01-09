using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;

namespace Welcome_To_Ooblterra.Enemies{
    public class WandererAI : WTOEnemy {
        //STATES
        private class Investigate : BehaviorState {
            public Investigate() {
                RandomRange = new Vector2(4, 9);
            }
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].TotalInvestigationSeconds = MyRandomInt;
                WandererList[enemyIndex].LogMessage("Investigating for: " + WandererList[enemyIndex].TotalInvestigationSeconds + "s");
                WandererList[enemyIndex].creatureAnimator.speed = 1f;
                WandererList[enemyIndex].agent.speed = 0f;
                WandererList[enemyIndex].creatureAnimator.SetBool("Investigating", true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].LowerTimerValue(ref WandererList[enemyIndex].TotalInvestigationSeconds);
                if(WandererList[enemyIndex].TotalInvestigationSeconds < 0.2) {
                    WandererList[enemyIndex].creatureAnimator.SetBool("Investigating", false);
                }
                WandererList[enemyIndex].targetPlayer = WandererList[enemyIndex].GetClosestPlayer();
                if (WandererList[enemyIndex].targetPlayer == null) {
                    return;
                }
                //Wanderer.shouldLookAtPlayer = Wanderer.GetDistanceFromPlayer(WandererList[enemyIndex].targetPlayer) < 4 && Wanderer.HasLineOfSightToPosition(WandererList[enemyIndex].targetPlayer.transform.position, 90);
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].creatureAnimator.SetBool("Investigating", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new DoneInvestigating(),
                new InDanger()
            };
        }
        private class Roam : BehaviorState {
            bool Wandering;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                Wandering = WandererList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(WandererList[enemyIndex].allAINodes[enemyRandom.Next(WandererList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                WandererList[enemyIndex].agent.speed = 7f;
                WandererList[enemyIndex].creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (!Wandering) {
                    WandererList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(WandererList[enemyIndex].allAINodes[enemyRandom.Next(WandererList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundNextPoint(),
                new InDanger()
            };
        }
        private class Flee : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].creatureAnimator.speed = 2f;
                WandererList[enemyIndex].creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].agent.speed = 10f;
                WandererList[enemyIndex].SetDestinationToPosition(WandererList[enemyIndex].ChooseFarthestNodeFromPosition(WandererList[enemyIndex].NearestPlayer(WandererList[enemyIndex].RegisteredThreats).transform.position).position);
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WandererList[enemyIndex].creatureAnimator.SetBool("Moving", false);
                WandererList[enemyIndex].creatureAnimator.speed = 1f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new NoLongerInDanger()
            };
        }
        private class Stunned : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: true);
                WandererList[enemyIndex].agent.speed = 0f;
                WandererList[enemyIndex].RegisteredThreats.Add(WandererList[enemyIndex].stunnedByPlayer);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: false);
                WandererList[enemyIndex].agent.speed = 10f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InDanger()
            };
        }
        
        //TRANSITIONS
        private class DoneInvestigating : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (WandererList[enemyIndex].TotalInvestigationSeconds <= 0);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class FoundNextPoint : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Vector3.Distance(WandererList[enemyIndex].transform.position, WandererList[enemyIndex].destination) < 5);
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class InDanger : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (WandererList[enemyIndex].RegisteredThreats.Count <= 0 || WandererList[enemyIndex].stunNormalizedTimer > 0) {
                    return false;
                }
                return (Vector3.Distance(WandererList[enemyIndex].transform.position, WandererList[enemyIndex].NearestPlayer(WandererList[enemyIndex].RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Flee();
            }
        }
        private class NoLongerInDanger : StateTransition {
            public override bool CanTransitionBeTaken() {
                return !(Vector3.Distance(WandererList[enemyIndex].transform.position, WandererList[enemyIndex].NearestPlayer(WandererList[enemyIndex].RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class HitByStunGun : StateTransition {
            public override bool CanTransitionBeTaken() {
                return WandererList[enemyIndex].stunNormalizedTimer > 0 && !(WandererList[enemyIndex].ActiveState is Stunned);
            }
            public override BehaviorState NextState() {
                return new Stunned();
            }

        }

        private List<PlayerControllerB> RegisteredThreats = new List<PlayerControllerB>();
        private float TotalInvestigationSeconds;
        public Transform WandererHead;
        bool shouldLookAtPlayer = false;
        private float HeadTurnTime;
        public static Dictionary<int, WandererAI> WandererList = new Dictionary<int, WandererAI>();
        private static int WandererID;

        public override void Start() {
            MyValidState = PlayerState.Outside;
            InitialState = new Investigate();
            //PrintDebugs = true;
            WandererID++;
            WTOEnemyID = WandererID;
            
            LogMessage($"Adding Wanderer {this} #{WandererID}");
            WandererList.Add(WandererID, this);
            if (!agent.isOnNavMesh) {
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
            stunNormalizedTimer = -1;
            GlobalTransitions.Add(new HitByStunGun());
            base.Start();
        }
        public override void Update() {
            base.Update();
            Resources.FindObjectsOfTypeAll(typeof(GameObject));
        }

        public void LateUpdate() {
            Quaternion lookRotation = Quaternion.Euler(-0.094f, 0.009f, -7.474f);
            if (shouldLookAtPlayer) {
                lookRotation = Quaternion.LookRotation(targetPlayer.transform.position - WandererHead.transform.position);
                lookRotation = Quaternion.Euler(2.445f, lookRotation.eulerAngles.y, -10.813f);
                WandererHead.rotation *= lookRotation;
                HeadTurnTime = 0;
            } else if (HeadTurnTime < 0.2) {
                WandererHead.transform.rotation = Quaternion.Slerp(lookRotation, Quaternion.Euler(-0.094f, 0.009f, -7.474f), HeadTurnTime / 0.2f);
                HeadTurnTime += Time.deltaTime;
            }
        }
        private PlayerControllerB NearestPlayer(List<PlayerControllerB> List) {
            float distance = 100000;
            PlayerControllerB nearestPlayer = null;
            if (!List.Any()){
                return nearestPlayer;
            }
            foreach (PlayerControllerB threat in List) {
                float enemydistance = Vector3.Distance(threat.transform.position, transform.position);
                if (enemydistance < distance) {
                    distance = enemydistance;
                    nearestPlayer = threat;
                }
            }
            return nearestPlayer;
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            creatureAnimator.SetTrigger("Hit");
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    creatureAnimator.SetTrigger("Death");
                    ItemPatch.SpawnItem(transform.position + new Vector3(0, 5, 0), internalID: 5);
                    Vector3 AdultSpawnPos = playerWhoHit.transform.position - Vector3.Scale(new Vector3(-5, 0, -5), playerWhoHit.transform.forward * -1);
                    Quaternion AdultSpawnRot = new Quaternion(0, Quaternion.LookRotation(playerWhoHit.transform.position - AdultSpawnPos).y, 0, 1);
                    GameObject AdultWanderer = Instantiate(MonsterPatch.AdultWandererContainer[0].enemyType.enemyPrefab, AdultSpawnPos, AdultSpawnRot);
                    AdultWanderer.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                    AdultWanderer.gameObject.GetComponentInChildren<AdultWandererAI>().SetMyTarget(playerWhoHit);
                    KillEnemyOnOwnerClient();
                    return;
                }
            }

            ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
            if (!RegisteredThreats.Contains(playerWhoHit)) RegisteredThreats.Add(playerWhoHit);
        }
        
    }
}

