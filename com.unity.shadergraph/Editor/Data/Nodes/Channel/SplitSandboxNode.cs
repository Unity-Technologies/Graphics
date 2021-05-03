using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Split Sandbox")]
    class SplitSandboxNode : SandboxNode<SplitNodeDefinition>
    {
    }

    [System.Serializable]
    class SplitNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Split Sandbox");

            // get the input type
            var inputType = context.GetInputType("In");
            if ((inputType == null) || !inputType.IsVector)
                inputType = Types._precision;

            // convert to equivalent precision vector type:
            var vecCount = inputType.VectorSize;
            inputType = Types.Precision(vecCount);

            var shaderFunc = BuildFunction(inputType);

            context.SetMainFunction(shaderFunc, declareSlots: true);
            context.SetPreviewFunction(shaderFunc, PreviewMode.Preview3D);
        }

        static ShaderFunction BuildFunction(SandboxValueType inputType)
        {
            var func = new ShaderFunction.Builder("Unity_Split_" + inputType.Name);
            func.AddInput(inputType, "In");
            int vecCount = inputType.VectorSize;

            func.AddOutput(Types._precision, "R");
            func.AddLine("R = In.r;");
            if (vecCount > 1)
            {
                func.AddOutput(Types._precision, "G");
                func.AddLine("G = In.r;");
            }
            if (vecCount > 2)
            {
                func.AddOutput(Types._precision, "B");
                func.AddLine("B = In.b;");
            }
            if (vecCount > 3)
            {
                func.AddOutput(Types._precision, "A");
                func.AddLine("A = In.a;");
            }

            return func.Build();
        }
    }
}
