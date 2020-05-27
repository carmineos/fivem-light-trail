using CitizenFX.Core;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace LightTrail
{
    public enum TrailMode
    {
        Off,
        On,
        Brake,
    }

    public enum TrailStatus
    {
        Empty,
        FadingIn,
        FadingOut,
        Full
    }

    public class TrailFx
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
                Handle = StartParticleFxLoopedOnEntityBone("veh_light_red_trail", entity, Offset.X, Offset.Y, Offset.Z, Rotation.X, Rotation.Y, Rotation.Z, boneIndex, Scale, true, true, true);
                SetParticleFxLoopedEvolution(Handle, "speed", Evolution, false);
                SetParticleFxLoopedColour(Handle, Color.X, Color.Y, Color.Z, false);
                SetParticleFxLoopedAlpha(Handle, Alpha);
            }
        }

        public void Stop()
        {
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

        public void FadeIn()
        {
            Alpha += (2.5f * GetFrameTime());
            Scale = Alpha;
            Evolution = Alpha;
        }

        public void FadeOut()
        {
            Alpha -= (2.5f * GetFrameTime());
            Scale = Alpha;
            Evolution = Alpha;
        }

        public async Task LoopBrakeMode()
        {
            if (!Enabled || !DoesParticleFxLoopedExist(Handle))
                return;

            switch (Status)
            {
                case TrailStatus.Empty:
                    Off();
                    Stop();
                    break;

                case TrailStatus.Full:
                    On();
                    Update();
                    break;

                case TrailStatus.FadingIn:
                    FadeIn();
                    Update();
                    break;

                case TrailStatus.FadingOut:
                    FadeOut();
                    Update();
                    break;
            }
            await Task.FromResult(0);
        }

        public bool FadeOutFinished => Alpha <= 0.0f && Scale <= 0.0f;

        public bool FadeInFinished => Alpha >= 1.0f && Scale >= 1.0f;
    }
}
