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

        public static LightUnitUILevel CautionLevel(string tooltip, float value) => new LightUnitUILevel
        {
            // Load the buildin caution icon with provided tooltip.
            content = new GUIContent( EditorGUIUtility.IconContent("console.warnicon.sml").image, tooltip),
            range = new Vector2(-1, value)
        };

        public GUIContent content;
        public Vector2    range;
    }

    static class LightUnitValuesTable
    {
        //TODO: Caution string
        public static readonly LightUnitUILevel[] k_LuxValueTable =
        {
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconBrightSky,     "Very Bright Sun",   new Vector2(80000, 120000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconOvercastSky,   "Overcast Sky",      new Vector2(10000, 80000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconSunriseSunset, "Sunrise or Sunset", new Vector2(1,     10000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconMoonlitSky,    "Moon Light",        new Vector2(0,     1)),
        };

        public static readonly LightUnitUILevel[] k_LumenValueTable =
        {
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconExterior,   "Exterior",   new Vector2(3000, 40000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconInterior,   "Interior",   new Vector2(300,  3000)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconDecorative, "Decorative", new Vector2(15,   300)),
            new LightUnitUILevel(HDRenderPipeline.defaultAsset.renderPipelineEditorResources.textures.iconCandle,     "Candle",     new Vector2(0,    15)),
        };

        public static readonly LightUnitUILevel[] k_CandelaValueTable =
        {
            // TODO
        };

        public static readonly LightUnitUILevel[] k_EV100ValueTable =
        {
            // TODO
        };

        public static readonly LightUnitUILevel[] k_KelvinValueTable =
        {
            // TODO
        };
    }

}
