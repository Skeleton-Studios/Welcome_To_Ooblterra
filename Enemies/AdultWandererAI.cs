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
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                WTOBase.LogToConsole("SPAWN WANDERER");
                creatureAnimator.SetBool("Spawn", value: true);
                AWandList[enemyIndex].creatureSFX.PlayOneShot(AWandList[enemyIndex].SpawnSound);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (SpawnTimer > SpawnTime) {
                    AWandList[enemyIndex].spawnFinished = true;
                } else {
                    SpawnTimer++;
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Spawn", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluateEnemyState()
            };
        }
        private class WaitForTargetLook : BehaviorState {
            private int LookWaitTime = 0;
            private int LookWaitTimer = 3500;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (LookWaitTime > LookWaitTimer) {
                    AWandList[enemyIndex].LostPatience = true;
                } else {
                    LookWaitTime++;
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EvaluatePlayerLook()
            };

        }
        private class Attack : BehaviorState {
            bool HasAttacked;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                AWandList[enemyIndex].AttackCooldownSeconds = 1.5f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(AWandList[enemyIndex].MainTarget.transform.position, AWandList[enemyIndex].transform.position) < AWandList[enemyIndex].AttackRange) {
                    if (AWandList[enemyIndex].AttackCooldownSeconds <= 0) {
                        AWandList[enemyIndex].LogMessage("Attacking!");
                        AWandList[enemyIndex].AttackCooldownSeconds = 1.5f;
                        HasAttacked = false;
                        
                        return;
                    }
                    if (AWandList[enemyIndex].AttackCooldownSeconds <= 1.2f) {
                        creatureAnimator.SetBool("Attacking", value: true);
                    }
                    if(AWandList[enemyIndex].AttackCooldownSeconds <= 0.76f && !HasAttacked) {
                        AWandList[enemyIndex].MeleeAttackPlayer(AWandList[enemyIndex].MainTarget);
                        HasAttacked = true;
                    }
                    return;
                }
                AWandList[enemyIndex].AttackCooldownSeconds = 0;
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
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
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
                TotalInvestigateTime = enemyRandom.Next(100, 300);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (investigating) {
                    investigateTimer++;
                    return;
                }
                if (investigateTimer > TotalInvestigateTime) {
                    investigating = false;
                    SearchInProgress = false;
                }
                
                if (!SearchInProgress) {
                    AWandList[base.enemyIndex].agent.speed = 7f;
                    AWandList[base.enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(AWandList[enemyIndex].allAINodes[enemyRandom.Next(AWandList[base.enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                    SearchInProgress = true;
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EnemyInOverworld()
            };
        }
        private class Chase : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
                AWandList[enemyIndex].agent.speed = 8f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                AWandList[enemyIndex].SetDestinationToPosition(AWandList[enemyIndex].MainTarget.transform.position);
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new EnemyInShipOrFacility(),
                new EnemyEnteredRange()
            };
        }
        private class Stunned : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: true);
                AWandList[enemyIndex].agent.speed = 0f;
                AWandList[enemyIndex].targetPlayer = AWandList[enemyIndex].stunnedByPlayer;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Stunned", value: false);
                AWandList[enemyIndex].agent.speed = 8f;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new NoLongerStunned()
            };
        }

        //STATE TRANSITIONS
        private class EvaluateEnemyState : StateTransition {
            
            public override bool CanTransitionBeTaken() {
                
                return AWandList[enemyIndex].spawnFinished;
            }
            public override BehaviorState NextState() {
                
                if (AWandList[enemyIndex].MainTarget == null) {
                    return new Roam();
                }
                return new WaitForTargetLook();
            }
        }
        private class EvaluatePlayerLook : StateTransition {
            public override bool CanTransitionBeTaken() {
                WTOBase.LogToConsole("Player sees Adult Wanderer: " + AWandList[enemyIndex].CheckForPlayerLOS().ToString());
                if (AWandList[enemyIndex].CheckForPlayerLOS()){
                    return true;
                }
                return AWandList[enemyIndex].LostPatience;
            }
            public override BehaviorState NextState() {
                return new Attack();
            }
        }
        private class EnemyInShipOrFacility : StateTransition {
            public override bool CanTransitionBeTaken() {
                
                return !AWandList[base.enemyIndex].PlayerIsTargetable(AWandList[base.enemyIndex].MainTarget);
            }
            public override BehaviorState NextState() {
                    return new Roam();
            }
        }
        private class EnemyInOverworld : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(AWandList[enemyIndex].targetPlayer == null) {
                    return false;
                }
                return AWandList[enemyIndex].PlayerCanBeTargeted(AWandList[enemyIndex].MainTarget);
            }
            public override BehaviorState NextState() {
                return new Chase();
            }
        }
        private class EnemyLeftRange : StateTransition{
            public override bool CanTransitionBeTaken() {
                
                return (AWandList[enemyIndex].PlayerIsTargetable(AWandList[enemyIndex].MainTarget) && (Vector3.Distance(AWandList[enemyIndex].transform.position, AWandList[enemyIndex].MainTarget.transform.position) > AWandList[enemyIndex].AttackRange));
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class EnemyKilled : StateTransition {
            
            public override bool CanTransitionBeTaken() {
                
                return (AWandList[enemyIndex].MainTarget.health < 0); 
            }
            public override BehaviorState NextState() {
                AWandList[enemyIndex].MainTarget = null;
                return new Roam();
            }
        }
        private class EnemyEnteredRange : StateTransition {
            
            public override bool CanTransitionBeTaken() {
                
                return (AWandList[enemyIndex].PlayerCanBeTargeted(AWandList[enemyIndex].MainTarget) && (Vector3.Distance(AWandList[enemyIndex].transform.position, AWandList[enemyIndex].MainTarget.transform.position) < AWandList[enemyIndex].AttackRange));
            }
            public override BehaviorState NextState() {
                return new Attack();
            }
        }
        private class HitByStunGun : StateTransition {
            
            public override bool CanTransitionBeTaken() {
                
                return AWandList[enemyIndex].stunNormalizedTimer > 0 && !(AWandList[enemyIndex].ActiveState is Stunned);
            }
            public override BehaviorState NextState() {
                return new Stunned();
            }
        }
        private class NoLongerStunned : StateTransition {
            public override bool CanTransitionBeTaken() {
                return AWandList[enemyIndex].stunNormalizedTimer <= 0;
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
        public static Dictionary<int, AdultWandererAI> AWandList = new Dictionary<int, AdultWandererAI>();
        public static int AWandID;
        public AudioClip SpawnSound;

        public override void Start() {
            InitialState = new Spawn();
            AWandID++;
            WTOEnemyID = AWandID;
            //PrintDebugs = true;
            LogMessage($"Adding Adult Wanderer {this} #{AWandID}");
            AWandList.Add(AWandID, this);
            MyValidState = PlayerState.Outside;
            enemyHP = 10;
            GlobalTransitions.Add(new HitByStunGun());
            base.Start();
        }
        public override void Update() {
            LowerTimerValue(ref AttackCooldownSeconds);
            base.Update();
        }
        private void MeleeAttackPlayer(PlayerControllerB Target) {
            LogMessage("Attacking player!");
            Target.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning, 0);
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
            if (isEnemyDead) { return; }
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            LogMessage("Adult Wanderer HP remaining: " + enemyHP);
            creatureAnimator.SetTrigger("Hit");
            if (IsOwner) {
                if (enemyHP <= 0) {
                    isEnemyDead = true;
                    creatureAnimator.SetTrigger("Killed");
                    creatureVoice.Stop();
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            //If we're attacked by a player, they need to be immediately set to our target player
            targetPlayer = playerWhoHit;
            MainTarget = playerWhoHit;
            OverrideState(new Attack());
        }
    }
}
