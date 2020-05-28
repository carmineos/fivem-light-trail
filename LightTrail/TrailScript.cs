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
        TrailVehicle localVehicle;
        List<TrailVehicle> remoteVehicles = new List<TrailVehicle>();

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
                await localVehicle.SetTrailMode(trailMode, true);

            }), false);

            Tick += Update;
        }

        List<TrailVehicle> queuedToRemove = new List<TrailVehicle>();

        int lastUpdateTime = 0;

        void UpdateRemotePlayers()
        {
            if (GetGameTimer() - lastUpdateTime > 100)
            {
                var remotePlayers = GetActivePlayers();

                foreach (var p in remotePlayers)
                {
                    if (!remoteVehicles.Any(v => v.PlayerId == p))
                    {
                        remoteVehicles.Add(new TrailVehicle(p));
                    }
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
            foreach (var veh in remoteVehicles)
            {
                if (!veh.IsValidPlayer)
                {
                    queuedToRemove.Add(veh);
                }
                else
                {
                    await veh.Update();
                }
            }

            // remove for disconnected players
            if (queuedToRemove.Count != 0)
            {
                foreach (var q in queuedToRemove)
                {
                    await q.StopAll();
                    remoteVehicles.Remove(q);
                }
                queuedToRemove.Clear();
            }
        }
    }
}
