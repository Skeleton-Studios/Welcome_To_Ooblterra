using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies {
    public class AdultWandererAI : WTOEnemy {

        //BEHAVIOR STATES
        private class Spawn : BehaviorState {
            private int SpawnTimer;
            private int SpawnTime = 80;
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
                creatureAnimator.SetBool("Moving", value: false);
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
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluatePlayerLook()
            };

        }
        private class Attack : BehaviorState {
            AdultWandererAI AdultWanderer;
            bool HasAttacked;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                AdultWanderer = self as AdultWandererAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                AdultWanderer = self as AdultWandererAI;
                if (Vector3.Distance(AdultWanderer.MainTarget.transform.position, self.transform.position) < AdultWanderer.AttackRange) {
                    if (AdultWanderer.AttackCooldownSeconds <= 0) {
                        AdultWanderer.LogMessage("Attacking!");
                        AdultWanderer.AttackCooldownSeconds = 0.95f;
                        HasAttacked = false;
                        creatureAnimator.SetBool("Attacking", value: true);
                        return;
                    }
                    if(AdultWanderer.AttackCooldownSeconds <= 0.76f && !HasAttacked) {
                        AdultWanderer.MeleeAttackPlayer(AdultWanderer.MainTarget);
                        HasAttacked = true;
                    }
                    return;
                }
                AdultWanderer.AttackCooldownSeconds = 0;
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
                TotalInvestigateTime = enemyRandom.Next(100, 300);
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
            AdultWandererAI Wanderer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
                Wanderer = self as AdultWandererAI;
                self.agent.speed = 8f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as AdultWandererAI;
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
        private class Stunned : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: true);
                self.agent.speed = 0f;
                self.targetPlayer = self.stunnedByPlayer;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: false);
                self.agent.speed = 8f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new NoLongerStunned()
            };
        }

        //STATE TRANSITIONS
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
                return SelfWanderer.PlayerCanBeTargeted(SelfWanderer.MainTarget);
            }
            public override BehaviorState NextState() {
                return new Chase();
            }
        }
        private class EnemyLeftRange : StateTransition{
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return (self.PlayerIsTargetable(SelfWanderer.MainTarget) && (Vector3.Distance(self.transform.position, SelfWanderer.MainTarget.transform.position) > SelfWanderer.AttackRange));
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
                SelfWanderer.MainTarget = null;
                return new Roam();
            }
        }
        private class EnemyEnteredRange : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return (SelfWanderer.PlayerCanBeTargeted(SelfWanderer.MainTarget) && (Vector3.Distance(self.transform.position, SelfWanderer.MainTarget.transform.position) < SelfWanderer.AttackRange));
            }
            public override BehaviorState NextState() {
                return new Attack();
            }
        }
        private class NoLongerStunned : StateTransition {
            AdultWandererAI SelfWanderer;
            public override bool CanTransitionBeTaken() {
                SelfWanderer = self as AdultWandererAI;
                return self.stunNormalizedTimer <= 0;
            }
            public override BehaviorState NextState() {
                return new Chase();
            }
        }

        private bool spawnFinished = false;
        public PlayerControllerB MainTarget = null;
        private bool LostPatience = false;
        private float AttackCooldownSeconds = 1.2f;
        public int AttackRange = 7;
        public override void Start() {
            InitialState = new Spawn();
            PrintDebugs = true;
            base.Start();
            MyValidState = PlayerState.Outside;
            enemyHP = 10;
        }
        public override void Update() {
            LowerTimerValue(ref AttackCooldownSeconds);
            base.Update();
        }
        private void MeleeAttackPlayer(PlayerControllerB Target) {
            LogMessage("Attacking player!");
            Target.DamagePlayer(80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning, 0);
            Target.JumpToFearLevel(1f);
        }
        public void SetMyTarget(PlayerControllerB player) {
            targetPlayer = player;
            MainTarget = player;
        }
        public bool CheckForPlayerLOS() {
            return MainTarget.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f);
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            creatureAnimator.SetTrigger("Hit");
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    creatureAnimator.SetTrigger("Killed");
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            //If we're attacked by a player, they need to be immediately set to our target player
            targetPlayer = playerWhoHit;
            OverrideState(new Attack());
        }
    }
}
