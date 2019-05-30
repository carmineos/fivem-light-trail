using CitizenFX.Core;
using System;
using System.Threading.Tasks;
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

        public enum TrailStatus
        {
            Empty,
            FadingIn,
            FadingOut,
            Full
        }

        class TrailFx
        {
            public int Handle { get; set; }
            public string BoneName { get; set; }
            public TrailStatus Status { get; set; } = TrailStatus.Empty;
            public bool Enabled { get; set; } = true;
            public Vector3 Offset { get; set; } = Vector3.Zero;
            public Vector3 Rotation { get; set; } = Vector3.Zero;
            public Vector3 Color { get; set; } = new Vector3(1.0f, 0.0f, 0.0f);
            public float Scale { get; set; } = 1.0f;
            public float Alpha { get; set; } = 1.0f;
            public float Evolution { get; set; } = 1.0f;

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

                if (!DoesParticleFxLoopedExist(Handle))
                {
                    // Get bone index
                    int boneIndex = GetEntityBoneIndexByName(entity, BoneName);

                    // don't do anything if the bone doesn't exist
                    if (boneIndex == -1)
                        return;

                    UseParticleFxAssetNextCall("core");
                    Handle = StartNetworkedParticleFxLoopedOnEntityBone("veh_light_red_trail", entity, Offset.X, Offset.Y, Offset.Z, Rotation.X, Rotation.Y, Rotation.Z, boneIndex, Scale, true, true, true);
                    SetParticleFxLoopedEvolution(Handle, "speed", Evolution, false);
                    SetParticleFxLoopedColour(Handle, Color.X, Color.Y, Color.Z, false);
                    SetParticleFxLoopedAlpha(Handle, Alpha);
                }
            }

            public void Stop()
            {
                Off();
                if (DoesParticleFxLoopedExist(Handle))
                    RemoveParticleFx(Handle, false);
            }

            public void On()
            {
                Alpha = 1.0f;
                Scale = 1.0f;
                Evolution = 1.0f;
            }

            public void Off()
            {
                Alpha = 0.0f;
                Scale = 0.0f;
                Evolution = 0.0f;
            }

            public void Update()
            {
                SetParticleFxLoopedAlpha(Handle, Alpha);
                SetParticleFxLoopedScale(Handle, Scale);
                SetParticleFxLoopedEvolution(Handle, "speed", Evolution, false);
            }

            public async Task LoopBrakeMode()
            {
                if (!Enabled || !DoesParticleFxLoopedExist(Handle))
                    return;

                switch (Status)
                {
                    case TrailStatus.Empty:
                        Off();
                        //Stop();
                        break;

                    case TrailStatus.Full:
                        On();
                        Update();
                        break;

                    case TrailStatus.FadingIn:
                        Alpha += (2.5f * GetFrameTime());
                        Scale = Alpha;
                        Evolution = Alpha;
                        Update();
                        break;

                    case TrailStatus.FadingOut:
                        Alpha -= (2.5f * GetFrameTime());
                        Scale = Alpha;
                        Evolution = Alpha;
                        Update();
                        break;
                }
                await Task.FromResult(0);
            }

            public bool FadeOutFinished => Alpha <= 0.0f && Scale <= 0.0f;

            public bool FadeInFinished => Alpha >= 1.0f && Scale >= 1.0f;
        }

        private async Task UpdateBrakeModeStatus(TrailFx trail)
        {
            switch (trail.Status)
            {
                case TrailStatus.Empty:
                    if (PlayerIsBraking)
                    {
                        trail.Status = TrailStatus.FadingIn;
                        trail.Start(m_playerVehicle);
                    }
                    break;

                case TrailStatus.FadingIn:
                    if (PlayerIsBraking)
                    {
                        if (trail.FadeInFinished)
                            trail.Status = TrailStatus.Full;
                    }
                    else
                        trail.Status = TrailStatus.FadingOut;
                    break;

                case TrailStatus.FadingOut:
                    if (PlayerIsBraking)
                        trail.Status = TrailStatus.FadingIn;
                    else
                    {
                        if (trail.FadeOutFinished)
                            trail.Status = TrailStatus.Empty;
                    }
                    break;

                case TrailStatus.Full:
                    if (!PlayerIsBraking)
                        trail.Status = TrailStatus.FadingOut;
                    break;
            }

            await Task.FromResult(0);
        }

        private async Task UpdateBrakeModeStatusAll()
        {
            UpdateBrakeModeStatus(m_trailLeft);
            UpdateBrakeModeStatus(m_trailRight);
            UpdateBrakeModeStatus(m_trailMiddle);

            await Task.FromResult(0);
        }

        private async Task LoopBrakeModeAll()
        {
            m_trailLeft.LoopBrakeMode();
            m_trailRight.LoopBrakeMode();
            m_trailMiddle.LoopBrakeMode();

            await Task.FromResult(0);
        }

        private async Task StopAll()
        {
            m_trailLeft.Stop();
            m_trailRight.Stop();
            m_trailMiddle.Stop();

            await Task.FromResult(0);
        }

        private async Task StartAll(int entity)
        {
            m_trailLeft.Start(entity);
            m_trailRight.Start(entity);
            m_trailMiddle.Start(entity);

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
                        await SetupTrailMode();
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
                    await StopAll();
                    break;
                case TrailMode.On:
                    m_trailLeft.BoneName = "taillight_l";
                    m_trailRight.BoneName = "taillight_r";
                    m_trailMiddle.BoneName = "taillight_m";
                    m_trailLeft.On();
                    m_trailRight.On();
                    m_trailMiddle.On();
                    await StartAll(m_playerVehicle);
                    break;
                case TrailMode.BrakeOnly:
                    m_trailLeft.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_l") != -1 ? "brakelight_l" : "taillight_l";
                    m_trailRight.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_r") != -1 ? "brakelight_r" : "taillight_r";
                    m_trailMiddle.BoneName = GetEntityBoneIndexByName(m_playerVehicle, "brakelight_m") != -1 ? "brakelight_m" : "taillight_m";
                    m_trailLeft.Off();
                    m_trailRight.Off();
                    m_trailMiddle.Off();
                    await StartAll(m_playerVehicle);
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
                case TrailMode.On:
                    break;

                case TrailMode.BrakeOnly:
                    await UpdateBrakeModeStatusAll();
                    await LoopBrakeModeAll();
                    break;
            }

            await Task.FromResult(0);
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
                        await StopAll();
                        m_playerVehicle = vehicle;
                        await SetupTrailMode();
                    }
                }
                else
                {
                    // If player isn't driving current vehicle or vehicle is dead
                    m_playerVehicle = -1;
                    await StopAll();
                }
            }
            else
            {
                // If player isn't in any vehicle
                m_playerVehicle = -1;
                await StopAll();
            }

            await Delay(500);
        }
    }
}
