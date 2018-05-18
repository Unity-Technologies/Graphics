using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Size")]
    class ScreenSpaceSize : VFXBlock
    {
        public enum SizeMode
        {
            PixelAbsolute,
            PixelRelativeToResolution,
            RatioRelativeToHeight,
            RatioRelativeToWidth,
            RatioRelativeToHeightAndWidth
        }

        public class InputPropertiesAbsolute
        {
            public float PixelSize = 16.0f;
        }

        public class InputPropertiesRelative
        {
            public float RelativeSize = 0.1f;
        }

        public class InputPropertiesResolution
        {
            public Vector2 ReferenceResolution = new Vector2(1920, 1080);
        }

        [SerializeField, VFXSetting]
        protected SizeMode sizeMode = SizeMode.PixelAbsolute;

        public override string name { get { return "Screen Space Size"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kOutput; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                switch (sizeMode)
                {
                    case SizeMode.PixelAbsolute: return PropertiesFromType("InputPropertiesAbsolute");
                    case SizeMode.PixelRelativeToResolution: return PropertiesFromType("InputPropertiesResolution").Concat(PropertiesFromType("InputPropertiesAbsolute"));
                    case SizeMode.RatioRelativeToHeight:
                    case SizeMode.RatioRelativeToHeightAndWidth:
                    case SizeMode.RatioRelativeToWidth:
                        return PropertiesFromType("InputPropertiesRelative");
                    default:
                        throw new NotImplementedException(string.Format("Not Implemented SizeMode: {0}", sizeMode));
                }
            }
        }


        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.SizeX, VFXAttributeMode.ReadWrite);

                if (GetData().IsCurrentAttributeWritten(VFXAttribute.SizeY) || sizeMode == SizeMode.RatioRelativeToHeightAndWidth || sizeMode == SizeMode.PixelRelativeToResolution)
                    yield return new VFXAttributeInfo(VFXAttribute.SizeY, VFXAttributeMode.ReadWrite);
            }
        }

        public override string source
        {
            get
            {
                string sizeString = string.Empty;
                switch (sizeMode)
                {
                    case SizeMode.PixelAbsolute:                          sizeString = "float2(PixelSize, PixelSize)";                                            break;
                    case SizeMode.PixelRelativeToResolution:              sizeString = "float2(PixelSize, PixelSize) * (_ScreenParams.xy/ReferenceResolution)";   break;
                    case SizeMode.RatioRelativeToWidth:                   sizeString = "float2(_ScreenParams.x, _ScreenParams.x) * RelativeSize";                 break;
                    case SizeMode.RatioRelativeToHeight:                  sizeString = "float2(_ScreenParams.y, _ScreenParams.y) * RelativeSize";                 break;
                    case SizeMode.RatioRelativeToHeightAndWidth:          sizeString = "float2(_ScreenParams.x, _ScreenParams.y) * RelativeSize";                 break;
                    default: throw new NotImplementedException(string.Format("Not Implemented SizeMode: {0}", sizeMode));
                }

                return string.Format(@"
float2 size = {0};
float clipPosW = TransformPositionVFXToClip(position).w;
float minSize = clipPosW / (0.5f * min(UNITY_MATRIX_P[0][0] * _ScreenParams.x,-UNITY_MATRIX_P[1][1] * _ScreenParams.y)); // max size in one pixel
float2 scale = {2};
size = minSize * scale;
{1}",
                    VFXBlockUtility.GetSizeVector(GetParent(), 2),
                    VFXBlockUtility.SetSizesFromVector(GetParent(), "size", 2),
                    sizeString);
            }
        }
    }
}
