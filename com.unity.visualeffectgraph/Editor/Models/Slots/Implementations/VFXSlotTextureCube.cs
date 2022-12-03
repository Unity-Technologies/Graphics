using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Cubemap))]
    class VFXSlotTextureCube : VFXSlotObject
    {
        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (value is Texture texture && texture.dimension != TextureDimension.Cube)
                manager.RegisterError("Slot_Value_Incorrect_TextureCube", VFXErrorType.Error, "This slot expects a Cubemap");

            base.GenerateErrors(manager);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTextureCubeValue(0, mode);
        }
    }
}
