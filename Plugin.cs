using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
/* will need to add this back once we add plugin files
using Welcome_To_Ooblterra.Patches;
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Welcome_To_Ooblterra{
    [BepInPlugin (modGUID, modName, modVersion)]
    public class WTOBase : BaseUnityPlugin {
        private const string modGUID = "SkullCrusher.WTO";
        private const string modName = "Welcome To Ooblterra";
        private const string modVersion = "0.1.0";

        private readonly Harmony WTOHarmony = new Harmony (modGUID);
        internal ManualLogSource WTOLogSource;
        private static WTOBase Instance;

        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            WTOLogSource = BepInEx.Logging.Logger.CreateLogSource (modGUID);
            WTOLogSource.LogInfo ("Begin test mod...");
            WTOHarmony.PatchAll (typeof (WTOBase));
            //myHarmony.PatchAll (typeof (<PatchedClass>));
        }

    }


}
