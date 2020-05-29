# Light Trail
|Master|Development|
|:-:|:-:|
|[![Build status](https://ci.appveyor.com/api/projects/status/qialhqew9j0i9528/branch/master?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-vstancer/branch/master) |[![Build status](https://ci.appveyor.com/api/projects/status/qialhqew9j0i9528/branch/development?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-vstancer/branch/development)|

### Description
The script aims to recreate the light trail effect from the popular Anime "Initial D".
Each vehicle can be set to have its trails either always on, or showing only while the vehicle is braking 

### Features of the script
* Add tail lights trails to vehicles
* Add brake lights trails to vehicles

### Client Commands
* `trail_mode <off|on|brake>`: Sets the player's vehicle trail mode
* `trail_print`: Prints info about all the vehicles with a trail on

### Example
Remember that you can write commands in the console, or, by prefixing them with a `/`, in chat:
* `trail_mode on`: Sets the player's vehicle trail mode to be in always on mode (trails will be always showing from tail lights)
* `trail_mode brake`: Sets the player's vehicle trail mode to be in brake mode (trails will appear from brake lights only when braking)
* `trail_mode off`: Removes the player's vehicle trail

[Source](https://github.com/carmineos/fivem-light-trail)
[Download](https://github.com/carmineos/fivem-light-trail/releases)
I am open to any kind of feedback. Report suggestions and bugs you find.

### Build
Open the `postbuild.bat` and edit the path of the resource folder. If in Debug configuration, the post build event will copy the following files to the specified path: the built assembly of the script and the `fxmanifest.lua`.

### Installation
1. Download the zip file from the release page
2. Extract the content of the zip to the resources folder of your server (it should be a folder named `light-trail`)
3. Enable the resource in your server config (`start light-trail`)

### Limitations
* In brake mode, if the player is holding brake and throttle control at the same time the brake status isn't triggered, this is because at the moment it's using brake pressure data of each wheel, once FiveM will add an extra-native to read [brake pedal pressure](https://github.com/E66666666/GTAVManualTransmission/blob/master/Gears/Memory/VehicleExtensions.cpp#L144), this should be fixed.

### Curiosities
The script could have used networked particle effects (net ptfx) and let GTA V engine sync them with all the players although there are some problems:
* FiveM OneSync servers at the moment don't support net ptfx yet, so the trails wouldn't sync with the players
* Even if FiveM OneSync server will support net ptfx in future, it won't be a good idea to rely on game engine to sync the net ptfx as that isn't done fast enough to allow the script to work in brake mode, as that requires to check each frame if a vehicle is braking.

### Credits
* [FiveM by CitizenFX](https://github.com/citizenfx/fivem)
* [hibiki](https://github.com/themonthofjune)
* Testers from S-Tier server

### Support
If you would like to support my work, you can through:
* [Patreon](https://patreon.com/carmineos)