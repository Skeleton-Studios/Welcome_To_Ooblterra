using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies;
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
            if (AWandList[enemyIndex].PlayerWithinRange(AWandList[enemyIndex].AttackRange)) {
                if (AWandList[enemyIndex].AttackCooldownSeconds <= 0) {
                    AWandList[enemyIndex].AttackCooldownSeconds = 1.5f;
                    HasAttacked = false;
                    return;
                }
                if (AWandList[enemyIndex].AttackCooldownSeconds <= 1.2f) {
                    creatureAnimator.SetBool("Attacking", value: true);
                }
                if(AWandList[enemyIndex].AttackCooldownSeconds <= 0.76f && !HasAttacked) {
                    AWandList[enemyIndex].LogMessage($"Attacking Player {AWandList[enemyIndex].targetPlayer}");
                    AWandList[enemyIndex].MeleeAttackPlayer(AWandList[enemyIndex].targetPlayer);
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
            new EnemyKilled(),
            new EnemyLeftRange()
        };
    }
    private class Roam : BehaviorState {
        public bool SearchInProgress;
        public bool investigating;
        public int investigateTimer;
        public int TotalInvestigateTime;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AWandList[enemyIndex].agent.speed = 7f;
            creatureAnimator.SetBool("Moving", value: true);
            if (!AWandList[enemyIndex].RoamPlanet.inProgress) {
                AWandList[enemyIndex].StartSearch(AWandList[enemyIndex].transform.position, AWandList[enemyIndex].RoamPlanet);
            }
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (!AWandList[enemyIndex].RoamPlanet.inProgress) {
                AWandList[enemyIndex].StartSearch(AWandList[enemyIndex].transform.position, AWandList[enemyIndex].RoamPlanet);
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            creatureAnimator.SetBool("Moving", value: false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new StartInvestigation(),
            new EnemyLeftShipOrFacility()
        };
    }
    private class Investigate : BehaviorState {
        public Investigate() {
            RandomRange = new Vector2(12, 17);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AWandList[enemyIndex].ReachedNextPoint = false;
            AWandList[enemyIndex].agent.speed = 0f;
            AWandList[enemyIndex].TotalInvestigationSeconds = MyRandomInt;
            AWandList[enemyIndex].creatureAnimator.speed = 1f;
            AWandList[enemyIndex].creatureAnimator.SetBool("Moving", false);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AWandList[enemyIndex].MoveTimerValue(ref AWandList[enemyIndex].TotalInvestigationSeconds);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AWandList[enemyIndex].ReachedNextPoint = false;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new DoneInvestigating(),
            new EnemyLeftShipOrFacility()
        };

    }
    private class Chase : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            creatureAnimator.SetBool("Moving", value: true);
            AWandList[enemyIndex].agent.speed = 8f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AWandList[enemyIndex].SetDestinationToPosition(AWandList[enemyIndex].targetPlayer.transform.position);
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
            AWandList[enemyIndex].SetTargetServerRpc((int)AWandList[enemyIndex].stunnedByPlayer.playerClientId);
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
                
            if (AWandList[enemyIndex].targetPlayer == null) {
                return new Roam();
            }
            return new WaitForTargetLook();
        }
    }
    private class EvaluatePlayerLook : StateTransition {
        public override bool CanTransitionBeTaken() {
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
            if (AWandList[base.enemyIndex].targetPlayer == null) {
                return false;
            }
            return !AWandList[base.enemyIndex].PlayerCanBeTargeted(AWandList[base.enemyIndex].targetPlayer);
        }
        public override BehaviorState NextState() {
                return new Roam();
        }
    }
    private class EnemyLeftShipOrFacility : StateTransition {
        public override bool CanTransitionBeTaken() {
            if(AWandList[base.enemyIndex].targetPlayer == null) {
                return false;
            }
            return AWandList[base.enemyIndex].PlayerCanBeTargeted(AWandList[base.enemyIndex].targetPlayer);
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class EnemyLeftRange : StateTransition{
        public override bool CanTransitionBeTaken() {
            return (AWandList[enemyIndex].PlayerCanBeTargeted(AWandList[enemyIndex].targetPlayer) && !AWandList[enemyIndex].PlayerWithinRange(AWandList[enemyIndex].AttackRange));
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class EnemyKilled : StateTransition {
            
        public override bool CanTransitionBeTaken() {
            if (AWandList[enemyIndex].targetPlayer == null) {
                return true;
            }
            return AWandList[enemyIndex].targetPlayer.isPlayerDead; 
        }
        public override BehaviorState NextState() {
            AWandList[enemyIndex].SetTargetServerRpc(-1);
            return new Roam();
        }
    }
    private class EnemyEnteredRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (AWandList[enemyIndex].targetPlayer == null) {
                return false;
            }    
            return (AWandList[enemyIndex].PlayerWithinRange(AWandList[enemyIndex].AttackRange));
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
    private class StartInvestigation : StateTransition {
        public override bool CanTransitionBeTaken() {
            return AWandList[enemyIndex].ReachedNextPoint;
        }
        public override BehaviorState NextState() {
            return new Investigate();
        }
    }
    private class DoneInvestigating : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (AWandList[enemyIndex].TotalInvestigationSeconds <= 0);
        }
        public override BehaviorState NextState() {
            return new Roam();
        }
    }

    private bool spawnFinished = false;
    private bool LostPatience = false;
    private float AttackCooldownSeconds = 1.2f;
    public int AttackRange = 7;
    public static Dictionary<int, AdultWandererAI> AWandList = new Dictionary<int, AdultWandererAI>();
    public static int AWandID;
    public AudioClip SpawnSound;
    private float TotalInvestigationSeconds;
    private bool ReachedNextPoint = false;
    private AISearchRoutine RoamPlanet = new();
    public BoxCollider AdultBox;
    public CapsuleCollider AdultCapsule;

    public override void Start() {
        InitialState = new Spawn();
        AWandID++;
        WTOEnemyID = AWandID;
        PrintDebugs = true;
        LogMessage($"Adding Adult Wanderer {this} #{AWandID}");
        AWandList.Add(AWandID, this);
        MyValidState = PlayerState.Outside;
        enemyHP = 10;
        GlobalTransitions.Add(new HitByStunGun());
        base.Start();
    }
    public override void Update() {
        MoveTimerValue(ref AttackCooldownSeconds);
        base.Update();
    }
    private void MeleeAttackPlayer(PlayerControllerB Target) {
        LogMessage("Attacking player!");
        Target.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning, 0);
        if (Target == GameNetworkManager.Instance.localPlayerController) {
            GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
        }
    }
    



    public override void ReachedNodeInSearch() {
        base.ReachedNodeInSearch();
        ReachedNextPoint = true;
    }

    public bool CheckForPlayerLOS() {
        return targetPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f);
    }
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        if (isEnemyDead) { return; }
        base.HitEnemy(force, playerWhoHit, playHitSFX);
        enemyHP -= force;
        LogMessage("Adult Wanderer HP remaining: " + enemyHP);
        creatureAnimator.SetTrigger("Hit");
        if (enemyHP <= 0) {   
            isEnemyDead = true;
            creatureAnimator.SetTrigger("Killed");
            creatureVoice.Stop();
            GameObject.Destroy(AdultBox);
            GameObject.Destroy(AdultCapsule);
            if (IsOwner) {
                KillEnemyOnOwnerClient();
            }
            return;
        }
        //If we're attacked by a player, they need to be immediately set to our target player
        SetTargetServerRpc((int)playerWhoHit.playerClientId);
        OverrideState(new Attack());
    }
}
