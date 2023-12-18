using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {
    public class GallenarmaAI : EnemyAI, INoiseListener {

        //BEHAVIOR STATES
        private class Asleep : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Sleeping", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Sleeping", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new HeardNoise()
            };
        }
        private class WakingUp : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                self.creatureAnimator.SetBool("Breaking", true);
                Gallenarma.TimeUntilChainsBroken = enemyRandom.Next(/*1800, 3600*/ 18, 36);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.TimeSpentBreakingChains++;
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Breaking", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WakeTimerDone()
            };
        }
        private class Patrol : BehaviorState {
            private bool canMakeNextPoint;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Patrolling", true);
                canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 5f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if(!canMakeNextPoint) {
                    canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Patrolling", true);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new HeardNoise(),
                new Boombox()
            };
        }
        private class GoToNoise : BehaviorState {

            bool OnRouteToNextPoint;
            Vector3 NextPoint;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                self.creatureAnimator.SetBool("Moving", true);
                if (Gallenarma.Noise.Loudness != -1) {
                    NextPoint = Gallenarma.Noise.NoisePos;
                } else {
                    NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5);
                    WTOBase.LogToConsole("Gallenarma off to random point!");
                }
                OnRouteToNextPoint = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(NextPoint).position);
                self.agent.speed = 7f;
                return;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (!OnRouteToNextPoint) {
                    if (Gallenarma.Noise.Loudness != -1) {
                        NextPoint = Gallenarma.Noise.NoisePos;
                    } else {
                        NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5);
                        WTOBase.LogToConsole("Gallenarma off to random point!");
                    }
                    OnRouteToNextPoint = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(NextPoint).position);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReachedNoise(),
                new Boombox()
            };
        }
        private class Investigate : BehaviorState {

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.InvestigatingTime = 0;
                Gallenarma.TotalInvestigateTime = enemyRandom.Next(/*260, 540*/ 50, 100);
                self.creatureAnimator.SetBool("Moving", true);
                self.agent.speed = 0f;

            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (Gallenarma.InvestigatingTime < Gallenarma.TotalInvestigateTime) {
                    Gallenarma.InvestigatingTime++;
                    self.creatureAnimator.SetBool("Investigating", true);
                    return;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ThingAtNoise(),
                new NothingFoundAtNoise(),
                new Boombox()
            };
        }
        private class Attack : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Attacking", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if(Vector3.Distance(self.GetClosestPlayer(cannotBeInShip: true).transform.position, self.transform.position) < 5) { 
                    Gallenarma.MeleeAttackPlayer(self.GetClosestPlayer(cannotBeInShip:true));
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Attacking", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new KilledVictim(),
                new VictimEscaped()
            };
        }
        private class StringUp : BehaviorState {

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Carrying", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                //Grab the player's body and attack it to our hand
                //Go to a point near the main entrance and string up our victim
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Carrying", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new Boombox(),
                new DoneStringUp()
            };
        }
        private class Dance : BehaviorState {

            private bool MovingToBoombox;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Dancing", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                
                if (Vector3.Distance(Gallenarma.myBoomboxPos, self.transform.position) < 3) {
                    Gallenarma.TimeSpentDancing++;
                    return;
                }
                //if we're not near our boombox, move to it
                if (!MovingToBoombox) {
                    self.SetDestinationToPosition(Gallenarma.myBoomboxPos);
                }
                
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Dancing", value: false);
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.TimeSpentDancing = 0;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new BoredOfDancing()
            };
        }

        //STATE TRANSITIONS
        private class HeardNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Gallenarma.Noise.Loudness != -1);
            }
            public override BehaviorState NextState() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (Gallenarma.asleep) { 
                    return new WakingUp();
                }
                return new GoToNoise();
            }
        }
        private class WakeTimerDone : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;

                return (Gallenarma.TimeSpentBreakingChains > Gallenarma.TimeUntilChainsBroken);
            }
            public override BehaviorState NextState() {
                //this is silly but fuck it
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.asleep = false;
                Gallenarma.Noise.Loudness = -1;
                return new Patrol();
            }
            private bool IsEnemyInRange() {
                PlayerControllerB nearestPlayer = self.CheckLineOfSightForClosestPlayer(45f, 20, 2);
                return nearestPlayer == null;
            }
        }
        private class NothingFoundAtNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                WTOBase.LogToConsole("Investigating Time: " + Gallenarma.InvestigatingTime.ToString());
                WTOBase.LogToConsole("Total Time: " + Gallenarma.TotalInvestigateTime.ToString());
                return Gallenarma.InvestigatingTime >= Gallenarma.TotalInvestigateTime;
            }
            public override BehaviorState NextState() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                    Gallenarma.Noise.Loudness = -1;
                    return new Patrol();
            }
        }
        private class ThingAtNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                PlayerControllerB PotentialTargetPlayer = self.CheckLineOfSightForClosestPlayer(180f, 5);
                if(PotentialTargetPlayer == null) {
                    return false;
                }

                if (Vector3.Distance(PotentialTargetPlayer.transform.position, self.transform.position) < 5) {
                    self.targetPlayer = PotentialTargetPlayer;
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return new Attack();
            }
        }
        private class KilledVictim : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(self.targetPlayer != null) { 
                    return self.targetPlayer.health <= 0;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new StringUp();
            }
        }
        private class Boombox : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Gallenarma.TameTimer > 0);
            }
            public override BehaviorState NextState() {
                return new Dance();
            }
        }
        private class BoredOfDancing : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Gallenarma.TameTimer <= 0 || Gallenarma.TimeSpentDancing > 3000);
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class VictimEscaped : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(self.targetPlayer == null) {
                    return true;
                }
                if (Vector3.Distance(self.targetPlayer.transform.position, self.transform.position) > 5) {
                    self.targetPlayer = null;
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class DoneStringUp : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return Gallenarma.VictimStrungUp;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class ReachedNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Vector3.Distance(self.transform.position, self.destination) < 3);
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }

        public DeadBodyInfo bodyBeingCarried;
        private RoundManager roundManager;
        private float AITimer;

        int TotalInvestigateTime;
        int InvestigatingTime;
        private bool asleep = true;
        private bool VictimStrungUp;
        private System.Random enemyRandom;
        private int TimeSpentBreakingChains = 0;
        private float hearNoiseCooldown;
        private bool inKillAnimation;
        private int TimeUntilChainsBroken;
        private int TimeSpentDancing;
        private BehaviorState InitialState = new Asleep();
        private BehaviorState ActiveState = null;
        private int TameTimer = 0;
        private Vector3 myBoomboxPos;

        private struct NoiseInfo {
            public Vector3 NoisePos { get; private set; }
            public float Loudness { get; set; }
            public NoiseInfo(Vector3 position, float loudness) {
                NoisePos = position;
                Loudness = loudness;
            }


        }
        private List<NoiseInfo> NoiseStack = new List<NoiseInfo>();
        private NoiseInfo Noise = new NoiseInfo(new Vector3(-1,-1,-1), -1);

        protected override string __getTypeName() {
            return "GallenarmaAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
            ActiveState = InitialState;
            enemyHP = 20;
            //Mouthdog Start() code
            roundManager = FindObjectOfType<RoundManager>();
            useSecondaryAudiosOnAnimatedObjects = true;
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //Debug for the animations not fucking working
            creatureAnimator.Rebind();
        }
        public override void Update() {
            base.Update();
            AITimer++;
            //don't run enemy ai if they're dead
            TameTimer--;
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

            //Custom Gallenarma Code
            bool RunUpdate = true;
            //don't run enemy ai if they're dead
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

        //If we're attacked by a player, they need to be immediately set to our target player
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            //If we're attacked by a player, they need to be immediately set to our target player
            targetPlayer = playerWhoHit;
            ActiveState = new Attack();
        }
        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0) {
            base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
            if (stunNormalizedTimer > 0f || noiseID == 7 || noiseID == 546 || inKillAnimation || hearNoiseCooldown >= 0f || timesNoisePlayedInOneSpot > 15) {
                //return;
            }
            if (noiseID == 5) {
                TameTimer = 120;
                myBoomboxPos = noisePosition;
            }

            hearNoiseCooldown = 0.03f;
            float num = Vector3.Distance(base.transform.position, noisePosition);
            Debug.Log($"Gallenarma '{base.gameObject.name}': Heard noise! Distance: {num} meters");
            float num2 = 18f * noiseLoudness;
            if (Physics.Linecast(base.transform.position, noisePosition, 256)) {
                noiseLoudness /= 2f;
                num2 /= 2f;
            }

            if (noiseLoudness < 0.025f) {
                return;
            }
            //NoiseStack.Insert(0, new NoiseInfo(noisePosition, noiseLoudness));
            Noise = new NoiseInfo(noisePosition, noiseLoudness);
        }
        private void MeleeAttackPlayer(PlayerControllerB target) {
            target.DamagePlayer(100, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
            target.JumpToFearLevel(1f);
        }
    }
}
