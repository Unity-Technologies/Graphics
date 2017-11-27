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

        /*  protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
          {
              foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                  yield return exp;

              yield return slotExpressions.First(o => o.name == "fresnelColor");
              yield return slotExpressions.First(o => o.name == "fresnelFactor");
          }*/


        /*protected override IEnumerable<VFXPropertyWithValue> inputProperties
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
        }*/


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
