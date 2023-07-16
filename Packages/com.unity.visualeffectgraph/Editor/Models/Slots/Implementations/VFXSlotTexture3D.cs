using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlotObject
    {
        internal override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (value is Texture texture && texture.dimension != TextureDimension.Tex3D)
                manager.RegisterError("Slot_Value_Incorrect_Texture3D", VFXErrorType.Error, "This slot expects a Texture3D");

            base.GenerateErrors(manager);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture3DValue(0, mode);
        }
    }
}
