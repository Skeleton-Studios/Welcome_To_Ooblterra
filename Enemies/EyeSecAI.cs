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
            public bool SearchInProgress;
            private int PatrolPointAttempts;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
                SearchInProgress = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 7f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(self.transform.position, self.destination) < 3 && SearchInProgress) {
                    Instance.LogMessage("Finding next patrol point");
                    PatrolPointAttempts = 0;
                    SearchInProgress = false;
                    return;
                }
                if (SearchInProgress) {
                    return;
                }
                PatrolPointAttempts++;
                Instance.LogMessage("Attempt #" + PatrolPointAttempts + " Didn't find patrol point, trying again...");
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
            bool ScanClipStarted = false;
            public ScanEnemies() {
                RandomRange = new Vector2(5, 8);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Scanning", value: true);
                Instance.SetScannerBoolOnServerRpc("Scanning", true);
                self.agent.speed = 0f;
                Instance.ScanFinished = false;
                investigateTimer = 0;
                Instance.IsScanning = true;
                Instance.Collider.enabled = true;
                self.creatureVoice.PlayOneShot(Instance.StartScanSFX);

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                if(AnimWaiter < 15) {
                    AnimWaiter++;
                    return;
                } else if(!ScanClipStarted) {
                    self.creatureVoice.clip = Instance.ScanSFX;
                    self.creatureVoice.loop = true;
                    self.creatureVoice.Play();
                    ScanClipStarted = true;
                }
                if(investigateTimer <= 360) {
                    Instance.ScanRoom();
                } else {
                    self.creatureVoice.Stop();
                    self.creatureVoice.loop = false;
                    self.creatureVoice.PlayOneShot(Instance.EndScanSFX);
                    Instance.ScanFinished = true;

                    Instance.ScanCooldownSeconds = MyRandomInt;
                    Instance.IsScanning = false;
                    Instance.SetScannerBoolOnServerRpc("Scanning", false);
                }
                investigateTimer++;
                                   
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Scanning", value: false);

                Instance.Collider.enabled = false;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReturnToPatrol(),
                new BeginAttack()
            };
        }
        private class Attack : BehaviorState {
            private float laserTimer = 0;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.MyLaser.SetLaserEnabled(true);
                creatureAnimator.SetBool("Attacking", value: true);
                self.agent.speed = 0f;
                self.creatureVoice.PlayOneShot(Instance.AttackSFX);
                
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: true);
                Instance.TryFlash();
                Instance.MyLaser.SetLaserEnabled(true);
                Instance.PlayerTracker.transform.position = self.targetPlayer.transform.position;
                Quaternion LookRot = new Quaternion();
                LookRot.SetLookRotation((self.targetPlayer.transform.position - self.transform.position) * -1);
                Instance.Head.transform.rotation = LookRot;
                laserTimer += Time.deltaTime;
                if (laserTimer > 2) {
                    self.targetPlayer.DamagePlayer(150, causeOfDeath: CauseOfDeath.Blast);
                    self.creatureVoice.PlayOneShot(Instance.BurnSFX);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Attacking", value: false);                
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FinishKill(),
                new PlayerOutOfRange(),
                new PlayerLeft()
            };
        }
        private class MoveToAttackPosition : BehaviorState {


            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.agent.speed = 9f;
                Instance.MyLaser.SetLaserEnabled(false);
                Instance.PlayerTracker.transform.position = self.transform.position;
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position, true).position);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InRangeOfPlayer(),
                new PlayerLeft()
            };
        }

        //STATE TRANSITIONS
        private class ShouldStartScanTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                bool CanInvestigate = Instance.enemyRandom.Next(0, 50) > 35;
                PlayerControllerB[] players = Instance.GetAllPlayersInLineOfSight(180, 30);

                if (players == null || Instance.ScanCooldownSeconds > 0) {
                    return false;
                }
                if(players.Length > 0 && CanInvestigate) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }
        private class ReturnToPatrol : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (Instance.ScanFinished) { 
                    return !Instance.FoundPlayerHoldingScrap;
                }
                return false;
            }
            public override BehaviorState NextState() {
                //Collider.enabled = false;
                return new Patrol();
            }

        }
        private class BeginAttack : StateTransition {
            public override bool CanTransitionBeTaken() {
                return Instance.FoundPlayerHoldingScrap;
            }
            public override BehaviorState NextState() {
                return new Attack();
            }

        }
        private class FinishKill : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(Instance.targetPlayer == null || Instance.targetPlayer.isPlayerDead) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                Instance.MyLaser.SetLaserEnabled(false);
                Instance.FoundPlayerHoldingScrap = false;
                Instance.targetPlayer = null;
                return new Patrol();
            }

        }
        private class PlayerOutOfRange : StateTransition {
            public override bool CanTransitionBeTaken() {
                return !Instance.HasLineOfSightToPosition(Instance.targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!Instance.PlayerIsTargetable(Instance.targetPlayer)) { 
                    return new Patrol();
                }
                return new MoveToAttackPosition();
            }

        }
        private class InRangeOfPlayer : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (!Instance.PlayerIsTargetable(Instance.targetPlayer)) {
                    return true;
                }
                return Instance.HasLineOfSightToPosition(Instance.targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!Instance.PlayerIsTargetable(Instance.targetPlayer)) {
                    return new Patrol();
                }
                return new Attack();
            }
        }
        private class PlayerLeft : StateTransition {
            public override bool CanTransitionBeTaken() {
                return !Instance.PlayerCanBeTargeted(Instance.targetPlayer);
            }
            public override BehaviorState NextState() {
                Instance.targetPlayer = null;
                return new Patrol();
            }
        }

        [SerializeField]
        public GameObject Head;
        public BoxCollider Collider;
        public GameObject Wheel;
        public Animator ScanAnim;
        public EyeSecLaser MyLaser;
        public Transform PlayerTracker;
        public static EyeSecAI Instance;

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
        private float ScanCooldownSeconds;
        private float FlashCooldownSeconds = 10f;
        private bool PlayingMoveSound;

        public override void Start() {
            Instance = this;
            InitialState = new Patrol();
            RefreshGrabbableObjectsInMapList();
            PrintDebugs = true;
            base.Start();

        }
        public override void Update() {
            LowerTimerValue(ref ScanCooldownSeconds);
            LowerTimerValue(ref FlashCooldownSeconds);
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
                ChangeOwnershipOfEnemy(victim.actualClientId);
                ScanCooldownSeconds = 5;
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
        private void TryFlash() {
            if(FlashCooldownSeconds <= 0) {
                Flash();
                FlashCooldownSeconds = 10f;
            }
        }
        public void Flash() {           
            creatureVoice.PlayOneShot(flashSFX);
            WalkieTalkie.TransmitOneShotAudio(creatureVoice, flashSFX);
            StunGrenadeItem.StunExplosion(transform.position, affectAudio: false, 2f, 4f, 2f);      
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            targetPlayer = playerWhoHit;
            ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
            OverrideState(new Attack());
        }

        [ServerRpc]
        internal void SetScannerBoolOnServerRpc(string name, bool state) {
            if (IsServer) {
                LogMessage("Changing anim!");
                ScanAnim.SetBool(name, state);
            }
        }
    }
}
