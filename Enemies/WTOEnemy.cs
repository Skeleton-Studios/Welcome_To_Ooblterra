using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

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

        internal BehaviorState InitialState;
        internal BehaviorState ActiveState = null;
        private System.Random enemyRandom;
        private int AITimer;
        private RoundManager roundManager;

        public override void Start() {
            base.Start();
            ActiveState = InitialState;
            roundManager = FindObjectOfType<RoundManager>();
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //Debug for the animations not fucking working
            creatureAnimator.Rebind();
        }
        public override void Update() {
            base.Update();
            AITimer++;
            bool RunUpdate = true;
            foreach (StateTransition transition in ActiveState.transitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    Debug.Log("Exiting: " + ActiveState.ToString());
                    ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
                    Debug.Log("Transitioning Via: " + transition.ToString());
                    ActiveState = transition.NextState();
                    Debug.Log("Entering: " + ActiveState.ToString());
                    ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
                    break;
                }
            }
            if (RunUpdate) {
                ActiveState.UpdateBehavior(this, enemyRandom, creatureAnimator);
            }
        }
    }
}
