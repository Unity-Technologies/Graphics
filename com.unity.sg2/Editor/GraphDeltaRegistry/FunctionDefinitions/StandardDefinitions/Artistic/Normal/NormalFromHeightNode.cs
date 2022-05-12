using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalFromHeightNode : IStandardNode
    {
        static string Name = "NormalFromHeight";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "NormalFromHeightTangent",
@"  worldDerivativeX = ddx(PositionWS);
    crossY = cross(ddy(PositionWS), NormalWS);
    d = dot(worldDerivativeX, crossY);
    surfGrad = ((d < 0.0 ? (-1.0f) : 1.0f) / max(0.000000000000001192093f, abs(d))) * (ddx(In)*crossY + ddy(In)*(cross(NormalWS, worldDerivativeX)));
    Out = SafeNormalize(NormalWS - (Strength * surfGrad));",
                    new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
                    new ParameterDescriptor("Strength", TYPE.Float, GraphType.Usage.In, new float[] { 0.01f }),
                    new ParameterDescriptor("worldDerivativeX", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("crossY", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("d", TYPE.Float, GraphType.Usage.Local),
                    new ParameterDescriptor("surfGrad", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("PositionWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Position),
                    new ParameterDescriptor("NormalWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Normal),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "NormalFromHeightWorld",
@"  TangentMatrix[0] = TangentWS;
    TangentMatrix[1] = BitangentWS;
    TangentMatrix[2] = NormalWS;
    worldDerivativeX = ddx(PositionWS);
    crossY = cross(ddy(PositionWS), TangentMatrix[2].xyz);
    d = dot(worldDerivativeX, crossY);
    surfGrad = ((d < 0.0 ? (-1.0f) : 1.0f) / max(0.000000000000001192093f, abs(d))) * (ddx(In)*crossY + ddy(In)*(cross(TangentMatrix[2].xyz, worldDerivativeX)));
    Out = SafeNormalize(TangentMatrix[2].xyz - (Strength * surfGrad));
    Out = TransformWorldToTangent(Out, TangentMatrix);",
                    new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.In),
                    new ParameterDescriptor("Strength", TYPE.Float, GraphType.Usage.In, new float[] { 0.01f }),
                    new ParameterDescriptor("worldDerivativeX", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("crossY", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("d", TYPE.Float, GraphType.Usage.Local),
                    new ParameterDescriptor("surfGrad", TYPE.Vec3, GraphType.Usage.Local),
                    new ParameterDescriptor("TangentMatrix", TYPE.Mat3, GraphType.Usage.Local),
                    new ParameterDescriptor("PositionWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Position),
                    new ParameterDescriptor("NormalWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Normal),
                    new ParameterDescriptor("TangentWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Tangent),
                    new ParameterDescriptor("BitangentWS", TYPE.Vec3, GraphType.Usage.Local, defaultValue: REF.WorldSpace_Bitangent),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "creates a normal from a height value",
            categories: new string[2] { "Artistic", "Normal" },
            synonyms: new string[2] { "convert to normal", "bump map" },
            displayName: "Normal From Height",
            selectableFunctions: new()
            {
                { "NormalFromHeightTangent", "Tangent" },
                { "NormalFromHeightWorld", "World" },
            },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a height map value to convert to a normal"
                ),
                new ParameterUIDescriptor(
                    name: "Strength",
                    tooltip: "the strength of the output normal"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "normal created from the input height value"
                )
            }
        );
    }
}
