using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies;
public class WTOEnemy : EnemyAI
{
    public abstract class BehaviorState
    {
        public Vector2 RandomRange = new(0, 0);
        public int MyRandomInt = 0;
        public int enemyIndex;
        public abstract void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);
        public abstract void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);
        public abstract void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator);

        public virtual List<StateTransition> transitions { get; set; }
    }
    public abstract class StateTransition
    {
        //public int enemyIndex { get; set; }
        public int enemyIndex;
        public abstract bool CanTransitionBeTaken();
        public abstract BehaviorState NextState();
    }
    public enum PlayerState
    {
        Dead,
        Outside,
        Inside,
        Ship
    }
    internal BehaviorState InitialState { get; set; }
    internal BehaviorState ActiveState = null;
    internal System.Random enemyRandom;
    internal int AITimer;
    internal RoundManager roundManager;
    internal bool PrintDebugs = false;
    internal PlayerState MyValidState = PlayerState.Inside;
    internal StateTransition nextTransition;
    internal List<StateTransition> GlobalTransitions = [];
    internal List<StateTransition> AllTransitions = [];
    internal int WTOEnemyID;

    public override string __getTypeName()
    {
        return GetType().Name;
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        _ = StartOfRound.Instance.livingPlayers;
    }

    public override void Start()
    {
        base.Start();

        //Initializers
        ActiveState = InitialState;
        roundManager = FindObjectOfType<RoundManager>();
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
        if (enemyType.isOutsideEnemy)
        {
            MyValidState = PlayerState.Outside;
        }
        else
        {
            MyValidState = PlayerState.Inside;
        }
        //Debug to make sure that the agent is actually on the navmesh
        if (!agent.isOnNavMesh && base.IsOwner)
        {
            WTOBase.LogToConsole("CREATURE " + this.__getTypeName() + " WAS NOT PLACED ON NAVMESH, DESTROYING...");
            KillEnemyOnOwnerClient();
        }
        //Fix for the animator sometimes deciding to just not work
        creatureAnimator.Rebind();
        ActiveState.enemyIndex = WTOEnemyID;
        ActiveState.OnStateEntered(WTOEnemyID, enemyRandom, creatureAnimator);

    }

    public override void Update()
    {
        if (isEnemyDead)
        {
            return;
        }
        base.Update();
        AITimer++;
        //don't run enemy ai if they're dead

        bool RunUpdate = true;

        //Reset transition list to match all those in our current state, along with any global transitions that exist regardless of state (stunned, mostly)
        AllTransitions.Clear();
        AllTransitions.AddRange(GlobalTransitions);
        AllTransitions.AddRange(ActiveState.transitions);

        foreach (StateTransition TransitionToCheck in AllTransitions)
        {
            TransitionToCheck.enemyIndex = WTOEnemyID;
            if (TransitionToCheck.CanTransitionBeTaken() && base.IsOwner)
            {
                RunUpdate = false;
                nextTransition = TransitionToCheck;
                TransitionStateServerRpc(nextTransition.ToString(), GenerateNextRandomInt());
                return;
            }
        }

        if (RunUpdate)
        {
            ActiveState.UpdateBehavior(WTOEnemyID, enemyRandom, creatureAnimator);
        }
    }

    internal void LogMessage(string message)
    {
        if (PrintDebugs && MonsterPatch.ShouldDebugEnemies)
        {
            WTOBase.LogToConsole(message);
        }
    }
    internal bool PlayerCanBeTargeted(PlayerControllerB myPlayer)
    {
        return (GetPlayerState(myPlayer) == MyValidState);
    }
    internal PlayerState GetPlayerState(PlayerControllerB myPlayer)
    {
        if (myPlayer.isPlayerDead)
        {
            return PlayerState.Dead;
        }
        if (myPlayer.isInsideFactory)
        {
            return PlayerState.Inside;
        }
        if (myPlayer.isInHangarShipRoom)
        {
            return PlayerState.Ship;
        }
        return PlayerState.Outside;
    }
    internal void MoveTimerValue(ref float Timer, bool ShouldRaise = false)
    {
        if (ShouldRaise)
        {
            Timer += Time.deltaTime;
            return;
        }
        if (Timer <= 0)
        {
            return;
        }
        Timer -= Time.deltaTime;
    }
    internal void OverrideState(BehaviorState state)
    {
        if (isEnemyDead)
        {
            return;
        }
        ActiveState = state;
        ActiveState.OnStateEntered(WTOEnemyID, enemyRandom, creatureAnimator);
        return;
    }
    public PlayerControllerB IsAnyPlayerWithinLOS(int range = 45, float width = 60, int proximityAwareness = -1, bool DoLinecast = true, bool PrintResults = false, bool SortByDistance = false)
    {
        float ShortestDistance = range;
        float NextDistance;
        PlayerControllerB ClosestPlayer = null;
        foreach (PlayerControllerB Player in StartOfRound.Instance.allPlayerScripts)
        {
            if (Player.isPlayerDead || !Player.isPlayerControlled)
            {
                continue;
            }
            if (IsTargetPlayerWithinLOS(Player, range, width, proximityAwareness, DoLinecast, PrintResults))
            {
                if (!SortByDistance)
                {
                    return Player;
                }
                NextDistance = Vector3.Distance(Player.transform.position, this.transform.position);
                if (NextDistance < ShortestDistance)
                {
                    ShortestDistance = NextDistance;
                    ClosestPlayer = Player;
                }
            }
        }
        return ClosestPlayer;
    }
    public bool IsTargetPlayerWithinLOS(PlayerControllerB player, int range = 45, float width = 60, int proximityAwareness = -1, bool DoLinecast = true, bool PrintResults = false)
    {
        float DistanceToTarget = Vector3.Distance(transform.position, player.gameplayCamera.transform.position);
        bool TargetInDistance = DistanceToTarget < (float)range;
        float AngleToTarget = Vector3.Angle(eye.transform.forward, player.gameplayCamera.transform.position - eye.transform.position);
        bool TargetWithinViewCone = AngleToTarget < width;
        bool TargetWithinProxAwareness = DistanceToTarget < proximityAwareness;
        bool LOSBlocked = (DoLinecast && Physics.Linecast(eye.transform.position, player.transform.position, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore));
        if (PrintResults)
        {
            LogMessage($"Target in Distance: {TargetInDistance} ({DistanceToTarget})" +
                $"Target within view cone: {TargetWithinViewCone} ({AngleToTarget})" +
                $"LOSBlocked: {LOSBlocked}");
        }
        return (TargetInDistance && TargetWithinViewCone) || TargetWithinProxAwareness && !LOSBlocked;
    }
    public bool IsTargetPlayerWithinLOS(int range = 45, float width = 60, int proximityAwareness = -1, bool DoLinecast = true, bool PrintResults = false)
    {
        if (targetPlayer == null)
        {
            LogMessage($"{this.__getTypeName()} called Target Player LOS check called with null target player; returning false!");
            return false;
        }
        return IsTargetPlayerWithinLOS(targetPlayer, range, width, proximityAwareness, DoLinecast, PrintResults);
    }
    public PlayerControllerB FindNearestPlayer(bool ValidateNav = false)
    {
        PlayerControllerB Result = null;
        float BestDistance = 20000;
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            PlayerControllerB NextPlayer = StartOfRound.Instance.allPlayerScripts[i];
            if (ValidateNav && !agent.CalculatePath(NextPlayer.transform.position, path1))
            {
                continue;
            }
            float PlayerToMonster = Vector3.Distance(this.transform.position, NextPlayer.transform.position);
            if (PlayerToMonster < BestDistance)
            {
                Result = NextPlayer;
                BestDistance = PlayerToMonster;
            }
        }

        if (Result == null)
        {
            LogMessage($"There is somehow no closest player. get fucked");
        }
        return Result;
    }

    internal bool IsPlayerReachable()
    {
        if (targetPlayer == null)
        {
            WTOBase.WTOLogSource.LogError("Player Reach Test has no target player or passed in argument!");
            return false;
        }
        Vector3 Position = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
        if (!RoundManager.Instance.GotNavMeshPositionResult)
        {
            LogMessage("Player Reach Test: No Navmesh position");
            return false;
        }
        agent.CalculatePath(Position, agent.path);
        bool HasPath = (agent.path.status == NavMeshPathStatus.PathComplete);
        LogMessage($"Player Reach Test: {HasPath}");
        return HasPath;
    }

    internal float PlayerDistanceFromShip()
    {
        if (targetPlayer == null)
        {
            WTOBase.WTOLogSource.LogError("PlayerNearShip check has no target player or passed in argument!");
            return -1;
        }
        float DistanceFromShip = Vector3.Distance(targetPlayer.transform.position, StartOfRound.Instance.shipBounds.transform.position);
        LogMessage($"PlayerNearShip check: {DistanceFromShip}");
        return DistanceFromShip;
    }

    internal bool PlayerWithinRange(float Range, bool IncludeYAxis = true)
    {
        //WTOBase.LogToConsole($"Distance from target player: {DistanceFromTargetPlayer(IncludeYAxis)}");
        return DistanceFromTargetPlayer(IncludeYAxis) <= Range;
    }
    internal bool PlayerWithinRange(PlayerControllerB player, float Range, bool IncludeYAxis = true)
    {
        return DistanceFromTargetPlayer(player, IncludeYAxis) <= Range;
    }
    private float DistanceFromTargetPlayer(bool IncludeYAxis)
    {
        if (targetPlayer == null)
        {
            WTOBase.WTOLogSource.LogError($"{this} attempted DistanceFromTargetPlayer with null target; returning -1!");
            return -1f;
        }
        if (IncludeYAxis)
        {
            return Vector3.Distance(targetPlayer.transform.position, this.transform.position);
        }
        Vector2 PlayerFlatLocation = new(targetPlayer.transform.position.x, targetPlayer.transform.position.z);
        Vector2 EnemyFlatLocation = new(transform.position.x, transform.position.z);
        return Vector2.Distance(PlayerFlatLocation, EnemyFlatLocation);
    }
    private float DistanceFromTargetPlayer(PlayerControllerB player, bool IncludeYAxis)
    {
        if (IncludeYAxis)
        {
            return Vector3.Distance(player.transform.position, this.transform.position);
        }
        Vector2 PlayerFlatLocation = new(targetPlayer.transform.position.x, targetPlayer.transform.position.z);
        Vector2 EnemyFlatLocation = new(transform.position.x, transform.position.z);
        return Vector2.Distance(PlayerFlatLocation, EnemyFlatLocation);
    }
    internal bool AnimationIsFinished(string AnimName)
    {
        if (!creatureAnimator.GetCurrentAnimatorStateInfo(0).IsName(AnimName))
        {
            LogMessage(__getTypeName() + ": Checking for animation " + AnimName + ", but current animation is " + creatureAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name);
            return true;
        }
        return (creatureAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
    }
    internal int GenerateNextRandomInt()
    {
        Vector2 Range = nextTransition.NextState().RandomRange;
        return enemyRandom.Next((int)Range.x, (int)Range.y);
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SetAnimTriggerOnServerRpc(string name)
    {
        if (IsServer)
        {
            creatureAnimator.SetTrigger(name);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    internal void SetAnimBoolOnServerRpc(string name, bool state)
    {
        if (IsServer)
        {
            creatureAnimator.SetBool(name, state);
        }
    }

    [ServerRpc]
    internal void TransitionStateServerRpc(string StateName, int RandomInt)
    {
        TransitionStateClientRpc(StateName, RandomInt);
    }
    [ClientRpc]
    internal void TransitionStateClientRpc(string StateName, int RandomInt)
    {
        TransitionState(StateName, RandomInt);
    }
    internal void TransitionState(string StateName, int RandomInt)
    {
        //Jesus fuck I can't believe I have to do this
        Type type = Type.GetType(StateName);
        StateTransition LocalNextTransition = (StateTransition)Activator.CreateInstance(type);
        LocalNextTransition.enemyIndex = WTOEnemyID;
        if (ActiveState != null && LocalNextTransition.NextState().GetType() == ActiveState.GetType())
        {
            return;
        }
        //LogMessage(StateName);
        var statename = ActiveState == null ? "null" : $"{ActiveState}";
        LogMessage($"{__getTypeName()} #{WTOEnemyID} is Exiting:  {statename}");
        ActiveState?.OnStateExit(WTOEnemyID, enemyRandom, creatureAnimator);
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

    [ServerRpc]
    internal void SetTargetServerRpc(int PlayerID)
    {
        SetTargetClientRpc(PlayerID);
    }
    [ClientRpc]
    internal void SetTargetClientRpc(int PlayerID)
    {
        if (PlayerID == -1)
        {
            targetPlayer = null;
            LogMessage($"Clearing target on {this}");
            return;
        }
        if (StartOfRound.Instance.allPlayerScripts[PlayerID] == null)
        {
            LogMessage($"Index invalid! {this}");
            return;
        }
        targetPlayer = StartOfRound.Instance.allPlayerScripts[PlayerID];
        LogMessage($"{this} setting target to: {targetPlayer.playerUsername}");
    }
}
