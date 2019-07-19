using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    //[VFXInfo] // TODO Put back InInsepctor visibility once C++ PR for strips has landed
    class VFXQuadStripOutput : VFXAbstractParticleOutput
    {
        [VFXSetting, SerializeField]
        protected StripTilingMode tilingMode = StripTilingMode.Stretch; 

        protected VFXQuadStripOutput() : base(true) { }
        public override string name { get { return "Quad Strip Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePlanarPrimitive"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }

        public override bool supportsUV { get { return true; } }

        public class InputProperties
        {
            public Texture2D mainTexture = VFXResources.defaultResources.particleTexture;
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "mainTexture");
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Write);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if (tilingMode == StripTilingMode.Stretch)
                    yield return "VFX_STRIPS_UV_STRECHED";

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(VFXPrimitiveType.Quad);
            }
        }
    }
}
