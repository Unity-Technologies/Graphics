using System.Collections.Generic;
using System.Linq;

using UnityEngine;


namespace UnityEditor.VFX
{
    [VFXHelpURL("Context-OutputPrimitive")]
    [VFXInfo(variantProvider = typeof(VFXPlanarPrimitiveVariantProvider))]
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
                if (shaderName != string.Empty)
                    return $"Output Particle {shaderName} {primitiveType.ToString()}";
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

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
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
