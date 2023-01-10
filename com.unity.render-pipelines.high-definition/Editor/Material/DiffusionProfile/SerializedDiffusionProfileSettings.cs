using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal sealed class SerializedDiffusionProfileSettings : IDisposable
    {
        internal DiffusionProfileSettings settings;
        internal DiffusionProfile objReference;

        internal SerializedProperty scatteringDistance;
        internal SerializedProperty scatteringDistanceMultiplier;
        internal SerializedProperty transmissionTint;
        internal SerializedProperty texturingMode;
        internal SerializedProperty smoothnessMultipliers;
        internal SerializedProperty lobeMix;
        internal SerializedProperty diffusePower;
        internal SerializedProperty transmissionMode;
        internal SerializedProperty thicknessRemap;
        internal SerializedProperty worldScale;
        internal SerializedProperty ior;

        // Render preview
        internal readonly RenderTexture profileRT;
        internal readonly RenderTexture transmittanceRT;

        internal SerializedDiffusionProfileSettings(DiffusionProfileSettings settings,
            SerializedObject serializedObject)
        {
            var serializedProfile =
                (new PropertyFetcher<DiffusionProfileSettings>(serializedObject).Find(x => x.profile));
            var rp = new RelativePropertyFetcher<DiffusionProfile>(serializedProfile);

            profileRT = new RenderTexture(256, 256, 0, GraphicsFormat.R16G16B16A16_SFloat);
            transmittanceRT = new RenderTexture(16, 256, 0, GraphicsFormat.R16G16B16A16_SFloat);

            this.settings = settings;
            objReference = settings.profile;

            scatteringDistance = rp.Find(x => x.scatteringDistance);
            scatteringDistanceMultiplier = rp.Find(x => x.scatteringDistanceMultiplier);
            transmissionTint = rp.Find(x => x.transmissionTint);
            texturingMode = rp.Find(x => x.texturingMode);
            smoothnessMultipliers = rp.Find(x => x.smoothnessMultipliers);
            lobeMix = rp.Find(x => x.lobeMix);
            diffusePower = rp.Find(x => x.diffuseShadingPower);
            transmissionMode = rp.Find(x => x.transmissionMode);
            thicknessRemap = rp.Find(x => x.thicknessRemap);
            worldScale = rp.Find(x => x.worldScale);
            ior = rp.Find(x => x.ior);
        }

        internal void Dispose() => ((IDisposable)this).Dispose();

        void IDisposable.Dispose()
        {
            CoreUtils.Destroy(profileRT);
            CoreUtils.Destroy(transmittanceRT);
        }
    }
}
