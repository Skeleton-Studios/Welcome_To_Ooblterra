using BepInEx;
using BepInEx.Configuration;


namespace Welcome_To_Ooblterra.Properties;
internal class WTOConfig {
    public static ConfigEntry<bool> WTOCustomSuits;



    private void Load() {

            WTOCustomSuits = WTOBase.ConfigFile.Bind("Items", "Add Suits", true, "Whether or not to add WTO's custom suits. \n Default value: true \n Accepted values: true, false \n Turn off if you have other custom suit mods and are at risk of exceeding the 12 suit limit.\n\n");


    }
}