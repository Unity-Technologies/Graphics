using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    struct LightUnitUILevel
    {
        public LightUnitUILevel(Texture2D icon, string tooltip, Vector2 range)
        {
            this.content = new GUIContent(icon, tooltip);
            this.range = range;
        }

        public GUIContent content;
        public Vector2    range;
    }

    static class LightUnitValuesTable
    {
        public static readonly LightUnitUILevel[] k_LuxValueTable =
        {
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.lightUnitVeryBrightSun,   "Very Bright Sun",   new Vector2(80000, 120000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.lightUnitOvercastSky,     "Overcast Sky",      new Vector2(10000, 80000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.lightUnitSunriseOrSunset, "Sunrise or Sunset", new Vector2(1,     10000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.lightUnitMoonLight,       "Moon Light",        new Vector2(0,     1)),
        };

        public static readonly LightUnitUILevel[] k_LumenValueTable =
        {
        };

        public static readonly LightUnitUILevel[] k_CandelaValueTable =
        {
        };

        public static readonly LightUnitUILevel[] k_EV100ValueTable =
        {
        };

        public static readonly LightUnitUILevel[] k_KelvinValueTable =
        {
        };
    }

}
