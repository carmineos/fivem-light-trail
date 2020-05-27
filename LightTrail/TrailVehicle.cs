using CitizenFX.Core;
using System;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace LightTrail
{
    public class TrailVehicle
    {
        private const string taillight_l = "taillight_l";
        private const string taillight_r = "taillight_r";
        private const string taillight_m = "taillight_m";
        private const string brakelight_l = "brakelight_l";
        private const string brakelight_r = "brakelight_r";
        private const string brakelight_m = "brakelight_m";

        private TrailMode m_trailMode = TrailMode.On;
        private int m_playerVehicle;

        private TrailFx m_trailLeft = new TrailFx(taillight_l);
        private TrailFx m_trailRight = new TrailFx(taillight_r);
        private TrailFx m_trailMiddle = new TrailFx(taillight_m);

        public int PlayerId = -1;
        public bool IsValidPlayer => PlayerId != -1 && NetworkIsPlayerActive(PlayerId);

        public TrailVehicle(int targetPlayer)
        {
            PlayerId = targetPlayer;

            SetupTrailMode();
        }

        private async Task SetupTrailMode()
        {
            await StopAll();

            switch (m_trailMode)
            {
                case TrailMode.Off:
                    break;
                case TrailMode.On:
                    m_trailLeft.BoneName = taillight_l;
                    m_trailRight.BoneName = taillight_r;
                    m_trailMiddle.BoneName = taillight_m;
                    m_trailLeft.On();
                    m_trailRight.On();
                    m_trailMiddle.On();
                    await StartAll(m_playerVehicle);
                    break;
                case TrailMode.Brake:
                    m_trailLeft.BoneName = GetEntityBoneIndexByName(m_playerVehicle, brakelight_l) != -1 ? brakelight_l : taillight_l;
                    m_trailRight.BoneName = GetEntityBoneIndexByName(m_playerVehicle, brakelight_r) != -1 ? brakelight_r : taillight_r;
                    m_trailMiddle.BoneName = GetEntityBoneIndexByName(m_playerVehicle, brakelight_m) != -1 ? brakelight_m : taillight_m;
                    m_trailLeft.Off();
                    m_trailRight.Off();
                    m_trailMiddle.Off();
                    await StartAll(m_playerVehicle);
                    break;
            }


            await Task.FromResult(0);
        }

        private async Task UpdateBrakeModeStatus(TrailFx trail)
        {
            switch (trail.Status)
            {
                case TrailStatus.Empty:
                    if (IsPlayerBraking)
                    {
                        trail.Status = TrailStatus.FadingIn;
                        trail.Start(m_playerVehicle);
                    }
                    break;

                case TrailStatus.FadingIn:
                    if (IsPlayerBraking)
                    {
                        if (trail.FadeInFinished)
                            trail.Status = TrailStatus.Full;
                    }
                    else
                        trail.Status = TrailStatus.FadingOut;
                    break;

                case TrailStatus.FadingOut:
                    if (IsPlayerBraking)
                        trail.Status = TrailStatus.FadingIn;
                    else
                    {
                        if (trail.FadeOutFinished)
                            trail.Status = TrailStatus.Empty;
                    }
                    break;

                case TrailStatus.Full:
                    if (!IsPlayerBraking)
                        trail.Status = TrailStatus.FadingOut;
                    break;
            }

            await Task.FromResult(0);
        }

        public async Task SetTrailMode(TrailMode trailMode, bool isLocal = false)
        {
            if (isLocal)
            {
                DecorSetInt(m_playerVehicle, "_trail_mode", (int)trailMode);
            }

            m_trailMode = trailMode;
            await SetupTrailMode();
        }

        private async Task UpdateBrakeModeStatusAll()
        {
            await UpdateBrakeModeStatus(m_trailLeft);
            await UpdateBrakeModeStatus(m_trailRight);
            await UpdateBrakeModeStatus(m_trailMiddle);
        }

        private async Task LoopBrakeModeAll()
        {
            await m_trailLeft.LoopBrakeMode();
            await m_trailRight.LoopBrakeMode();
            await m_trailMiddle.LoopBrakeMode();
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

        public async Task Update()
        {
            await GetPlayerVehicle();
            await UpdatePlayerVehicle();
        }

        TrailMode m_lastTrailMode;

        private async Task UpdatePlayerVehicle()
        {
            if (!DoesEntityExist(m_playerVehicle))
            {
                return;
            }

            m_trailMode = (TrailMode)DecorGetInt(m_playerVehicle, "_trail_mode");

            if (m_lastTrailMode != m_trailMode)
            {
                m_lastTrailMode = m_trailMode;
                await SetupTrailMode();
            }

            await UpdateVehiclePtfx(m_playerVehicle);
        }

        private bool IsPlayerBraking => (GetEntitySpeed(m_playerVehicle) != 0.0f 
            && GetEntitySpeedVector(m_playerVehicle, true).Y > 0.0f 
            && (IsControlPressed(1, (int)Control.VehicleBrake) || IsDisabledControlPressed(1, (int)Control.VehicleBrake)));

        private async Task UpdateVehiclePtfx(int entity)
        {
            switch (m_trailMode)
            {
                case TrailMode.Off:
                case TrailMode.On:
                    break;

                case TrailMode.Brake:
                    await UpdateBrakeModeStatusAll();
                    await LoopBrakeModeAll();
                    break;
            }
        }

        /// <summary>
        /// Updates the <see cref="m_playerVehicle"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetPlayerVehicle()
        {
            var playerPed = GetPlayerPed(PlayerId);

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

            await BaseScript.Delay(500);
        }
    }
}
