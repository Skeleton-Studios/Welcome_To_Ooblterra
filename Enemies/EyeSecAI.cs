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

    public class EyeSecAI : WTOEnemy {

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
                EyeSecSelf = self as EyeSecAI;
                EyeSecSelf.ScanFinished = false;
                investigateTimer = 0;
                //grab the scan collider. enable it
                Collider.enabled = true;

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if(investigateTimer <= 360) {
                    ScanRoom();
                } else {
                    EyeSecSelf.ScanFinished = true;
                }
                investigateTimer++;
                                   
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
                Collider.enabled = false;
                return new Patrol();
            }

        }
        private class BeginAttack : StateTransition {
            EyeSecAI SelfEyeSec;
            public override bool CanTransitionBeTaken() {
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

        [SerializeField]
        public static GameObject Head;
        public static BoxCollider Collider;

        [HideInInspector]
        public static List<GrabbableObject> grabbableObjectsInMap = new List<GrabbableObject>();
        public bool FoundPlayerHoldingScrap = false;
        public bool ScanFinished = false;
        public bool IsScanning;

        public override void Start() {
            InitialState = new Patrol();
            RefreshGrabbableObjectsInMapList();
            PrintDebugs = true;
            base.Start();
            
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
        public static void ScanRoom() {
            Head.transform.Rotate(0, 1, 0);
        }
        private void OnTriggerStay(Collider other) {
            if (!IsScanning) {
                return;
            }
            PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
            if (!other.gameObject.CompareTag("Player")) {
                return;
            }
            if (!PlayerCanBeTargeted(victim)) {
                return;
            }
            //grab a list of all the items he has and check if its in the grabbable objects list
            if (grabbableObjectsInMap.Contains(victim.currentlyHeldObjectServer)) {
                //if it is...
                FoundPlayerHoldingScrap = true;
                ScanFinished = true;
                targetPlayer = victim;
                return;
            }
        }
    }
}
