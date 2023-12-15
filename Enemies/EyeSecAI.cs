using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static Welcome_To_Ooblterra.Enemies.BabyLurkerAI;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {


    public class EyeSecAI : EnemyAI {

        //BEHAVIOR STATES
        private class Patrol : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (investigate) {
                    investigateTimer++;
                    if(investigateTimer > 500) {
                        investigate = false;
                    }
                    return;
                }
                if (Vector3.Distance(self.transform.position, self.destination) < 5) {
                    investigate = true;
                }
                if (!SearchInProgress) {
                    self.agent.speed = 7f;
                    self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ShouldStartScanTransition()
            };
        }
        private class ScanEnemies : BehaviorState {
            public bool investigate;
            public int investigateTimer;
            public EyeSecAI EyeSecSelf;
            public bool ShouldPatrol;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Scanning", value: true);
                //need to do work on the prefab in order to be able to fully implement this. Current idea:
                if (EyeSecSelf == null) {
                    EyeSecSelf = self as EyeSecAI;
                }
                EyeSecSelf.ScanFinished = false;
                //grab the scan collider. enable it

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                //scan from bottom of prefab to +500, adding +1 to the y value each time on the collider 
                //if a player holding scrap
                EyeSecSelf.FoundPlayerHoldingScrap = true;
                    //self.targetPlayer = 
                //if reached top of scan and found no scrap
                    EyeSecSelf.FoundPlayerHoldingScrap = false;
                    EyeSecSelf.ScanFinished = true;
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Scanning", value: false);
                //Disable the scan collider and put it back to the root
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReturnToPatrol(),
                new BeginAttack()
            };
        }
        private class Attack : BehaviorState {
            private int laserTimer = 0;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: true);

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (self.HasLineOfSightToPosition(self.targetPlayer.transform.position)) {
                    creatureAnimator.SetBool("Attacking", value: true);
                    laserTimer++;
                    if (laserTimer > 150) {
                        self.targetPlayer.DamagePlayer(150, hasDamageSFX: true, callRPC: true, CauseOfDeath.Blast, 0);
                    }
                } else {
                    creatureAnimator.SetBool("Attacking", value: false);
                    laserTimer = 0;
                    self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position, true).position);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FinishKill()
            };
        }
        //STATE TRANSITIONS
        private class ShouldStartScanTransition : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                if(players.Length > 0 && SelfEyeSec.enemyRandom.Next(0,50) > 35) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }
        private class ReturnToPatrol : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                if (SelfEyeSec.ScanFinished) { 
                    return !SelfEyeSec.FoundPlayerHoldingScrap;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }

        }
        private class BeginAttack : StateTransition {
            EyeSecAI SelfEyeSec;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                return SelfEyeSec.FoundPlayerHoldingScrap;
            }
            public override BehaviorState NextState() {
                return new Attack();
            }

        }
        private class FinishKill : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                if(self.targetPlayer.health < 0) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }

        }
        
        public static List<GrabbableObject> grabbableObjectsInMap = new List<GrabbableObject>();
        private BehaviorState InitialState = new Patrol();
        private BehaviorState ActiveState = null;
        private System.Random enemyRandom;
        private RoundManager roundManager;
        private float AITimer;
        private List<PlayerControllerB> scannedPlayers;
        public bool FoundPlayerHoldingScrap = false;
        public bool ScanFinished = false;
        protected override string __getTypeName() {
            return "EyeSecAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
            ActiveState = InitialState;
            RefreshGrabbableObjectsInMapList();
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
        public static void RefreshGrabbableObjectsInMapList() {
            grabbableObjectsInMap.Clear();
            GrabbableObject[] array = FindObjectsOfType<GrabbableObject>();
            for (int i = 0; i < array.Length; i++) {
                if (array[i].scrapValue != 0) {
                    grabbableObjectsInMap.Add(array[i]);
                }
            }
        }
    }
}
