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
            GallenarmaAI Gallenarma;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.LogMessage("Exiting asleep state!");
                Gallenarma.SetAnimTriggerOnServerRpc("WakeUp");
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
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.Awakening = true;
                Gallenarma.SecondsUntilChainsBroken = MyRandomInt;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.LowerTimerValue(ref Gallenarma.SecondsUntilChainsBroken);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WakeTimerDone()
            };
        }
        private class Patrol : BehaviorState {
            private bool canMakeNextPoint;
            GallenarmaAI Gallenarma;
            public Patrol() {
                RandomRange = new Vector2(0, self.allAINodes.Length - 1);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.Awakening = false;
                self.creatureSFX.maxDistance = 15;
                
                Gallenarma.SetAnimBoolOnServerRpc("Investigating", false);
                Gallenarma.SetAnimBoolOnServerRpc("Attack", false);
                Gallenarma.SetAnimBoolOnServerRpc("Moving", true);
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
                Gallenarma.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new HeardNoise(),
                new Boombox()
            };
        }
        private class GoToNoise : BehaviorState {
            public GoToNoise() {
                RandomRange = new Vector2(0, self.allAINodes.Length - 1);
            }
            bool OnRouteToNextPoint;
            Transform NodeNearestToTargetNoise;
            NoiseInfo CurrentNoise;
            Vector3 NextPoint;
            GallenarmaAI Gallenarma;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                //pass the latest noise to our gallenarma and tell it to go there
                Gallenarma = self as GallenarmaAI;
                Gallenarma.SetAnimBoolOnServerRpc("Moving", true);
                if (Gallenarma.LatestNoise.Loudness != -1) {
                    NodeNearestToTargetNoise = self.ChooseClosestNodeToPosition(Gallenarma.LatestNoise.Location);
                    CurrentNoise = Gallenarma.LatestNoise;
                    OnRouteToNextPoint = self.SetDestinationToPosition(NodeNearestToTargetNoise.position, true);
                    self.agent.speed = 7f;
                    return;
                }
                Gallenarma.LogMessage("Noise does not have any loudness!");
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Gallenarma.LatestNoise.Location != CurrentNoise.Location) {
                    CurrentNoise = Gallenarma.LatestNoise;
                    NodeNearestToTargetNoise = self.ChooseClosestNodeToPosition(CurrentNoise.Location);
                    OnRouteToNextPoint = self.SetDestinationToPosition(NodeNearestToTargetNoise.position, true);
                    self.agent.speed = 6f;
                }
                if (!OnRouteToNextPoint) {
                    Gallenarma.LogMessage("Noise was unreachable!");
                    if (Gallenarma.LatestNoise.Loudness != -1) {
                        NextPoint = Gallenarma.LatestNoise.Location;
                    } else {
                        NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[MyRandomInt].transform.position, 5);
                        WTOBase.LogToConsole("Gallenarma off to random point!");
                    }
                    OnRouteToNextPoint = self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(NextPoint).position);
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new ReachedNoise(),
                new Boombox()
            };
        }
        private class Investigate : BehaviorState {
            GallenarmaAI Gallenarma;
            public Investigate() {
                RandomRange = new Vector2(5, 9);
            }
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.LatestNoise.Invalidate();
                Gallenarma.creatureVoice.PlayOneShot(Gallenarma.Search[enemyRandom.Next(0, Gallenarma.Search.Count)]);
                Gallenarma.TotalInvestigateSeconds = MyRandomInt;
                Gallenarma.SetAnimBoolOnServerRpc("Investigating", true);
                self.agent.speed = 0f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (Gallenarma.TotalInvestigateSeconds < 0.8) {
                    Gallenarma.SetAnimBoolOnServerRpc("Investigating", false);
                }

                if (Gallenarma.TotalInvestigateSeconds > 0) {
                    Gallenarma.LowerTimerValue(ref Gallenarma.TotalInvestigateSeconds);
                    return;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.SetAnimBoolOnServerRpc("Investigating", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundEnemy(),
                new NothingFoundAtNoise(),
                new Boombox(),
                new HeardNoise()
            };
        }
        private class Attack : BehaviorState {
            GallenarmaAI Gallenarma;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.SetAnimBoolOnServerRpc("Attack", true);
                self.agent.speed = 0f;
                Gallenarma.HasAttackedThisCycle = false;
                Gallenarma.AttackTimerSeconds = Gallenarma.AttackTime;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.TryMeleeAttackPlayer();
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.SetAnimBoolOnServerRpc("Attack", false);
                Gallenarma.HasAttackedThisCycle = false;
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new KilledVictim(),
                new VictimEscaped()
            };
        }
        private class Chase : BehaviorState {
            GallenarmaAI Gallenarma;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.creatureVoice.clip = Gallenarma.Growl;
                Gallenarma.creatureVoice.Play();
                Gallenarma.creatureVoice.loop = true;
                Gallenarma.SetAnimBoolOnServerRpc("Investigating", false);
                Gallenarma.SetAnimBoolOnServerRpc("Moving", true);
                self.agent.speed = 6f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                self.SetMovingTowardsTargetPlayer(Gallenarma.targetPlayer);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.creatureVoice.loop = false;
                Gallenarma.SetAnimBoolOnServerRpc("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundEnemy(),
                new LostTrackOfPlayer()
            };
        }
        private class Dance : BehaviorState {
            GallenarmaAI Gallenarma;
            private bool MovingToBoombox;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                Gallenarma.SecondsUntilBored = 150;
                self.SetDestinationToPosition(Gallenarma.myBoomboxPos);
                Gallenarma.SetAnimBoolOnServerRpc("Moving", true);
                self.agent.speed = 5f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Vector3.Distance(Gallenarma.myBoomboxPos, self.transform.position) < 5) {
                    Gallenarma.SetAnimBoolOnServerRpc("Dancing", true);
                    self.agent.speed = 0f;
                    Gallenarma.IsDancing = true;
                    Gallenarma.LowerTimerValue(ref Gallenarma.SecondsUntilBored);
                    return;
                }
                //if we're not near our boombox, move to it
                if (!MovingToBoombox) {
                    self.SetDestinationToPosition(Gallenarma.myBoomboxPos);
                    MovingToBoombox = true;
                }
                
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.SetAnimBoolOnServerRpc("Dancing", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new BoredOfDancing()
            };
        }
        private class Stunned : BehaviorState {

            GallenarmaAI Gallenarma;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma = self as GallenarmaAI;
                creatureAnimator.SetTrigger("Stunned");
                self.targetPlayer = self.stunnedByPlayer;
                Gallenarma.ChangeOwnershipOfEnemy(self.targetPlayer.actualClientId);
                Gallenarma.LatestNoise = new NoiseInfo(self.stunnedByPlayer.transform.position, 5);
                Gallenarma.StunTimeSeconds = 3.15f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Gallenarma.LowerTimerValue(ref Gallenarma.StunTimeSeconds);
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
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Gallenarma.LatestNoise.Loudness != -1);
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
                if (Gallenarma.SecondsUntilChainsBroken > 0) {
                    Gallenarma.SetAnimBoolOnServerRpc("Breaking", Gallenarma.SecondsUntilChainsBroken <= 2);
                    return false;
                }
                return Gallenarma.AnimationIsFinished("Gallenarma Break");
            }
            public override BehaviorState NextState() {
                //this is silly but fuck it
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                Gallenarma.asleep = false;
                Gallenarma.LatestNoise.Loudness = -1;
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
                return Gallenarma.TotalInvestigateSeconds <= 0;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class FoundEnemy : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                PlayerControllerB PotentialTargetPlayer = self.CheckLineOfSightForClosestPlayer(180f, Gallenarma.AttackRange);
                if(PotentialTargetPlayer == null) {
                    return false;
                }

                if (Vector3.Distance(PotentialTargetPlayer.transform.position, self.transform.position) < Gallenarma.AttackRange) {
                    self.targetPlayer = PotentialTargetPlayer;
                    Gallenarma.ChangeOwnershipOfEnemy(self.targetPlayer.actualClientId);
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
                self.targetPlayer = null;
                return new Patrol();
            }
        }
        private class Boombox : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Gallenarma.TameTimerSeconds > 0);
            }
            public override BehaviorState NextState() {
                return new Dance();
            }
        }
        private class BoredOfDancing : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (Gallenarma.IsDancing) {
                    return Gallenarma.SecondsUntilBored <= 0;
                }                    
                return Gallenarma.TameTimerSeconds <= 0;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class VictimEscaped : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (self.targetPlayer == null) {
                    return true;
                }
                if (Vector3.Distance(self.targetPlayer.transform.position, self.transform.position) > Gallenarma.AttackRange) {
                    Gallenarma.AttackTimerSeconds = 0;
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
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                if (self.targetPlayer == null || !Gallenarma.PlayerCanBeTargeted(self.targetPlayer)){  
                    self.targetPlayer = null;
                    return true;
                }
                float PlayerDistanceFromGallenarma = Vector3.Distance(self.transform.position, self.targetPlayer.transform.position);
                float playerDistanceFromLastNoise = Vector3.Distance(Gallenarma.LatestNoise.Location, self.targetPlayer.transform.position);
                float DistanceToCheck = Math.Min(playerDistanceFromLastNoise, PlayerDistanceFromGallenarma);
                
                return DistanceToCheck > 10;
            }
            public override BehaviorState NextState() {
                return new Patrol();
            }
        }
        private class StunTimerFinished : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return Gallenarma.StunTimeSeconds < 0.1;
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class ReachedNoise : StateTransition {
            public override bool CanTransitionBeTaken() {
                GallenarmaAI Gallenarma = self as GallenarmaAI;
                return (Vector3.Distance(self.transform.position, Gallenarma.destination) < 1);
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
