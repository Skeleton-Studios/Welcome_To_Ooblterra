using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies {
    public class WTOEnemy : EnemyAI {
        public abstract class BehaviorState {
            public Vector2 RandomRange = new Vector2(0, 0);
            public int MyRandomInt = 0;
            public int enemyIndex;
            public abstract void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);

            public virtual List<StateTransition> transitions { get; set; }
        }
        public abstract class StateTransition {
            //public int enemyIndex { get; set; }
            public int enemyIndex;
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
        internal int WTOEnemyID;

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
            ActiveState.enemyIndex = WTOEnemyID;
            ActiveState.OnStateEntered(WTOEnemyID, enemyRandom, creatureAnimator);

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

            foreach (StateTransition transition in AllTransitions) {
                transition.enemyIndex = WTOEnemyID;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    nextTransition = transition;
                    TransitionStateServerRpc(nextTransition.ToString(), GenerateNextRandomInt(nextTransition.NextState().RandomRange));
                    return;
                }
            }

            if (RunUpdate) {
                ActiveState.UpdateBehavior(WTOEnemyID, enemyRandom, creatureAnimator);
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

        [ServerRpc]
        internal void TransitionStateServerRpc(string StateName, int RandomInt) {
            TransitionStateClientRpc(StateName, RandomInt);
        }
        [ClientRpc]
        internal void TransitionStateClientRpc(string StateName, int RandomInt) {
            TransitionState(StateName, RandomInt);
        }
        internal void TransitionState(string StateName, int RandomInt) {
            //Jesus fuck I can't believe I have to do this
            Type type = Type.GetType(StateName);
            StateTransition LocalNextTransition = (StateTransition)Activator.CreateInstance(type);
            LocalNextTransition.enemyIndex = WTOEnemyID;
            if (LocalNextTransition.NextState().GetType() == ActiveState.GetType()) {
                return;
            }
            //LogMessage(StateName);
            LogMessage($"{__getTypeName()} #{WTOEnemyID} is Exiting:  {ActiveState}");
            ActiveState.OnStateExit(WTOEnemyID, enemyRandom, creatureAnimator);
            LogMessage($"{__getTypeName()} #{WTOEnemyID} is Transitioning via:  {LocalNextTransition}");
            ActiveState = LocalNextTransition.NextState();
            ActiveState.MyRandomInt = RandomInt;
            ActiveState.enemyIndex = WTOEnemyID;
            LogMessage($"{__getTypeName()} #{WTOEnemyID} is Entering:  {ActiveState}");
            ActiveState.OnStateEntered(WTOEnemyID, enemyRandom, creatureAnimator);

            //Debug Prints 
            StartOfRound.Instance.ClientPlayerList.TryGetValue(NetworkManager.Singleton.LocalClientId, out var value);
            LogMessage($"CREATURE: {enemyType.name} #{WTOEnemyID} STATE: {ActiveState} ON PLAYER: #{value} ({StartOfRound.Instance.allPlayerScripts[value].playerUsername})");
        }

        internal void LowerTimerValue(ref float Timer) {
            if (Timer == 0) {
                return;
            }
            Timer -= Time.deltaTime;
        }
        internal void OverrideState(BehaviorState state) {
            ActiveState = state;
            ActiveState.OnStateEntered(WTOEnemyID, enemyRandom, creatureAnimator);
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

        internal int GenerateNextRandomInt(Vector2 Range) {
            Range = nextTransition.NextState().RandomRange;
            return enemyRandom.Next((int)Range.x, (int)Range.y);
        }
    }
}
