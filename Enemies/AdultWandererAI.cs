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
            private int SpawnTimer;
            private int SpawnTime = 50;
            AdultWandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
                Wanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as AdultWandererAI;
                if (SpawnTimer > SpawnTime) {
                    Wanderer.spawnFinished = true;
                } else {
                    SpawnTimer++;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluateEnemyState()
            };
        }
        private class WaitForTargetLook : BehaviorState {
            private int LookWaitTime = 0;
            private int LookWaitTimer = 3500;
            AdultWandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Idle", value: false);
                Wanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as AdultWandererAI;
                if (LookWaitTime > LookWaitTimer) {
                    Wanderer.LostPatience = true;
                } else {
                    LookWaitTime++;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Idle", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluatePlayerLook()
            };

        }
        private class Attack : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            AdultWandererAI AdultWanderer;
            public int AttackCooldown = 60;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: true);
                AdultWanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                AdultWanderer = self as AdultWandererAI;
                if (Vector3.Distance(AdultWanderer.MainTarget.transform.position, self.transform.position) < 10) {
                    if(AttackCooldown <= 0) {
                        AttackCooldown = 500;
                        AdultWanderer.MeleeAttackPlayer(AdultWanderer.MainTarget);
                        return;
                    }
                    AttackCooldown--;
                    return;
                }
                AttackCooldown = 0;
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
                creatureAnimator.SetBool("Moving", value: true);
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
                creatureAnimator.SetBool("Moving", value: false);
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
                Wanderer = self as AdultWandererAI;
                self.agent.speed = 7f;
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
                SelfWanderer = self as AdultWandererAI;
                if (SelfWanderer.MainTarget == null) {
                    return new Roam();
                }
                return new WaitForTargetLook();
            }
        }

        private class EvaluatePlayerLook : StateTransition {
            public override bool CanTransitionBeTaken() {
                AdultWandererAI SelfWanderer = self as AdultWandererAI;
                WTOBase.LogToConsole("Player sees Adult Wanderer: " + SelfWanderer.CheckForPlayerLOS().ToString());
                if (SelfWanderer.CheckForPlayerLOS()){
                    return true;
                }
                return SelfWanderer.LostPatience;
            }
            public override BehaviorState NextState() {
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
            public override bool CanTransitionBeTaken() {
                AdultWandererAI SelfWanderer = self as AdultWandererAI;
                if(SelfWanderer.targetPlayer == null) {
                    return false;
                }
                return self.PlayerIsTargetable(SelfWanderer.MainTarget, true);
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
        private bool LostPatience = false;
        private bool MainTargetReachable = false;
        protected override string __getTypeName() {
            return "AdultWandererAI";
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
                    Debug.Log("Exiting: " + ActiveState.ToString());
                    ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
                    ActiveState = transition.NextState();
                    Debug.Log("Entering: " + ActiveState.ToString());
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
        public void SetMyTarget(PlayerControllerB player) {
            targetPlayer = player;
            MainTarget = player;
        }
        public bool CheckForPlayerLOS() {
            return MainTarget.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f);
        }
    }
}
