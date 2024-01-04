using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;
using System;

namespace Welcome_To_Ooblterra.Enemies {
    public class WTOEnemy : EnemyAI {
        public abstract class BehaviorState {

            public abstract void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);

            public virtual List<StateTransition> transitions { get; set; }
        }
        public abstract class StateTransition {
            public EnemyAI self { get; set; }
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

        public override string __getTypeName() {
            return GetType().Name;
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }
        public override void Start() {
            base.Start();
            ActiveState = InitialState;
            roundManager = FindObjectOfType<RoundManager>();
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //Debug for the animations not fucking working
            if (!agent.isOnNavMesh && base.IsOwner) {
                WTOBase.LogToConsole("CREATURE " + this.__getTypeName() + " WAS NOT PLACED ON NAVMESH, DESTROYING...");
                KillEnemyOnOwnerClient();
            }
            creatureAnimator.Rebind();
            ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
            if (enemyType.isOutsideEnemy) {
                MyValidState = PlayerState.Outside;
            } else {
                MyValidState = PlayerState.Inside;
            }
        }
        public override void Update() {
            base.Update();
            AITimer++;

            //don't run enemy ai if they're dead
            if (isEnemyDead || !ventAnimationFinished) {
                return;
            }
            
            bool RunUpdate = true;
            foreach (StateTransition transition in GlobalTransitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    nextTransition = transition;                   
                    TransitionStateServerRpc(nextTransition.ToString());
                    return;
                }
            }
            foreach (StateTransition transition in ActiveState.transitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    nextTransition = transition; 
                    TransitionStateServerRpc(nextTransition.ToString());
                    return;
                }
            }
            if (RunUpdate) {
                ActiveState.UpdateBehavior(this, enemyRandom, creatureAnimator);
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

        [ServerRpc(RequireOwnership = false)]
        internal void TransitionStateServerRpc(string StateName) {
            TransitionStateClientRpc(StateName);
        }
        [ClientRpc]
        internal void TransitionStateClientRpc(string StateName) {
            TransitionState(StateName);
        }
        internal void TransitionState(string StateName) {

            LogMessage(StateName);
            //Jesus fuck I can't believe I have to do this
            Type type = Type.GetType(StateName);
            StateTransition LocalTransition = (StateTransition)Activator.CreateInstance(type);
            LocalTransition.self = this;

            //LogMessage("Exiting: " + ActiveState.ToString());
            ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
            //LogMessage("Transitioning Via: " + LocalTransition.ToString());
            ActiveState = LocalTransition.NextState();
            //LogMessage("Entering: " + ActiveState.ToString());
            ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
            StartOfRound.Instance.ClientPlayerList.TryGetValue(NetworkManager.Singleton.LocalClientId, out var value);
            LogMessage($"CREATURE: {enemyType.name} #{thisEnemyIndex} STATE: {ActiveState} ON PLAYER: #{value} ({StartOfRound.Instance.allPlayerScripts[value].playerUsername})");
        }
        internal void LowerTimerValue(ref float Timer) {
            if (Timer == 0) {
                return;
            }
            Timer -= Time.deltaTime;
        }
        internal void OverrideState(BehaviorState state) {
            ActiveState = state;
            ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
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
    }

}
