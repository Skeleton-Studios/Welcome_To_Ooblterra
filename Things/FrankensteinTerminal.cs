using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinTerminal {

    public FrankensteinBodyPoint TargetBodyPoint;
    //Create a new terminal, change what it displays and what you can type into it

    //for now we're just gonna have two commands: revive and fail.
    //  Later this will be tied to a minigame and your performance in it will determine which happens


    //Both commands should fail unless the associated BodyPoint has a body
    private bool TableHasBody() {
        return TargetBodyPoint.HasBody;
    }

    //if revive, bring the player back from the dead, steal this from the roundmanager
    //also make sure to find some way to gag the player


    //if fail, spawn a mimic from the player's body

}
