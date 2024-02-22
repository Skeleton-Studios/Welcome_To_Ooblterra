using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies;
public class AdultWandererAI : WTOEnemy<AdultWandererAI> {

        //BEHAVIOR STATES
    private class Spawn : BehaviorState {
        private int SpawnTimer;
        private int SpawnTime = 80;
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            WTOBase.LogToConsole("SPAWN WANDERER");
            ThisEnemy.creatureAnimator.SetBool("Spawn", value: true);
            ThisEnemy.creatureSFX.PlayOneShot(ThisEnemy.SpawnSound);
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (SpawnTimer > SpawnTime) {
                ThisEnemy.spawnFinished = true;
            } else {
                SpawnTimer++;
            }
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Spawn", value: false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new EvaluateEnemyState()
        };
    }
    private class WaitForTargetLook : BehaviorState {
        private int LookWaitTime = 0;
        private int LookWaitTimer = 3500;
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Moving", value: false);
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (LookWaitTime > LookWaitTimer) {
                ThisEnemy.LostPatience = true;
            } else {
                LookWaitTime++;
            }
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new EvaluatePlayerLook()
        };

    }
    private class Attack : BehaviorState {
        bool HasAttacked;
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.AttackCooldownSeconds = 1.5f;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (Vector3.Distance(ThisEnemy.MainTarget.transform.position, ThisEnemy.transform.position) < ThisEnemy.AttackRange) {
                if (ThisEnemy.AttackCooldownSeconds <= 0) {
                    ThisEnemy.AttackCooldownSeconds = 1.5f;
                    HasAttacked = false;
                    return;
                }
                if (ThisEnemy.AttackCooldownSeconds <= 1.2f) {
                    ThisEnemy.creatureAnimator.SetBool("Attacking", value: true);
                }
                if(ThisEnemy.AttackCooldownSeconds <= 0.76f && !HasAttacked) {
                    ThisEnemy.LogMessage($"Attacking Player {ThisEnemy.MainTarget}");
                    ThisEnemy.MeleeAttackPlayer(ThisEnemy.MainTarget);
                    HasAttacked = true;
                }
                return;
            }
            ThisEnemy.AttackCooldownSeconds = 0;
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Attacking", value: false);
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
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.agent.speed = 7f;
            ThisEnemy.creatureAnimator.SetBool("Moving", value: true);
            if (!ThisEnemy.RoamPlanet.inProgress) {
                ThisEnemy.StartSearch(ThisEnemy.transform.position, ThisEnemy.RoamPlanet);
            }
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (!ThisEnemy.RoamPlanet.inProgress) {
                ThisEnemy.StartSearch(ThisEnemy.transform.position, ThisEnemy.RoamPlanet);
            }
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Moving", value: false);
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
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.ReachedNextPoint = false;
            ThisEnemy.agent.speed = 0f;
            ThisEnemy.TotalInvestigationSeconds = MyRandomInt;
            ThisEnemy.creatureAnimator.speed = 1f;
            ThisEnemy.creatureAnimator.SetBool("Moving", false);
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.LowerTimerValue(ref ThisEnemy.TotalInvestigationSeconds);
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.ReachedNextPoint = false;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new DoneInvestigating(),
            new EnemyLeftShipOrFacility()
        };

    }
    private class Chase : BehaviorState {
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Moving", value: true);
            ThisEnemy.agent.speed = 8f;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.SetDestinationToPosition(ThisEnemy.MainTarget.transform.position);
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Moving", value: false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new EnemyInShipOrFacility(),
            new EnemyEnteredRange()
        };
    }
    private class Stunned : BehaviorState {
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Stunned", value: true);
            ThisEnemy.agent.speed = 0f;
            ThisEnemy.targetPlayer = ThisEnemy.stunnedByPlayer;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {

        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureAnimator.SetBool("Stunned", value: false);
            ThisEnemy.agent.speed = 8f;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new NoLongerStunned()
        };
    }

    //STATE TRANSITIONS
    private class EvaluateEnemyState : StateTransition {
            
        public override bool CanTransitionBeTaken() {
                
            return ThisEnemy.spawnFinished;
        }
        public override BehaviorState NextState() {
                
            if (ThisEnemy.MainTarget == null) {
                return new Roam();
            }
            return new WaitForTargetLook();
        }
    }
    private class EvaluatePlayerLook : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.CheckForPlayerLOS()){
                return true;
            }
            return ThisEnemy.LostPatience;
        }
        public override BehaviorState NextState() {
            return new Attack();
        }
    }
    private class EnemyInShipOrFacility : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.MainTarget == null) {
                return false;
            }
            return !ThisEnemy.PlayerCanBeTargeted(ThisEnemy.MainTarget);
        }
        public override BehaviorState NextState() {
                return new Roam();
        }
    }
    private class EnemyLeftShipOrFacility : StateTransition {
        public override bool CanTransitionBeTaken() {
            if(ThisEnemy.MainTarget == null) {
                return false;
            }
            return ThisEnemy.PlayerCanBeTargeted(ThisEnemy.MainTarget);
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class EnemyLeftRange : StateTransition{
        public override bool CanTransitionBeTaken() {
            bool IsInAttackRange = (Vector3.Distance(ThisEnemy.transform.position, ThisEnemy.MainTarget.transform.position) > ThisEnemy.AttackRange);
            return (ThisEnemy.PlayerCanBeTargeted(ThisEnemy.MainTarget) && IsInAttackRange);
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class EnemyKilled : StateTransition {
            
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.MainTarget == null) {
                return true;
            }
            return ThisEnemy.MainTarget.isPlayerDead; 
        }
        public override BehaviorState NextState() {
            ThisEnemy.MainTarget = null;
            return new Roam();
        }
    }
    private class EnemyEnteredRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.MainTarget == null) {
                return false;
            }    
            return (ThisEnemy.PlayerCanBeTargeted(ThisEnemy.MainTarget) && (Vector3.Distance(ThisEnemy.transform.position, ThisEnemy.MainTarget.transform.position) < ThisEnemy.AttackRange));
        }
        public override BehaviorState NextState() {
            return new Attack();
        }
    }
    private class HitByStunGun : StateTransition {
            
        public override bool CanTransitionBeTaken() {
                
            return ThisEnemy.stunNormalizedTimer > 0 && !(ThisEnemy.ActiveState is Stunned);
        }
        public override BehaviorState NextState() {
            return new Stunned();
        }
    }
    private class NoLongerStunned : StateTransition {
        public override bool CanTransitionBeTaken() {
            return ThisEnemy.stunNormalizedTimer <= 0;
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class StartInvestigation : StateTransition {
        public override bool CanTransitionBeTaken() {
            return ThisEnemy.ReachedNextPoint;
        }
        public override BehaviorState NextState() {
            return new Investigate();
        }
    }
    private class DoneInvestigating : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (ThisEnemy.TotalInvestigationSeconds <= 0);
        }
        public override BehaviorState NextState() {
            return new Roam();
        }
    }

    private bool spawnFinished = false;
    public PlayerControllerB MainTarget = null;
    private bool LostPatience = false;
    private float AttackCooldownSeconds = 1.2f;
    public int AttackRange = 7;
    public static Dictionary<int, AdultWandererAI> AWandList = new Dictionary<int, AdultWandererAI>();
    public static int AWandID;
    public AudioClip SpawnSound;
    private float TotalInvestigationSeconds;
    private bool ReachedNextPoint = false;
    private AISearchRoutine RoamPlanet = new();

    public override void Start() {
        InitialState = new Spawn();
        AWandID++;
        //WTOEnemyID = AWandID;
        PrintDebugs = true;
        LogMessage($"Adding Adult Wanderer {this} #{AWandID}");
        AWandList.Add(AWandID, this);
        MyValidState = PlayerState.Outside;
        enemyHP = 10;
        GlobalTransitions.Add(new HitByStunGun());
        base.Start();
    }
    public override void Update() {
        LowerTimerValue(ref AttackCooldownSeconds);
        base.Update();
    }
    private void MeleeAttackPlayer(PlayerControllerB Target) {
        LogMessage("Attacking player!");
        Target.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning, 0);
        Target.JumpToFearLevel(1f);
    }
    [ServerRpc]
    public void SetTargetServerRpc(int PlayerID) {
        SetTargetClientRpc(PlayerID);
    }
    [ClientRpc]
    public void SetTargetClientRpc(int PlayerID) {
        SetMyTarget(PlayerID);
    }

    public override void ReachedNodeInSearch() {
        base.ReachedNodeInSearch();
        ReachedNextPoint = true;
    }
    public void SetMyTarget(int PlayerID) {
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        MainTarget = StartOfRound.Instance.allPlayerScripts[PlayerID]; 
    }
    public bool CheckForPlayerLOS() {
        return MainTarget.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f, 68f);
    }
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
        if (isEnemyDead) { return; }
        base.HitEnemy(force, playerWhoHit, playHitSFX);
        enemyHP -= force;
        LogMessage("Adult Wanderer HP remaining: " + enemyHP);
        ThisEnemy.creatureAnimator.SetTrigger("Hit");
        if (IsOwner) {
            if (enemyHP <= 0) {
                isEnemyDead = true;
                ThisEnemy.creatureAnimator.SetTrigger("Killed");
                creatureVoice.Stop();
                KillEnemyOnOwnerClient();
                return;
            }
        }
        //If we're attacked by a player, they need to be immediately set to our target player
        targetPlayer = playerWhoHit;
        MainTarget = playerWhoHit;
        OverrideState(new Attack());
    }
}
