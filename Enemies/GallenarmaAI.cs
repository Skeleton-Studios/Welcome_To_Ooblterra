using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Enemies;
public class GallenarmaAI : WTOEnemy<GallenarmaAI>, INoiseListener {
    //BEHAVIOR STATES
    private class Asleep : BehaviorState {
        public override void OnStateEntered(WTOEnemy enemyInstance) {

        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.TryRandomAwake();
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimTriggerOnServerRpc("WakeUp");
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new HeardNoise()
        };
    }
    private class WakingUp : BehaviorState {
        public WakingUp() { 
            RandomRange = new Vector2(15, 35);
        }
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimTriggerOnServerRpc("WakeUp");
            ThisEnemy.Awakening = true;
            ThisEnemy.SecondsUntilChainsBroken = MyRandomInt;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.LowerTimerValue(ref ThisEnemy.SecondsUntilChainsBroken);
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new WakeTimerDone()
        };
    }
    private class Patrol : BehaviorState {
        private bool canMakeNextPoint;
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.Awakening = false;
            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Attack", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", true);
            ThisEnemy.agent.speed = 5f;
            ThisEnemy.StartSearch(ThisEnemy.transform.position, ThisEnemy.RoamLab);
            ThisEnemy.HasBeenEnragedThisCycle = false;
            /*
            if (ThisEnemy.IsOwner) { 
                canMakeNextPoint = ThisEnemy.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(ThisEnemy.allAINodes[enemyRandom.Next(ThisEnemy.allAINodes.Length - 1)].transform.position, 15), checkForPath: true);
            }
            */
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (!ThisEnemy.RoamLab.inProgress) {
                ThisEnemy.StartSearch(ThisEnemy.transform.position, ThisEnemy.RoamLab);
            }
            /*
            if (ThisEnemy.IsOwner) {
                if (!canMakeNextPoint) {
                canMakeNextPoint = ThisEnemy.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(ThisEnemy.allAINodes[enemyRandom.Next(ThisEnemy.allAINodes.Length - 1)].transform.position, 15), checkForPath: true);
                }
                if(Vector3.Distance(ThisEnemy.transform.position, ThisEnemy.destination) < 10) {
                    canMakeNextPoint = false;
                }
            }
            */
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.StopSearch(ThisEnemy.RoamLab, clear: false);
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", false);
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
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            //pass the latest noise to our gallenarma and tell it to go there
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", true);
            if (ThisEnemy.LatestNoise.Loudness != -1) {

                CurrentNoise = ThisEnemy.LatestNoise;
                if (ThisEnemy.agent.isOnNavMesh) {
                    TargetNoisePosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(ThisEnemy.LatestNoise.Location, 5);
                    OnRouteToNextPoint = ThisEnemy.SetDestinationToPosition(TargetNoisePosition, true);
                }
                ThisEnemy.agent.speed = 7f;
                return;
            }
            ThisEnemy.LogMessage("Noise does not have any loudness!");
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (ThisEnemy.LatestNoise.Location != CurrentNoise.Location) {
                CurrentNoise = ThisEnemy.LatestNoise;

                if (ThisEnemy.agent.isOnNavMesh) {
                    RangeOfTargetNoise = ThisEnemy.ChooseClosestNodeToPosition(CurrentNoise.Location);
                    OnRouteToNextPoint = ThisEnemy.SetDestinationToPosition(RangeOfTargetNoise.position, true);
                }
                ThisEnemy.agent.speed = 7.5f;
            }
            if (!OnRouteToNextPoint && ThisEnemy.agent.isOnNavMesh) {
                ThisEnemy.LogMessage("Noise was unreachable!");
                if (ThisEnemy.LatestNoise.Loudness != -1) {
                    NextPoint = ThisEnemy.LatestNoise.Location;
                } else {
                    NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(ThisEnemy.allAINodes[MyRandomInt].transform.position, 15);
                    WTOBase.LogToConsole("Gallenarma off to random point!");
                }
                if (ThisEnemy.agent.isOnNavMesh) {
                    OnRouteToNextPoint = ThisEnemy.SetDestinationToPosition(ThisEnemy.ChooseClosestNodeToPosition(NextPoint).position);
                }
            }
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", false);
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
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.LatestNoise.Invalidate();
            ThisEnemy.creatureVoice.PlayOneShot(ThisEnemy.Search[ThisEnemy.enemyRandom.Next(0, ThisEnemy.Search.Count)]);
            ThisEnemy.TotalInvestigateSeconds = MyRandomInt;
            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", true);
            ThisEnemy.agent.speed = 0f;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (ThisEnemy.TotalInvestigateSeconds < 0.8) {
                ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
            }

            if (ThisEnemy.TotalInvestigateSeconds > 0) {
                ThisEnemy.LowerTimerValue(ref ThisEnemy.TotalInvestigateSeconds);
                return;
            }
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundEnemy(),
            new NothingFoundAtNoise(),
            new Boombox(),
            new HeardNoise()
        };
    }
    private class Attack : BehaviorState {
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.creatureVoice.clip = ThisEnemy.Growl;
            ThisEnemy.creatureVoice.Play();
            ThisEnemy.creatureVoice.loop = true;
            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Attack", true);
            ThisEnemy.HasAttackedThisCycle = false;
            ThisEnemy.AttackTimerSeconds = 1.667f;
            if (ThisEnemy.agent.isOnNavMesh) {
                ThisEnemy.SetMovingTowardsTargetPlayer(ThisEnemy.targetPlayer);
            }
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
            if (ThisEnemy.DistanceFromPlayer(ThisEnemy.targetPlayer) > ThisEnemy.AttackRange) {
                ThisEnemy.agent.speed = 7f;
            } else {
                ThisEnemy.agent.speed = 2f;
            }
            ThisEnemy.TryMeleeAttackPlayer(120);
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimBoolOnServerRpc("Attack", false);
            ThisEnemy.HasAttackedThisCycle = false;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new KilledVictim(),
            new VictimEscaped()
        };
    }
    private class Chase : BehaviorState {
        public override void OnStateEntered(WTOEnemy enemyInstance) {

            ThisEnemy.SetAnimBoolOnServerRpc("Investigating", false);
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", true);
            if (ThisEnemy.agent.isOnNavMesh) {
                ThisEnemy.SetMovingTowardsTargetPlayer(ThisEnemy.targetPlayer);
            }
            ThisEnemy.agent.speed = 7f;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
                
                
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.creatureVoice.loop = false;
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new ReachedChasedEnemy(),
            new LostTrackOfPlayer()
        };
    }
    private class Dance : BehaviorState {
        private bool MovingToBoombox;
        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.SecondsUntilBored = 150;
            if (ThisEnemy.agent.isOnNavMesh) {
                ThisEnemy.SetDestinationToPosition(ThisEnemy.myBoomboxPos);
            }
            ThisEnemy.SetAnimBoolOnServerRpc("Moving", true);
            ThisEnemy.agent.speed = 5f;
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            if (Vector3.Distance(ThisEnemy.myBoomboxPos, ThisEnemy.transform.position) < 5) {
                ThisEnemy.SetAnimBoolOnServerRpc("Dancing", true);
                ThisEnemy.agent.speed = 0f;
                ThisEnemy.IsDancing = true;
                ThisEnemy.LowerTimerValue(ref ThisEnemy.SecondsUntilBored);
                return;
            }
            //if we're not near our boombox, move to it
            if (!MovingToBoombox && ThisEnemy.agent.isOnNavMesh) {
                ThisEnemy.SetDestinationToPosition(ThisEnemy.myBoomboxPos);
                MovingToBoombox = true;
            }
                
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimBoolOnServerRpc("Dancing", false);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new BoredOfDancing()
        };
    }
    private class Enraged : BehaviorState {

        public override void OnStateEntered(WTOEnemy enemyInstance) {
            ThisEnemy.SetAnimTriggerOnServerRpc("Stunned");
            ThisEnemy.agent.speed = 0f;
            //ThisEnemy.targetPlayer = ThisEnemy.stunnedByPlayer;
            ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
            //ThisEnemy.LatestNoise = new NoiseInfo(ThisEnemy.stunnedByPlayer.transform.position, 5);
            ThisEnemy.StunTimeSeconds = 3.15f;
            ThisEnemy.HasBeenEnragedThisCycle = true;
            ThisEnemy.creatureVoice.clip = ThisEnemy.Growl;
            ThisEnemy.creatureVoice.Play();
            ThisEnemy.creatureVoice.loop = true;
            ThisEnemy.creatureSFX.clip = ThisEnemy.GallenarmaBeatChest;
            ThisEnemy.creatureSFX.Play();
            ThisEnemy.targetPlayer.JumpToFearLevel(1f);
        }
        public override void UpdateBehavior(WTOEnemy enemyInstance) {
            ThisEnemy.LowerTimerValue(ref ThisEnemy.StunTimeSeconds);
        }
        public override void OnStateExit(WTOEnemy enemyInstance) {
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new EnrageAnimFinished()
        };
    }

    //STATE TRANSITIONS
    private class HeardNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (ThisEnemy.LatestNoise.Loudness != -1);
        }
        public override BehaviorState NextState() {
            if (ThisEnemy.asleep) { 
                return new WakingUp();
            }
            return new GoToNoise();
        }
    }
    private class WakeTimerDone : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.SecondsUntilChainsBroken > 0) {
                ThisEnemy.SetAnimBoolOnServerRpc("Breaking", ThisEnemy.SecondsUntilChainsBroken <= 2);
                return false;
            }
            return ThisEnemy.AnimationIsFinished("Gallenarma Break");
        }
        public override BehaviorState NextState() {
            //this is silly but fuck it
            ThisEnemy.asleep = false;
            ThisEnemy.LatestNoise.Loudness = -1;
            return new Patrol();
        }
        private bool IsEnemyInRange() {
            PlayerControllerB nearestPlayer = ThisEnemy.CheckLineOfSightForClosestPlayer(45f, 20, 2);
            return nearestPlayer == null;
        }
    }
    private class NothingFoundAtNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return ThisEnemy.TotalInvestigateSeconds <= 0;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class FoundEnemy : StateTransition {
        public override bool CanTransitionBeTaken() {
            /*
            PlayerControllerB PotentialTargetPlayer = ThisEnemy.CheckLineOfSightForClosestPlayer(180f, 7);
            if (PotentialTargetPlayer == null) {
                return false;
            }
            ThisEnemy.targetPlayer = PotentialTargetPlayer;
            ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
            return true;
            */
            PlayerControllerB PlayerClosestToGallenama = ThisEnemy.GetClosestPlayerToPosition(ThisEnemy.transform.position);
            if (Vector3.Distance(ThisEnemy.transform.position, PlayerClosestToGallenama.transform.position) < 3) {
                ThisEnemy.targetPlayer = PlayerClosestToGallenama;
                ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
                return true;
            }
            if (ThisEnemy.LatestNoise.Loudness == -1) {
                return false;
            } else {
                PlayerControllerB PlayerClosestToLastNoise = ThisEnemy.GetClosestPlayerToPosition(ThisEnemy.transform.position);
                
                if (PlayerClosestToLastNoise == null || Vector3.Distance(ThisEnemy.transform.position, PlayerClosestToLastNoise.transform.position) > 7) {
                    return false;
                }
                ThisEnemy.targetPlayer = PlayerClosestToLastNoise;
                ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
                return true;
            }
        }
        public override BehaviorState NextState() {
                return new Enraged();
        }
    }
    private class ReachedChasedEnemy : StateTransition {
        public override bool CanTransitionBeTaken() {
            PlayerControllerB PotentialTargetPlayer = ThisEnemy.CheckLineOfSightForClosestPlayer(180f, 7);
            if(PotentialTargetPlayer == null) {
                return false;
            }

            if (Vector3.Distance(PotentialTargetPlayer.transform.position, ThisEnemy.transform.position) < 4) {
                ThisEnemy.targetPlayer = PotentialTargetPlayer;
                ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
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
            PlayerControllerB PotentialTargetPlayer = ThisEnemy.CheckLineOfSightForClosestPlayer(180f, 7);
            if (PotentialTargetPlayer == null) {
                return false;
            }

            if (Vector3.Distance(PotentialTargetPlayer.transform.position, ThisEnemy.transform.position) < 7) {
                ThisEnemy.targetPlayer = PotentialTargetPlayer;
                ThisEnemy.ChangeOwnershipOfEnemy(ThisEnemy.targetPlayer.actualClientId);
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
            if(ThisEnemy.targetPlayer != null) { 
                return ThisEnemy.targetPlayer.health <= 0;
            }
            return false;
        }
        public override BehaviorState NextState() {
            ThisEnemy.creatureVoice.Stop();
            ThisEnemy.creatureVoice.loop = false;
            ThisEnemy.targetPlayer = null;
            return new Patrol();
        }
    }
    private class Boombox : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (ThisEnemy.TameTimerSeconds > 0);
        }
        public override BehaviorState NextState() {
            return new Dance();
        }
    }
    private class BoredOfDancing : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.IsDancing) {
                return ThisEnemy.SecondsUntilBored <= 0;
            }                    
            return ThisEnemy.TameTimerSeconds <= 0;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class VictimEscaped : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (ThisEnemy.targetPlayer == null) {
                return true;
            }
            if (Vector3.Distance(ThisEnemy.targetPlayer.transform.position, ThisEnemy.transform.position) > ThisEnemy.AttackRange + 2.5) {
                ThisEnemy.AttackTimerSeconds = 0;
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
            if (ThisEnemy.targetPlayer == null || !ThisEnemy.PlayerCanBeTargeted(ThisEnemy.targetPlayer)){
                ThisEnemy.targetPlayer = null;
                return true;
            }
            float PlayerDistanceFromGallenarma = Vector3.Distance(ThisEnemy.transform.position, ThisEnemy.targetPlayer.transform.position);
            float playerDistanceFromLastNoise = Vector3.Distance(ThisEnemy.LatestNoise.Location, ThisEnemy.targetPlayer.transform.position);
            float DistanceToCheck = Math.Min(playerDistanceFromLastNoise, PlayerDistanceFromGallenarma);
                
            return DistanceToCheck > 15;
        }
        public override BehaviorState NextState() {
            return new Patrol();
        }
    }
    private class EnrageAnimFinished : StateTransition {
        public override bool CanTransitionBeTaken() {
            return ThisEnemy.StunTimeSeconds < 0.1;
        }
        public override BehaviorState NextState() {
            return new Chase();
        }
    }
    private class ReachedNoise : StateTransition {
        public override bool CanTransitionBeTaken() {
            return (Vector3.Distance(ThisEnemy.transform.position, ThisEnemy.destination) < 3);
        }
        public override BehaviorState NextState() {
            return new Investigate();
        }
    }
    private class HitByStunGun : StateTransition {
        public override bool CanTransitionBeTaken() {
            return ThisEnemy.stunNormalizedTimer > 0 && !(ThisEnemy.ActiveState is Chase);
        }
        public override BehaviorState NextState() {
            return new Enraged();
        }
    }

    public float TotalInvestigateSeconds;
    public float RandomAwakeTimerSeconds = 60f;
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
    private AISearchRoutine RoamLab = new AISearchRoutine();
    private bool HasBeenEnragedThisCycle;
    public BoxCollider GallenarmaHitbox;
    public AudioClip Growl;
    public AudioClip GallenarmaScream;
    public AudioClip GallenarmaBeatChest;
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
        //WTOEnemyID = GallenarmaID;
        LogMessage($"Adding Gallenarma {this} at {GallenarmaID}");
        GallenarmaList.Add(GallenarmaID, this);
        GlobalTransitions.Add(new HitByStunGun());
        base.Start();
        if (enemyRandom.Next(1, 10) > 8) {
            LatestNoise = new NoiseInfo(transform.position, 2);
        }
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
            GallenarmaHitbox.enabled = false;
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
        if (!(ActiveState is Attack || ActiveState is Chase) || ActiveState is Enraged) {
            OverrideState(new Enraged());
        }
    }
    public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0) {
        base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
        if (noiseID == 5) {
            TameTimerSeconds = 2;
            myBoomboxPos = noisePosition;
        } else if (stunNormalizedTimer > 0f || noiseID == 7 || noiseID == 546 || hearNoiseCooldown > 0f || timesNoisePlayedInOneSpot > 15 || isEnemyDead) {
            return;
        }
        if (TotalInvestigateSeconds > 0) {
            noiseLoudness *= 3f;
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

        if (noiseLoudness < 0.1f) {
            return;
        }
        if (!(ActiveState is Attack || ActiveState is Chase )) { 
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
       
        if(enemyRandom.Next(0, 100) < 50) {
            LatestNoise = new NoiseInfo(transform.position, 2);
        }
        RandomAwakeTimerSeconds = 60f;
    }
    public PlayerControllerB GetClosestPlayerToPosition(Vector3 position) {
        if(position == new Vector3(-1, -1, -1)) {
            return null;
        }
        PlayerControllerB NearestPlayer = null;
        float CurrentNearestPlayerDistance = 20000;
        float NextPlayerDistance;
        foreach(PlayerControllerB NextPlayer in StartOfRound.Instance.allPlayerScripts) {
            NextPlayerDistance = Vector3.Distance(position, NextPlayer.transform.position);
            if (NextPlayerDistance < CurrentNearestPlayerDistance) {
                NearestPlayer = NextPlayer;
                CurrentNearestPlayerDistance = NextPlayerDistance;
            }
        }
        return NearestPlayer;
    }
}
