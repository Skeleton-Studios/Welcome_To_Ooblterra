using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies;

public abstract class WTOEnemy<T> : WTOEnemy where T : WTOEnemy<T>
{
    public static T ThisEnemy { get; private set; }

    public WTOEnemy()
    {
        if(ThisEnemy != null)
        {
            LogMessage("Singleton Instantiated twice! Skipping");
            return;
        }
        ThisEnemy = this as T;
    }
}

public abstract class WTOEnemy : EnemyAI {
    public abstract class BehaviorState {
        public Vector2 RandomRange = new Vector2(0, 0);
        public int MyRandomInt = 0;
        
        public abstract void OnStateEntered(WTOEnemy enemyInstance);
        public abstract void UpdateBehavior(WTOEnemy enemyInstance);
        public abstract void OnStateExit(WTOEnemy enemyInstance);

        public virtual List<StateTransition> transitions { get; set; }
    }
    public abstract class StateTransition {
        //public int enemyIndex { get; set; }
       
        public abstract bool CanTransitionBeTaken();
        public abstract BehaviorState NextState();
    }
    public enum PlayerState {
        Dead,
        Outside,
        Inside
    }
    internal BehaviorState InitialState { get; set; }
    internal BehaviorState ActiveState = null;
    internal System.Random enemyRandom;
    internal int AITimer;
    internal RoundManager roundManager;
    internal bool PrintDebugs = false;
    internal PlayerState MyValidState = PlayerState.Inside;
    internal StateTransition nextTransition;
    internal List<StateTransition> GlobalTransitions = new List<StateTransition>();
    internal List<StateTransition> AllTransitions = new List<StateTransition>();
    internal WTOEnemy _WTOEnemyInstance;

    public override string __getTypeName() {
        return GetType().Name;
    }
    public override void DoAIInterval() {
        base.DoAIInterval();
        _ = StartOfRound.Instance.livingPlayers;
    }
    public override void Start() {
        base.Start();
            
        //Initializers
            ActiveState = InitialState;
            roundManager = FindObjectOfType<RoundManager>();
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            if (enemyType.isOutsideEnemy) {
                MyValidState = PlayerState.Outside;
            } else {
                MyValidState = PlayerState.Inside;
            }
        //Debug to make sure that the agent is actually on the navmesh
            if (!agent.isOnNavMesh && base.IsOwner) {
                WTOBase.LogToConsole("CREATURE " + this.__getTypeName() + " WAS NOT PLACED ON NAVMESH, DESTROYING...");
                KillEnemyOnOwnerClient();
            }
        //Fix for the animator sometimes deciding to just not work
            creatureAnimator.Rebind();
       
        ActiveState.OnStateEntered(_WTOEnemyInstance);

    }
    public override void Update() {
        base.Update();
        AITimer++;
        //don't run enemy ai if they're dead
            if (isEnemyDead || !ventAnimationFinished) {
                return;
            }
        bool RunUpdate = true;

        //Reset transition list to match all those in our current state, along with any global transitions that exist regardless of state (stunned, mostly)
            AllTransitions.Clear();
            AllTransitions.AddRange(GlobalTransitions);
            AllTransitions.AddRange(ActiveState.transitions);

        foreach (StateTransition TransitionToCheck in AllTransitions) {
            
            if (TransitionToCheck.CanTransitionBeTaken() && IsOwner) {
                RunUpdate = false;
                nextTransition = TransitionToCheck;
                TransitionStateServerRpc(nextTransition.ToString(), GenerateNextRandomInt(nextTransition.NextState().RandomRange));
                return;
            }
        }

        if (RunUpdate) {
            ActiveState.UpdateBehavior(_WTOEnemyInstance);
        }
    }

    internal void LogMessage(string message) {
        if (PrintDebugs) {
            Debug.Log(message);
        }
    }
    internal bool PlayerCanBeTargeted(PlayerControllerB myPlayer) {
        return (GetPlayerState(myPlayer) == MyValidState);
    }
    internal PlayerState GetPlayerState(PlayerControllerB myPlayer) {
        if (myPlayer.isPlayerDead) {
            return PlayerState.Dead;
        }
        if (myPlayer.isInsideFactory) {
            return PlayerState.Inside;
        }
        return PlayerState.Outside;
    }
    internal void TransitionState(string StateName, int RandomInt) {
        //Jesus fuck I can't believe I have to do this
        Type type = Type.GetType(StateName);
        StateTransition LocalNextTransition = (StateTransition)Activator.CreateInstance(type);
        
        if (LocalNextTransition.NextState().GetType() == ActiveState.GetType()) {
            return;
        }
        //LogMessage(StateName);
        LogMessage($"{__getTypeName()} #{_WTOEnemyInstance} is Exiting:  {ActiveState}");
        ActiveState.OnStateExit(_WTOEnemyInstance);
        LogMessage($"{__getTypeName()} #{_WTOEnemyInstance} is Transitioning via:  {LocalNextTransition}");
        ActiveState = LocalNextTransition.NextState();
        ActiveState.MyRandomInt = RandomInt;
        
        LogMessage($"{__getTypeName()} #{_WTOEnemyInstance} is Entering:  {ActiveState}");
        ActiveState.OnStateEntered(_WTOEnemyInstance);

        //Debug Prints 
        StartOfRound.Instance.ClientPlayerList.TryGetValue(NetworkManager.Singleton.LocalClientId, out var value);
        LogMessage($"CREATURE: {enemyType.name} #{_WTOEnemyInstance} STATE: {ActiveState} ON PLAYER: #{value} ({StartOfRound.Instance.allPlayerScripts[value].playerUsername})");
    }
    internal void LowerTimerValue(ref float Timer) {
        if (Timer <= 0) {
            return;
        }
        Timer -= Time.deltaTime;
    }
    internal void OverrideState(BehaviorState state) {
        ActiveState = state;
        ActiveState.OnStateEntered(_WTOEnemyInstance);
        return;
    }
    internal float DistanceFromPlayer(PlayerControllerB player) {
        return Vector3.Distance(player.transform.position, this.transform.position);
    }
    internal bool AnimationIsFinished(string AnimName) {
        if (!creatureAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimName)) {
            LogMessage(__getTypeName() + ": Checking for animation " + AnimName + ", but current animation is " + creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            return true;
        }
        return (creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }
    internal int GenerateNextRandomInt(Vector2 Range) {
        Range = nextTransition.NextState().RandomRange;
        return enemyRandom.Next((int)Range.x, (int)Range.y);
    }

    [ServerRpc]
    internal void SetAnimTriggerOnServerRpc(string name) {
        if (IsServer) {
            creatureAnimator.SetTrigger(name);
        }
    }      
    [ServerRpc]
    internal void SetAnimBoolOnServerRpc(string name, bool state) {
        if (IsServer) {
            creatureAnimator.SetBool(name, state);
        }
    }
    [ServerRpc]
    internal void TransitionStateServerRpc(string StateName, int RandomInt) {
        TransitionStateClientRpc(StateName, RandomInt);
    }
    [ClientRpc]
    internal void TransitionStateClientRpc(string StateName, int RandomInt) {
        TransitionState(StateName, RandomInt);
    }
}
