

// The contents of this file should be kept is sync with the ShaderLibrary\HDROutput.hlsl "Color Space transforms" section.
namespace UnityEngine.Rendering
{
    internal static class ColorSpaceUtils
    {
        public static readonly Matrix4x4 Rec709ToRec2020Mat = new Matrix4x4(
            new Vector4(0.627402f, 0.069095f, 0.016394f, 0.0f),
            new Vector4(0.329292f, 0.919544f, 0.088028f, 0.0f),
            new Vector4(0.043306f, 0.011360f, 0.895578f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        );

        public static readonly Matrix4x4 Rec709ToP3D65Mat = new Matrix4x4(
            new Vector4(0.822462f, 0.033194f, 0.017083f, 0.0f),
            new Vector4(0.177538f, 0.966806f, 0.072397f, 0.0f),
            new Vector4(0.000000f, 0.000000f, 0.910520f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        );

        public static readonly Matrix4x4 Rec2020ToRec709Mat = new Matrix4x4(
            new Vector4(1.660496f, -0.124547f, -0.018154f, 0.0f),
            new Vector4(-0.587656f, 1.132895f, -0.100597f, 0.0f),
            new Vector4(-0.072840f, -0.008348f, 1.118751f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        );

        public static readonly Matrix4x4 Rec2020ToP3D65Mat = new Matrix4x4(
            new Vector4(1.343578f, -0.065298f, 0.002822f, 0.0f),
            new Vector4(-0.28218f, 1.075788f, -0.019599f, 0.0f),
            new Vector4(-0.0613986f, -0.010491f, 1.016777f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        );

        public static readonly Matrix4x4 P3D65ToRec2020Mat = new Matrix4x4(
            new Vector4(0.753833f, 0.045744f, -0.001210f, 0.0f),
            new Vector4(0.198597f, 0.941777f, 0.017602f, 0.0f),
            new Vector4(0.04757f, 0.012479f, 0.983609f, 0.0f),
            new Vector4(0.0f, 0.0f, 0.0f, 0.0f)
        );
    }
}
