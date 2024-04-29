using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using System.Runtime.CompilerServices;
using Welcome_To_Ooblterra.Enemies.EnemyThings;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies;
public class BabyLurkerAI : WTOEnemy {

    [Header("Defaults")]
    public bool ThrowProjectile;
    public int ProjectilesThrown;
    public GameObject projectileTemplate;
    private float launchVelocity = 700f;
    public Transform LaunchTransform;
    public static int AttackRange = 5;
    private float JumpCooldownSeconds = 3f;

    //STATES
    private class Spawn : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].targetPlayer = BabyLurkerList[enemyIndex].FindNearestPlayer();
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (BabyLurkerList[enemyIndex].targetPlayer == null) {
                BabyLurkerList[enemyIndex].LogMessage($"Attempting to find nearest player...");
                BabyLurkerList[enemyIndex].targetPlayer = BabyLurkerList[enemyIndex].FindNearestPlayer();
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundNearestPlayer()
        };

    }
    private class ChasePlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].SetDestinationToPosition(BabyLurkerList[enemyIndex].targetPlayer.transform.position);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].SetDestinationToPosition(BabyLurkerList[enemyIndex].targetPlayer.transform.position);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new TargetPlayerIsInLOS()
        };
    }
    private class WaitForAttackCooldown : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new CooldownFinished(),
            new PlayerLeftRange()
        };

    }
    private class ThrowSelfAtPlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            
            
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //if(BabyLurkerList[enemyIndex].CheckIfWeAreFirstToJump()){
                //BabyLurkerList[enemyIndex].ThrowingSelfAtPlayer = true;
                BabyLurkerList[enemyIndex].LaunchProjectile(BabyLurkerList[enemyIndex].LaunchTransform);
            //} else { 
            //    BabyLurkerList[enemyIndex].JumpCooldownSeconds = .1f;
            //}
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            //new CooldownStarted()
        };

    }

    //TRANSITIONS
    private class TargetPlayerIsInLOS : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].IsTargetPlayerWithinLOS(range: AttackRange, width: 90);
        }
        public override BehaviorState NextState() {
            return new ThrowSelfAtPlayer();
        }
    }
    private class PlayerLeftRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            return !BabyLurkerList[enemyIndex].IsTargetPlayerWithinLOS(range: AttackRange, width: 90);
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }
    private class FoundNearestPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].targetPlayer != null; 
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }
    private class CooldownFinished : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].JumpCooldownSeconds <= 0f;
        }
        public override BehaviorState NextState() {
            return new ThrowSelfAtPlayer();
        }
    }
    private class CooldownStarted : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].JumpCooldownSeconds > 0f;
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }

    private GameObject LiveProjectile;

    public static Dictionary<int, BabyLurkerAI> BabyLurkerList = new();
    private static int BabyLurkerID;
    public bool ThrowingSelfAtPlayer;
    public GameObject LurkerBody;
    private List<PlayerControllerB> LivingPlayers = new();

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new Spawn();
        PrintDebugs = false;
        BabyLurkerID++;
        WTOEnemyID = BabyLurkerID;
        agent.speed = 5f;
        //SetTargetServerRpc((int)StartOfRound.Instance.allPlayerScripts[0].playerClientId);
        LogMessage($"Adding BabyLurker {this} #{BabyLurkerID}");
        BabyLurkerList.Add(BabyLurkerID, this); 
        base.Start();
        LaunchTransform.rotation = Quaternion.Euler(new Vector3(45, 0, enemyRandom.Next(-10, 10)));
    }

    public override void Update() {
        base.Update();
        MoveTimerValue(ref JumpCooldownSeconds);
        if(targetPlayer != null && (targetPlayer.isPlayerDead || !targetPlayer.isInsideFactory)) {
            //get all living players
            LivingPlayers.Clear();
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                if(!player.isPlayerDead && player.isInsideFactory && player.isPlayerControlled) {
                    LivingPlayers.Add(player);
                }
            }
            //set target to random player
            if(LivingPlayers.Count > 0) {
                targetPlayer = LivingPlayers[enemyRandom.Next(0, LivingPlayers.Count)];
            } else {
                LogMessage("No target for baby lurkers!");
            }
        }
    }

    public bool CheckIfWeAreFirstToJump() {
        foreach(BabyLurkerAI BabyLurker in BabyLurkerList.Values) {
            if (BabyLurker.ThrowingSelfAtPlayer) {
                return false;
            }
        }
        return true;
    }

    public void LaunchProjectile(Transform LaunchTransform) {
        ThrowingSelfAtPlayer = true;
        if(ProjectilesThrown > 0) {
            return;
        }
        LurkerBody.SetActive(false);
        creatureSFX.volume = 0;
        creatureVoice.volume = 0;
        ProjectilesThrown++;
        LiveProjectile = Instantiate(projectileTemplate, LaunchTransform.position, LaunchTransform.rotation);
        LiveProjectile.GetComponent<BabyLurkerProjectile>().OwningLurker = this;
        LiveProjectile.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, launchVelocity, 0));
        this.agent.speed = 0f;
        
    }
}
