## Welcome To Ooblterra

Repository for the Welcome To Ooblterra mod for Lethal Company: https://thunderstore.io/c/lethal-company/p/Skeleton_Studios/Welcome_To_Ooblterra/

This repository contains the everything to build the .dll file for the mod. All of the Unity asset bundles are created in separate private repository.

Please feel free to create bug reports here for any bugs you might find when playing Welcome To Ooblterra. 

If you want to build the mod .dll for yourself, follow these steps:

1. Create an R2Modman profile that contains [Welcome To Ooblterra](https://thunderstore.io/c/lethal-company/p/Skeleton_Studios/Welcome_To_Ooblterra/) and ensure it downloads all dependencies.

2. Build the mod using dotnet or Visual Studio: 
```powershell
dotnet restore
dotnet tool restore
dotnet clean -c Debug # or Release
dotnet build -c Debug # or Release
```

3. Copy the mod to your R2Modman profile
```powershell
cp bin/Debug/netstandard2.1/Welcome_To_Ooblterra.dll your_r2modman_profile_path\BepInEx\plugins\Skeleton_Studios-Welcome_To_Ooblterra\Welcome_To_Ooblterra.dll
```
(replace Debug with Release if applicable)

You can also create a `Welcome_To_Ooblterra.csproj.user` file (use the `Welcome_To_Ooblterra.csproj.user.example` file as a base) and specify the R2Modman path in there.
This will automatically copy the built dll each time you build the project

4. Launch the game

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

Deelon - Programming