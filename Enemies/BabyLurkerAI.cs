using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.AI;

namespace Welcome_To_Ooblterra.Enemies {
    public class BabyLurkerAI : WTOEnemy {

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
            bool canMakeNextPoint;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Patrolling", true);
                canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 7f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (!canMakeNextPoint) {
                    canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Patrolling", true);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> { 
                new EnemySpottedTransition()
            };
        }
        private class Lunge : BehaviorState {
            private Ray ray;
            private RaycastHit rayHit;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                BabyLurkerAI me = self as BabyLurkerAI;
                me.LungeComplete = false;
                self.creatureAnimator.SetBool("Lunging", true);
                ray = new Ray(self.transform.position + Vector3.up, self.transform.forward);
                Vector3 pos = ((!Physics.Raycast(ray, out rayHit, 17f, StartOfRound.Instance.collidersAndRoomMask)) ? ray.GetPoint(17f) : rayHit.point);
                pos = me.roundManager.GetNavMeshPosition(pos);
                self.SetDestinationToPosition(self.targetPlayer.transform.position);
                self.agent.speed = 13f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                BabyLurkerAI me = self as BabyLurkerAI;
                self.agent.speed -= Time.deltaTime * 5f;
                if (self.agent.speed < 1.5f){
                    me.LungeComplete = true;
                    if(me.LungeCooldown < 1) { 
                        me.LungeCooldown = 500;
                    }
                }
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
            bool isRepositioning;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", true);
                isRepositioning = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
                self.agent.speed = 8f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (!isRepositioning) {
                    isRepositioning = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
                }
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
                BabyLurkerAI me = self as BabyLurkerAI;
                if (me.IsEnemyInRange()) {
                    return new Lunge();
                }
                return new Roam();
            }
        }
        private class InPositionTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                BabyLurkerAI me = self as BabyLurkerAI;
                return (Vector3.Distance(self.transform.position, self.destination) < 3) && me.LungeCooldown <= 0; 
            }
            public override BehaviorState NextState() {
                BabyLurkerAI me = self as BabyLurkerAI;
                if (me.IsEnemyInRange()) {
                    return new Lunge();
                } 
                return new Roam();
            }
        }
        private class EnemySpottedTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                self.targetPlayer = self.CheckLineOfSightForClosestPlayer(180f, 60, 2);
                return (self.targetPlayer != null);
            }
            public override BehaviorState NextState() {
                return new Reposition();
            }
        }
        private class EnemyAliveTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                BabyLurkerAI me = self as BabyLurkerAI;
                return self.targetPlayer.health > 0 && me.LungeComplete;
            }
            public override BehaviorState NextState() {
                return new Reposition();
            }
        }
        private class EnemyKilledTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                BabyLurkerAI me = self as BabyLurkerAI;
                return self.targetPlayer.health <= 0 && me.LungeComplete;
            }
            public override BehaviorState NextState() {
                return new StayOnPlayer();
            }
        }

        private bool LungeComplete;
        private int LungeCooldown;
        protected override string __getTypeName() {
            return "BabyLurkerAI";
        }
        public override void Start() {
            InitialState = new Spawn();
            PrintDebugs = true;
            base.Start();
        }
        public override void Update() {
            base.Update();
            if(LungeCooldown > 0) {
                LungeCooldown--;
            }
        }
        private bool IsEnemyInRange() {
            PlayerControllerB nearestPlayer = CheckLineOfSightForClosestPlayer(180f, 20, 2);
            if (nearestPlayer != null) {
                targetPlayer = nearestPlayer;
            }
            return nearestPlayer != null;
        }
    }
}
