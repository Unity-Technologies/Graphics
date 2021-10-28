using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture2D))]
    class VFXSlotTexture2D : VFXSlotObject
    {
        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (value is Texture texture && texture.dimension != TextureDimension.Tex2D)
                manager.RegisterError("Slot_Value_Incorrect_Texture2D", VFXErrorType.Error, "This slot expects a Texture2D");

            base.GenerateErrors(manager);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture2DValue(0, mode);
        }
    }
}
