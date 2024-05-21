using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace UnityEditor.VFX.HDRP
{
    internal class VFXDistortionPlanarPrimitiveOutputSubvariantProvider : VariantProvider
    {
        private readonly VFXPrimitiveType mainPrimitiveType;

        public VFXDistortionPlanarPrimitiveOutputSubvariantProvider(VFXPrimitiveType type)
        {
            mainPrimitiveType = type;
        }

        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var primitive in Enum.GetValues(typeof(VFXPrimitiveType)).Cast<VFXPrimitiveType>())
            {
                if (primitive == mainPrimitiveType)
                    continue;

                yield return new Variant(
                    "Output Particle|HDRP Distortion".AppendLabel(primitive.ToString()),
                    VFXLibraryStringHelper.Separator("Output Advanced", 4),
                    typeof(VFXDistortionPlanarPrimitiveOutput),
                    new[] {new KeyValuePair<string, object>("primitiveType", primitive)}
                );
            }
        }
    }

    internal class VFXDistortionPlanarPrimitiveOutputProvider : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            yield return new Variant(
                "Output Particle|HDRP Distortion".AppendLabel(VFXPrimitiveType.Quad.ToString()),
                VFXLibraryStringHelper.Separator("Output Advanced", 4),
                typeof(VFXDistortionPlanarPrimitiveOutput),
                new[] {new KeyValuePair<string, object>("primitiveType", VFXPrimitiveType.Quad)},
                () => new VFXDistortionPlanarPrimitiveOutputSubvariantProvider(VFXPrimitiveType.Quad));
        }
    }

    [VFXInfo(variantProvider = typeof(VFXDistortionPlanarPrimitiveOutputProvider))]
    class VFXDistortionPlanarPrimitiveOutput : VFXAbstractDistortionOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VFXPrimitiveType primitiveType = VFXPrimitiveType.Quad;

        //[VFXSetting] // tmp dont expose as settings atm
        public bool useGeometryShader = false;

        public override string name => "Output Particle".AppendLabel("HDRP Distortion", false) + $"\n{primitiveType.ToString()}";
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleDistortionPlanarPrimitive"); } }
        public override VFXTaskType taskType
        {
            get
            {
                if (useGeometryShader)
                    return VFXTaskType.ParticlePointOutput;

                return VFXPlanarPrimitiveHelper.GetTaskType(primitiveType);
            }
        }
        public override bool supportsUV { get { return true; } }

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

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                foreach (var property in properties)
                    yield return property;

                if (primitiveType == VFXPrimitiveType.Octagon)
                    foreach (var property in PropertiesFromType(typeof(VFXPlanarPrimitiveHelper.OctagonInputProperties)))
                        yield return property;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (primitiveType == VFXPrimitiveType.Octagon)
                yield return slotExpressions.First(o => o.name == "cropFactor");
        }
    }
}
