using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;
using GameNetcodeStuff;
using static LethalLib.Modules.Enemies;

namespace Welcome_To_Ooblterra.Enemies {
    public class BabyLurkerAI : EnemyAI {

        //BEHAVIOR STATES
        private class Spawn : BehaviorState {

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawning", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawning", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new SpawnTransition(500)
            };
        }
        private class Roam : BehaviorState {
            private int InvestigatingTime;
            public int TotalInvestigationTime;
            private bool HasRoamPoint;
            public AISearchRoutine roamMap;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.StartSearch(self.transform.position, roamMap);
                self.creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (!roamMap.inProgress) {
                    self.StartSearch(self.transform.position, roamMap);
                }

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.StopSearch(roamMap);
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> { 
                new EnemySpottedTransition()
            };
        }
        private class Lunge : BehaviorState {
            private bool endingLunge;
            private Ray ray;
            private RaycastHit rayHit;
            private RoundManager roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Lunging", true);
                endingLunge = false;
                ray = new Ray(self.transform.position + Vector3.up, self.transform.forward);
                Vector3 pos = ((!Physics.Raycast(ray, out rayHit, 17f, StartOfRound.Instance.collidersAndRoomMask)) ? ray.GetPoint(17f) : rayHit.point);
                pos = roundManager.GetNavMeshPosition(pos);
                self.SetDestinationToPosition(pos);
                self.agent.speed = 13f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) { 
                
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Lunging", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> { 
                new EnemyKilledTransition(),
                new EnemyAliveTransition()
            };
        }
        private class Reposition : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", true);
                
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InPositionTransition(),
            };
        }
        private class StayOnPlayer : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Staying", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Staying", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new SpawnTransition(500),
            };
        }

        //STATE TRANSITIONS
        private class SpawnTransition : StateTransition {
            private int SpawnTime;
            private int TotalTime = 500;
            public SpawnTransition(int newtime){
                TotalTime = newtime;
            }
            public override bool CanTransitionBeTaken() {
                if (!(SpawnTime > TotalTime)) {
                    SpawnTime++;
                    return false;
                }
                return true;
            }
            public override BehaviorState NextState() {
                if (IsEnemyInRange()) {
                    return new Lunge();
                }
                return new Roam();
            }

            private bool IsEnemyInRange() {
                PlayerControllerB nearestPlayer = self.CheckLineOfSightForClosestPlayer(45f, 20, 2);
                return nearestPlayer == null;
            }
        }
        private class InPositionTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Vector3.Distance(self.transform.position, self.destination) < 3); 
            }
            public override BehaviorState NextState() {
                if (IsEnemyInRange()) {
                    return new Lunge();
                } 
                return new Roam();
            }
            private bool IsEnemyInRange() {
                PlayerControllerB nearestPlayer = self.CheckLineOfSightForClosestPlayer(45f, 20, 2);
                return nearestPlayer == null;
            }
        }
        private class EnemySpottedTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                self.targetPlayer = self.CheckLineOfSightForClosestPlayer(45f, 20, 2);
                return (self.targetPlayer == null);
            }
            public override BehaviorState NextState() {
                return new Reposition();
            }
        }
        private class EnemyAliveTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                return self.targetPlayer.health <= 0;
            }
            public override BehaviorState NextState() {
                return new Reposition();
            }
        }
        private class EnemyKilledTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                return self.targetPlayer.health <= 0;
            }
            public override BehaviorState NextState() {
                return new StayOnPlayer();
            }
        }

        private System.Random enemyRandom;
        private RoundManager roundManager;
        private float AITimer;
        private BehaviorState InitialState = new Spawn();
        private BehaviorState ActiveState = null;

        protected override string __getTypeName() {
            return "BabyLurkerAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }
        public override void Start() {
            base.Start();
            ActiveState = InitialState;
        }
        public override void Update() {
            if (isEnemyDead || !ventAnimationFinished) {
                return;
            }

            base.Update();
            AITimer++;

            //play the stun animation if they're stunned 
            //TODO: SetLayerWeight switches between the basic animation layer (0) and the stun animation layer (1). 
            //The wanderer will need a similar setup if we want to be able to stun him, plus a stun animation 
            /*
            if (stunNormalizedTimer > 0f && !isEnemyDead) {
                if (stunnedByPlayer != null && currentBehaviourStateIndex != 2 && base.IsOwner) {
                    creatureAnimator.SetLayerWeight(1, 1f);
                }
            } else {
                creatureAnimator.SetLayerWeight(1, 0f);
            }
            */
            bool RunUpdate = true;
            //don't run enemy ai if they're dead
            foreach (StateTransition transition in ActiveState.transitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
                    ActiveState = transition.NextState();
                    ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
                    break;
                }
            }
            if (RunUpdate) {
                ActiveState.UpdateBehavior(this, enemyRandom, creatureAnimator);
            }
            //Custom Monster Code

        }
    }
}
