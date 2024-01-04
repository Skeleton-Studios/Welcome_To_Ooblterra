using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using static UnityEngine.GraphicsBuffer;

namespace Welcome_To_Ooblterra.Enemies {

    public class WandererAI : WTOEnemy {
        //STATES
        private class Investigate : BehaviorState {

            private WandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                Wanderer.TotalInvestigationSeconds = 500; //enemyRandom.Next(4, 9);
                Wanderer.LogMessage("Investigating for: " + Wanderer.TotalInvestigationSeconds + "s");
                self.creatureAnimator.speed = 1f;
                self.agent.speed = 0f;
                self.creatureAnimator.SetBool("Investigating", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer.LowerTimerValue(ref Wanderer.TotalInvestigationSeconds);
                if(Wanderer.TotalInvestigationSeconds < 0.2) {
                    self.creatureAnimator.SetBool("Investigating", false);
                }
                self.targetPlayer = self.GetClosestPlayer();
                if (self.targetPlayer == null) {
                    return;
                }
                //Wanderer.shouldLookAtPlayer = Wanderer.GetDistanceFromPlayer(self.targetPlayer) < 4 && Wanderer.HasLineOfSightToPosition(self.targetPlayer.transform.position, 90);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Investigating", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new DoneInvestigating(),
                new InDanger()
            };
        }
        private class Roam : BehaviorState {
            WandererAI Wanderer;

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 7f;
                self.creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundNextPoint(),
                new InDanger()
            };
        }
        private class Flee : BehaviorState {

            private WandererAI Wanderer;
            
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                self.creatureAnimator.speed = 2f;
                self.creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.agent.speed = 10f;
                self.SetDestinationToPosition(self.ChooseFarthestNodeFromPosition(Wanderer.NearestPlayer(Wanderer.RegisteredThreats).transform.position).position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
                self.creatureAnimator.speed = 1f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new NoLongerInDanger()
            };
        }
        private class Stunned : BehaviorState {

            WandererAI MyWanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: true);
                self.agent.speed = 0f;
                MyWanderer = self as WandererAI;
                MyWanderer.RegisteredThreats.Add(self.stunnedByPlayer);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: false);
                self.agent.speed = 10f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InDanger()
            };
        }
        
        //TRANSITIONS
        private class DoneInvestigating : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                return (MyWanderer.TotalInvestigationSeconds <= 0);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class FoundNextPoint : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Vector3.Distance(self.transform.position, self.destination) < 5);
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class InDanger : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                if (MyWanderer.RegisteredThreats.Count <= 0 || self.stunNormalizedTimer > 0) {
                    return false;
                }
                return (Vector3.Distance(self.transform.position, MyWanderer.NearestPlayer(MyWanderer.RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Flee();
            }
        }
        private class NoLongerInDanger : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                return !(Vector3.Distance(self.transform.position, MyWanderer.NearestPlayer(MyWanderer.RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class HitByStunGun : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                return self.stunNormalizedTimer > 0 && !(MyWanderer.ActiveState is Stunned);
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

        public override void Start() {
            MyValidState = PlayerState.Outside;
            InitialState = new Investigate();
            base.Start();
            if (!agent.isOnNavMesh) {
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
            stunNormalizedTimer = -1;
            GlobalTransitions.Add(new HitByStunGun());
            //PrintDebugs = true;
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
                    ItemPatch.SpawnItem(transform.position + new Vector3(0, 5, 0), 5);
                    Vector3 AdultSpawnPos = playerWhoHit.transform.position - Vector3.Scale(new Vector3(-5, 0, -5), playerWhoHit.transform.forward * -1);
                    Quaternion AdultSpawnRot = new Quaternion(0, Quaternion.LookRotation(playerWhoHit.transform.position - AdultSpawnPos).y, 0, 1);
                    GameObject obj = UnityEngine.Object.Instantiate(MonsterPatch.AdultWandererContainer[0].enemyType.enemyPrefab, AdultSpawnPos, AdultSpawnRot);
                    obj.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                    obj.gameObject.GetComponentInChildren<AdultWandererAI>().SetMyTarget(playerWhoHit);
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            if(!RegisteredThreats.Contains(playerWhoHit)) RegisteredThreats.Add(playerWhoHit);
        }
        
    }
}

