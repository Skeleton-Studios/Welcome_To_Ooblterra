## Setup

1. Clone this repository, and [WTO-Assets](https://github.com/Skeleton-Studios/WTO-Assets)
2. Download and decompress [NetcodePatcher-2.4.0](https://github.com/EvaisaDev/UnityNetcodePatcher/releases/tag/2.4.0)
3. Create a new r2modman profile
4. Install `LethalLevelLoader` to the profile (this will also install `LethalLib`)
5. Install `Welcome_To_Ooblterra` so that it exists in the r2modman directory (when building from visual studio, this will override the mod dll with the newly built one)
6. Set paths in csproj for 
   - `LethalCompanyPath`
   - `WTOAssetsPath`
   - `NetcodePatcherPath`
   - `R2ModManPath`
7. Install project dependencies
8. Copy contents of `steamapps\common\Lethal Company\Lethal Company_Data\Managed` to `NetcodePatcher-2.4.0\deps`

## CREDITS

SkullCrusher - Programming and Design

Gasparatus - Music

StrangerFolk - Sound Design

Jonas "Lazarus" Bocash, comfycookie404, Supared - Visual Design (Textures, Models)

EchoSem - Animation

IAmBreeze, Hanori, Wasomute - Playtesting

Joseph 'Jojo' Evans - Sound design, Voice Acting

Evaisa - Programming (API for custom items and interior)

IAmBatby - Programming (created API for custom moon adapted from old Ooblterra code, thank you!)

HeyImNoop - Programming (created API for custom monsters adapted from old Ooblterra code, thank you!)

Badhamknibbs - Programming (Custom interior assistance) 

AlexCodesGames - Programming (Code for custom suits has been lightly adapted from AdditionalSuits)

Bizzlemip - Programming (custom moon code has been heavily adapted from MoonAPI)
