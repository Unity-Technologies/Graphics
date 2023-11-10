using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXHelpURL("Block-SubpixelAntiAliasing")]
    [VFXInfo(name = "Subpixel Anti-Aliasing", category = "Output")]
    class SubpixelAA : VFXBlock
    {
        public override string name { get { return "Subpixel Anti-Aliasing"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.Output; } }
        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                return @"
float2 localSize = size * float2(scaleX, scaleY);
float clipPosW = TransformPositionVFXToClip(position).w;
float minSize = clipPosW / (0.5f * min(abs(UNITY_MATRIX_P[0][0]) * _ScreenParams.x, abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y)); // max size in one pixel
float2 clampedSize = max(localSize,minSize);
float fade = (localSize.x * localSize.y) / (clampedSize.x * clampedSize.y);
alpha *= fade;
localSize = clampedSize;
scaleX = localSize.x / size;
scaleY = localSize.y / size;";
            }
        }
    }
}
