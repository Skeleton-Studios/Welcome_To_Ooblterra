using BepInEx;
using BepInEx.Configuration;


namespace Welcome_To_Ooblterra.Properties;
internal class WTOConfig {
    public static ConfigEntry<bool> OoblterraEnabled;
    public static ConfigEntry<string> CustomInteriorEnabled;
    public static ConfigEntry<bool> WTOCustomSuits;
    public static ConfigEntry<string> SpawnScrapStatus;
    public static ConfigEntry<string> SpawnIndoorEnemyStatus;
    public static ConfigEntry<string> SpawnOutdoorEnemyStatus;
    public static ConfigEntry<string> SpawnAmbientEnemyStatus;
    public static ConfigEntry<string> SpawnSecurityStatus;


    private void Load() {

        OoblterraEnabled = WTOBase.ConfigFile.Bind("General", "Enable Ooblterra", true, "Whether or not to enable Ooblterra in the moons list and allow navigation to it. \n Default value: true \n Accepted values: true, false \n\n");
            
            CustomInteriorEnabled = WTOBase.ConfigFile.Bind("General", "Enable Ooblterra Interior", "CustomLevelOnly", "If the custom interior map is allowed to spawn, and where. Draws over the \"Manor\" configuration.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n The Off value will cause Ooblterra to use the \"Manor\" interior map by default. \n\n ");

            WTOCustomSuits = WTOBase.ConfigFile.Bind("Items", "Add Suits", true, "Whether or not to add WTO's custom suits. \n Default value: true \n Accepted values: true, false \n Turn off if you have other custom suit mods and are at risk of exceeding the 12 suit limit.\n\n");

            SpawnScrapStatus = WTOBase.ConfigFile.Bind("Items", "Custom Scrap", "CustomLevelOnly", "If custom scrap should be allowed to spawn, and where.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n\n");

            SpawnIndoorEnemyStatus = WTOBase.ConfigFile.Bind("Enemies", "Custom Enemies (Indoors)", "CustomLevelOnly", "If custom indoor enemies should be allowed to spawn, and where.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n\n");

            SpawnOutdoorEnemyStatus = WTOBase.ConfigFile.Bind("Enemies", "Custom Enemies (Outdoor)", "CustomLevelOnly", "If custom outdoor enemies should be allowed to spawn, and where.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n\n");

            SpawnAmbientEnemyStatus = WTOBase.ConfigFile.Bind("Enemies", "Custom Enemies (Daytime)", "CustomLevelOnly", "If custom daytime enemies should be allowed to spawn, and where.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n Daytime Enemies are ambient types that spawn before you land, such as birds, bees, and locusts. \n\n");
            
            SpawnSecurityStatus = WTOBase.ConfigFile.Bind("Enemies", "Custom Security", "CustomLevelOnly", "If custom security objects should be allowed to spawn, and where.\n Default value: CustomLevelOnly \n Accepted values: Off, CustomLevelOnly, AllLevels \n Security objects are objects such as Landmines and Turrets that can be disabled via the terminal. \n\n");

    }
}