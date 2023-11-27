using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


namespace UnityEditor.VFX
{
    class VFXPlanarPrimitiveOutputProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var primitive in Enum.GetValues(typeof(VFXPrimitiveType)))
            {
                yield return new Variant(
                    $"Output Particle {primitive}",
                    "Output",
                    typeof(VFXPlanarPrimitiveOutput),
                    new[] {new KeyValuePair<string, object>("primitiveType", primitive)});
            }
        }
    }

    [VFXHelpURL("Context-OutputPrimitive")]
    [VFXInfo(variantProvider = typeof(VFXPlanarPrimitiveOutputProvider))]
    class VFXPlanarPrimitiveOutput : VFXShaderGraphParticleOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies what primitive type to use for this output. Triangle outputs have fewer vertices, octagons can be used to conform the geometry closer to the texture to avoid overdraw, and quads are a good middle ground.")]
        protected VFXPrimitiveType primitiveType = VFXPrimitiveType.Quad;

        //[VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public bool useGeometryShader = false;

        public override string name
        {
            get
            {
                return $"Output Particle {primitiveType.ToString()}";
            }
        }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePlanarPrimitive"); } }
        public override VFXTaskType taskType
        {
            get
            {
                if (useGeometryShader)
                    return VFXTaskType.ParticlePointOutput;

                return VFXPlanarPrimitiveHelper.GetTaskType(primitiveType);
            }
        }
        public override bool supportsUV { get { return GetOrRefreshShaderGraphObject() == null; } }
        public override bool implementsMotionVector { get { return true; } }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if (useGeometryShader)
                    yield return "USE_GEOMETRY_SHADER";

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(primitiveType);
            }
        }

        protected IEnumerable<VFXPropertyWithValue> optionalInputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(GetFlipbookType(), "mainTexture", new TooltipAttribute("Specifies the base color (RGB) and opacity (A) of the particle.")), (usesFlipbook ? null : VFXResources.defaultResources.particleTexture));
            }
        }
        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                if (GetOrRefreshShaderGraphObject() == null)
                    properties = properties.Concat(optionalInputProperties);

                if (primitiveType == VFXPrimitiveType.Octagon)
                    properties = properties.Concat(PropertiesFromType(typeof(VFXPlanarPrimitiveHelper.OctagonInputProperties)));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;
            if (GetOrRefreshShaderGraphObject() == null)
            {
                yield return slotExpressions.First(o => o.name == "mainTexture");
            }
            if (primitiveType == VFXPrimitiveType.Octagon)
                yield return slotExpressions.First(o => o.name == "cropFactor");
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return "primitiveType";
            }
        }
    }
}
