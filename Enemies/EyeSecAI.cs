using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
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
                self.agent.speed = 0f;
                EyeSecSelf = self as EyeSecAI;
                EyeSecSelf.ScanFinished = false;
                investigateTimer = 0;
                EyeSecSelf.IsScanning = true;
                EyeSecSelf.Collider.enabled = true;

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if(investigateTimer <= 360) {
                    EyeSecSelf.ScanRoom();
                } else {
                    EyeSecSelf.ScanFinished = true;
                    EyeSecSelf.ScanCooldown = 300;
                    EyeSecSelf.IsScanning = false;
                }
                investigateTimer++;
                                   
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Scanning", value: false);
                EyeSecSelf.Collider.enabled = false;
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
                if (self.HasLineOfSightToPosition(self.targetPlayer.transform.position + new Vector3(0, 5, 0), 360f)){
                    creatureAnimator.SetBool("Attacking", value: true);
                    laserTimer++;
                    if (laserTimer > 250) {
                        self.targetPlayer.DamagePlayer(150, hasDamageSFX: true, callRPC: true, CauseOfDeath.Blast, 0);
                    }
                } else {
                    WTOBase.LogToConsole("No LOS");
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
                EyeSecAI SelfEyeSec = self as EyeSecAI;
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight(180);
                if(players == null || SelfEyeSec.ScanCooldown > 0) {
                    return false;
                }
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
                SelfEyeSec = self as EyeSecAI;
                if (SelfEyeSec.ScanFinished) { 
                    return !SelfEyeSec.FoundPlayerHoldingScrap;
                }
                return false;
            }
            public override BehaviorState NextState() {
                //Collider.enabled = false;
                return new Patrol();
            }

        }
        private class BeginAttack : StateTransition {
            EyeSecAI SelfEyeSec;
            public override bool CanTransitionBeTaken() {
                SelfEyeSec = self as EyeSecAI;
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
                SelfEyeSec = self as EyeSecAI;
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
        public GameObject Head;
        public BoxCollider Collider;
        public GameObject Wheel;

        [HideInInspector]
        private static List<GrabbableObject> grabbableObjectsInMap = new List<GrabbableObject>();
        private bool FoundPlayerHoldingScrap = false;
        private bool ScanFinished = false;
        private bool IsScanning;
        private int ScanCooldown;


        public override void Start() {
            InitialState = new Patrol();
            RefreshGrabbableObjectsInMapList();
            //PrintDebugs = true;
            base.Start();

        }

        public override void Update() {
            if(ScanCooldown > 0) {
                ScanCooldown--;
            }
            base.Update();
            SpinWheel();
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
        private void ScanRoom() {
            Head.transform.Rotate(0, 1, 0);
        }
        public void ScanOurEnemy(Collider other) {
            
            if (!IsScanning) {
                return;
            }

            PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();

            if (victim == null) {
                return;
            }
            if (!PlayerCanBeTargeted(victim)) {
                return;
            }
            LogMessage("Player found, time to scan him...");
            //grab a list of all the items he has and check if its in the grabbable objects list
            if (grabbableObjectsInMap.Contains(victim.currentlyHeldObjectServer)) {
                //if it is...
                LogMessage("Player is guilty!");
                FoundPlayerHoldingScrap = true;
                ScanFinished = true;
                targetPlayer = victim;
                ScanCooldown = 300;
                IsScanning = false;
                return;
            }
        }

        private void SpinWheel() {
            Wheel.transform.forward = agent.transform.forward;
            if (agent.speed > 0f) {
                Wheel.transform.Rotate(-1, 0, 0);
            }
            
        }
    }
}
