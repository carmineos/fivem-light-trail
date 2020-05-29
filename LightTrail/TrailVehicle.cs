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

        private TrailMode m_trailMode = TrailMode.Off;
        private int m_playerVehicle;

        private TrailFx m_trailLeft = new TrailFx(taillight_l);
        private TrailFx m_trailRight = new TrailFx(taillight_r);
        private TrailFx m_trailMiddle = new TrailFx(taillight_m);

        public TrailMode TrailMode => m_trailMode;
        public int PlayerIndex { get; private set; } = -1;
        public int PlayerVehicle
        {
            get => m_playerVehicle;
            set
            {
                if (m_playerVehicle == value)
                    return;

                if(DoesEntityExist(m_playerVehicle))
                    DecorRemove(m_playerVehicle, TrailScript.DecorName);

                m_playerVehicle = value;

                if (DoesEntityExist(m_playerVehicle))
                    DecorSetInt(m_playerVehicle, TrailScript.DecorName, (int)m_trailMode);
            }
        }

        public TrailVehicle(int targetPlayer)
        {
            PlayerIndex = targetPlayer;

            SetupTrailMode(m_trailMode);
        }

        private async Task SetupTrailMode(TrailMode trailMode)
        {
            await StopAll();

            m_trailMode = trailMode;

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

        public async Task SetTrailModeAsync(TrailMode trailMode)
        {
            if (m_trailMode == trailMode)
                return;

            await SetupTrailMode(trailMode);
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

        public async Task StopAll()
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

        public async Task UpdatePlayerVehicle()
        {
            if (!DoesEntityExist(m_playerVehicle))
            {
                return;
            }

            await UpdateVehiclePtfx(m_playerVehicle);
        }

        private bool IsPlayerBraking => GetEntitySpeedVector(m_playerVehicle, true).Y > 0.0f
            // Temporary solution until GetVehicleBrakePressure is added to FiveM https://github.com/E66666666/GTAVManualTransmission/blob/master/Gears/Memory/VehicleExtensions.cpp#L138-L145 
            && AreVehicleWheelsBraking();

        private bool AreVehicleWheelsBraking()
        {
            int numWheels = GetVehicleNumberOfWheels(m_playerVehicle);

            for (int i = 0; i < numWheels; i++)
            {
                if (!(GetVehicleWheelBrakePressure(m_playerVehicle, i) > 0.0f))
                    return false;
            }

            return true;
        }

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
        public async Task GetPlayerVehicle()
        {
            var playerPed = GetPlayerPed(PlayerIndex);
            
            if (!IsPedInAnyVehicle(playerPed, false))
            {
                PlayerVehicle = -1;
                await StopAll();
            }

            int vehicle = GetVehiclePedIsIn(playerPed, false);

            if (GetPedInVehicleSeat(vehicle, -1) != playerPed || IsEntityDead(vehicle))
            {
                PlayerVehicle = -1;
                await StopAll();
            }

            if (vehicle != m_playerVehicle)
            {
                await StopAll();
                PlayerVehicle = vehicle;
                await SetupTrailMode(m_trailMode);
            }
        }

        public override string ToString()
        {
            return $"PlayerIndex: {PlayerIndex}, Vehicle: {m_playerVehicle}, TrailMode: {m_trailMode}";
        }
    }
}
