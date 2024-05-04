using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class BearTrap : MonoBehaviour {

    public int DamageAmount = 5;
    private float TimeSincePlayerDamaged = 0f;

    private float SecondsUntilNextRiseAttempt = 400;
    
    public Animator BearTrapAnim;
    //public float BearTrapStartPos;
    //public float BearTrapEndPos;
    public bool IsBearTrapRaised = false;
    public System.Random BearTrapRandom;
    public bool IsBearTrapClosed;
    private List<PlayerControllerB> PlayerInRangeList = new();
    private int BearTrapRiseChance = 70;


    public void OnTriggerEnter(Collider other) {
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (!PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null) {
                WTOBase.LogToConsole($"Bear Trap: Adding Player {PlayerInRange} to player in range list...");
                PlayerInRangeList.Add(PlayerInRange);
                SetBearTrapOpenState(false);
            }
        } catch { }
    }
    public void OnTriggerExit(Collider other) {
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null) {
                WTOBase.LogToConsole($"Bear Trap: Removing Player {PlayerInRange} from player in range list...");
                PlayerInRange.movementSpeed = 4.6f;
                PlayerInRange.jumpForce = 13;
                PlayerInRangeList.Remove(PlayerInRange);
                MoveBearTrap(false);
                SetBearTrapOpenState(true);
            }
        } catch { }
    }

    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (!other.gameObject.CompareTag("Player")) {
            return;
        }
        if (IsBearTrapClosed) {
            AcidWater.DamageOverlappingPlayer(victim, 1f, ref TimeSincePlayerDamaged, 10);
            if (victim.health > 1) {
                victim.movementSpeed = 0.4f;
                victim.jumpForce = 1;
            } else {
                victim.jumpForce = 13;
                victim.movementSpeed = 4.6f;
            }
        } 
    }

    private void Start() {
        BearTrapRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }

    private void Update() {
        if (IsBearTrapRaised) {
            return;
        }
        if(SecondsUntilNextRiseAttempt > 0) {
            SecondsUntilNextRiseAttempt -= Time.deltaTime;
        } else { 
            TryBearTrapRise();
        }
    }

    private void TryBearTrapRise() {
        if (BearTrapRandom.Next(0, 100) > 70) {
            MoveBearTrapServerRpc(true);
            return;
        }
        SecondsUntilNextRiseAttempt = BearTrapRandom.Next(35, 80);
        if(BearTrapRiseChance > 0) {
            BearTrapRiseChance -= 10;
        }
    }

    [ServerRpc]
    public void MoveBearTrapServerRpc(bool ShouldRaise) {
        MoveBearTrap(ShouldRaise);
    }

    private void MoveBearTrap(bool ShouldRaise) {
        IsBearTrapRaised = ShouldRaise;
        if (ShouldRaise) {
            //play animation for bear trap rising
            BearTrapAnim.SetBool("IsRaised", true);
        } else {
            //play animation for bear trap descending
            BearTrapAnim.SetBool("IsRaised", false);
            SecondsUntilNextRiseAttempt = BearTrapRandom.Next(35, 80);
        }
    }
    private void SetBearTrapOpenState(bool isOpen) {
        if (isOpen) {
            BearTrapAnim.SetBool("CloseTrap", false);
            IsBearTrapClosed = false;
            return;
        }
        BearTrapAnim.SetBool("CloseTrap", true);
        IsBearTrapClosed = true;

    }

}
