using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
internal class SprintTotem : GrabbableObject {

    public AudioClip TotemBreakSound;
    public AudioSource AudioPlayer;
    public List<MeshRenderer> TotemPieces = new List<MeshRenderer>();
    public MeshRenderer TotemCenter;

    private float TotemSecondsRemaining = 15;
    private float TotemPercentage = 100;
    private int DesiredTotemPieces;
    private int TotemPiecesRemaining = 6;
    private bool DoOnce;
    private int StartingScrapValue;

    public void Awake() {
        SetScrapValue(120);
        StartingScrapValue = scrapValue;
    }
    public override void Update() {
        base.Update();
        if (!isPocketed && heldByPlayerOnServer && playerHeldBy.isSprinting) {
            SetPlayerSpeedAndStamina();
            ReduceTotemPercentage();
            DoOnce = true;
        }
        if (!DoOnce) {
            return;
        }
        if (isPocketed) {
            if (playerHeldBy.isSprinting) {
                playerHeldBy.sprintMultiplier = 2.25f;
            } else { 
                playerHeldBy.sprintMultiplier = 1f;
            }
            DoOnce = false;
        }
    }
    private void SetPlayerSpeedAndStamina() {
        if(playerHeldBy == null) {
            return;
        }
        playerHeldBy.sprintMultiplier = 3f;
        playerHeldBy.sprintMeter = 1f;
    }
    private void ReduceTotemPercentage() {
        TotemSecondsRemaining -= Time.deltaTime;
        TotemPercentage = TotemSecondsRemaining / 15;
        DesiredTotemPieces = (int)Math.Ceiling(TotemPercentage * 6);
        WTOBase.LogToConsole($"Seconds Remaining: {TotemSecondsRemaining} || Percentage: {TotemPercentage * 100} || Desired Totem Pieces: {DesiredTotemPieces}");
        if (DesiredTotemPieces <= 0) {
            AudioPlayer.PlayOneShot(TotemBreakSound);
            DestroyObjectInHand(playerHeldBy);
            return;
        }
        if(DesiredTotemPieces != TotemPiecesRemaining){
            TotemPiecesRemaining = DesiredTotemPieces;
            UpdateTotemServerRPC(MakeNewTotemValue());
        }
    }
    private int MakeNewTotemValue() {
        return scrapValue - (StartingScrapValue / 6);
    }
        
    [ServerRpc]
    private void UpdateTotemServerRPC(int TotemValue) {
        UpdateTotemClientRPC(TotemValue);
    }
    [ClientRpc]
    private void UpdateTotemClientRPC(int TotemValue) {
        UpdateTotem(TotemValue);
    }

    private void UpdateTotem(int TotemValue) {
        SetScrapValue(TotemValue);
        DestroyNextTotemSegment(false);
    }
    private void DestroyNextTotemSegment(bool silent){
        if (!silent) { 
            AudioPlayer.PlayOneShot(TotemBreakSound);
        }
        Destroy(TotemPieces[TotemPieces.Count - 1]);
        TotemPieces.Remove(TotemPieces[TotemPieces.Count - 1]);
    }
}
