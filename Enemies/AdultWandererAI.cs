using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {
    public class AdultWandererAI : EnemyAI {
        private class Spawn : BehaviorState {
            private int spawnTimer;
            private int SpawnTime = 500;
            AdultWandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
                Wanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if(spawnTimer < SpawnTime) {
                    Wanderer.spawnFinished = true;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluateEnemyState()
            };
        }
        private class Attack : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            AdultWandererAI AdultWanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: true);
                AdultWanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(AdultWanderer.MainTarget.transform.position, self.transform.position) < 10) {
                    AdultWanderer.MeleeAttackPlayer(AdultWanderer.MainTarget);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EnemyLeftRange(),
                new EnemyKilled()
            };
        }
        private class Roam : BehaviorState {
            public bool SearchInProgress;
            public bool investigating;
            public int investigateTimer;
            public int TotalInvestigateTime;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (investigating) {
                    investigateTimer++;
                    return;
                }
                if (investigateTimer > TotalInvestigateTime) {
                    investigating = false;
                    SearchInProgress = false;
                }
                
                if (!SearchInProgress) {
                    self.agent.speed = 7f;
                    self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                    SearchInProgress = true;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EnemyInOverworld()
            };
        }
        private class Chase : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            AdultWandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
                Wanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.SetDestinationToPosition(Wanderer.MainTarget.transform.position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EnemyInShipOrFacility(),
                new EnemyEnteredRange()
            };
        }
        private class EvaluateEnemyState : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return SelfWanderer.spawnFinished;
            }
            public override BehaviorState NextState() {
                if (SelfWanderer.MainTarget == null) {
                    return new Roam();
                }
                return new Attack();
            }
        }
        private class EnemyInShipOrFacility : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return !self.PlayerIsTargetable(SelfWanderer.MainTarget);
            }
            public override BehaviorState NextState() {
                    return new Roam();
            }
        }
        private class EnemyInOverworld : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                if(SelfWanderer.MainTarget = null) {
                    return false;
                }
                return self.PlayerIsTargetable(SelfWanderer.MainTarget);
            }
            public override BehaviorState NextState() {
                return new Chase();
            }
        }
        private class EnemyLeftRange : StateTransition{
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return !(self.PlayerIsTargetable(SelfWanderer.MainTarget) && (Vector3.Distance(self.transform.position, SelfWanderer.MainTarget.transform.position) < 10));
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class EnemyKilled : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return (SelfWanderer.MainTarget.health < 0); 
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class EnemyEnteredRange : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return (self.PlayerIsTargetable(SelfWanderer.MainTarget) && (Vector3.Distance(self.transform.position, SelfWanderer.MainTarget.transform.position) < 10));
            }
            public override BehaviorState NextState() {
                return new Attack();
            }
        }

        private BehaviorState InitialState = new Spawn();
        private BehaviorState ActiveState = null;
        private System.Random enemyRandom;
        private RoundManager roundManager;
        private float AITimer;
        private bool spawnFinished = false;
        public PlayerControllerB MainTarget = null;
        protected override string __getTypeName() {
            return "AdultWandererAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
        }
        public override void Update() {
            base.Update();
            AITimer++;
            //don't run enemy ai if they're dead

            if (isEnemyDead || !ventAnimationFinished) {
                return;
            }

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

            //Custom Monster Code
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
        }
        private void MeleeAttackPlayer(PlayerControllerB target) {
            target.DamagePlayer(80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning, 0);
            target.JumpToFearLevel(1f);
        }
    }
}
