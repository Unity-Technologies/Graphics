using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterDeformerPresets
    {
        static internal void ApplyWaterSphereDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.amplitude = 1.0f;
            waterDeformer.regionSize = new Vector2(10.0f, 10.0f);
        }

        static internal void ApplyWaterBoxDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.regionSize = new Vector2(5.0f, 5.0f);
            waterDeformer.boxBlend = new Vector2(2.0f, 1.0f);
            waterDeformer.amplitude = -1f;
            waterDeformer.cubicBlend = true;
        }

        static internal void ApplyWaterShoreWaveDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.regionSize = new Vector2(40.0f, 20.0f);
            waterDeformer.waveLength = 3.5f;
            waterDeformer.amplitude = 3.5f;
            waterDeformer.waveRepetition = 6;
            waterDeformer.waveSpeed = 12.0f;
            waterDeformer.waveBlend = new Vector2(0.45f, 0.5f);
            waterDeformer.breakingRange = new Vector2(0.4f, 0.8f);
            waterDeformer.deepFoamRange = new Vector2(0.3f, 0.7f);
            waterDeformer.waveOffset = 0.0f;
            waterDeformer.surfaceFoamDimmer = 1.0f;
            waterDeformer.deepFoamDimmer = 1.0f;
        }

        static internal void ApplyWaterBowWaveDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.regionSize = new Vector2(3.5f, 10.0f);
            waterDeformer.amplitude = -0.5f;
            waterDeformer.bowWaveElevation = 0.2f;
        }

        static internal void ApplyWaterTextureDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.regionSize = new Vector2(10f, 10.0f);
            waterDeformer.amplitude = 0.5f;
            waterDeformer.texture = null;
            waterDeformer.range = new Vector2(0.0f, 1.0f);
        }

        static internal void ApplyWaterMaterialDeformerPreset(WaterDeformer waterDeformer)
        {
            waterDeformer.regionSize = new Vector2(10f, 10.0f);
            waterDeformer.resolution = new Vector2Int(256, 256);
            waterDeformer.updateMode = CustomRenderTextureUpdateMode.OnLoad;
            waterDeformer.amplitude = 1.0f;
            waterDeformer.material = null;
        }
    }
}
