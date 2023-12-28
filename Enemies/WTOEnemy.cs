using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using UnityEngine.Networking;
using Unity.Netcode;

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
        internal PlayerState MyValidState;
        internal StateTransition nextTransition;
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

            //play the stun animation if they're stunned 
            //TODO: SetLayerWeight switches between the basic animation layer (0) and the stun animation layer (1). 
            //The wanderer will need a similar setup if we want to be able to stun him, plus a stun animation 
            /*
            if (stunNormalizedTimer > 0f && !isEnemyDead) {
                if (stunnedByPlayer != null && currentBehaviourStateIndex != 2 && base.IsOwner) {
                    creatureAnimator.SetLayerWeight(1, 1f);
                }
            } else {
                creatureAnimator.SetLayerWeight(1, 0f);
            }
            */
            bool RunUpdate = true;
            foreach (StateTransition transition in ActiveState.transitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    nextTransition = transition;
                    if (base.IsOwner) { 
                        TransitionStateServerRPC();
                    }
                    /*
                    LogMessage("Exiting: " + ActiveState.ToString());
                    ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
                    LogMessage("Transitioning Via: " + transition.ToString());
                    ActiveState = transition.NextState();
                    LogMessage("Entering: " + ActiveState.ToString());
                    ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
                    */
                    break;
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
            return (ValidatePlayer(myPlayer) == MyValidState);
        }
        internal PlayerState ValidatePlayer(PlayerControllerB myPlayer) {
            if (myPlayer.isPlayerDead) {
                return PlayerState.Dead;
            }
            if (myPlayer.isInsideFactory) {
                return PlayerState.Inside;
            }
            return PlayerState.Outside;
        }

        [ServerRpc]
        public void TransitionStateServerRPC() {
            TransitionStateClientRpc();
        }
        [ClientRpc]
        public void TransitionStateClientRpc() {
            TransitionState();
        }

        public void TransitionState() {
            LogMessage("Exiting: " + ActiveState.ToString());
            ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
            LogMessage("Transitioning Via: " + nextTransition.ToString());
            ActiveState = nextTransition.NextState();
            LogMessage("Entering: " + ActiveState.ToString());
            ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
        }
    }

}
