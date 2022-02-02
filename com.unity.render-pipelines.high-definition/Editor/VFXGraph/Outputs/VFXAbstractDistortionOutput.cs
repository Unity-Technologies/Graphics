using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace UnityEditor.VFX.HDRP
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

        public override sealed bool CanBeCompiled()
        {
            return (VFXLibrary.currentSRPBinder is VFXHDRPBinder) && base.CanBeCompiled();
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
                foreach (var prop in properties)
                    yield return prop;

                switch (distortionMode)
                {
                    case DistortionMode.ScreenSpace:
                        yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "distortionBlurMap", new TooltipAttribute("Distortion Map: RG for Distortion (centered on .5 gray), B for Blur Mask.")), (usesFlipbook ? null : VFXResources.defaultResources.noiseTexture));
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(Vector2), "distortionScale", new TooltipAttribute("Screen-Space Distortion Scale")), Vector2.one);
                        break;
                    case DistortionMode.NormalBased:
                        yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "normalMap", new TooltipAttribute("Normal Map")));
                        yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "smoothnessMap", new TooltipAttribute("Smoothness Map (Alpha)")));
                        yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "alphaMask", new TooltipAttribute("Alpha Mask (Alpha)")));
                        yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "distortionScale", new TooltipAttribute("World-space Distortion Scale")));
                        break;
                }
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(float), "blurScale", new TooltipAttribute("Distortion Blur Scale")));
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

                switch (distortionMode)
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
