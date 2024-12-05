using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
internal class WideDoorway : NetworkBehaviour {

#pragma warning disable 0649 // Assigned in Unity Editor
    public BoxCollider OverlapTrigger;
    public Transform Doorway;
    public AudioClip CloseSound;
    public AudioSource CloseSoundSource;
#pragma warning restore 0649

    private const float TotalDistanceToTravel = -4;
    private float DistanceToTravelEachTime;
    private Vector3 FinalDoorPosition;

    //Lerp Stuff
    private float timeElapsed;
    private const float CloseTime = 0.2f;
    private Vector3 DoorStartPosition;
    private Vector3 CurrentDoorPosition;
    private Vector3 TargetDoorPosition;
    private readonly List<Collider> CurrentColliderList = [];
    private bool ShouldFall;

    private int TotalTimesBeforeClose;
    private System.Random MyRandom; 

    private void Start() {
        MyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        int PlayerCount = GameObject.FindGameObjectsWithTag("Player").Count();
        int MinTimesBeforeClose = (3 * PlayerCount - 1) + 6;
        int MaxTimesBeforeClose = MinTimesBeforeClose + 5;
        TotalTimesBeforeClose = MyRandom.Next(MinTimesBeforeClose, MaxTimesBeforeClose);
        DistanceToTravelEachTime = TotalDistanceToTravel / TotalTimesBeforeClose;
        FinalDoorPosition = Doorway.transform.position + new Vector3(0, TotalDistanceToTravel, 0);
        DoorStartPosition = Doorway.transform.position;
    }

    public void RaiseDoor() {
        Doorway.transform.position = DoorStartPosition;
        CloseSoundSource.Play();
    }

    private void Update() {
        if (!ShouldFall) {
            return;
        }
        StartCoroutine(CloseDoor());

        if (CloseSoundSource.isPlaying) {
            return;
        }
        
        CloseSoundSource.Play();
    }
    private void OnTriggerEnter(Collider other) {
        if (CurrentColliderList.Contains(other) || !other.gameObject.CompareTag("Player")) {
            return;
        }
        if (Doorway.transform.position.y <= FinalDoorPosition.y ){
            return;
        }
        CurrentColliderList.Add(other);
        WTOBase.LogToConsole("Doorway Trigger Entered!");
        StopCoroutine(CloseDoor());
        SetDoorVariables();
        ShouldFall = true;
    }
    private void OnTriggerExit(Collider other) {
        CurrentColliderList.Remove(other);
    }

    private void SetDoorVariables() {
        TargetDoorPosition = Doorway.transform.position + new Vector3(0, DistanceToTravelEachTime, 0);
        ShouldFall = false;
        CurrentDoorPosition = Doorway.transform.position;
        timeElapsed = 0;
    }
    IEnumerator CloseDoor() {
        timeElapsed += Time.deltaTime;
        WTOBase.LogToConsole($"Current Lerp Position: {timeElapsed / CloseTime}");
        Doorway.transform.position = Vector3.Lerp(CurrentDoorPosition, TargetDoorPosition, timeElapsed / CloseTime);
        if (timeElapsed / CloseTime >= 1) {
            ShouldFall = false;
            StopCoroutine(CloseDoor());
        }
        yield return null;
    }
}
