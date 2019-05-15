using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace LightTrail
{
    public class Script : BaseScript
    {
        public const string dict = "core";
        public const string particleName = "veh_light_red_trail";
        public const string evolutionPropertyName = "speed";
        //public const string particleName = "veh_slipstream";
        //public const string evolutionPropertyName = "slipstream";

        public const string brakelight_l = "brakelight_l";
        public const string brakelight_r = "brakelight_r";
        public const string brakelight_m = "brakelight_m";

        public const string taillight_l = "taillight_l";
        public const string taillight_r = "taillight_r";
        public const string taillight_m = "taillight_m";

        public const string decorNameLeft = "light_trail_l";
        public const string decorNameRight = "light_trail_r";
        public const string decorNameMiddle = "light_trail_m";

        public Vector3 offset = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 color = new Vector3(1.0f, 0.0f, 0.0f);
        public float scale = 1.0f;
        public float alpha = 1.0f;

        #region Private Members

        private int ptfxHandle1;
        private int ptfxHandle2;
        private int ptfxHandle3;

        private int PlayerPed = -1;
        private int CurrentVehicle = -1;
        
        #endregion



        public bool PTFXShouldBeEnabled => CurrentVehicle != -1;
            //&& GetEntitySpeed(CurrentVehicle) != 0.0f
            //&& (IsControlJustPressed(1, (int)Control.VehicleBrake) || IsDisabledControlJustPressed(1, (int)Control.VehicleBrake));

        public Script()
        {
            // Register decorators
            DecorRegister(decorNameLeft, 3);
            DecorRegister(decorNameRight, 3);
            DecorRegister(decorNameMiddle, 3);

            Tick += Initialize;
        }

        public async Task Initialize()
        {
            Tick -= Initialize;

            RequestNamedPtfxAsset(dict);
            while (!HasNamedPtfxAssetLoaded(dict)) await Delay(0);

            Tick += GetCurrentVehicle;
            Tick += UpdateCurrentVehicle;
            Tick += UpdatePlayersVehicles;
        }

        public async Task UpdatePlayersVehicles()
        {
            for (int i = 0; i < 255; i++)
            {
                var ped = GetPlayerPed(i);

                if (IsPedInAnyVehicle(ped, false))
                {
                    int vehicle = GetVehiclePedIsIn(ped, false);

                    if (GetPedInVehicleSeat(vehicle, -1) == ped && !IsEntityDead(vehicle) && vehicle != CurrentVehicle)
                    {
                        SetupParticle(vehicle);
                    }
                }
            }

            await Task.FromResult(0);
        }

        public async Task UpdateCurrentVehicle()
        {
            if (PTFXShouldBeEnabled)
            {
                UseParticleFxAssetNextCall(dict);
            }

            await Task.FromResult(0);
        }

        public void SetupParticle(int entity)
        {
            UseParticleFxAssetNextCall(dict);
            var boneL = GetEntityBoneIndexByName(entity, brakelight_l) != -1 ? brakelight_l : taillight_l;
            var boneR = GetEntityBoneIndexByName(entity, brakelight_r) != -1 ? brakelight_r : taillight_r;
            var boneM = GetEntityBoneIndexByName(entity, brakelight_m) != -1 ? brakelight_m : taillight_m;

            AddSynchedParticleFxLooped(entity, decorNameLeft, boneL);
            AddSynchedParticleFxLooped(entity, decorNameRight, boneR);
            AddSynchedParticleFxLooped(entity, decorNameMiddle, boneM);

            // TODO: Implement onTick update
            // Use alpha or add/remove ptfx according on if the vehicle is braking
        }

        public void AddSynchedParticleFxLooped(int entity, string decorName, string boneName)
        {
            // If the decor exists
            if (DecorExistOn(entity, decorName))
            {
                // Get the decor (which holds the handle)
                var handle = DecorGetInt(entity, decorName);

                if (!DoesParticleFxLoopedExist(handle))
                {
                    StartParticleFx(ref handle, particleName, entity, boneName, offset, rotation, scale, color);
                }
            }
        }


        public void StartParticleFx(ref int handle, string ptfxName, int entity, string boneName, Vector3 offset, Vector3 rotation, float scale, Vector3 color)
        {
            // Get bone index
            int boneIndex = GetEntityBoneIndexByName(entity, boneName);

            // Be sure the bone exists on the entity
            if (boneIndex == -1)
                return;

            // Create the looped ptfx
            handle = StartParticleFxLoopedOnEntityBone(ptfxName, entity, offset.X, offset.Y, offset.Z, rotation.X, rotation.Y, rotation.Z, boneIndex, scale, false, false, false);
            SetParticleFxLoopedEvolution(handle, evolutionPropertyName, 1.0f, false);
            SetParticleFxLoopedColour(handle, color.X, color.Y, color.Z, false);
            SetParticleFxLoopedAlpha(handle, alpha);
        }

        /// <summary>
        /// Removes the ptfxs and the associated decors from the entity
        /// </summary>
        /// <param name="vehicle">The entity</param>
        public void ResetEntity(int vehicle)
        {
            RemoveSynchedParticleFxLooped(vehicle, decorNameLeft);
            RemoveSynchedParticleFxLooped(vehicle, decorNameRight);
            RemoveSynchedParticleFxLooped(vehicle, decorNameMiddle);
        }

        /// <summary>
        /// Removes the ptfx and the associated decor from the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="decorName"></param>
        public void RemoveSynchedParticleFxLooped(int entity, string decorName)
        {
            // If the decor exists
            if (DecorExistOn(entity, decorName))
            {
                // Get the decor (which holds the handle)
                var handle = DecorGetInt(entity, decorName);

                // If the ptfx exists
                if (DoesParticleFxLoopedExist(handle))
                    // Remove the ptfx
                    RemoveParticleFx(handle, false);

                // Remove the decor
                DecorRemove(entity, decorName);
            }
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

                        // Create PTFX
                        if (!DoesParticleFxLoopedExist(ptfxHandle1))
                        {
                            var boneLeft = GetEntityBoneIndexByName(vehicle, brakelight_l) != -1 ? brakelight_l : taillight_l;

                            StartParticleFx(ref ptfxHandle1, particleName, CurrentVehicle, boneLeft, offset, rotation, scale, color);
                            DecorSetInt(CurrentVehicle, decorNameLeft, 3);
                        }
                        if (!DoesParticleFxLoopedExist(ptfxHandle2))
                        {
                            var boneRight = GetEntityBoneIndexByName(vehicle, brakelight_r) != -1 ? brakelight_r : taillight_r;

                            StartParticleFx(ref ptfxHandle2, particleName, CurrentVehicle, boneRight, offset, rotation, scale, color);
                            DecorSetInt(CurrentVehicle, decorNameRight, 3);
                        }
                        if (!DoesParticleFxLoopedExist(ptfxHandle3))
                        {
                            var boneMiddle = GetEntityBoneIndexByName(vehicle, brakelight_m) != -1 ? brakelight_m : taillight_m;

                            StartParticleFx(ref ptfxHandle3, particleName, CurrentVehicle, boneMiddle, offset, rotation, scale, color);
                            DecorSetInt(CurrentVehicle, decorNameMiddle, 3);
                        }
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    CurrentVehicle = -1;
                    ResetEntity(CurrentVehicle);
                }
            }
            else
            {
                // If player isn't in any vehicle
                CurrentVehicle = -1;
                ResetEntity(CurrentVehicle);
            }

            await Task.FromResult(0);
        }
    }

}
