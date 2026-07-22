using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    internal class SprintTotem : GrabbableObject
    {

#pragma warning disable 0649 // Assigned in Unity Editor
        public AudioClip TotemBreakSound;
        public AudioSource AudioPlayer;
        public List<MeshRenderer> TotemPieces = new();
        public MeshRenderer TotemCenter;
        public float TotemDuration = 15f;
#pragma warning restore 0649

        private float TotemSecondsRemaining = 15f;
        private int LastSentTotemPieces = 0;

        // The player's sprint multiplier when the item was picked up.
        // This is used as a baseline to return to once this item stops
        // providing sprint.
        // This is done this way to allow other mods that might increase sprint 
        // to have a higher chance of working with our mod.
        // If we just forced sprint back to 1 at the end, then that would override
        // any other mods that might have increased sprint.
        private float? OriginalSprintMultiplier = null;

        // Set when the server first sees someone pick up the item.
        // This is used to determine furture scrap values based on the number of pieces remaining.
        // If ScrapValuePerPiece is 0, then the value gets snapshotted the next time the totem countdown tick
        // first starts. 
        // This is done this way to allow the scrap value to be changed externally after the item is created, but before it is picked up.
        private int ScrapValuePerPiece = 0;

        private static readonly WTOBase.WTOLogger Log = new(typeof(SprintTotem), LogSourceType.Item);

        private void Awake()
        {
            // This gets called when this is instantiated BEFORE LoadItemSaveData().
            // Start() is called after LoadItemSaveData() so that cannot be used for this initialisation.
            TotemSecondsRemaining = TotemDuration;
            LastSentTotemPieces = TotemPieces.Count;
            PrintState("Awake: ");
        }

        public override int GetItemDataToSave()
        {
            // Bitpack the calculated ScrapValuePerPiece and LastSentTotemPieces. We will snap the duration upwards
            // based on how many pieces there were.
            // We uses pieces are they are an exact value - no floats to worry about.
            int scrapValuePerPiece = ScrapValuePerPiece & 0xFFFF; // 16 bits for scrap value per piece
            int pieces = LastSentTotemPieces & 0xFFFF; // 16 bits for pieces remaining
            return (scrapValuePerPiece << 16) | pieces;
        }

        public override void LoadItemSaveData(int saveData)
        {
            // If this is loaded as 0, then that's fine. It will get assigned based on
            // the current scrap value in ReduceTotemPercentage() when the item is picked up.
            // We presume that the scrap value is correctly saved/loaded by the game itself.
            ScrapValuePerPiece = (saveData >> 16) & 0xFFFF;
            LastSentTotemPieces = saveData & 0xFFFF;

            // Derive time remaining from the number of pieces remaining and the total duration.
            // only needed for server, but doesn't hurt putting it on clients too.
            TotemSecondsRemaining = (LastSentTotemPieces / (float)TotemPieces.Count) * TotemDuration;

            SetTotemPieces(LastSentTotemPieces);

            PrintState("LoadItemSaveData: ");

            // If the LastSentTotemPieces was somehow 0, set it to -1 here after the effects are applied.
            // This means we get visual rendering of 0 pieces, but then the next countdown tick will
            // cause -1 to go to 0 and destroy the totem.
            // If we left this at 0, then the totem would not be destroyed as the countdown tick would not trigger
            // the UpdateTotemClientRpc() call to destroy the totem.
            if (LastSentTotemPieces == 0)
            {
                LastSentTotemPieces = -1;
            }
        }

        public override void Update()
        {
            base.Update();

            if(playerHeldBy == null || !isHeld)
            {
                return;
            }

            if (IsOwner)
            {
                if (!OriginalSprintMultiplier.HasValue)
                {
                    // Store pick up sprint multiplier.
                    OriginalSprintMultiplier = playerHeldBy.sprintMultiplier;
                }

                // Assign sprint multiplier based on whether the item is held or pocketed.
                playerHeldBy.sprintMultiplier = playerHeldBy.isSprinting ?
                    isPocketed ? 2.25f : 3.0f :
                    OriginalSprintMultiplier.Value;
            }

            if (IsServer && !isPocketed && playerHeldBy.isSprinting)
            {
                // Server does tick countdown and syncs to clients.
                ReduceTotemPercentage();
            }
        }

        public override void DiscardItem()
        {
            if (IsOwner && playerHeldBy != null && OriginalSprintMultiplier.HasValue)
            {
                // Restore the original sprint multiplier when the item is discarded.
                playerHeldBy.sprintMultiplier = OriginalSprintMultiplier.Value;
                OriginalSprintMultiplier = null;
            }

            base.DiscardItem();
        }

        private void ReduceTotemPercentage()
        {
            // Store scrap value at the point when the countdown starts.
            // This is set here to catch the scrap value being changed externally after the item is
            // created, but before it is picked up.
            // Once ScrapValuePerPiece is set, it's then used as a multiplier against the number of pieces
            // to set future scrap values.
            if (ScrapValuePerPiece == 0)
            {
                ScrapValuePerPiece = scrapValue / TotemPieces.Count;
            }

            TotemSecondsRemaining = Mathf.Max(TotemSecondsRemaining - Time.deltaTime, 0.0f);

            float percent = TotemSecondsRemaining / TotemDuration;
            int piecesRemaining = (int)Math.Ceiling(percent * TotemPieces.Count);

            if (piecesRemaining != LastSentTotemPieces)
            {
                LastSentTotemPieces = piecesRemaining;
                int newScrapValue = ScrapValuePerPiece * piecesRemaining;

                PrintState("ReduceTotemPercentage: ");

                UpdateTotemClientRpc(LastSentTotemPieces, newScrapValue);
            }
        }

        [ClientRpc]
        private void UpdateTotemClientRpc(int pieces, int newScrapValue)
        {
            SetScrapValue(newScrapValue);
            AudioPlayer.PlayOneShot(TotemBreakSound);
            SetTotemPieces(pieces);

            if (pieces == 0)
            {
                // Destroy needs to fire on all clients.
                itemUsedUp = true;
                DestroyObjectInHand(playerHeldBy);
            }
        }

        private void SetTotemPieces(int pieces)
        {
            // Sync visible pieces (just hide the ones that should not be visible)
            for (int i = 0; i < TotemPieces.Count; i++)
            {
                TotemPieces[i].enabled = i < pieces;
            }
        }

        private void PrintState(string prefix="")
        {
            Log.Info($"{prefix} TotemSecondsRemaining={TotemSecondsRemaining}, LastSentTotemPieces={LastSentTotemPieces}, ScrapValuePerPiece={ScrapValuePerPiece}, ScrapValue(lc)={scrapValue}");
        }
    }
}
