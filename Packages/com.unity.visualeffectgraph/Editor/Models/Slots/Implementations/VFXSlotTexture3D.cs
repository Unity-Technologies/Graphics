using UnityEngine;
using UnityEngine.Rendering;


namespace UnityEditor.VFX
{
    [VFXInfo(type = typeof(Texture3D))]
    class VFXSlotTexture3D : VFXSlotObject
    {
        internal override void GenerateErrors(VFXErrorReporter report)
        {
            if (value is Texture texture && texture != null && texture.dimension != TextureDimension.Tex3D)
                report.RegisterError("Slot_Value_Incorrect_Texture3D", VFXErrorType.Warning, $"The selected texture {(string.IsNullOrEmpty(this.property.name) ? "" : $"'{this.property.name}' ")}is not a 3D texture", this.owner as VFXModel);

            base.GenerateErrors(report);
        }

        public override VFXValue DefaultExpression(VFXValue.Mode mode)
        {
            return new VFXTexture3DValue(0, mode);
        }
    }
}
