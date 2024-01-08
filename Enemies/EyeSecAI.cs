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
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
                SearchInProgress = EyeSecList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(EyeSecList[enemyIndex].allAINodes[enemyRandom.Next(EyeSecList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                EyeSecList[enemyIndex].agent.speed = 9f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(EyeSecList[enemyIndex].transform.position, EyeSecList[enemyIndex].destination) < 10 && SearchInProgress) {
                    EyeSecList[enemyIndex].LogMessage("Finding next patrol point");
                    PatrolPointAttempts = 0;
                    SearchInProgress = false;
                    return;
                }
                if (SearchInProgress) {
                    EyeSecList[enemyIndex].agent.speed = 9f;
                    return;
                }
                PatrolPointAttempts++;
                EyeSecList[enemyIndex].LogMessage("Attempt #" + PatrolPointAttempts + " Didn't find patrol point, trying again...");
                if (PatrolPointAttempts < 10) {
                    SearchInProgress = EyeSecList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(EyeSecList[enemyIndex].allAINodes[enemyRandom.Next(EyeSecList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                } else {
                    SearchInProgress = true;
                    EyeSecList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(EyeSecList[enemyIndex].transform.position, 50));

                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ShouldStartScanTransition()
            };
        }
        private class ScanEnemies : BehaviorState {
            public int AnimWaiter = 0;
            public int investigateTimer;
            public ScanEnemies() {
                RandomRange = new Vector2(5, 8);
            }
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecList[enemyIndex].StartScanVisuals();
                investigateTimer = 0;
                EyeSecList[enemyIndex].agent.speed = 0f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (AnimWaiter < 15) {
                    AnimWaiter++;
                    return;
                } else if (investigateTimer <= 360) {
                    EyeSecList[enemyIndex].ScanRoom();
                } else {
                    EyeSecList[enemyIndex].StopScanVisuals(EyeSecList[enemyIndex].EndScanSFX, MyRandomInt);
                }
                investigateTimer++;
                                   
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecList[enemyIndex].ScannerCollider.enabled = false;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReturnToPatrol(),
                new BeginAttack()
            };
        }
        private class Attack : BehaviorState {
            private float laserTimer = 0;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                //creatureAnimator.SetBool("Attacking", value: true);
                EyeSecList[enemyIndex].StartAttackVisuals();
                EyeSecList[enemyIndex].agent.speed = 0f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                //creatureAnimator.SetBool("Attacking", value: true);
                EyeSecList[enemyIndex].TryFlash();
                EyeSecList[enemyIndex].TrackPlayerWithHead();
                laserTimer += Time.deltaTime;
                if (laserTimer > 2) {
                    EyeSecList[enemyIndex].targetPlayer.DamagePlayer(150, causeOfDeath: CauseOfDeath.Blast);
                    EyeSecList[enemyIndex].creatureVoice.PlayOneShot(EyeSecList[enemyIndex].BurnSFX);
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecList[enemyIndex].StopAttackVisuals();
                //creatureAnimator.SetBool("Attacking", value: false);                
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FinishKill(),
                new PlayerOutOfRange(),
                new PlayerLeft(),
                new Stunned()
            };
        }
        private class MoveToAttackPosition : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecList[enemyIndex].agent.speed = 9f;
                EyeSecList[enemyIndex].StopAttackVisuals();
                EyeSecList[enemyIndex].PlayerTracker.transform.position = EyeSecList[enemyIndex].transform.position;
                EyeSecList[enemyIndex].SetDestinationToPosition(EyeSecList[enemyIndex].ChooseClosestNodeToPosition(EyeSecList[enemyIndex].targetPlayer.transform.position, true).position);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                EyeSecList[enemyIndex].SetDestinationToPosition(EyeSecList[enemyIndex].ChooseClosestNodeToPosition(EyeSecList[enemyIndex].targetPlayer.transform.position).position);
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new InRangeOfPlayer(),
                new PlayerLeft(),
                new Stunned()
            };
        }
        private class ShutDown : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                //Animate the eye going down
                EyeSecList[enemyIndex].agent.speed = 0f;
                EyeSecList[enemyIndex].LogMessage("Eyesec shut down!");
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WakeBackUp()
            };
        }

        //STATE TRANSITIONS
        private class ShouldStartScanTransition : StateTransition {
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                bool CanInvestigate = EyeSecList[enemyIndex].enemyRandom.Next(0, 50) > 35;
                PlayerControllerB[] players = EyeSecList[enemyIndex].GetAllPlayersInLineOfSight(180, 3);

                if (players == null || EyeSecList[enemyIndex].ScanCooldownSeconds > 0) {
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
                if (EyeSecList[enemyIndex].ScanFinished) { 
                    return !EyeSecList[enemyIndex].FoundPlayerHoldingScrap;
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
                return EyeSecList[enemyIndex].FoundPlayerHoldingScrap;
            }
            public override BehaviorState NextState() {
                EyeSecList[enemyIndex].StopScanVisuals(EyeSecList[enemyIndex].EndScanSFX, 0);
                return new Attack();
            }

        }
        private class FinishKill : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(EyeSecList[enemyIndex].targetPlayer == null || EyeSecList[enemyIndex].targetPlayer.isPlayerDead) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                EyeSecList[enemyIndex].FoundPlayerHoldingScrap = false;
                EyeSecList[enemyIndex].targetPlayer = null;
                return new Patrol();
            }

        }
        private class PlayerOutOfRange : StateTransition {
            public override bool CanTransitionBeTaken() {
                return !EyeSecList[enemyIndex].HasLineOfSightToPosition(EyeSecList[enemyIndex].targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!EyeSecList[enemyIndex].PlayerIsTargetable(EyeSecList[enemyIndex].targetPlayer)) { 
                    return new Patrol();
                }
                return new MoveToAttackPosition();
            }

        }
        private class InRangeOfPlayer : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (!EyeSecList[enemyIndex].PlayerIsTargetable(EyeSecList[enemyIndex].targetPlayer)) {
                    return true;
                }
                return EyeSecList[enemyIndex].HasLineOfSightToPosition(EyeSecList[enemyIndex].targetPlayer.transform.position, 360f);
            }
            public override BehaviorState NextState() {
                if (!EyeSecList[enemyIndex].PlayerIsTargetable(EyeSecList[enemyIndex].targetPlayer)) {
                    return new Patrol();
                }
                return new Attack();
            }
        }
        private class PlayerLeft : StateTransition {
            public override bool CanTransitionBeTaken() {
                return !EyeSecList[enemyIndex].PlayerCanBeTargeted(EyeSecList[enemyIndex].targetPlayer);
            }
            public override BehaviorState NextState() {
                EyeSecList[enemyIndex].targetPlayer = null;
                return new Patrol();
            }
        }
        private class Stunned : StateTransition {
            public override bool CanTransitionBeTaken() {
                return EyeSecList[enemyIndex].stunNormalizedTimer > 0f;
            }
            public override BehaviorState NextState() {
                EyeSecList[enemyIndex].StopAttackVisuals();
                EyeSecList[enemyIndex].StopScanVisuals(EyeSecList[enemyIndex].ShutdownSFX, 0);
                EyeSecList[enemyIndex].ShutdownTimerSeconds = 30f;
                EyeSecList[enemyIndex].targetPlayer = null;
                return new ShutDown();
            }
        }
        private class WakeBackUp : StateTransition {
            public override bool CanTransitionBeTaken() {
                return EyeSecList[enemyIndex].stunNormalizedTimer <= 0f && EyeSecList[enemyIndex].ShutdownTimerSeconds <= 0f;
            }
            public override BehaviorState NextState() {
                EyeSecList[enemyIndex].ScanCooldownSeconds = 0;
                return new Patrol();
            }
        }

        [SerializeField]
        public GameObject Head;
        public BoxCollider ScannerCollider;
        public GameObject Wheel;
        public Animator ScanAnim;
        public EyeSecLaser MyLaser;
        public Transform PlayerTracker;
        public static Dictionary<int, EyeSecAI> EyeSecList = new Dictionary<int, EyeSecAI>();
        public static int EyeSecID;

        public AudioClip flashSFX;
        public AudioClip StartScanSFX;
        public AudioClip EndScanSFX;
        public AudioClip AttackSFX;
        public AudioClip MoveSFX;
        public AudioClip ScanSFX;
        public AudioClip BurnSFX;
        public AudioClip ShutdownSFX;

        [HideInInspector]
        private static List<GrabbableObject> grabbableObjectsInMap = new List<GrabbableObject>();
        private bool FoundPlayerHoldingScrap = false;
        private bool ScanFinished = false;
        private bool IsScanning;
        private float ScanCooldownSeconds = 0f;
        private float FlashCooldownSeconds = 10f;
        private float ShutdownTimerSeconds = 0f;
        private bool PlayingMoveSound;

        public override void Start() {
            InitialState = new Patrol();
            RefreshGrabbableObjectsInMapList();
            PrintDebugs = true;
            base.Start();
            EyeSecID++;
            WTOEnemyID = EyeSecID;
            LogMessage($"Adding EyeSec {this} at {EyeSecID}");
            EyeSecList.Add(EyeSecID, this);

        }
        public override void Update() {
            LowerTimerValue(ref ScanCooldownSeconds);
            LowerTimerValue(ref FlashCooldownSeconds);
            LowerTimerValue(ref ShutdownTimerSeconds);
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
                postStunInvincibilityTimer = 0.05f;
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
            LogMessage("Player hit us!");
            if(ActiveState is ShutDown) {
                return;
            }
            OverrideState(new Attack());
        }

        [ServerRpc]
        internal void SetScannerBoolOnServerRpc(string name, bool state) {
            if (IsServer) {
                LogMessage("Changing anim!");
                ScanAnim.SetBool(name, state);
            }
        }

        public void StartScanVisuals() {
            creatureVoice.PlayOneShot(StartScanSFX);
            creatureVoice.clip = ScanSFX;
            creatureVoice.loop = true;
            creatureVoice.Play();

            SetScannerBoolOnServerRpc("Scanning", true);
            
            ScannerCollider.enabled = true;
            ScanFinished = false;
            IsScanning = true;

        }
        public void StopScanVisuals(AudioClip StopSound, int NextScanTime) {
            creatureVoice.Stop();
            creatureVoice.loop = false;
            if(StopSound != null) { 
                creatureVoice.PlayOneShot(StopSound);
            }
            SetScannerBoolOnServerRpc("Scanning", false);
            ScannerCollider.enabled = false;
            ScanFinished = true;
            IsScanning = false;
            ScanCooldownSeconds = NextScanTime;
        }
        public void StartAttackVisuals() {
            MyLaser.SetLaserEnabled(true);
            creatureVoice.PlayOneShot(AttackSFX);
            PlayerTracker.transform.position = targetPlayer.transform.position;

        }
        public void StopAttackVisuals() {
            MyLaser.SetLaserEnabled(false);
        }
        public void TrackPlayerWithHead() {
            Quaternion LookRot = new Quaternion();
            LookRot.SetLookRotation((targetPlayer.transform.position - transform.position) * -1);
            Head.transform.rotation = LookRot;
        }
    }
}
