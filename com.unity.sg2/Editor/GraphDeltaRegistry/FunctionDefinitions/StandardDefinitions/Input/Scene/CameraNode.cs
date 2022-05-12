using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CameraNode : IStandardNode
    {
        public static string Name = "Camera";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"Position = _WorldSpaceCameraPos;
Direction = (-1 * mul((float3x3)UNITY_MATRIX_M, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz));
Orthographic = unity_OrthoParams.w;
NearPlane = _ProjectionParams.y;
FarPlane = _ProjectionParams.z;
ZBufferSign = _ProjectionParams.x;
Width = unity_OrthoParams.x;
Height = unity_OrthoParams.y;",
            new ParameterDescriptor("Position", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("Direction", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("Orthographic", TYPE.Float, Usage.Out),
            new ParameterDescriptor("NearPlane", TYPE.Float, Usage.Out),
            new ParameterDescriptor("FarPlane", TYPE.Float, Usage.Out),
            new ParameterDescriptor("ZBufferSign", TYPE.Float, Usage.Out),
            new ParameterDescriptor("Width", TYPE.Float, Usage.Out),
            new ParameterDescriptor("Height", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a constantly increasing value used for animated effects.",
            categories: new string[2] { "Input", "Scene" },
            synonyms: new string[7] { "Position", "Direction", "Orthographic", "NearPlane", "FarPlane", "Width", "Height" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[8] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "Position of the Camera's GameObject in world space"
                ),
                new ParameterUIDescriptor(
                    name: "Direction",
                    tooltip: "The Camera's forward vector direction"
                ),
                new ParameterUIDescriptor(
                    name: "Orthographic",
                    tooltip: "Returns 1 if the Camera is orthographic, otherwise 0"
                ),
                new ParameterUIDescriptor(
                    name: "NearPlane",
                    displayName: "Near Plane",
                    tooltip: "The Camera's near plane distance"
                ),
                new ParameterUIDescriptor(
                    name: "FarPlane",
                    displayName: "Far Plane",
                    tooltip: "The Camera's far plane distance"
                ),
                new ParameterUIDescriptor(
                    name: "ZBufferSign",
                    displayName: "Z Buffer Sign",
                    tooltip: "Returns -1 when using a reversed Z Buffer, otherwise 1"
                ),
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "The Camera's width if orthographic"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "The Camera's height if orthographic"
                )
            }
        );
    }
}
