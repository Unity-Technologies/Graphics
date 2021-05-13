using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Sandbox.UnitTests
{
    class SandboxTypeTests
    {

        [Test]
        public void TestDefaultTypesExist()
        {
            Assert.NotNull(Types._bool, "Types._bool is null");

            Assert.NotNull(Types._float, "Types._float is null");
            Assert.NotNull(Types._float2, "Types._float2 is null");
            Assert.NotNull(Types._float3, "Types._float3 is null");
            Assert.NotNull(Types._float4, "Types._float4 is null");

            Assert.NotNull(Types._half, "Types._half is null");
            Assert.NotNull(Types._half2, "Types._half2 is null");
            Assert.NotNull(Types._half3, "Types._half3 is null");
            Assert.NotNull(Types._half4, "Types._half4 is null");

            Assert.NotNull(Types._precision, "Types._precision is null");
            Assert.NotNull(Types._precision2, "Types._precision2 is null");
            Assert.NotNull(Types._precision3, "Types._precision3 is null");
            Assert.NotNull(Types._precision4, "Types._precision4 is null");

            Assert.NotNull(Types._precision3x3, "Types._precision3x3 is null");

            Assert.NotNull(Types._UnityTexture2D, "Types._UnityTexture2D is null");
            Assert.NotNull(Types._UnitySamplerState, "Types._UnitySamplerState is null");
        }

        [Test]
        public void DefaultTypesHaveCommonTypes()
        {
            var allDefaultTypes = Types.Default.AllTypes;
            Assert.IsTrue(Types._bool.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "bool")));
            Assert.IsTrue(Types._int.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "int")));
            Assert.IsTrue(Types._float.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "float")));
            Assert.IsTrue(Types._float3.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "float3")));
            Assert.IsTrue(Types._float4.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "float4")));
            Assert.IsTrue(Types._half.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "half")));
            Assert.IsTrue(Types._half2.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "half2")));
            Assert.IsTrue(Types._half4.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "half4")));
            Assert.IsTrue(Types._UnityTexture2D.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "UnityTexture2D")));
            Assert.IsTrue(Types._UnitySamplerState.ValueEquals(allDefaultTypes.FirstOrDefault(t => t.Name == "UnitySamplerState")));
        }

        [Test]
        public void TestDefaultTypes()
        {
            var allDefaultTypes = Types.Default.AllTypes;
            foreach (var t in allDefaultTypes)
            {
                // names should not be null
                Assert.IsTrue(t.Name != null);

                // value equals should be false between any two unique types
                foreach (var t2 in allDefaultTypes)
                {
                    if (!ReferenceEquals(t, t2))
                        Assert.IsFalse(t.ValueEquals(t2), t.Name + " ValueEquals " + t2.Name);
                }

                // check if it has a definition
                var def = t.Definition;
                if (def != null)
                {
                    Assert.IsTrue(def.GetTypeName() == t.Name);
                }

                if (t.IsPlaceholder)
                {
                    Assert.IsTrue(t.Name.Contains('$'), t.Name);
                }
                else
                {
                    if (t.IsVector)
                    {
                        var vecDef = t.GetDefinition<VectorTypeDefinition>();
                        Assert.NotNull(vecDef, t.Name);
                        Assert.IsTrue(vecDef == def, t.Name);
                        Assert.IsTrue(t.VectorDimension == def.VectorDimension, t.Name);
                    }
                    else
                    {
                        if (t.IsScalar)
                            Assert.IsTrue(t.VectorDimension == 1, t.Name);
                        else
                            Assert.IsTrue(t.VectorDimension == 0, t.Name);
                    }

                    if (t.IsMatrix)
                    {
                        var matDef = t.GetDefinition<MatrixTypeDefinition>();
                        Assert.NotNull(matDef, t.Name);
                        Assert.IsTrue(matDef == def, t.Name);
                        Assert.IsTrue(t.MatrixRows == def.MatrixRows, t.Name);
                        Assert.IsTrue(t.MatrixColumns == def.MatrixColumns, t.Name);
                    }
                    else
                    {
                        Assert.IsTrue(t.MatrixRows == 0, t.Name);
                        Assert.IsTrue(t.MatrixColumns == 0, t.Name);
                    }

                    if (t.IsTexture)
                    {
                        if (t.IsBareResource)
                        {
                        }
                        else
                        {
                            var texDef = t.GetDefinition<TextureTypeDefinition>();
                            Assert.NotNull(texDef, t.Name);
                            Assert.IsTrue(texDef == def, t.Name);
                        }
                    }

                    if (t.IsSamplerState)
                    {
                        if (t.IsBareResource)
                        {
                        }
                        else
                        {
                            var ssDef = t.GetDefinition<SamplerStateTypeDefinition>();
                            Assert.NotNull(ssDef, t.Name);
                            Assert.IsTrue(ssDef == def, t.Name);
                        }
                    }

                }
                Assert.IsFalse(t.HasHLSLDeclaration);   // no default types have HLSL declarations (at least currently..)
            }
        }

        [Test]
        public void VectorDimensionCorrect()
        {
            Assert.IsTrue(Types._float.VectorDimension == 1);
            Assert.IsTrue(Types._bool.VectorDimension == 1);
            Assert.IsTrue(Types._half4.VectorDimension == 4);
            Assert.IsTrue(Types._float2.VectorDimension == 2);
            Assert.IsTrue(Types._precision.VectorDimension == 1);
            Assert.IsTrue(Types._precision3x3.VectorDimension == 0);
            Assert.IsTrue(Types._UnityTexture2D.VectorDimension == 0);
            Assert.IsTrue(Types._UnitySamplerState.VectorDimension == 0);
        }

        [Test]
        public void TestTypeEquality()
        {
            Assert.IsTrue(Types._float.Equals(Types._float));
            Assert.IsTrue(Types._half.Equals(Types._half));
            Assert.IsTrue(Types._precision.Equals(Types._precision));
            Assert.IsTrue(Types._precision2.Equals(Types._precision2));
            Assert.IsTrue(Types._precision4x4.Equals(Types._precision4x4));

            Assert.IsFalse(Types._float.Equals(Types._float2));
            Assert.IsFalse(Types._half3.Equals(Types._half4));
            Assert.IsFalse(Types._float2.Equals(Types._half2));
            Assert.IsFalse(Types._float2.Equals(Types._precision2));
            Assert.IsFalse(Types._precision3.Equals(Types._half3));

            Assert.IsTrue(Types._UnityTexture2D.Equals(Types._UnityTexture2D));
            Assert.IsFalse(Types._UnityTexture2D.Equals(Types._UnitySamplerState));

        }

        [Test]
        public void Struct_TestEmpty()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            var structDef = builder.Build();
            var structType = new SandboxType(structDef);
        }

        [Test]
        public void Struct_AddingSameEmptyStruct()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            var structDef = builder.Build();

            var builder2 = new StructTypeDefinition.Builder("MyStruct");
            var structDef2 = builder2.Build();

            Assert.IsTrue(structDef.ValueEquals(structDef2));

            Assert.IsFalse(ReferenceEquals(structDef, structDef2));

            var types = new Types(Types.Default);
            var r1 = types.AddType(structDef);
            Assert.NotNull(r1);
            var r2 = types.AddType(structDef2);
            Assert.NotNull(r2);
            Assert.IsTrue(ReferenceEquals(r1, r2));
            Assert.IsTrue(r1.ValueEquals(r2));
        }

        [Test]
        public void Struct_AddingSameStructsWithField()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            builder.AddField(Types._float, "myfloat");
            var structDef = builder.Build();

            var builder2 = new StructTypeDefinition.Builder("MyStruct");
            builder2.AddField(Types._float, "myfloat");
            var structDef2 = builder2.Build();

            Assert.IsTrue(structDef.ValueEquals(structDef2));

            Assert.IsFalse(ReferenceEquals(structDef, structDef2));

            var types = new Types(Types.Default);
            var r1 = types.AddType(structDef);
            Assert.NotNull(r1);
            var r2 = types.AddType(structDef2);
            Assert.NotNull(r2);
            Assert.IsTrue(ReferenceEquals(r1, r2));
            Assert.IsTrue(r1.ValueEquals(r2));
        }

        ShaderFunction DoItFunction()
        {
            var funcBuilder = new ShaderFunction.Builder("DoIt");
            funcBuilder.AddInput(Types._float, "i");
            funcBuilder.AddOutput(Types._float, "o");
            funcBuilder.AddLine("i = o;");
            return funcBuilder.Build();
        }

        [Test]
        public void Struct_AddingSameStructsWithFunction()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            builder.AddFunction(DoItFunction());
            var structDef = builder.Build();

            var builder2 = new StructTypeDefinition.Builder("MyStruct");
            builder2.AddFunction(DoItFunction());
            var structDef2 = builder2.Build();

            Assert.IsTrue(structDef.ValueEquals(structDef2));

            Assert.IsFalse(ReferenceEquals(structDef, structDef2));

            var types = new Types(Types.Default);
            var r1 = types.AddType(structDef);
            Assert.NotNull(r1);
            var r2 = types.AddType(structDef2);
            Assert.NotNull(r2);
            Assert.IsTrue(ReferenceEquals(r1, r2));
            Assert.IsTrue(r1.ValueEquals(r2));
        }

        [Test]
        public void Struct_AddingCollidingStructs()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            builder.AddField(Types._float, "myfloat");
            var structDef = builder.Build();

            var builder2 = new StructTypeDefinition.Builder("MyStruct");
            builder2.AddField(Types._float, "myfloat2");
            var structDef2 = builder2.Build();

            Assert.IsFalse(structDef.ValueEquals(structDef2));

            Assert.IsFalse(ReferenceEquals(structDef, structDef2));

            var types = new Types(Types.Default);
            var r1 = types.AddType(structDef);
            Assert.NotNull(r1);
            var r2 = types.AddType(structDef2);     // error: colliding type
            Assert.IsNull(r2);
        }

        [Test]
        public void Struct_AddingDifferentStructs()
        {
            var builder = new StructTypeDefinition.Builder("MyStruct");
            builder.AddField(Types._float, "myfloat");
            var structDef = builder.Build();

            var builder2 = new StructTypeDefinition.Builder("MyStruct2");
            builder2.AddField(Types._float, "myfloat");
            var structDef2 = builder2.Build();

            Assert.IsFalse(structDef.ValueEquals(structDef2));

            var types = new Types(Types.Default);
            var r1 = types.AddType(structDef);
            Assert.NotNull(r1);
            var r2 = types.AddType(structDef2);
            Assert.NotNull(r2);
            Assert.IsFalse(ReferenceEquals(r1, r2));
        }

    }
}
