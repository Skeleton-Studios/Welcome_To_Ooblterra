using DunGen;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies.EnemyThings;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static System.Net.Mime.MediaTypeNames;

namespace Welcome_To_Ooblterra.Enemies;
public class EnforcerAI : WTOEnemy {

    [Header("Balance Constants")]
    public static float SecondsUntilBeginStalking = 5f;
    public static float StalkSpeed = 6f;
    public static float ChaseSpeed = 6.5f;
    public static float RangeForPotentialTarget = 2f;
    public static int EnforcerAttackDamage = 100;

    [Header("Defaults")]
    public Material[] EnforcerMatList;
    public float EyeSweepAnimSeconds = 2f;
    public float ActiveCamoLerpTime = 0.5f;

    //STATES
    private class GoToHidingSpot : BehaviorState {
        public GoToHidingSpot() {

        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = 8f;
            EnforcerList[enemyIndex].SetActiveCamoState(true);
            EnforcerList[enemyIndex].DetermineNextHidePoint();
            EnforcerList[enemyIndex].SetDestinationToPosition(EnforcerList[enemyIndex].NextHidePoint.transform.position);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundHidingSpot()
        };

    }
    private class WaitForPlayerToPass : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].ReachedNextPoint = false;
            EnforcerList[enemyIndex].transform.rotation = EnforcerList[enemyIndex].NextHidePoint.transform.rotation;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //if a player enters range, consider them our potential target
            if (EnforcerList[enemyIndex].PotentialTarget != null) {
                //check if we still have LOS to the potential target
                //.CONTAINS IS A LINQ FUNCTION AND IS PROBABLY SLOW
                if (EnforcerList[enemyIndex].GetAllPlayersInLineOfSight().Contains<PlayerControllerB>(EnforcerList[enemyIndex].PotentialTarget)) {
                    EnforcerList[enemyIndex].IsBeingSeenByPotentialTarget = EnforcerList[enemyIndex].PotentialTarget.HasLineOfSightToPosition(EnforcerList[enemyIndex].transform.position);
                } else {
                    EnforcerList[enemyIndex].ShouldChasePotentialTarget = !EnforcerList[enemyIndex].IsBeingSeenByPotentialTarget;
                }
            } else {
                PlayerControllerB PlayerInConsideration = EnforcerList[enemyIndex].GetClosestPlayer(requireLineOfSight: true);
                //set the player in consideration to our potential target if he's in range
                if (PlayerInConsideration != null && EnforcerList[enemyIndex].DistanceFromTargetPlayer(PlayerInConsideration) < RangeForPotentialTarget) {
                    EnforcerList[enemyIndex].PotentialTarget = PlayerInConsideration;
                } 
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new PlayerPassedBy()
        };

    }
    private class StalkPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = StalkSpeed;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //this might start pushing the player if the player stands still
            EnforcerList[enemyIndex].SetMovingTowardsTargetPlayer(EnforcerList[enemyIndex].targetPlayer);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new StalkedPlayerSeesUs()
        };

    }
    private class ChasePlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].agent.speed = ChaseSpeed;
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
    private class AttackPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            EnforcerList[enemyIndex].SetActiveCamoState(false);
            EnforcerList[enemyIndex].agent.speed = 2f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Wait for the active camo to fully disable before attacking
            if(EnforcerList[enemyIndex].EnforcerActiveCamoState == false) {
                EnforcerList[enemyIndex].targetPlayer.DamagePlayer(EnforcerAttackDamage, causeOfDeath: CauseOfDeath.Mauling);
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

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
            return EnforcerList[enemyIndex].targetPlayer.HasLineOfSightToPosition(EnforcerList[enemyIndex].transform.position, 20);
        }
        public override BehaviorState NextState() {
            return new AttackPlayer();
        }
    }
    private class PlayerEnteredAttackRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].DistanceFromTargetPlayer() < 2f;
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
            return EnforcerList[enemyIndex].DistanceFromTargetPlayer() > 2f;
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }
    private class LostPlayerDuringChase : StateTransition {
        public override bool CanTransitionBeTaken() {
            return EnforcerList[enemyIndex].EnforcerSeesPlayer;
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
            //I GOTTA BAD FEELING ABOUT THIS
            return EnforcerList[enemyIndex].GetAllPlayersInLineOfSight().Contains<PlayerControllerB>(EnforcerList[enemyIndex].PotentialTarget);
        }
        public override BehaviorState NextState() {
            EnforcerList[enemyIndex].StopCoroutine("SearchAreaForPlayer");
            return new ChasePlayer();
        }
    }


    public static Dictionary<int, EnforcerAI> EnforcerList = new();
    private static int EnforcerID;
    private List<EnforcerHidePoint> EnforcerHidePoints = new();
    private bool ReachedNextPoint = false;
    private bool EnforcerActiveCamoState = true;
    private EnforcerHidePoint NextHidePoint;
    private PlayerControllerB PotentialTarget;
    private bool IsBeingSeenByPotentialTarget;
    private bool ShouldChasePotentialTarget;
    private bool EnforcerShouldKeepTrackOfTargetPlayer;
    private Vector3 LastKnownTargetPlayerPosition;
    private bool EnforcerSearchComplete = false;
    private bool EnforcerSeesPlayer;

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new GoToHidingSpot();
        PrintDebugs = true;
        EnforcerID++;
        WTOEnemyID = EnforcerID;
        LogMessage($"Adding Enforcer {this} #{EnforcerID}");
        EnforcerList.Add(EnforcerID, this);
        base.Start();
        //This might cause a lag spike because im using LINQ
        EnforcerHidePoints = FindObjectsOfType<EnforcerHidePoint>().ToList();
        
    }
    public override void Update() {
        if (!EnforcerShouldKeepTrackOfTargetPlayer || targetPlayer == null) {
            return;
        }
        //.CONTAINS IS A LINQ FUNCTION AND IS PROBABLY SLOW
        //I AM PLAYING WITH DANGER PUTTING IT ON THE UPDATE
        //BUT ZEEKERS HAS FORCED MY HAND
        if (GetAllPlayersInLineOfSight().Contains<PlayerControllerB>(targetPlayer)) {
            LastKnownTargetPlayerPosition = targetPlayer.transform.position;
            EnforcerSeesPlayer = true;
        } else {
        }
    }
    public override void ReachedNodeInSearch() {
        base.ReachedNodeInSearch();
        ReachedNextPoint = true;
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
            StartActiveCamo();
        } else {
            StopActiveCamo();
        }
    }

    public void DetermineNextHidePoint() {
        NextHidePoint = EnforcerHidePoints[enemyRandom.Next(0, EnforcerHidePoints.Count)];
    }
    IEnumerator StartActiveCamo() {
        timeElapsed += Time.deltaTime;
        LerpPos = Mathf.Lerp(0, 1, timeElapsed / ActiveCamoLerpTime);
        foreach (Material NextMat in EnforcerMatList) {
            NextMat.SetFloat("", LerpPos);
        }
        if (timeElapsed / ActiveCamoLerpTime >= 1) {
            EnforcerActiveCamoState = true;
            yield return null;
        }
    }
    IEnumerator StopActiveCamo() {
        yield return null;
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
            LogMessage("Reached nearby point #1");
            //Play animation where eye sweeps
            yield return new WaitForSeconds(EyeSweepAnimSeconds);
            i++;
        }
        EnforcerSearchComplete = true;
        yield return null;

    }
}
