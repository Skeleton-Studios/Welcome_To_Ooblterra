using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;
using UnityEngine.Networking;

namespace Welcome_To_Ooblterra.Enemies {
    public class GallenarmaAI : WTOEnemy, INoiseListener {

        //BEHAVIOR STATES
        private class Asleep : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.LogMessage("Exiting asleep state!");
                Instance.SetAnimTriggerOnServerRpc("WakeUp");
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new HeardNoise()
            };
        }
        private class WakingUp : BehaviorState {
            public WakingUp() { 
                RandomRange = new Vector2(15, 35);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.Awakening = true;
                Instance.SecondsUntilChainsBroken = MyRandomInt;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.LowerTimerValue(ref Instance.SecondsUntilChainsBroken);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WakeTimerDone()
            };
        }
        private class Patrol : BehaviorState {
            private bool canMakeNextPoint;
            public Patrol() {
                RandomRange = new Vector2(0, Instance.allAINodes.Length - 1);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.Awakening = false;
                self.creatureSFX.maxDistance = 15;

                Instance.SetAnimBoolOnServerRpc("Investigating", false);
                Instance.SetAnimBoolOnServerRpc("Attack", false);
                Instance.SetAnimBoolOnServerRpc("Moving", true);
                self.agent.speed = 5f;
                if (self.IsOwner) { 
                    canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 10), checkForPath: true);
                }
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (self.IsOwner) {
                    if (!canMakeNextPoint) {
                    canMakeNextPoint = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                    }
                    if(Vector3.Distance(self.transform.position, self.destination) < 10) {
                        canMakeNextPoint = false;
                    }
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new HeardNoise(),
                new Boombox()
            };
        }
        private class GoToNoise : BehaviorState {
            public GoToNoise() {
                RandomRange = new Vector2(0, Instance.allAINodes.Length - 1);
            }
            bool OnRouteToNextPoint;
            Transform NodeNearestToTargetNoise;
            NoiseInfo CurrentNoise;
            Vector3 NextPoint;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                //pass the latest noise to our gallenarma and tell it to go there
                Instance.SetAnimBoolOnServerRpc("Moving", true);
                if (Instance.LatestNoise.Loudness != -1) {
                    NodeNearestToTargetNoise = self.ChooseClosestNodeToPosition(Instance.LatestNoise.Location);
                    CurrentNoise = Instance.LatestNoise;
                    OnRouteToNextPoint = self.SetDestinationToPosition(NodeNearestToTargetNoise.position, true);
                    self.agent.speed = 7f;
                    return;
                }
                Instance.LogMessage("Noise does not have any loudness!");
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Instance.LatestNoise.Location != CurrentNoise.Location) {
                    CurrentNoise = Instance.LatestNoise;
                    NodeNearestToTargetNoise = self.ChooseClosestNodeToPosition(CurrentNoise.Location);
                    OnRouteToNextPoint = self.SetDestinationToPosition(NodeNearestToTargetNoise.position, true);
                    self.agent.speed = 6f;
                }
                if (!OnRouteToNextPoint) {
                    Instance.LogMessage("Noise was unreachable!");
                    if (Instance.LatestNoise.Loudness != -1) {
                        NextPoint = Instance.LatestNoise.Location;
                    } else {
                        NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[MyRandomInt].transform.position, 5);
                        WTOBase.LogToConsole("Gallenarma off to random point!");
                    }
                    OnRouteToNextPoint = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(NextPoint).position);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReachedNoise(),
                new Boombox()
            };
        }
        private class Investigate : BehaviorState {
            public Investigate() {
                RandomRange = new Vector2(5, 9);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.LatestNoise.Invalidate();
                Instance.creatureVoice.PlayOneShot(Instance.Search[enemyRandom.Next(0, Instance.Search.Count)]);
                Instance.TotalInvestigateSeconds = MyRandomInt;
                Instance.SetAnimBoolOnServerRpc("Investigating", true);
                self.agent.speed = 0f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Instance.TotalInvestigateSeconds < 0.8) {
                    Instance.SetAnimBoolOnServerRpc("Investigating", false);
                }

                if (Instance.TotalInvestigateSeconds > 0) {
                    Instance.LowerTimerValue(ref Instance.TotalInvestigateSeconds);
                    return;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Investigating", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundEnemy(),
                new NothingFoundAtNoise(),
                new Boombox(),
                new HeardNoise()
            };
        }
        private class Attack : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Attack", true);
                self.agent.speed = 0f;
                Instance.HasAttackedThisCycle = false;
                Instance.AttackTimerSeconds = Instance.AttackTime;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.TryMeleeAttackPlayer();
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Attack", false);
                Instance.HasAttackedThisCycle = false;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new KilledVictim(),
                new VictimEscaped()
            };
        }
        private class Chase : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.creatureVoice.clip = Instance.Growl;
                Instance.creatureVoice.Play();
                Instance.creatureVoice.loop = true;
                Instance.SetAnimBoolOnServerRpc("Investigating", false);
                Instance.SetAnimBoolOnServerRpc("Moving", true);
                self.agent.speed = 6f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                self.SetMovingTowardsTargetPlayer(Instance.targetPlayer);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.creatureVoice.loop = false;
                Instance.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundEnemy(),
                new LostTrackOfPlayer()
            };
        }
        private class Dance : BehaviorState {
            private bool MovingToBoombox;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SecondsUntilBored = 150;
                self.SetDestinationToPosition(Instance.myBoomboxPos);
                Instance.SetAnimBoolOnServerRpc("Moving", true);
                self.agent.speed = 5f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(Instance.myBoomboxPos, self.transform.position) < 5) {
                    Instance.SetAnimBoolOnServerRpc("Dancing", true);
                    self.agent.speed = 0f;
                    Instance.IsDancing = true;
                    Instance.LowerTimerValue(ref Instance.SecondsUntilBored);
                    return;
                }
                //if we're not near our boombox, move to it
                if (!MovingToBoombox) {
                    self.SetDestinationToPosition(Instance.myBoomboxPos);
                    MovingToBoombox = true;
                }
                
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.SetAnimBoolOnServerRpc("Dancing", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new BoredOfDancing()
            };
        }
        private class Stunned : BehaviorState {

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetTrigger("Stunned");
                self.targetPlayer = self.stunnedByPlayer;
                Instance.ChangeOwnershipOfEnemy(self.targetPlayer.actualClientId);
                Instance.LatestNoise = new NoiseInfo(self.stunnedByPlayer.transform.position, 5);
                Instance.StunTimeSeconds = 3.15f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Instance.LowerTimerValue(ref Instance.StunTimeSeconds);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new StunTimerFinished()
            };
        }

        //STATE TRANSITIONS
        private class HeardNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                Instance.LogMessage($"Latest noise ");
                return (Instance.LatestNoise.Loudness != -1);
            }
            public override BehaviorState NextState() {
                if (Instance.asleep) { 
                    return new WakingUp();
                }
                return new GoToNoise();
            }
        }
        private class WakeTimerDone : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (Instance.SecondsUntilChainsBroken > 0) {
                    Instance.SetAnimBoolOnServerRpc("Breaking", Instance.SecondsUntilChainsBroken <= 2);
                    return false;
                }
                return Instance.AnimationIsFinished("Gallenarma Break");
            }
            public override BehaviorState NextState() {
                //this is silly but fuck it
                Instance.asleep = false;
                Instance.LatestNoise.Loudness = -1;
                return new Patrol();
            }
            private bool IsEnemyInRange() {
                PlayerControllerB nearestPlayer = Instance.CheckLineOfSightForClosestPlayer(45f, 20, 2);
                return nearestPlayer == null;
            }
        }
        private class NothingFoundAtNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                return Instance.TotalInvestigateSeconds <= 0;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class FoundEnemy : StateTransition {
            public override bool CanTransitionBeTaken() {
                PlayerControllerB PotentialTargetPlayer = Instance.CheckLineOfSightForClosestPlayer(180f, Instance.AttackRange);
                if(PotentialTargetPlayer == null) {
                    return false;
                }

                if (Vector3.Distance(PotentialTargetPlayer.transform.position, Instance.transform.position) < Instance.AttackRange) {
                    Instance.targetPlayer = PotentialTargetPlayer;
                    Instance.ChangeOwnershipOfEnemy(Instance.targetPlayer.actualClientId);
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Attack();
            }
        }
        private class KilledVictim : StateTransition {
            public override bool CanTransitionBeTaken() {
                if(Instance.targetPlayer != null) { 
                    return Instance.targetPlayer.health <= 0;
                }
                return false;
            }
            public override BehaviorState NextState() {
                Instance.targetPlayer = null;
                return new Patrol();
            }
        }
        private class Boombox : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Instance.TameTimerSeconds > 0);
            }
            public override BehaviorState NextState() {
                return new Dance();
            }
        }
        private class BoredOfDancing : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (Instance.IsDancing) {
                    return Instance.SecondsUntilBored <= 0;
                }                    
                return Instance.TameTimerSeconds <= 0;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class VictimEscaped : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (Instance.targetPlayer == null) {
                    return true;
                }
                if (Vector3.Distance(Instance.targetPlayer.transform.position, Instance.transform.position) > Instance.AttackRange) {
                    Instance.AttackTimerSeconds = 0;
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Chase();
            }
        }
        private class LostTrackOfPlayer : StateTransition {
            public override bool CanTransitionBeTaken() {
                if (Instance.targetPlayer == null || !Instance.PlayerCanBeTargeted(Instance.targetPlayer)){
                    Instance.targetPlayer = null;
                    return true;
                }
                float PlayerDistanceFromGallenarma = Vector3.Distance(Instance.transform.position, Instance.targetPlayer.transform.position);
                float playerDistanceFromLastNoise = Vector3.Distance(Instance.LatestNoise.Location, Instance.targetPlayer.transform.position);
                float DistanceToCheck = Math.Min(playerDistanceFromLastNoise, PlayerDistanceFromGallenarma);
                
                return DistanceToCheck > 10;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class StunTimerFinished : StateTransition {
            public override bool CanTransitionBeTaken() {
                return Instance.StunTimeSeconds < 0.1;
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class ReachedNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Vector3.Distance(Instance.transform.position, Instance.destination) < 3);
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }

        public float TotalInvestigateSeconds;
        private bool asleep = true;
        private bool Awakening = false;
        private float SecondsUntilChainsBroken;
        private float hearNoiseCooldown;
        private float SecondsUntilBored;
        private float TameTimerSeconds = 0;
        private Vector3 myBoomboxPos;
        private float AttackTimerSeconds = 0f;
        private readonly int AttackRange = 3;
        private float StunTimeSeconds;
        private bool IsDancing;
        private bool HasAttackedThisCycle;
        private readonly float AttackTime = 1.92f;
        public static GallenarmaAI Instance;

        public AudioClip Growl;
        public List<AudioClip> Search = new List<AudioClip>();

        private struct NoiseInfo {
            public Vector3 Location { get; private set; }
            public float Loudness { get; set; }
            public NoiseInfo(Vector3 position, float loudness) {
                Location = position;
                Loudness = loudness;
            }

            public void Invalidate() {
                Location = new Vector3(-1, -1, -1);
                Loudness = -1;
            }
        }
        private NoiseInfo LatestNoise = new NoiseInfo(new Vector3(-1,-1,-1), -1);

        public override void Start() {
            Instance = this;
            InitialState = new Asleep();
            enemyHP = 20;
            PrintDebugs = true;
            base.Start();
        }
        public override void Update() {
            LowerTimerValue(ref TameTimerSeconds);
            if(TameTimerSeconds <= 0) {
                IsDancing = false;
            }
            LowerTimerValue(ref hearNoiseCooldown);
            base.Update();
        }

        //If we're attacked by a player, they need to be immediately set to our target player
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            SetAnimTriggerOnServerRpc("Hit");
            if (enemyHP <= 0) {
                SetAnimTriggerOnServerRpc("Killed");
                if (base.IsOwner) {
                    KillEnemyOnOwnerClient();

                }
                return;
            }

            //If we're attacked by a player, they need to be immediately set to our target player
            targetPlayer = playerWhoHit;
            ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
            OverrideState(new Investigate());
        }
        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0) {
            base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
            if (noiseID == 5) {
                TameTimerSeconds = 2;
                myBoomboxPos = noisePosition;
            } else if (stunNormalizedTimer > 0f || noiseID == 7 || noiseID == 546 || hearNoiseCooldown > 0f || timesNoisePlayedInOneSpot > 15 || isEnemyDead) {
                return;
            }

            if (Awakening) {
                float randomTimeReduction = (float)(SecondsUntilChainsBroken - (0.01 * enemyRandom.Next(50, 250)));
                SecondsUntilChainsBroken = Math.Max(randomTimeReduction, 0.8f);
            }
            hearNoiseCooldown = 0.03f;
            float num = Vector3.Distance(base.transform.position, noisePosition);
            Debug.Log($"Gallenarma '{gameObject.name}': Heard noise! Distance: {num} meters. Location: {noisePosition}");
            float num2 = 18f * noiseLoudness;
            if (Physics.Linecast(base.transform.position, noisePosition, 256)) {
                noiseLoudness /= 2f;
                num2 /= 2f;
            }

            if (noiseLoudness < 0.025f) {
                return;
            }
            if (!(ActiveState is Attack || ActiveState is Chase)) { 
                ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
            }
            //NoiseStack.Insert(0, new NoiseInfo(noisePosition, noiseLoudness));
            LatestNoise = new NoiseInfo(noisePosition, noiseLoudness);
        }
        public void TryMeleeAttackPlayer() {
            LowerTimerValue(ref AttackTimerSeconds);
            if(targetPlayer == null) {
                return;
            }
            if (AttackTimerSeconds <= .71f && Vector3.Distance(targetPlayer.transform.position, transform.position) < AttackRange && !HasAttackedThisCycle) {
                LogMessage("Attacking!");
                targetPlayer.DamagePlayer(120, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
                HasAttackedThisCycle = true;
            }
            if(AttackTimerSeconds <= 0f) {
                AttackTimerSeconds = AttackTime;
                HasAttackedThisCycle = false;
            }
        }
    }
}
