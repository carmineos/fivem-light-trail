using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace LightTrail
{
    public class Script : BaseScript
    {
        //public string dict = "core";
        //public string particleName = "veh_light_red_trail";

        public string dict = "scr_minigametennis";
        public string particleName = "scr_tennis_ball_trail";
        public string boneName1 = "brakelight_l";
        public string boneName2 = "brakelight_r";

        public int ptfxHandle1;
        public int ptfxHandle2;

        public int PlayerPed = -1;
        public int CurrentVehicle = -1;
        public Vector3 offset = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 color = new Vector3(1.0f, 0.0f, 0.0f);
        public float scale = 0.5f;

        public bool EnablePTFX => CurrentVehicle != -1;
            //&& GetEntitySpeed(CurrentVehicle) != 0.0f
            //&& (IsControlJustPressed(1, (int)Control.VehicleBrake) || IsDisabledControlJustPressed(1, (int)Control.VehicleBrake));

        public Script()
        {
            Tick += Initialize;
        }

        public async Task Initialize()
        {
            Tick -= Initialize;

            RequestNamedPtfxAsset(dict);
            while (!HasNamedPtfxAssetLoaded(dict)) await Delay(0);

            Tick += GetCurrentVehicle;
            Tick += Loop;
        }

        public async Task Loop()
        {
            if(EnablePTFX)
            {
                UseParticleFxAssetNextCall(dict);

                if (!DoesParticleFxLoopedExist(ptfxHandle1))
                {
                    StartParticleFx(ref ptfxHandle1, particleName, CurrentVehicle, boneName1, offset, rotation, scale, color);
                }

                if (!DoesParticleFxLoopedExist(ptfxHandle2))
                {
                    StartParticleFx(ref ptfxHandle2, particleName, CurrentVehicle, boneName2, offset, rotation, scale, color);
                }
            }

            await Task.FromResult(0);
        }

        public void StartParticleFx(ref int handle, string ptfxName, int entity, string boneName, Vector3 offset, Vector3 rotation, float scale, Vector3 color)
        {
            int boneIndex = GetEntityBoneIndexByName(entity, boneName);
            //Vector3 bonePosition = GetWorldPositionOfEntityBone(entity, boneIndex);

            handle = StartParticleFxLoopedOnEntityBone(ptfxName, entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, scale, false, false, false);
            SetParticleFxLoopedColour(handle, color.X, color.Y, color.Z, false);
        }

        public void Reset()
        {
            if (DoesParticleFxLoopedExist(ptfxHandle1)) RemoveParticleFx(ptfxHandle1, false);
            if (DoesParticleFxLoopedExist(ptfxHandle2)) RemoveParticleFx(ptfxHandle2, false);
        }

        /// <summary>
        /// Updates the <see cref="CurrentVehicle"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetCurrentVehicle()
        {
            PlayerPed = PlayerPedId();

            if (IsPedInAnyVehicle(PlayerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(PlayerPed, false);

                if (GetPedInVehicleSeat(vehicle, -1) == PlayerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != CurrentVehicle)
                    {
                        CurrentVehicle = vehicle;
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    CurrentVehicle = -1;
                    Reset();
                }
            }
            else
            {
                // If player isn't in any vehicle
                CurrentVehicle = -1;
                Reset();
            }

            await Task.FromResult(0);
        }
    }
}
