//using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine.Pool;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Multiply Sandbox")]
    class MultiplySandboxNode : SandboxNode<MultiplyNodeDefinition>
    {
    }

    [Serializable]
    class MultiplyNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Multiply Sandbox");

            var AType = context.GetInputType("A");
            var BType = context.GetInputType("B");

            var shaderFunc = BuildFunction(AType, BType);
            context.SetMainFunction(shaderFunc, declareSlots: true);
            context.SetPreviewFunction(shaderFunc);
        }

        static Matrix4x4 kDefaultMatrix2 = new Matrix4x4(new Vector4(2.0f, 2.0f, 2.0f, 2.0f), new Vector4(2.0f, 2.0f, 2.0f, 2.0f), new Vector4(2.0f, 2.0f, 2.0f, 2.0f), new Vector4(2.0f, 2.0f, 2.0f, 2.0f));

        static ShaderFunction BuildFunction(SandboxType AType, SandboxType BType)
        {
            if (AType == null)
                AType = Types._precision;

            if (BType == null)
                BType = Types._precision;

            bool useMulFunction = true;
            SandboxType outputType = null;
            if (AType.IsVectorOrScalar && BType.IsVectorOrScalar)
            {
                // vector multiplication follows dynamic vector rules
                AType = BType = outputType = SandboxNodeUtils.DetermineDynamicVectorType(new List<SandboxType>() { AType, BType });

                // mul on vectors is defined to be dot product, but we want component-wise multiplication
                useMulFunction = false;
            }
            else if (AType.IsVectorOrScalar && BType.IsMatrix)
            {
                // vector * matrix: cast input to expected input size, determine output size
                AType = Types.PrecisionVector(BType.MatrixRows);
                outputType = Types.PrecisionVector(BType.MatrixColumns);
            }
            else if (AType.IsMatrix && BType.IsVectorOrScalar)
            {
                // matrix * vector: cast input to expected input size, determine output size
                BType = Types.PrecisionVector(AType.MatrixColumns);
                outputType = Types.PrecisionVector(AType.MatrixRows);
            }
            else if (AType.IsMatrix && BType.IsMatrix)
            {
                // matrix * matrix: if matrix sizes match, determine output size
                if (AType.MatrixColumns == BType.MatrixRows)
                {
                    outputType = Types.PrecisionMatrix(AType.MatrixRows, BType.MatrixColumns);
                }
                else
                {
                    // matrix sizes do not match... cast down to minimum shared dimension
                    int sharedDimension = Math.Min(AType.MatrixColumns, BType.MatrixRows);

                    // NOTE: technically we could downcast only the shared dimension of the input types
                    // but we only support square matrix types in old ShaderGraph
                    // so we make all the matrices square (matching old behavior)
                    AType = BType = outputType = Types.PrecisionMatrix(sharedDimension, sharedDimension);
                }
            }

            var func = new ShaderFunction.Builder("Unity_MultiplySB_" + AType.Name + "_" + BType.Name);
            func.AddInput(AType, "A", new DynamicDefaultValue() { matrixDefault = Matrix4x4.zero });
            func.AddInput(BType, "B", new DynamicDefaultValue() { matrixDefault = kDefaultMatrix2 });
            func.AddOutput(outputType, "Out");
            if (useMulFunction)
                func.AddLine("Out = mul(A, B);");
            else
                func.AddLine("Out = A * B;");
            return func.Build();
        }
    }
}
