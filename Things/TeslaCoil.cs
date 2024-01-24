using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things {
    internal class TeslaCoil : NetworkBehaviour {

        public BoxCollider RangeBox;


        [HideInInspector]
        public bool TeslaCoilOn;

        private List<PlayerControllerB> PlayerInRangeList;
    }
}
