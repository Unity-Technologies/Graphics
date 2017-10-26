using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.VFX;
using System;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Output")]
    class SubpixelAA : VFXBlock
    {
        public override string name { get { return "Subpixel Anti-Aliasing"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdateAndOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                string outSource = @"
#ifdef VFX_WORLD_SPACE
float clipPosW = mul(UNITY_MATRIX_VP,float4(position,1.0f)).w;
#else
float clipPosW = mul(UNITY_MATRIX_MVP,float4(position,1.0f)).w;
#endif

float minSize = clipPosW / (0.5f * min(UNITY_MATRIX_P[0][0] * _ScreenParams.x,-UNITY_MATRIX_P[1][1] * _ScreenParams.y)); // max size in one pixel
float2 clampedSize = max(size,minSize);
float fade = (size.x * size.y) / (clampedSize.x * clampedSize.y);
alpha *= fade;
size = clampedSize;";

                return outSource;
            }
        }
    }
}
