using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Things
{
    public class HideSpot : NetworkBehaviour{
        // This is here so that missing script recovery has a better time identifying the 
        // appropriate script to use. If there's no properties on a script, then it does
        // not have any unique information to identify itself with.
        public string HideSpotMarker = "";
    }
}
