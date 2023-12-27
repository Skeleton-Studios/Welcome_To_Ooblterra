using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.Enemies {

    public class EyeSecAI : WTOEnemy {


        //BEHAVIOR STATES
        private class Patrol : BehaviorState {
            EyeSecAI SelfEyeSec;
            public bool SearchInProgress;
            private int PatrolPointAttempts;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                SelfEyeSec = self as EyeSecAI;
                creatureAnimator.SetBool("Moving", value: true);
                SearchInProgress = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 7f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(self.transform.position, self.destination) < 3 && SearchInProgress) {
                    SelfEyeSec.LogMessage("Finding next patrol point");
                    PatrolPointAttempts = 0;
                    SearchInProgress = false;
                    return;
                }
                if (SearchInProgress) {
                    return;
                }
                PatrolPointAttempts++;
                SelfEyeSec.LogMessage("Attempt #" + PatrolPointAttempts + " Didn't find patrol point, trying again...");
                if (PatrolPointAttempts < 10) {
                    SearchInProgress = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                } else {
                    SearchInProgress = true;
                    self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.transform.position, 50));
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
            public int AnimWaiter = 0;
            public int investigateTimer;
            public EyeSecAI EyeSecSelf;
            public bool ShouldPatrol;
            bool ScanClipStarted = false;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecSelf = self as EyeSecAI;
                creatureAnimator.SetBool("Scanning", value: true);
                EyeSecSelf.ScanAnim.SetBool("Scanning", value: true);
                self.agent.speed = 0f;
                EyeSecSelf = self as EyeSecAI;
                EyeSecSelf.ScanFinished = false;
                investigateTimer = 0;
                EyeSecSelf.IsScanning = true;
                EyeSecSelf.Collider.enabled = true;
                self.creatureVoice.PlayOneShot(EyeSecSelf.StartScanSFX);

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                if(AnimWaiter < 15) {
                    AnimWaiter++;
                    return;
                } else if(!ScanClipStarted) {
                    self.creatureVoice.clip = EyeSecSelf.ScanSFX;
                    self.creatureVoice.loop = true;
                    self.creatureVoice.Play();
                    ScanClipStarted = true;
                }
                if(investigateTimer <= 360) {
                    EyeSecSelf.ScanRoom();
                } else {
                    self.creatureVoice.Stop();
                    self.creatureVoice.loop = false;
                    self.creatureVoice.PlayOneShot(EyeSecSelf.EndScanSFX);
                    EyeSecSelf.ScanFinished = true;
                    
                    EyeSecSelf.ScanCooldown = 300;
                    EyeSecSelf.IsScanning = false;
                    EyeSecSelf.ScanAnim.SetBool("Scanning", value: false);
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
            public EyeSecAI EyeSecSelf;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecSelf = self as EyeSecAI;
                EyeSecSelf.MyLaser.SetLaserEnabled(true);
                creatureAnimator.SetBool("Attacking", value: true);
                self.agent.speed = 0f;
                self.creatureVoice.PlayOneShot(EyeSecSelf.AttackSFX);
                //EyeSecSelf.Flash();
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: true);
                EyeSecSelf.MyLaser.SetLaserEnabled(true);
                EyeSecSelf.PlayerTracker.transform.position = self.targetPlayer.transform.position;
                Quaternion LookRot = new Quaternion();
                LookRot.SetLookRotation((self.targetPlayer.transform.position - self.transform.position) * -1);
                EyeSecSelf.Head.transform.rotation = LookRot;
                laserTimer++;
                if (laserTimer > 120) {
                    self.targetPlayer.DamagePlayer(150, causeOfDeath: CauseOfDeath.Blast);
                    self.creatureVoice.PlayOneShot(EyeSecSelf.BurnSFX);
                } 
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: false);
                
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FinishKill(),
                new PlayerOutOfRange()
            };
        }

        private class MoveToAttackPosition : BehaviorState {
            public EyeSecAI EyeSecSelf;


            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecSelf = self as EyeSecAI;
                self.agent.speed = 9f;
                EyeSecSelf.MyLaser.SetLaserEnabled(false);
                EyeSecSelf.PlayerTracker.transform.position = self.transform.position;
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position, true).position);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InRangeOfPlayer()
            };
        }
        //STATE TRANSITIONS
        private class ShouldStartScanTransition : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                SelfEyeSec = self as EyeSecAI;
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight(180, 30);
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
                if(self.targetPlayer == null || self.targetPlayer.isPlayerDead) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                SelfEyeSec = self as EyeSecAI;
                SelfEyeSec.MyLaser.SetLaserEnabled(false);
                SelfEyeSec.PlayerTracker.transform.position = (self.transform.position);
                SelfEyeSec.FoundPlayerHoldingScrap = false;
                self.targetPlayer = null;
                return new Patrol();
            }

        }
        private class PlayerOutOfRange : StateTransition {
            EyeSecAI SelfEyeSec;
            public override bool CanTransitionBeTaken() {
                SelfEyeSec = self as EyeSecAI;
                return !self.HasLineOfSightToPosition(self.targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!self.PlayerIsTargetable(self.targetPlayer)) { 
                    return new Patrol();
                }
                return new MoveToAttackPosition();
            }

        }
        private class InRangeOfPlayer : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (!self.PlayerIsTargetable(self.targetPlayer)) {
                    return true;
                }
                return self.HasLineOfSightToPosition(self.targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!self.PlayerIsTargetable(self.targetPlayer)) {
                    return new Patrol();
                }
                return new Attack();
            }
        }

        [SerializeField]
        public GameObject Head;
        public BoxCollider Collider;
        public GameObject Wheel;
        public Animator ScanAnim;
        public EyeSecLaser MyLaser;
        public Transform PlayerTracker;

        public AudioClip flashSFX;
        public AudioClip StartScanSFX;
        public AudioClip EndScanSFX;
        public AudioClip AttackSFX;
        public AudioClip MoveSFX;
        public AudioClip ScanSFX;
        public AudioClip BurnSFX;

        [HideInInspector]
        private static List<GrabbableObject> grabbableObjectsInMap = new List<GrabbableObject>();
        private bool FoundPlayerHoldingScrap = false;
        private bool ScanFinished = false;
        private bool IsScanning;
        private int ScanCooldown;
        private bool PlayingMoveSound;

        public override void Start() {
            InitialState = new Patrol();
            RefreshGrabbableObjectsInMapList();
            PrintDebugs = true;
            base.Start();

        }
        public override void Update() {
            if(ScanCooldown > 0) {
                ScanCooldown--;
            }
            
            SpinWheel();
            base.Update();
            
            
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
            //Wheel.transform.forward = agent.transform.forward;
            if(agent.speed > 0) {
                Wheel.transform.Rotate(-160 * Time.deltaTime, 0, 0);
                if (!PlayingMoveSound) {
                    creatureSFX.clip = MoveSFX;
                    creatureSFX.Play();
                    PlayingMoveSound = true;
                }
            } else {
                if (PlayingMoveSound) {
                    creatureSFX.Stop();
                    PlayingMoveSound = false;
                }
            }
        }

        [ClientRpc]
        public void RadarBoosterFlashClientRpc() {
            NetworkManager networkManager = base.NetworkManager;
            if ((object)networkManager != null && networkManager.IsListening) {
                if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost)) {
                    ClientRpcParams clientRpcParams = default(ClientRpcParams);
                    FastBufferWriter bufferWriter = __beginSendClientRpc(720948839u, clientRpcParams, RpcDelivery.Reliable);
                    __endSendClientRpc(ref bufferWriter, 720948839u, clientRpcParams, RpcDelivery.Reliable);
                }

                if (__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost)) {
                    Flash();
                }
            }
        }

        public void Flash() {           
            creatureVoice.PlayOneShot(flashSFX);
            WalkieTalkie.TransmitOneShotAudio(creatureVoice, flashSFX);
            StunGrenadeItem.StunExplosion(transform.position, affectAudio: false, 1f, 2f, 2f);      
        }
    }
}
