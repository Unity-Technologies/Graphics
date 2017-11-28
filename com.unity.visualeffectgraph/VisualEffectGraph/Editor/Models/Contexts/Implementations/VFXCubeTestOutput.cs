using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXCubeTestOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Cube test Output"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXParticleCube"; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kParticleHexahedronOutput; } }

        [VFXSetting, SerializeField]
        bool useNormalMap = false;

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Front, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Side, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Up, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Angle, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Pivot, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (useNormalMap)
                yield return slotExpressions.First(o => o.name == "normalMap");
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (useNormalMap)
                    return base.inputProperties.Concat(PropertiesFromType("InputProperties"));
                else
                    return base.inputProperties;
            }
        }

        public class InputProperties
        {
            public Texture2D normalMap;
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (useNormalMap)
                    yield return "VFX_USE_NORMAL_MAP";
            }
        }


        /*protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (flipBook == FlipbookMode.Off)
                    yield return "frameInterpolationMode";
            }
        }*/
    }
}
