using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Enemies;
public class GallenarmaAI : WTOEnemy, INoiseListener {
    //BEHAVIOR STATES
    private class Asleep : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].TryRandomAwake();
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimTriggerOnServerRpc("WakeUp");
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new HeardNoise()
        };
    }
    private class WakingUp : BehaviorState {
        public WakingUp() { 
            RandomRange = new Vector2(15, 35);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimTriggerOnServerRpc("WakeUp");
            GallenarmaList[enemyIndex].Awakening = true;
            GallenarmaList[enemyIndex].SecondsUntilChainsBroken = MyRandomInt;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].LowerTimerValue(ref GallenarmaList[enemyIndex].SecondsUntilChainsBroken);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new WakeTimerDone()
        };
    }
    private class Patrol : BehaviorState {
        private bool canMakeNextPoint;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].Awakening = false;
            GallenarmaList[enemyIndex].creatureSFX.maxDistance = 15;

            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Attack", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            GallenarmaList[enemyIndex].agent.speed = 5f;
            if (GallenarmaList[enemyIndex].IsOwner) { 
                canMakeNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].allAINodes[enemyRandom.Next(GallenarmaList[enemyIndex].allAINodes.Length - 1)].transform.position, 15), checkForPath: true);
            }
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (GallenarmaList[enemyIndex].IsOwner) {
                if (!canMakeNextPoint) {
                canMakeNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].allAINodes[enemyRandom.Next(GallenarmaList[enemyIndex].allAINodes.Length - 1)].transform.position, 15), checkForPath: true);
                }
                if(Vector3.Distance(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].destination) < 10) {
                    canMakeNextPoint = false;
                }
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new HeardNoise(),
            new Boombox()
        };
    }
    private class GoToNoise : BehaviorState {
        bool OnRouteToNextPoint;
        Transform RangeOfTargetNoise;
        Vector3 TargetNoisePosition;
        NoiseInfo CurrentNoise;
        Vector3 NextPoint;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //pass the latest noise to our gallenarma and tell it to go there
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            if (GallenarmaList[enemyIndex].LatestNoise.Loudness != -1) {
                TargetNoisePosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].LatestNoise.Location, 5);
                CurrentNoise = GallenarmaList[enemyIndex].LatestNoise;
                OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(TargetNoisePosition, true);
                GallenarmaList[enemyIndex].agent.speed = 7f;
                return;
            }
            GallenarmaList[enemyIndex].LogMessage("Noise does not have any loudness!");
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (GallenarmaList[enemyIndex].LatestNoise.Location != CurrentNoise.Location) {
                CurrentNoise = GallenarmaList[enemyIndex].LatestNoise;
                RangeOfTargetNoise = GallenarmaList[enemyIndex].ChooseClosestNodeToPosition(CurrentNoise.Location);
                OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(RangeOfTargetNoise.position, true);
                GallenarmaList[enemyIndex].agent.speed = 6f;
            }
            if (!OnRouteToNextPoint) {
                GallenarmaList[enemyIndex].LogMessage("Noise was unreachable!");
                if (GallenarmaList[enemyIndex].LatestNoise.Loudness != -1) {
                    NextPoint = GallenarmaList[enemyIndex].LatestNoise.Location;
                } else {
                    NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].allAINodes[MyRandomInt].transform.position, 15);
                    WTOBase.LogToConsole("Gallenarma off to random point!");
                }
                OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].ChooseClosestNodeToPosition(NextPoint).position);
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
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
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].LatestNoise.Invalidate();
            GallenarmaList[enemyIndex].creatureVoice.PlayOneShot(GallenarmaList[enemyIndex].Search[enemyRandom.Next(0, GallenarmaList[enemyIndex].Search.Count)]);
            GallenarmaList[enemyIndex].TotalInvestigateSeconds = MyRandomInt;
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", true);
            GallenarmaList[enemyIndex].agent.speed = 0f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (GallenarmaList[enemyIndex].TotalInvestigateSeconds < 0.8) {
                GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            }

            if (GallenarmaList[enemyIndex].TotalInvestigateSeconds > 0) {
                GallenarmaList[enemyIndex].LowerTimerValue(ref GallenarmaList[enemyIndex].TotalInvestigateSeconds);
                return;
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundEnemy(),
            new NothingFoundAtNoise(),
            new Boombox(),
            new HeardNoise()
        };
    }
    private class Attack : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].creatureVoice.clip = GallenarmaList[enemyIndex].Growl;
            GallenarmaList[enemyIndex].creatureVoice.Play();
            GallenarmaList[enemyIndex].creatureVoice.loop = true;
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Attack", true);
            GallenarmaList[enemyIndex].HasAttackedThisCycle = false;
            GallenarmaList[enemyIndex].AttackTimerSeconds = 1.667f;
            GallenarmaList[enemyIndex].SetMovingTowardsTargetPlayer(GallenarmaList[enemyIndex].targetPlayer);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            if (GallenarmaList[enemyIndex].DistanceFromPlayer(GallenarmaList[enemyIndex].targetPlayer) > GallenarmaList[enemyIndex].AttackRange) {
                GallenarmaList[enemyIndex].agent.speed = 7f;
            } else {
                GallenarmaList[enemyIndex].agent.speed = 2f;
            }
            GallenarmaList[enemyIndex].TryMeleeAttackPlayer(120);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Attack", false);
            GallenarmaList[enemyIndex].HasAttackedThisCycle = false;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new KilledVictim(),
            new VictimEscaped()
        };
    }
    private class Chase : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            GallenarmaList[enemyIndex].SetMovingTowardsTargetPlayer(GallenarmaList[enemyIndex].targetPlayer);
            GallenarmaList[enemyIndex].agent.speed = 7f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                
                
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].creatureVoice.loop = false;
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundEnemy(),
            new LostTrackOfPlayer()
        };
    }
    private class Dance : BehaviorState {
        private bool MovingToBoombox;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SecondsUntilBored = 150;
            GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].myBoomboxPos);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            GallenarmaList[enemyIndex].agent.speed = 5f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (Vector3.Distance(GallenarmaList[enemyIndex].myBoomboxPos, GallenarmaList[enemyIndex].transform.position) < 5) {
                GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Dancing", true);
                GallenarmaList[enemyIndex].agent.speed = 0f;
                GallenarmaList[enemyIndex].IsDancing = true;
                GallenarmaList[enemyIndex].LowerTimerValue(ref GallenarmaList[enemyIndex].SecondsUntilBored);
                return;
            }
            //if we're not near our boombox, move to it
            if (!MovingToBoombox) {
                GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].myBoomboxPos);
                MovingToBoombox = true;
            }
                
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Dancing", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new BoredOfDancing()
        };
    }
    private class Enraged : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimTriggerOnServerRpc("Stunned");
            //GallenarmaList[enemyIndex].targetPlayer = GallenarmaList[enemyIndex].stunnedByPlayer;
            GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
            //GallenarmaList[enemyIndex].LatestNoise = new NoiseInfo(GallenarmaList[enemyIndex].stunnedByPlayer.transform.position, 5);
            GallenarmaList[enemyIndex].StunTimeSeconds = 3.15f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].LowerTimerValue(ref GallenarmaList[enemyIndex].StunTimeSeconds);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new EnrageAnimFinished()
        };
    }

    //STATE TRANSITIONS
    private class HeardNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (GallenarmaList[enemyIndex].LatestNoise.Loudness != -1);
        }
        public override BehaviorState NextState() {
            if (GallenarmaList[enemyIndex].asleep) { 
                return new WakingUp();
            }
            return new GoToNoise();
        }
    }
    private class WakeTimerDone : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (GallenarmaList[enemyIndex].SecondsUntilChainsBroken > 0) {
                GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Breaking", GallenarmaList[enemyIndex].SecondsUntilChainsBroken <= 2);
                return false;
            }
            return GallenarmaList[enemyIndex].AnimationIsFinished("Gallenarma Break");
        }
        public override BehaviorState NextState() {
            //this is silly but fuck it
            GallenarmaList[enemyIndex].asleep = false;
            GallenarmaList[enemyIndex].LatestNoise.Loudness = -1;
            return new Patrol();
        }
        private bool IsEnemyInRange() {
            PlayerControllerB nearestPlayer = GallenarmaList[enemyIndex].CheckLineOfSightForClosestPlayer(45f, 20, 2);
            return nearestPlayer == null;
        }
    }
    private class NothingFoundAtNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GallenarmaList[enemyIndex].TotalInvestigateSeconds <= 0;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class FoundEnemy : StateTransition {
        public override bool CanTransitionBeTaken() {
            PlayerControllerB PotentialTargetPlayer = GallenarmaList[enemyIndex].CheckLineOfSightForClosestPlayer(180f, 7);
            if(PotentialTargetPlayer == null) {
                return false;
            }

            if (Vector3.Distance(PotentialTargetPlayer.transform.position, GallenarmaList[enemyIndex].transform.position) < 7) {
                GallenarmaList[enemyIndex].targetPlayer = PotentialTargetPlayer;
                GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
                return true;
            }
            return false;
        }
        public override BehaviorState NextState() {
            return new Attack();
        }
    }
    private class EnemyBackInRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            PlayerControllerB PotentialTargetPlayer = GallenarmaList[enemyIndex].CheckLineOfSightForClosestPlayer(180f, 7);
            if (PotentialTargetPlayer == null) {
                return false;
            }

            if (Vector3.Distance(PotentialTargetPlayer.transform.position, GallenarmaList[enemyIndex].transform.position) < 7) {
                GallenarmaList[enemyIndex].targetPlayer = PotentialTargetPlayer;
                GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
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
            if(GallenarmaList[enemyIndex].targetPlayer != null) { 
                return GallenarmaList[enemyIndex].targetPlayer.health <= 0;
            }
            return false;
        }
        public override BehaviorState NextState() {
            GallenarmaList[enemyIndex].creatureVoice.Stop();
            GallenarmaList[enemyIndex].creatureVoice.loop = false;
            GallenarmaList[enemyIndex].targetPlayer = null;
            return new Patrol();
        }
    }
    private class Boombox : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (GallenarmaList[enemyIndex].TameTimerSeconds > 0);
        }
        public override BehaviorState NextState() {
            return new Dance();
        }
    }
    private class BoredOfDancing : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (GallenarmaList[enemyIndex].IsDancing) {
                return GallenarmaList[enemyIndex].SecondsUntilBored <= 0;
            }                    
            return GallenarmaList[enemyIndex].TameTimerSeconds <= 0;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class VictimEscaped : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (GallenarmaList[enemyIndex].targetPlayer == null) {
                return true;
            }
            if (Vector3.Distance(GallenarmaList[enemyIndex].targetPlayer.transform.position, GallenarmaList[enemyIndex].transform.position) > GallenarmaList[enemyIndex].AttackRange + 2.5) {
                GallenarmaList[enemyIndex].AttackTimerSeconds = 0;
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
            if (GallenarmaList[enemyIndex].targetPlayer == null || !GallenarmaList[enemyIndex].PlayerCanBeTargeted(GallenarmaList[enemyIndex].targetPlayer)){
                GallenarmaList[enemyIndex].targetPlayer = null;
                return true;
            }
            float PlayerDistanceFromGallenarma = Vector3.Distance(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].targetPlayer.transform.position);
            float playerDistanceFromLastNoise = Vector3.Distance(GallenarmaList[enemyIndex].LatestNoise.Location, GallenarmaList[enemyIndex].targetPlayer.transform.position);
            float DistanceToCheck = Math.Min(playerDistanceFromLastNoise, PlayerDistanceFromGallenarma);
                
            return DistanceToCheck > 10;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class EnrageAnimFinished : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GallenarmaList[enemyIndex].StunTimeSeconds < 0.1;
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class ReachedNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (Vector3.Distance(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].destination) < 3);
        }
        public override BehaviorState NextState() {
            return new Investigate();
        }
    }
    private class HitByStunGun : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GallenarmaList[enemyIndex].stunNormalizedTimer > 0 && !(GallenarmaList[enemyIndex].ActiveState is Chase);
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }

    public float TotalInvestigateSeconds;
    public float RandomAwakeTimerSeconds = 5f;
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
    public static Dictionary<int, GallenarmaAI> GallenarmaList = new Dictionary<int, GallenarmaAI>();
    public static int GallenarmaID;

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
        GallenarmaID++;
        WTOEnemyID = GallenarmaID;
        LogMessage($"Adding Gallenarma {this} at {GallenarmaID}");
        GallenarmaList.Add(GallenarmaID, this);
        GlobalTransitions.Add(new HitByStunGun());
        base.Start();
        //if (enemyRandom.Next(1, 10) > 8) {
        //    LatestNoise = new NoiseInfo(transform.position, 2);
        //}
    }
    public override void Update() {
        LowerTimerValue(ref AttackTimerSeconds);
        LowerTimerValue(ref TameTimerSeconds);
        if(TameTimerSeconds <= 0) {
            IsDancing = false;
        }
        LowerTimerValue(ref hearNoiseCooldown);
        LowerTimerValue(ref RandomAwakeTimerSeconds);
        base.Update();
    }

    //If we're attacked by a player, they need to be immediately set to our target player
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
        if (isEnemyDead) { return; }
        base.HitEnemy(force, playerWhoHit, playHitSFX);
        enemyHP -= force;
        SetAnimTriggerOnServerRpc("Hit");
        if (enemyHP <= 0) {
            SetAnimTriggerOnServerRpc("Killed");
            isEnemyDead = true;
            creatureVoice.Stop();
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
        LatestNoise = new NoiseInfo(noisePosition, noiseLoudness);
    }
    public void TryMeleeAttackPlayer(int damage) {
            
        if(targetPlayer == null) {
            return;
        }
        if (AttackTimerSeconds <= 0.8f && Vector3.Distance(targetPlayer.transform.position, transform.position) < AttackRange && !HasAttackedThisCycle) {
            LogMessage("Attacking!");
            targetPlayer.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
            HasAttackedThisCycle = true;
        }
        if(AttackTimerSeconds <= 0f) {
            AttackTimerSeconds = AttackTime;
            HasAttackedThisCycle = false;
        }
    }
    public void TryRandomAwake() {
        if (RandomAwakeTimerSeconds > 0f) {
            return;
        }
        /*
        if(enemyRandom.Next(0, 100) < 2) {
            LatestNoise = new NoiseInfo(Instance.transform.position, 2);
        }
        RandomAwakeTimerSeconds = 5f;
        */
    }
}
