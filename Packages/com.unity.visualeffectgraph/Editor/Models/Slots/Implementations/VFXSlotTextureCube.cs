using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlotObject
    {
        internal override void GenerateErrors(VFXErrorReporter report)
        {
            if (value is Texture texture && texture != null && texture.dimension != TextureDimension.Cube)
                report.RegisterError("Slot_Value_Incorrect_TextureCube", VFXErrorType.Error, $"The selected texture {(string.IsNullOrEmpty(this.property.name) ? "" : $"'{this.property.name}' ")}is not a Cubemap texture", this.owner as VFXModel);

            base.GenerateErrors(report);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTextureCubeValue(0, mode);
        }
    }
}
