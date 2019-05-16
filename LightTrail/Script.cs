using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace LightTrail
{
    public class Script : BaseScript
    {
        #region Script Constants

        public const string dict = "core";
        public const string particleName = "veh_light_red_trail";
        public const string evolutionPropertyName = "speed";
        public const string brakelight_l = "brakelight_l";
        public const string brakelight_r = "brakelight_r";
        public const string brakelight_m = "brakelight_m";
        public const string taillight_l = "taillight_l";
        public const string taillight_r = "taillight_r";
        public const string taillight_m = "taillight_m";

        #endregion

        #region Private Members

        private TrailMode trailMode = TrailMode.BrakeOnly;
        private int playerVehicle;
        private TrailFx trailL = new TrailFx("left");
        private TrailFx trailR = new TrailFx("right");
        private TrailFx trailM = new TrailFx("middle");

        #endregion

        public enum TrailMode
        {
            Off,
            On,
            BrakeOnly,
        }

        class TrailFx
        {
            public string Name { get; set; }
            public int Handle { get; set; }
            public string BoneName { get; set; }
            public bool Enabled { get; set; } = true;
            public Vector3 Offset { get; set; } = Vector3.Zero;
            public Vector3 Rotation { get; set; } = Vector3.Zero;
            public Vector3 Color { get; set; } = new Vector3(1.0f, 0.0f, 0.0f);
            public float Scale { get; set; } = 1.0f;
            public float Alpha { get; set; } = 1.0f;

            public TrailFx(string name)
            {
                Name = name;
            }
        }

        public Script()
        {
            RegisterCommand("trail_mode", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"LightTrail: Missing argument off|on|brake");
                    return;
                }

                switch (args[0])
                {
                    case "off":
                        trailMode = TrailMode.Off;
                        break;
                    case "on":
                        trailMode = TrailMode.On;
                        SetupTrailMode();
                        break;
                    case "brake":
                        trailMode = TrailMode.BrakeOnly;
                        SetupTrailMode();
                        break;
                    default:
                        Debug.WriteLine($"LightTrail: Error parsing {args[0]}");
                        return;
                }

                Debug.WriteLine($"LightTrail: Switched trail mode to {trailMode}");

            }), false);

            Tick += Initialize;
        }

        public async Task Initialize()
        {
            Tick -= Initialize;

            RequestNamedPtfxAsset(dict);

            while (!HasNamedPtfxAssetLoaded(dict))
            {
                await Delay(0);
            }

            await SetupTrailMode();

            Tick += GetPlayerVehicle;
            Tick += UpdatePlayerVehicle;
        }

        private async Task SetupTrailMode()
        {
            switch (trailMode)
            {
                case TrailMode.Off:
                    break;
                case TrailMode.On:
                    trailL.BoneName = taillight_l; // GetEntityBoneIndexByName(playerVehicle, taillight_l) != -1 ? taillight_l : brakelight_l;
                    trailR.BoneName = taillight_r; // GetEntityBoneIndexByName(playerVehicle, taillight_r) != -1 ? taillight_r : brakelight_r;
                    trailM.BoneName = taillight_m; // GetEntityBoneIndexByName(playerVehicle, taillight_m) != -1 ? taillight_m : brakelight_m;
                    break;
                case TrailMode.BrakeOnly:
                    trailL.BoneName = GetEntityBoneIndexByName(playerVehicle, brakelight_l) != -1 ? brakelight_l : taillight_l;
                    trailR.BoneName = GetEntityBoneIndexByName(playerVehicle, brakelight_r) != -1 ? brakelight_r : taillight_r;
                    trailM.BoneName = GetEntityBoneIndexByName(playerVehicle, brakelight_m) != -1 ? brakelight_m : taillight_m;
                    break;
            }
            await Task.FromResult(0);
        }

        private async Task UpdatePlayerVehicle()
        {
            if (!DoesEntityExist(playerVehicle))
                return;

            await UpdateVehiclePtfx(playerVehicle);
        }

        private bool PlayerIsBraking => (GetEntitySpeed(playerVehicle) != 0.0f && GetEntitySpeedVector(playerVehicle, true).Y > 0.0f && 
            (IsControlPressed(1, (int)Control.VehicleBrake) || IsDisabledControlPressed(1, (int)Control.VehicleBrake) || 
            IsControlJustPressed(1, (int)Control.VehicleBrake) || IsDisabledControlJustPressed(1, (int)Control.VehicleBrake)));

        private async Task UpdateVehiclePtfx(int entity)
        {
            switch (trailMode)
            {
                case TrailMode.Off:
                    await Reset();
                    break;
                    
                case TrailMode.On:
                    UseParticleFxAssetNextCall(dict);
                    
                    StartForTrail(trailL);
                    StartForTrail(trailR);
                    StartForTrail(trailM);
                    break;

                case TrailMode.BrakeOnly:

                    if (PlayerIsBraking)
                    {
                        //Debug.WriteLine($"Speed: {GetEntitySpeed(playerVehicle)}, Vector: {GetEntitySpeedVector(playerVehicle, true)}, Brake: {(IsControlPressed(1, (int)Control.VehicleBrake) || IsDisabledControlPressed(1, (int)Control.VehicleBrake) || IsControlJustPressed(1, (int)Control.VehicleBrake) || IsDisabledControlJustPressed(1, (int)Control.VehicleBrake))}");

                        UseParticleFxAssetNextCall(dict);
                        StartForTrail(trailL);
                        StartForTrail(trailR);
                        StartForTrail(trailM);
                    }
                    else
                    {
                        // TODO: Use a timed fading end instead 
                        await Reset();
                    }
                    break;
            }

            void StartForTrail(TrailFx trail)
            {
                if (!DoesParticleFxLoopedExist(trail.Handle))
                {
                    int handle = -1;

                    switch (trail.Name)
                    {
                        case "left":
                            StartParticleFx(ref handle, particleName, entity, trail.BoneName, trail.Color, trail.Offset, trail.Rotation, trail.Scale, trail.Alpha);
                            break;
                        case "middle":
                            StartParticleFx(ref handle, particleName, entity, trail.BoneName, trail.Color, trail.Offset, trail.Rotation, trail.Scale, trail.Alpha);
                            break;
                        case "right":
                            StartParticleFx(ref handle, particleName, entity, trail.BoneName, trail.Color, trail.Offset, trail.Rotation, trail.Scale, trail.Alpha);
                            break;
                    }

                    trail.Handle = handle;
                }
            }

            await Task.FromResult(0);
        }

        public void StartParticleFx(ref int handle, string ptfxName, int entity, string boneName, Vector3 color, Vector3 offset, Vector3 rotation, float scale, float alpha)
        {
            // Get bone index
            int boneIndex = GetEntityBoneIndexByName(entity, boneName);

            // don't do anything if the bone doesn't exist
            if (boneIndex == -1)
                return;

            // create the looped ptfx
            // TODO: Replace with StartNetworkedParticleFxLoopedOnEntityBone
            handle = StartParticleFxLoopedOnEntityBone_2(ptfxName, entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, scale, false, false, false);
            SetParticleFxLoopedEvolution(handle, evolutionPropertyName, 1.0f, false);
            SetParticleFxLoopedColour(handle, color.X, color.Y, color.Z, false);
            SetParticleFxLoopedAlpha(handle, alpha);
        }

        /// <summary>
        /// Updates the <see cref="playerVehicle"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetPlayerVehicle()
        {
            var playerPed = PlayerPedId();

            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != playerVehicle)
                    {
                        await Reset();
                        playerVehicle = vehicle;
                    }
                }
                else
                {
                    // If player isn't driving current vehicle or vehicle is dead
                    playerVehicle = -1;
                    await Reset();
                }
            }
            else
            {
                // If player isn't in any vehicle
                playerVehicle = -1;
                await Reset();
            }

            await Delay(500);
        }

        private async Task Reset()
        {
            if (DoesParticleFxLoopedExist(trailL.Handle)) RemoveParticleFx(trailL.Handle, false);
            if (DoesParticleFxLoopedExist(trailR.Handle)) RemoveParticleFx(trailR.Handle, false);
            if (DoesParticleFxLoopedExist(trailM.Handle)) RemoveParticleFx(trailM.Handle, false);

            await Task.FromResult(0);
        }
    }
}
