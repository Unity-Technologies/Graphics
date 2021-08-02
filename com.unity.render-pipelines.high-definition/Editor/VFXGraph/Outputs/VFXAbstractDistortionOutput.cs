using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractDistortionOutput : VFXAbstractParticleOutput
    {
        public VFXAbstractDistortionOutput(bool strip = false) : base(strip) { }

        public enum DistortionMode
        {
            ScreenSpace,
            NormalBased
        }

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.All), Tooltip("How the distortion is handled")]
        protected DistortionMode distortionMode = DistortionMode.ScreenSpace;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.All), Tooltip("Whether Distortion scales with the distance")]
        protected bool scaleByDistance = true;

        public class InputPropertiesDistortionScreenSpace
        {
            [Tooltip("Distortion Map: RG for Distortion (centered on .5 gray), B for Blur Mask.")]
            public Texture2D distortionBlurMap = null;
            [Tooltip("Screen-Space Distortion Scale")]
            public Vector2 distortionScale = Vector2.one;
        }

        public class InputPropertiesDistortionNormalBased
        {
            [Tooltip("Normal Map")]
            public Texture2D normalMap = null;
            [Tooltip("Smoothness Map (Alpha)")]
            public Texture2D smoothnessMap = null;
            [Tooltip("Alpha Mask (Alpha)")]
            public Texture2D alphaMask = null;
            [Tooltip("World-space Distortion Scale")]
            public float distortionScale = 1.0f;
        }

        public class InputPropertiesCommon
        {
            [Tooltip("Distortion Blur Scale")]
            public float blurScale = 1.0f;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "colorMapping";
                yield return "blendMode";
                yield return "castShadows";
                yield return "sort";
                yield return "useAlphaClipping";
                yield return "excludeFromTAA";
            }
        }

        public override void OnEnable()
        {
            blendMode = BlendMode.Additive;
            base.OnEnable();
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;

                switch (distortionMode)
                {
                    case DistortionMode.ScreenSpace:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesDistortionScreenSpace"));
                        break;
                    case DistortionMode.NormalBased:
                        properties = properties.Concat(PropertiesFromType("InputPropertiesDistortionNormalBased"));
                        break;
                }

                return properties.Concat(PropertiesFromType("InputPropertiesCommon"));
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            switch (distortionMode)
            {
                case DistortionMode.ScreenSpace:
                    yield return slotExpressions.First(o => o.name == "distortionBlurMap");
                    break;
                case DistortionMode.NormalBased:
                    yield return slotExpressions.First(o => o.name == "normalMap");
                    yield return slotExpressions.First(o => o.name == "smoothnessMap");
                    yield return slotExpressions.First(o => o.name == "alphaMask");

                    break;
            }
            yield return slotExpressions.First(o => o.name == "distortionScale");
            yield return slotExpressions.First(o => o.name == "blurScale");
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXOutputRenderState}", renderState);

                var shaderTags = new VFXShaderWriter();
                shaderTags.Write("Tags { \"Queue\"=\"Transparent\" \"IgnoreProjector\"=\"True\" \"RenderType\"=\"Transparent\" }");

                yield return new KeyValuePair<string, VFXShaderWriter>("${VFXShaderTags}", shaderTags);

                foreach (var additionnalStencilReplacement in subOutput.GetStencilStateOverridesStr())
                {
                    yield return additionnalStencilReplacement;
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var define in base.additionalDefines)
                    yield return define;

                switch(distortionMode)
                {
                    case DistortionMode.ScreenSpace:
                        yield return "DISTORTION_SCREENSPACE";

                        break;
                    case DistortionMode.NormalBased:
                        yield return "DISTORTION_NORMALBASED";
                        break;
                }

                if (scaleByDistance)
                    yield return "DISTORTION_SCALE_BY_DISTANCE";
            }
        }

        protected override void WriteBlendMode(VFXShaderWriter writer)
        {
            writer.WriteLine("Blend One One");
        }

        protected override bool needsExposureWeight { get { return false; } }
    }
}
