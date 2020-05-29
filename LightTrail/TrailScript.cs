using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using CitizenFX.Core;

namespace LightTrail
{
    class TrailScript : BaseScript
    {
        public const string DecorName = "_trail_mode";
        
        TrailVehicle localVehicle;
        Dictionary<int, TrailVehicle> remoteVehicles = new Dictionary<int, TrailVehicle>();

        public TrailScript()
        {
            RegisterCommand("trail_mode", new Action<int, dynamic>(async (source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"LightTrail: Missing argument off|on|brake");
                    return;
                }

                if (!Enum.TryParse<TrailMode>(args[0], true, out TrailMode trailMode))
                {
                    Debug.WriteLine($"LightTrail: Error parsing {args[0]}");
                    return;
                }

                Debug.WriteLine($"LightTrail: Switched trail mode to {trailMode}");
                await localVehicle.SetTrailModeAsync(trailMode);
                DecorSetInt(localVehicle.PlayerVehicle, DecorName, (int)trailMode);

            }), false);

            Tick += Update;
        }

        int lastUpdateTime = 0;

        void UpdateRemotePlayers()
        {
            if (GetGameTimer() - lastUpdateTime > 100)
            {
                var remotePlayers = GetActivePlayers();

                foreach (var player in remotePlayers)
                {
                    if (player == GetPlayerIndex())
                        continue;

                    if(!remoteVehicles.ContainsKey(player))
                        remoteVehicles[player] = new TrailVehicle(player);
                }

                lastUpdateTime = GetGameTimer();
            }
        }

        async Task Update()
        {
            // set up local player trails
            if (localVehicle == null)
            {
                localVehicle = new TrailVehicle(PlayerId());
            }

            // update local vehicle
            await localVehicle.Update();

            // set up remote player trails
            UpdateRemotePlayers();

            // update remote vehicles
            foreach (var player in remoteVehicles.Keys)
            {
                var trail = remoteVehicles[player];

                // If the player is not valid
                if (player == -1 || !NetworkIsPlayerActive(player))
                {
                    await trail.StopAll();
                    remoteVehicles.Remove(player);
                    continue;
                }

                // If there is no decor on the vehicle
                if(!DecorExistOn(trail.PlayerVehicle, DecorName))
                {
                    await trail.StopAll();
                    remoteVehicles.Remove(player);
                    continue;
                }

                var decorTrailMode = (TrailMode)DecorGetInt(trail.PlayerVehicle, DecorName);

                // If the decor is set to Off
                if(decorTrailMode == TrailMode.Off)
                {
                    await trail.StopAll();
                    remoteVehicles.Remove(player);
                    continue;
                }

                await trail.SetTrailModeAsync(decorTrailMode);
                await trail.Update();
            }
        }
    }
}
