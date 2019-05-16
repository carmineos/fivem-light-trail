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
        private TrailMode m_trailMode = TrailMode.BrakeOnly;
        private int m_playerVehicle;

        private TrailFx m_trailLeft = new TrailFx("taillight_l");
        private TrailFx m_trailRight = new TrailFx("taillight_r");
        private TrailFx m_trailMiddle = new TrailFx("taillight_m");

        public enum TrailMode
        {
            Off,
            On,
            BrakeOnly,
        }

        class TrailFx
        {
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
                BoneName = name;
            }

            public void Start(int entity)
            {
                if (!Enabled)
                {
                    return;
                }

                ResetFade();

                // reset the fade before returning
                if (m_isFadingOut)
                {
                    return;
                }

                UseParticleFxAssetNextCall("core");

                if (!DoesParticleFxLoopedExist(Handle))
                {
                    int handle = -1;

                    StartParticleFx(ref handle, "veh_light_red_trail", entity, BoneName, Color, Offset, Rotation, Scale, Alpha);

                    Handle = handle;
                }
            }

            public void Reset()
            {
                if (DoesParticleFxLoopedExist(Handle))
                {
                    RemoveParticleFx(Handle, false);
                }
            }

            public void ResetFade()
            {
                Alpha = 1.0f;
                Scale = 1.0f;

                SetParticleFxLoopedAlpha(Handle, Alpha);
                SetParticleFxLoopedScale(Handle, Scale);
            }

            private bool m_isFadingOut = false;

            public void FadeOut()
            {
                if (!Enabled || !DoesParticleFxLoopedExist(Handle))
                {
                    return;
                }

                if (!m_isFadingOut && Alpha > 0.0f)
                {
                    m_isFadingOut = true;
                }

                if (m_isFadingOut)
                {
                    Alpha -= (0.9f * GetFrameTime());
                    Scale = Alpha;

                    if (Alpha < 0.0f)
                    {
                        m_isFadingOut = false;

                        Alpha = 0.0f;
                        Scale = 0.0f;

                        RemoveParticleFx(Handle, false);

                        return;
                    }

                    SetParticleFxLoopedAlpha(Handle, Alpha);
                    SetParticleFxLoopedScale(Handle, Scale);
                }
            }
        }

        private async Task FadeOutAll()
        {
            m_trailLeft.FadeOut();
            m_trailRight.FadeOut();
            m_trailMiddle.FadeOut();

            await Task.FromResult(0);
        }

        private async Task StartAll(int entity)
        {
            m_trailLeft.Start(entity);
            m_trailRight.Start(entity);
            m_trailMiddle.Start(entity);

            await Task.FromResult(0);
        }

        private async Task ResetAll()
        {
            m_trailLeft.Reset();
            m_trailRight.Reset();
            m_trailMiddle.Reset();

            await Task.FromResult(0);
        }

        public Script()
        {
            RegisterCommand("trail_mode", new Action<int, dynamic>(async (source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"LightTrail: Missing argument off|on|brake");
                    return;
                }

                switch (args[0])
                {
                    case "off":
                        m_trailMode = TrailMode.Off;
                        break;
                    case "on":
                        m_trailMode = TrailMode.On;
                        await SetupTrailMode();
                        break;
                    case "brake":
                        m_trailMode = TrailMode.BrakeOnly;
                        await SetupTrailMode();
                        break;
                    default:
                        Debug.WriteLine($"LightTrail: Error parsing {args[0]}");
                        return;
                }

                Debug.WriteLine($"LightTrail: Switched trail mode to {m_trailMode}");

            }), false);

            Tick += Initialize;
        }

        public async Task Initialize()
        {
            Tick -= Initialize;

            await SetupTrailMode();

            Tick += GetPlayerVehicle;
            Tick += UpdatePlayerVehicle;
        }

        private async Task SetupTrailMode()
        {
            switch (m_trailMode)
            {
                case TrailMode.Off:
                    break;
                case TrailMode.On:
                    m_trailLeft.BoneName = "taillight_l";
                    m_trailRight.BoneName = "taillight_r";
                    m_trailMiddle.BoneName = "taillight_m";
                    break;
                case TrailMode.BrakeOnly:
                    m_trailLeft.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_l") != -1 ? "brakelight_l" : "taillight_l";
                    m_trailRight.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_r") != -1 ? "brakelight_r" : "taillight_r";
                    m_trailMiddle.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_m") != -1 ? "brakelight_m" : "taillight_m";
                    break;
            }
            await Task.FromResult(0);
        }

        private async Task UpdatePlayerVehicle()
        {
            if (!DoesEntityExist(m_playerVehicle))
            {
                return;
            }

            await UpdateVehiclePtfx(m_playerVehicle);
        }

        private bool PlayerIsBraking => (GetEntitySpeed(m_playerVehicle) != 0.0f && GetEntitySpeedVector(m_playerVehicle, true).Y > 0.0f && 
            (IsControlPressed(1, (int)Control.VehicleBrake) || IsDisabledControlPressed(1, (int)Control.VehicleBrake)));

        private async Task UpdateVehiclePtfx(int entity)
        {
            switch (m_trailMode)
            {
                case TrailMode.Off:
                    await ResetAll();
                    break;
                case TrailMode.On:
                    await StartAll(entity);
                    break;
                case TrailMode.BrakeOnly:

                    if (PlayerIsBraking)
                    {
                        await StartAll(entity);
                    }
                    else
                    {
                        await FadeOutAll();
                    }

                    break;
            }

            await Task.FromResult(0);
        }

        private static void StartParticleFx(ref int handle, string ptfxName, int entity, string boneName, Vector3 color, Vector3 offset, Vector3 rotation, float scale, float alpha)
        {
            // Get bone index
            int boneIndex = GetEntityBoneIndexByName(entity, boneName);

            // don't do anything if the bone doesn't exist
            if (boneIndex == -1)
                return;

            // create the looped ptfx
            // TODO: Replace with StartNetworkedParticleFxLoopedOnEntityBone
            handle = StartParticleFxLoopedOnEntityBone_2(ptfxName, entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, scale, true, true, true);
            SetParticleFxLoopedEvolution(handle, "speed", 1.0f, false);
            SetParticleFxLoopedColour(handle, color.X, color.Y, color.Z, false);
            SetParticleFxLoopedAlpha(handle, alpha);
        }

        /// <summary>
        /// Updates the <see cref="m_playerVehicle"/>
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
                    if (vehicle != m_playerVehicle)
                    {
                        await ResetAll();
                        m_playerVehicle = vehicle;
                    }
                }
                else
                {
                    // If player isn't driving current vehicle or vehicle is dead
                    m_playerVehicle = -1;
                    await ResetAll();
                }
            }
            else
            {
                // If player isn't in any vehicle
                m_playerVehicle = -1;
                await ResetAll();
            }

            await Delay(500);
        }
    }
}
