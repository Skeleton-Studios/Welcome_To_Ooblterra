using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies.EnemyThings;

namespace Welcome_To_Ooblterra.Enemies;

public class EnforcerAI : WTOEnemy {

    [Header("Balance Constants")]
    public static float SecondsUntilBeginStalking = 5f;
    public static float StalkSpeed = 6f;
    public static float ChaseSpeed = 10f; 
    public static float RangeForPotentialTarget = 10f;
    public static int EnforcerAttackDamage = 100;
    public static float ActiveCamoVisibility = 0.025f;

    private static float SecondsUntilNextSound = 15f;

    [Header("Defaults")]
    public SkinnedMeshRenderer[] Meshes;
    public Material ActiveCamoMaterial; 
    public float EyeSweepAnimSeconds = 2f;
    public float ActiveCamoLerpTime = 2f;
    public AudioClip[] RandomIdleSounds;
    public BoxCollider ScanNode;

    private List<Material> EnforcerMatList = new(); 

    //STATES
    private class GoToHidingSpot : BehaviorState {
        bool EnforcerMovingToHidePoint;
        Vector3 NextDestination;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerMovingToHidePoint = false;
            EnforcerList[enemyIndex].PlayerStareAtTimer = 0f;
            EnforcerList[enemyIndex].IsBeingSeenByPotentialTarget = false;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            EnforcerList[enemyIndex].EnforcerShouldKeepTrackOfTargetPlayer = false;
            EnforcerList[enemyIndex].PotentialTarget = null;
            EnforcerList[enemyIndex].targetPlayer = null; 
            EnforcerList[enemyIndex].agent.speed = 8f; 
            EnforcerList[enemyIndex].SetActiveCamoState(true);
            EnforcerList[enemyIndex].DetermineNextHidePoint();
            EnforcerList[enemyIndex].creatureSFX.volume = 1f;


        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (!EnforcerMovingToHidePoint) {
                EnforcerList[enemyIndex].LogMessage("Current hide point not valid, choosing another...");
                EnforcerList[enemyIndex].DetermineNextHidePoint();
                NextDestination = RoundManager.Instance.GetRandomNavMeshPositionInRadius(EnforcerList[enemyIndex].NextHidePoint.transform.position, radius: 2);
                EnforcerMovingToHidePoint = EnforcerList[enemyIndex].SetDestinationToPosition(NextDestination, true);
            }
            if (Vector3.Distance(EnforcerList[enemyIndex].transform.position, NextDestination) < 1) {
                EnforcerList[enemyIndex].ReachedNextPoint = true;
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundHidingSpot()
        };

    }
    private class WaitForPlayerToPass : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].ReachedNextPoint = false;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
            EnforcerList[enemyIndex].transform.rotation = EnforcerList[enemyIndex].NextHidePoint.transform.rotation;
            EnforcerList[enemyIndex].ShouldChasePotentialTarget = false;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].RandomlyPlayIdleSound(SecondsUntilNextSound);
            //if a player enters range, consider them our potential target
            if (EnforcerList[enemyIndex].PotentialTarget != null) {
                EnforcerList[enemyIndex].LogMessage($"Range check for target player: {EnforcerList[enemyIndex].PotentialTarget.playerUsername}; " +
                    $"\nin Range/Enforcer LOS? {EnforcerList[enemyIndex].IsTargetPlayerWithinLOS(EnforcerList[enemyIndex].PotentialTarget, range: (int)RangeForPotentialTarget, width: 180)}; " +
                    $"\nPlayer has LOS to Enforcer? {EnforcerList[enemyIndex].CheckPlayerLOSForEnforcer(EnforcerList[enemyIndex].PotentialTarget)}");
                
                if (EnforcerList[enemyIndex].PlayerWithinRange(EnforcerList[enemyIndex].PotentialTarget, RangeForPotentialTarget)) {
                    EnforcerList[enemyIndex].IsBeingSeenByPotentialTarget = EnforcerList[enemyIndex].CheckPlayerLOSForEnforcer(EnforcerList[enemyIndex].PotentialTarget);
                } else {
                    EnforcerList[enemyIndex].ShouldChasePotentialTarget = !EnforcerList[enemyIndex].IsBeingSeenByPotentialTarget;
                    if (!EnforcerList[enemyIndex].ShouldChasePotentialTarget) {
                        EnforcerList[enemyIndex].LogMessage($"Player left while looking at us, do not pursue!");
                        EnforcerList[enemyIndex].PotentialTarget = null;
                    }
                }
            } else { 
                PlayerControllerB PlayerInConsideration = EnforcerList[enemyIndex].GetClosestPlayer(requireLineOfSight: true);
                //set the player in consideration to our potential target if he's in range
                if (PlayerInConsideration != null && EnforcerList[enemyIndex].PlayerWithinRange(PlayerInConsideration, RangeForPotentialTarget)) {
                    EnforcerList[enemyIndex].PotentialTarget = PlayerInConsideration;
                    EnforcerList[enemyIndex].LogMessage($"Setting target player: {EnforcerList[enemyIndex].PotentialTarget.playerUsername}");
                }
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new PlayerPassedBy(),
            new PlayerStaringAtUs()
        };

    }
    private class StalkPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = StalkSpeed;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            EnforcerList[enemyIndex].EnforcerShouldKeepTrackOfTargetPlayer = true;
            EnforcerList[enemyIndex].creatureSFX.volume = 0.2f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //this might start pushing the player if the player stands still
            if (EnforcerList[enemyIndex].PlayerWithinRange(2f)) {
                EnforcerList[enemyIndex].agent.speed = 0f;
                EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
                EnforcerList[enemyIndex].RandomlyPlayIdleSound(SecondsUntilNextSound + 3);
                return;
            }
            EnforcerList[enemyIndex].agent.speed = StalkSpeed;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            EnforcerList[enemyIndex].SetMovingTowardsTargetPlayer(EnforcerList[enemyIndex].targetPlayer);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new StalkedPlayerSeesUs() 
        };

    }
    private class ChasePlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = ChaseSpeed;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            EnforcerList[enemyIndex].SetMovingTowardsTargetPlayer(EnforcerList[enemyIndex].targetPlayer);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new PlayerEnteredAttackRange(),
            new LostPlayerDuringChase() 
        };

    } 
     
    private class ScreamAtPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = 0; 
            EnforcerList[enemyIndex].SetAnimTriggerOnServerRpc("Scream");
            EnforcerList[enemyIndex].SetActiveCamoState(false);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FinishedScream() 
        };

    }
    private class AttackPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].AttackCooldownSeconds = 1.5f;
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Attacking", true);
            EnforcerList[enemyIndex].HasAttackedThisCycle = false;

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (!EnforcerList[enemyIndex].PlayerWithinRange(2.9f, false)) {
                EnforcerList[enemyIndex].agent.speed = 2f;
            } else {
                EnforcerList[enemyIndex].agent.speed = 0f;
            }
            EnforcerList[enemyIndex].TryMeleeAttackPlayer(120);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].SetAnimBoolOnServerRpc("Attacking", false);
            EnforcerList[enemyIndex].HasAttackedThisCycle = false;  
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new PlayerKilled(),
            new PlayerLeftAttackRange()
        };

    }
    private class SearchForPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].StartCoroutine(EnforcerList[enemyIndex].SearchAreaForPlayer());
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new PlayerNotFoundDuringSearch(),
            new PlayerFoundDuringSearch()
        };

    }

    //TRANSITIONS
    private class FoundHidingSpot : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].ReachedNextPoint;
        }
        public override BehaviorState NextState() {
            return new WaitForPlayerToPass();
        }
    }
    private class PlayerPassedBy : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].ShouldChasePotentialTarget;
        }
        public override BehaviorState NextState() {
            EnforcerList[enemyIndex].targetPlayer = EnforcerList[enemyIndex].PotentialTarget;
            return new StalkPlayer();
        }
    }
    private class StalkedPlayerSeesUs : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].targetPlayer.HasLineOfSightToPosition(EnforcerList[enemyIndex].eye.position, 30);
        }
        public override BehaviorState NextState() {
            return new ScreamAtPlayer();
        }
    }
    private class PlayerEnteredAttackRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].PlayerWithinRange(2f, false);
        }
        public override BehaviorState NextState() {
            return new AttackPlayer();
        }
    }
    private class PlayerKilled : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].targetPlayer.isPlayerDead;
        }
        public override BehaviorState NextState() {
            return new GoToHidingSpot();
        }
    }
    private class PlayerLeftAttackRange : StateTransition {

        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].PlayerWithinRange(3f, false);
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }
    private class LostPlayerDuringChase : StateTransition {
        public override bool CanTransitionBeTaken() {
            return !EnforcerList[enemyIndex].EnforcerSeesPlayer;
        }
        public override BehaviorState NextState() {
            return new SearchForPlayer();
        }
    }
    private class PlayerNotFoundDuringSearch : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].EnforcerSearchComplete;
        }
        public override BehaviorState NextState() {
            return new GoToHidingSpot();
        }
    }
    private class PlayerFoundDuringSearch : StateTransition {
        public override bool CanTransitionBeTaken() {
            //I GOTTA better FEELING ABOUT THIS
            return EnforcerList[enemyIndex].IsTargetPlayerWithinLOS(EnforcerList[enemyIndex].targetPlayer, width: 120);
        }
        public override BehaviorState NextState() {
            EnforcerList[enemyIndex].StopCoroutine("SearchAreaForPlayer");
            return new ChasePlayer();
        }
    }
    private class PlayerStaringAtUs : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].PlayerStareAtTimer > 5f;
        }
        public override BehaviorState NextState() {
            return new GoToHidingSpot();
        }
    }

    private class FinishedScream : StateTransition {
        public override bool CanTransitionBeTaken() {
            //I GOTTA better FEELING ABOUT THIS
            return EnforcerList[enemyIndex].EnforcerScreamSeconds <= 0f; 
        }
        public override BehaviorState NextState() {
            return new AttackPlayer();
        }
    }

    public static Dictionary<int, EnforcerAI> EnforcerList = new();
    private static int EnforcerID;
    private List<EnforcerHidePoint> EnforcerHidePoints = new();
    private bool ReachedNextPoint = false;
    private bool EnforcerActiveCamoState = false;
    private EnforcerHidePoint NextHidePoint;
    private PlayerControllerB PotentialTarget;
    private bool IsBeingSeenByPotentialTarget;
    private bool ShouldChasePotentialTarget;
    private bool EnforcerShouldKeepTrackOfTargetPlayer;
    private Vector3 LastKnownTargetPlayerPosition;
    private bool EnforcerSearchComplete = false;
    private bool EnforcerSeesPlayer;
    private float IdleSoundCooldownSeconds = 7f;
    private float AttackCooldownSeconds = 0f;
    private float EnforcerScreamSeconds = 2.5f;
    private bool HasAttackedThisCycle;
    private float PlayerStareAtTimer = 0f;

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new GoToHidingSpot();
        EnforcerID++;
        PrintDebugs = true;
        WTOEnemyID = EnforcerID;
        LogMessage($"Adding Enforcer {this} #{EnforcerID}");
        EnforcerList.Add(EnforcerID, this);
        //This might cause a lag spike because im using LINQ 
        EnforcerHidePoints = FindObjectsOfType<EnforcerHidePoint>().ToList();
        foreach(SkinnedMeshRenderer NextMesh in Meshes) {
            Material[] TempMaterialsArray = new Material[NextMesh.materials.Length];
            for(int i = 0; i < NextMesh.materials.Length; i++) {
                Material NextMat = new Material(ActiveCamoMaterial);
                //NextMat.SetFloat("_ACAmount", ActiveCamoVisibility);
                NextMat.SetTexture("_MainTexture", NextMesh.materials[i].GetTexture("_BaseColorMap"));
                TempMaterialsArray[i] = NextMat;
                EnforcerMatList.Add(NextMat);
            }
            NextMesh.materials = TempMaterialsArray;
        } 
        base.Start();
    }
    public override void Update() {
        base.Update();
        MoveTimerValue(ref AttackCooldownSeconds);
        if (IsBeingSeenByPotentialTarget) {
            PlayerStareAtTimer += Time.deltaTime;
        } else { 
           PlayerStareAtTimer = 0f;
        }

        if (ActiveState is ScreamAtPlayer) {
            LogMessage($"SCREAM SECONDS: {EnforcerScreamSeconds}");
            MoveTimerValue(ref EnforcerScreamSeconds);
        }

        if (!EnforcerShouldKeepTrackOfTargetPlayer || targetPlayer == null) {
            return;
        }

        if (IsTargetPlayerWithinLOS(width: 250)) {
            LastKnownTargetPlayerPosition = targetPlayer.gameplayCamera.transform.position;
            EnforcerSeesPlayer = true;
        } else {
            EnforcerSeesPlayer = false;
        }

    }

    float timeElapsed = 0f;
    float LerpPos = 0f;
    public void SetActiveCamoState(bool SetCamoOn) {
        if(EnforcerActiveCamoState == SetCamoOn) {
            return;
        }
        timeElapsed = 0f;
        LerpPos = 0f;
        EnforcerActiveCamoState = SetCamoOn; 
        if (EnforcerActiveCamoState) { 
            ScanNode.enabled = false;
            StartCoroutine(StartActiveCamo());
        } else {
            ScanNode.enabled = true;
            StartCoroutine(StopActiveCamo());
        }
    }
    public void DetermineNextHidePoint() { 
        if(EnforcerHidePoints == null) {
            EnforcerHidePoints = FindObjectsOfType<EnforcerHidePoint>().ToList();
            LogMessage($"Hide Point count: {EnforcerHidePoints.Count}");
        }
        NextHidePoint = EnforcerHidePoints[enemyRandom.Next(0, EnforcerHidePoints.Count)];
    } 
    IEnumerator StartActiveCamo() {
        while ((timeElapsed / ActiveCamoLerpTime) < 1) { 
            timeElapsed += Time.deltaTime;
            LerpPos = Mathf.Lerp(1, 0, timeElapsed / ActiveCamoLerpTime);
            LogMessage($"Active Camo at {(1 - LerpPos) * 100}%...");
            foreach (Material NextMat in EnforcerMatList) {
                NextMat.SetFloat("_TextureLerp", LerpPos); 
            }
        }
        EnforcerActiveCamoState = true;
        yield return null;
    }
    IEnumerator StopActiveCamo() {
        while ((timeElapsed / ActiveCamoLerpTime) < 1) {
            timeElapsed += Time.deltaTime;
            LerpPos = Mathf.Lerp(0, 1, timeElapsed / ActiveCamoLerpTime);
            LogMessage($"Active Camo at {(1 - LerpPos) * 100}%...");
            foreach (Material NextMat in EnforcerMatList) {
                NextMat.SetFloat("_TextureLerp", LerpPos);
            }
        }
        EnforcerActiveCamoState = false;
        yield return null;
    }

    private bool CheckPlayerLOSForEnforcer(PlayerControllerB player, int range = 45, float width = 60) {
        float num = Vector3.Distance(player.transform.position, eye.position);
        return num < (float)range && (Vector3.Angle(player.playerEye.transform.forward, eye.position - player.gameplayCamera.transform.position) < width);
    }

    IEnumerator SearchAreaForPlayer() {
        SetDestinationToPosition(LastKnownTargetPlayerPosition);
        while (Vector3.Distance(transform.position, LastKnownTargetPlayerPosition) < 0.02f) {
            yield return null;
        }
        LogMessage("Reached last player location; picking points nearby and searching");
        for(int i = 0; i < enemyRandom.Next(1, 3);) {
            Vector3 NextSearchPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(LastKnownTargetPlayerPosition);
            SetDestinationToPosition(NextSearchPoint);
            while (Vector3.Distance(transform.position, NextSearchPoint) < 0.02f) {
                yield return null;
            }
            LogMessage($"Reached nearby point #{i}");
            //Play animation where eye sweeps
            yield return new WaitForSeconds(EyeSweepAnimSeconds);
            i++;
        }
        EnforcerSearchComplete = true;
        yield return null;

    }

    private void RandomlyPlayIdleSound(float newIdleCooldown = 7f) {
        if(IdleSoundCooldownSeconds > 0) {
            MoveTimerValue(ref IdleSoundCooldownSeconds);
            return;
        }
        if(enemyRandom.Next(0, 100) < 15) {
            creatureVoice.PlayOneShot(RandomIdleSounds[enemyRandom.Next(0, RandomIdleSounds.Count())]);
            IdleSoundCooldownSeconds = newIdleCooldown;
        }
    }
    public void TryMeleeAttackPlayer(int damage) {
        if (targetPlayer == null) {
            return;
        }
        if (AttackCooldownSeconds <= 0.8f && Vector3.Distance(targetPlayer.transform.position, transform.position) < 3 && !HasAttackedThisCycle) {
            LogMessage("Gallenarma Attacking!");
            targetPlayer.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0, force: ((this.transform.position - targetPlayer.transform.position) * 40f));
            HasAttackedThisCycle = true;
        }
        if (AttackCooldownSeconds <= 0f) {
            AttackCooldownSeconds = 1;
            HasAttackedThisCycle = false;
        }
    }

}
