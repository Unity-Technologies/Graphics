using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXQuadOutput : VFXAbstractParticleOutput
    {
        [VFXSetting, SerializeField]
        protected FlipbookMode flipBook;

        //[VFXSetting] // tmp dont expose as settings atm
        public bool useGeometryShader = false;

        public enum FlipbookMode
        {
            Off,
            Flipbook,
            FlipbookBlend,
        }

        public override string name { get { return "Quad Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticleQuad"; } }
        public override VFXTaskType taskType { get { return useGeometryShader ? VFXTaskType.kParticlePointOutput : VFXTaskType.kParticleQuadOutput; } }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if (flipBook != FlipbookMode.Off)
                {
                    yield return "USE_FLIPBOOK";
                    if (flipBook == FlipbookMode.FlipbookBlend)
                        yield return "USE_FLIPBOOK_INTERPOLATION";
                }

                if (useGeometryShader)
                    yield return "USE_GEOMETRY_SHADER";
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
                yield return new VFXAttributeInfo(VFXAttribute.Pivot, VFXAttributeMode.Read);

                foreach (var size in VFXBlockUtility.GetReadableSizeAttributes(GetData()))
                    yield return size;

                if (flipBook != FlipbookMode.Off)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "texture");

            if (flipBook != FlipbookMode.Off)
            {
                var flipBookSizeExp = slotExpressions.First(o => o.name == "flipBookSize");
                yield return flipBookSizeExp;
                yield return new VFXNamedExpression(VFXValue.Constant(Vector2.one) / flipBookSizeExp.exp, "invFlipBookSize");
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                string inputPropertiesType = "InputProperties";
                if (flipBook != FlipbookMode.Off) inputPropertiesType = "InputPropertiesFlipbook";

                foreach (var property in PropertiesFromType(inputPropertiesType))
                    yield return property;

                foreach (var property in base.inputProperties)
                    yield return property;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (flipBook == FlipbookMode.Off)
                    yield return "frameInterpolationMode";
            }
        }

        public class InputProperties
        {
            public Texture2D texture;
        }

        public class InputPropertiesFlipbook
        {
            public Texture2D texture;
            public Vector2 flipBookSize = new Vector2(5, 5);
        }
    }
}
