using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Welcome_To_Ooblterra.Items;
internal class HeavyRailgun : GrabbableObject {

    public float PlayerSpeedWhenReadied = 0.2f;
    public float PlayerSpeedWhenHeld = 0.6f;

    public AudioSource NoiseMaker;
    public AudioClip FiringSound;
    public AudioClip EmptyClick;

    private bool RailgunFired;
    private bool RailgunIsFiring;
    private bool RailgunLowered;

    public override void Start() {
        base.Start();
    }
    public override void Update() {
        base.Update();
        if (RailgunLowered) {
            playerHeldBy.movementSpeed = PlayerSpeedWhenReadied;
        } else {
            playerHeldBy.movementSpeed = PlayerSpeedWhenHeld;
        }
    }
    public override void ItemInteractLeftRight(bool right) {
        base.ItemInteractLeftRight(right);
        if (right && !RailgunLowered) {
            //Animate the railgun lowering
            LowerRailgun(true);
            return;
        }
        if (!right && !RailgunIsFiring) {
            FireRailgun();
        }
        if (RailgunFired) {
            NoiseMaker.PlayOneShot(EmptyClick);
            return;
        }
    }
    private void LowerRailgun(bool ShouldLower) { 
        RailgunLowered = ShouldLower;
    }
    private void FireRailgun() {
        RailgunIsFiring = true;
        NoiseMaker.PlayOneShot(FiringSound);
        //Animate the railgun firing and destroy anything in front of the beam
        RailgunFired = true;
    }
}
