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
        public override List<StateTransition> transitions { get; set; } = [
            new HeardNoise()
        ];
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
            GallenarmaList[enemyIndex].MoveTimerValue(ref GallenarmaList[enemyIndex].SecondsUntilChainsBroken);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = [
            new WakeTimerDone()
        ];
    }
    private class Patrol : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].Awakening = false;
            GallenarmaList[enemyIndex].creatureVoice.clip = null;
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Breaking", true);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Attack", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            GallenarmaList[enemyIndex].agent.speed = 5f;
            GallenarmaList[enemyIndex].StartSearch(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].RoamLab);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (!GallenarmaList[enemyIndex].RoamLab.inProgress) {
                GallenarmaList[enemyIndex].StartSearch(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].RoamLab);
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].StopSearch(GallenarmaList[enemyIndex].RoamLab, clear: false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = [
            new HeardNoise(),
            new Boombox()
        ];
    }
    private class GoToNoise : BehaviorState {
        bool OnRouteToNextPoint;
        Vector3 RangeOfTargetNoise;
        Vector3 TargetNoisePosition;
        NoiseInfo CurrentNoise;
        Vector3 NextPoint;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //pass the latest noise to our gallenarma and tell it to go there
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            if (GallenarmaList[enemyIndex].LatestNoise.Loudness != -1) {

                CurrentNoise = GallenarmaList[enemyIndex].LatestNoise;
                if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                    TargetNoisePosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].LatestNoise.Location, 5);
                    OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(TargetNoisePosition, true);
                }
                GallenarmaList[enemyIndex].agent.speed = 7f;
                return;
            }
            GallenarmaList[enemyIndex].LogMessage("Noise does not have any loudness!");
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (GallenarmaList[enemyIndex].LatestNoise.Location != CurrentNoise.Location) {
                CurrentNoise = GallenarmaList[enemyIndex].LatestNoise;

                if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                    RangeOfTargetNoise = RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].LatestNoise.Location, 2);
                    OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(RangeOfTargetNoise, true);
                }
                GallenarmaList[enemyIndex].agent.speed = 7.5f;
            }
            if (!OnRouteToNextPoint && GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                GallenarmaList[enemyIndex].LogMessage("Noise was unreachable!");
                if (GallenarmaList[enemyIndex].LatestNoise.Loudness != -1) {
                    NextPoint = GallenarmaList[enemyIndex].LatestNoise.Location;
                } else {
                    NextPoint = RoundManager.Instance.GetRandomNavMeshPositionInRadius(GallenarmaList[enemyIndex].allAINodes[MyRandomInt].transform.position, 15);
                    WTOBase.LogToConsole("Gallenarma off to random point!");
                }
                if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                    OnRouteToNextPoint = GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].ChooseClosestNodeToPosition(NextPoint).position);
                }
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
        }
        public override List<StateTransition> transitions { get; set; } = [
            new ReachedNoise(),
            new Boombox()
        ];
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
                GallenarmaList[enemyIndex].MoveTimerValue(ref GallenarmaList[enemyIndex].TotalInvestigateSeconds);
                return;
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
        }
        public override List<StateTransition> transitions { get; set; } = [
            new FoundEnemy(),
            new NothingFoundAtNoise(),
            new Boombox(),
            new HeardNoise()
        ];
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
            if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                GallenarmaList[enemyIndex].SetMovingTowardsTargetPlayer(GallenarmaList[enemyIndex].targetPlayer);
            }
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            if (!GallenarmaList[enemyIndex].PlayerWithinRange(GallenarmaList[enemyIndex].AttackRange, false)) {
                GallenarmaList[enemyIndex].agent.speed = 9.1f;
            } else { 
                GallenarmaList[enemyIndex].agent.speed = 2f;
            }
            GallenarmaList[enemyIndex].TryMeleeAttackPlayer(120);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Attack", false);
            GallenarmaList[enemyIndex].HasAttackedThisCycle = false;
        }
        public override List<StateTransition> transitions { get; set; } = [
            new KilledVictim(),
            new VictimEscaped()
        ];
    }
    private class Chase : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Investigating", false);
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                GallenarmaList[enemyIndex].SetMovingTowardsTargetPlayer(GallenarmaList[enemyIndex].targetPlayer);
            }
            GallenarmaList[enemyIndex].agent.speed = 9.1f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                
                
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].creatureVoice.loop = false;
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", false);
            
        }
        public override List<StateTransition> transitions { get; set; } = [
            new ReachedChasedEnemy(),
            new LostTrackOfPlayer()
        ];
    }
    private class Dance : BehaviorState {
        private bool MovingToBoombox;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SecondsUntilBored = 150;
            if (GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].myBoomboxPos);
            }
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Moving", true);
            GallenarmaList[enemyIndex].agent.speed = 5f;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (Vector3.Distance(GallenarmaList[enemyIndex].myBoomboxPos, GallenarmaList[enemyIndex].transform.position) < 5) {
                GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Dancing", true);
                GallenarmaList[enemyIndex].agent.speed = 0f;
                GallenarmaList[enemyIndex].IsDancing = true;
                GallenarmaList[enemyIndex].MoveTimerValue(ref GallenarmaList[enemyIndex].SecondsUntilBored);
                return;
            }
            //if we're not near our boombox, move to it
            if (!MovingToBoombox && GallenarmaList[enemyIndex].agent.isOnNavMesh) {
                GallenarmaList[enemyIndex].SetDestinationToPosition(GallenarmaList[enemyIndex].myBoomboxPos);
                MovingToBoombox = true;
            }
                
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimBoolOnServerRpc("Dancing", false);
        }
        public override List<StateTransition> transitions { get; set; } = [
            new BoredOfDancing()
        ];
    }
    private class Enraged : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].SetAnimTriggerOnServerRpc("Stunned");
            GallenarmaList[enemyIndex].agent.speed = 0f;
            GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
            GallenarmaList[enemyIndex].StunTimeSeconds = 3.15f;
            GallenarmaList[enemyIndex].creatureVoice.clip = GallenarmaList[enemyIndex].Growl;
            GallenarmaList[enemyIndex].creatureVoice.Play();
            GallenarmaList[enemyIndex].creatureVoice.loop = true;
            GallenarmaList[enemyIndex].creatureSFX.clip = GallenarmaList[enemyIndex].GallenarmaBeatChest;
            GallenarmaList[enemyIndex].creatureSFX.Play();
            WTOBase.LogToConsole($"Gallenarma: Setting fear effect on Player {GallenarmaList[enemyIndex].targetPlayer.playerUsername}!");
            if (GallenarmaList[enemyIndex].targetPlayer == GameNetworkManager.Instance.localPlayerController) {
                GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
            }
            
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GallenarmaList[enemyIndex].MoveTimerValue(ref GallenarmaList[enemyIndex].StunTimeSeconds);
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override List<StateTransition> transitions { get; set; } = [
            new EnrageAnimFinished()
        ];
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
            if (GallenarmaList[enemyIndex].ActiveState is Investigate) {
                //angers the gallenarma if it hears a noise while in its investigate state
                PlayerControllerB PlayerClosestToLastNoise = GallenarmaList[enemyIndex].GetClosestPlayerToPosition(GallenarmaList[enemyIndex].LatestNoise.Location);
                if (PlayerClosestToLastNoise == null || Vector3.Distance(GallenarmaList[enemyIndex].transform.position, PlayerClosestToLastNoise.transform.position) > 15) {
                    return new GoToNoise();
                }
                GallenarmaList[enemyIndex].SetTargetServerRpc((int)PlayerClosestToLastNoise.playerClientId);
                GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
                return new Enraged();
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
            //Angers the gallenarma if there's a player within 5 units of its investigation point
            PlayerControllerB PlayerClosestToGallenama = GallenarmaList[enemyIndex].GetClosestPlayerToPosition(GallenarmaList[enemyIndex].transform.position);
            if (Vector3.Distance(GallenarmaList[enemyIndex].transform.position, PlayerClosestToGallenama.transform.position) < 5) {
                GallenarmaList[enemyIndex].SetTargetServerRpc((int)PlayerClosestToGallenama.playerClientId);
                GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
                return true;
            }
            if (GallenarmaList[enemyIndex].LatestNoise.Loudness == -1) {
                return false;
            } else {
                //angers the gallenarma if a player is within 7 units of the last noise it heard
                PlayerControllerB PlayerClosestToLastNoise = GallenarmaList[enemyIndex].GetClosestPlayerToPosition(GallenarmaList[enemyIndex].transform.position);
                
                if (PlayerClosestToLastNoise == null || Vector3.Distance(GallenarmaList[enemyIndex].transform.position, PlayerClosestToLastNoise.transform.position) > 7) {
                    return false;
                }
                GallenarmaList[enemyIndex].SetTargetServerRpc((int)PlayerClosestToLastNoise.playerClientId);
                GallenarmaList[enemyIndex].ChangeOwnershipOfEnemy(GallenarmaList[enemyIndex].targetPlayer.actualClientId);
                return true;
            }
        }
        public override BehaviorState NextState() {
                return new Enraged();
        }
    }
    private class ReachedChasedEnemy : StateTransition {
        public override bool CanTransitionBeTaken() {
            PlayerControllerB PotentialTargetPlayer = GallenarmaList[enemyIndex].CheckLineOfSightForClosestPlayer(180f, 7);
            if(PotentialTargetPlayer == null) {
                return false;
            }

            if (Vector3.Distance(PotentialTargetPlayer.transform.position, GallenarmaList[enemyIndex].transform.position) < 4) {
                GallenarmaList[enemyIndex].SetTargetServerRpc((int)PotentialTargetPlayer.actualClientId);
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
                GallenarmaList[enemyIndex].SetTargetServerRpc((int)PotentialTargetPlayer.actualClientId);
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
            GallenarmaList[enemyIndex].SetTargetServerRpc(-1);
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
            if (!GallenarmaList[enemyIndex].IsPlayerReachable()) {
                return true;
            }
            if (!GallenarmaList[enemyIndex].PlayerWithinRange(GallenarmaList[enemyIndex].AttackRange + 1, false)) {
                GallenarmaList[enemyIndex].AttackTimerSeconds = 0;
                return true;
            }
            return false;
        }
        public override BehaviorState NextState() {
            if (!GallenarmaList[enemyIndex].IsPlayerReachable()) {
                return new Patrol();
            }
            return new Chase();
        }
    }
    private class LostTrackOfPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {
            if (GallenarmaList[enemyIndex].targetPlayer == null || !GallenarmaList[enemyIndex].PlayerCanBeTargeted(GallenarmaList[enemyIndex].targetPlayer) || !GallenarmaList[enemyIndex].IsPlayerReachable()){
                GallenarmaList[enemyIndex].SetTargetServerRpc(-1);
                return true;
            }
            float PlayerDistanceFromGallenarma = Vector3.Distance(GallenarmaList[enemyIndex].transform.position, GallenarmaList[enemyIndex].targetPlayer.transform.position);
            float playerDistanceFromLastNoise = Vector3.Distance(GallenarmaList[enemyIndex].LatestNoise.Location, GallenarmaList[enemyIndex].targetPlayer.transform.position);
            float DistanceToCheck = Math.Min(playerDistanceFromLastNoise, PlayerDistanceFromGallenarma);
                
            return DistanceToCheck > 15;
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
            return GallenarmaList[enemyIndex].stunNormalizedTimer > 0 && GallenarmaList[enemyIndex].ActiveState is not Chase;
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
    public static Dictionary<int, GallenarmaAI> GallenarmaList = [];
    public static int GallenarmaID;
    private readonly AISearchRoutine RoamLab = new();
    public BoxCollider GallenarmaHitbox;
    public CapsuleCollider GallenarmaCapsule;
    public AudioClip Growl;
    public AudioClip GallenarmaScream;
    public AudioClip GallenarmaBeatChest;
    public List<AudioClip> Search = [];
    private float FlinchTimerSeconds = 0f;

    private struct NoiseInfo(Vector3 position, float loudness)
    {
        public Vector3 Location { get; private set; } = position;
        public float Loudness { get; set; } = loudness;

        public void Invalidate() {
            Location = new Vector3(-1, -1, -1);
            Loudness = -1;
        }
    }
    private NoiseInfo LatestNoise = new(new Vector3(-1,-1,-1), -1);

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
        if (enemyRandom.Next(1, 10) > 8) {
            LatestNoise = new NoiseInfo(transform.position, 2);
        }
    }
    public override void Update() {
        MoveTimerValue(ref AttackTimerSeconds);
        MoveTimerValue(ref TameTimerSeconds);
        MoveTimerValue(ref FlinchTimerSeconds);
        if(TameTimerSeconds <= 0) {
            IsDancing = false;
        }
        MoveTimerValue(ref hearNoiseCooldown);
        MoveTimerValue(ref RandomAwakeTimerSeconds);
        base.Update();
    }

    //If we're attacked by a player, they need to be immediately set to our target player
    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        if (isEnemyDead) { return; }
        base.HitEnemy(force, playerWhoHit, playHitSFX);
        enemyHP -= force;
        if(FlinchTimerSeconds <= 0f) { 
            SetAnimTriggerOnServerRpc("Hit");
            FlinchTimerSeconds = 4f;
        }
        if (enemyHP <= 0) {
            SetAnimTriggerOnServerRpc("Killed");
            GameObject.Destroy(GallenarmaHitbox);
            GameObject.Destroy(GallenarmaCapsule);
            isEnemyDead = true;
            creatureVoice.Stop();
            if (base.IsOwner) {
                KillEnemyOnOwnerClient();

            }
            return;
        }

        //If we're attacked by a player, they need to be immediately set to our target player
        SetTargetServerRpc((int)playerWhoHit.playerClientId);
        ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
        if (!(ActiveState is Attack || ActiveState is Chase || ActiveState is Enraged)) {
            OverrideState(new Attack());
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
        WTOBase.LogToConsole($"Gallenarma '{gameObject.name}': Heard noise! Distance: {num} meters. Location: {noisePosition}");
        if (Physics.Linecast(base.transform.position, noisePosition, 256)) {
            noiseLoudness /= 2f;
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
            LogMessage("Gallenarma Attacking!");
            targetPlayer.DamagePlayer(damage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0, force: ((this.transform.position - targetPlayer.transform.position) * 40f));
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
