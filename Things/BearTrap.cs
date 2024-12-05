using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class BearTrap : MonoBehaviour, IHittable
{

    public int DamageAmount = 5;
    private float TimeSincePlayerDamaged = 0.5f;

    private float SecondsUntilNextRiseAttempt = 200;

    public Animator BearTrapAnim;
    //public float BearTrapStartPos;
    //public float BearTrapEndPos;
    public bool IsBearTrapRaised = false;
    public System.Random BearTrapRandom;
    public bool IsBearTrapClosed;
    private readonly List<PlayerControllerB> PlayerInRangeList = [];
    private int BearTrapRiseChance = 70;
    public AudioClip CloseSound;

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (!IsBearTrapClosed)
        {
            SetBearTrapOpenState(false);
        }
        if (IsBearTrapRaised)
        {
            MoveBearTrap(false);
        }
        return true;
    }

    public void OnTriggerEnter(Collider other)
    {
        try
        {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (!PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null)
            {
                WTOBase.LogToConsole($"Bear Trap: Adding Player {PlayerInRange} to player in range list...");
                PlayerInRangeList.Add(PlayerInRange);
                SetBearTrapOpenState(false);
                GetComponent<AudioSource>().PlayOneShot(CloseSound);
            }
        }
        catch { }
    }
    public void OnTriggerExit(Collider other)
    {
        try
        {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null)
            {
                WTOBase.LogToConsole($"Bear Trap: Removing Player {PlayerInRange} from player in range list...");
                PlayerInRange.movementSpeed = 4.6f;
                PlayerInRange.jumpForce = 13;
                PlayerInRangeList.Remove(PlayerInRange);
                MoveBearTrap(false);
                SetBearTrapOpenState(true);
            }
        }
        catch { }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.gameObject.CompareTag("Player"))
        {
            return;
        }
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (IsBearTrapClosed)
        {
            AcidWater.DamageOverlappingPlayer(victim, 0.5f, ref TimeSincePlayerDamaged, 5);
            if (victim.health > 1)
            {
                victim.movementSpeed = 0.4f;
                victim.jumpForce = 1;
            }
            else
            {
                victim.jumpForce = 13;
                victim.movementSpeed = 4.6f;
            }
        }
    }

    private void Start()
    {
        BearTrapRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }

    private void Update()
    {
        if (IsBearTrapRaised)
        {
            return;
        }
        if (SecondsUntilNextRiseAttempt > 0)
        {
            SecondsUntilNextRiseAttempt -= Time.deltaTime;
        }
        else
        {
            TryBearTrapRise();
        }
    }

    private void TryBearTrapRise()
    {
        if (BearTrapRandom.Next(0, 100) > 70)
        {
            MoveBearTrapServerRpc(true);
            return;
        }
        SecondsUntilNextRiseAttempt = BearTrapRandom.Next(15, 50);
        if (BearTrapRiseChance > 0)
        {
            BearTrapRiseChance -= 10;
        }
    }

    [ServerRpc]
    public void MoveBearTrapServerRpc(bool ShouldRaise)
    {
        MoveBearTrap(ShouldRaise);
    }

    private void MoveBearTrap(bool ShouldRaise)
    {
        IsBearTrapRaised = ShouldRaise;
        if (ShouldRaise)
        {
            SetBearTrapOpenState(true);
            BearTrapAnim.SetBool("IsRaised", true);
        }
        else
        {
            //play animation for bear trap descending
            BearTrapAnim.SetBool("IsRaised", false);
            SecondsUntilNextRiseAttempt = BearTrapRandom.Next(15, 50);
        }
    }
    private void SetBearTrapOpenState(bool isOpen)
    {
        if (isOpen)
        {
            BearTrapAnim.SetBool("CloseTrap", false);
            IsBearTrapClosed = false;
            return;
        }
        BearTrapAnim.SetBool("CloseTrap", true);
        IsBearTrapClosed = true;

    }

}
