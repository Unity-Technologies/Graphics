using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

public static class SandboxNodeUtils
{
    public static SandboxType DetermineDynamicVectorType(IEnumerable<SandboxType> inputTypes)
    {
        // Dynamic vector is chosen to be the minimum vector size (ignoring scalars),
        // falling back to scalars if that's all there is.
        int minVectorDimension = 5;
        foreach (var vt in inputTypes)
        {
            if ((vt != null) && (vt.IsVector))
            {
                var dim = vt.VectorDimension;
                if (dim > 1)
                    minVectorDimension = Math.Min(minVectorDimension, dim);
            }
        }
        if (minVectorDimension < 5)
            return Types.PrecisionVector(minVectorDimension);
        else
            return Types._precision;
    }

    public static SandboxType DetermineDynamicVectorType(ISandboxNodeBuildContext context, ShaderFunction dynamicShaderFunc)
    {
        var dynamicInputTypes = dynamicShaderFunc.Parameters.Select(p => (p.Type == Types._dynamicVector) ? context.GetInputType(p.Name) : null);
        return DetermineDynamicVectorType(dynamicInputTypes);
    }

    internal static void ProvideFunctionToRegistry(ShaderFunction function, FunctionRegistry registry)
    {
        // hmm...  currently provide function doesn't ensure any dependency ordering between functions
        // it's just relying on them being provided in dependency order.  So we must provide dependencies first:
        if (function.FunctionsCalled != null)
        {
            foreach (var subsig in function.FunctionsCalled)
            {
                // some function calls can just be a signature, no declaration provided.
                // but if there is a declaration, provide it!
                if (subsig is ShaderFunction subfunction)
                {
                    ProvideFunctionToRegistry(subfunction, registry);
                }
            }
        }

        // check parameter types -- if any are not built in, provide them
        foreach (var p in function.Parameters)
        {
            if (p.Type.HasHLSLDeclaration)
            {
                registry.ProvideFunction(p.Type.Name, sb =>
                    p.Type.AddHLSLTypeDeclarationString(sb));
            }
        }

        // then provide the main function last
        registry.ProvideFunction(function.Name, sb =>
        {
            function.AppendHLSLDeclarationString(sb);
        });
    }

    // shim function to help map to old AbstractMaterialNode / MaterialSlot system
    internal static MaterialSlot CreateBoundSlot(Binding binding, int slotId, string displayName, string shaderOutputName, ShaderStageCapability shaderStageCapability, bool hidden = false)
    {
        switch (binding)
        {
            case Binding.ObjectSpaceNormal:
                return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
            case Binding.ObjectSpaceTangent:
                return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
            case Binding.ObjectSpaceBitangent:
                return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
            case Binding.ObjectSpacePosition:
                return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
            case Binding.ViewSpaceNormal:
                return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
            case Binding.ViewSpaceTangent:
                return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
            case Binding.ViewSpaceBitangent:
                return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
            case Binding.ViewSpacePosition:
                return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
            case Binding.WorldSpaceNormal:
                return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
            case Binding.WorldSpaceTangent:
                return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
            case Binding.WorldSpaceBitangent:
                return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
            case Binding.WorldSpacePosition:
                return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
            case Binding.AbsoluteWorldSpacePosition:
                return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.AbsoluteWorld, shaderStageCapability, hidden);
            case Binding.TangentSpaceNormal:
                return new NormalMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
            case Binding.TangentSpaceTangent:
                return new TangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
            case Binding.TangentSpaceBitangent:
                return new BitangentMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
            case Binding.TangentSpacePosition:
                return new PositionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
            case Binding.MeshUV0:
                return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV0, shaderStageCapability, hidden);
            case Binding.MeshUV1:
                return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV1, shaderStageCapability, hidden);
            case Binding.MeshUV2:
                return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV2, shaderStageCapability, hidden);
            case Binding.MeshUV3:
                return new UVMaterialSlot(slotId, displayName, shaderOutputName, UVChannel.UV3, shaderStageCapability, hidden);
            case Binding.ScreenPosition:
                return new ScreenPositionMaterialSlot(slotId, displayName, shaderOutputName, ScreenSpaceType.Default, shaderStageCapability, hidden);
            case Binding.ObjectSpaceViewDirection:
                return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Object, shaderStageCapability, hidden);
            case Binding.ViewSpaceViewDirection:
                return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.View, shaderStageCapability, hidden);
            case Binding.WorldSpaceViewDirection:
                return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.World, shaderStageCapability, hidden);
            case Binding.TangentSpaceViewDirection:
                return new ViewDirectionMaterialSlot(slotId, displayName, shaderOutputName, CoordinateSpace.Tangent, shaderStageCapability, hidden);
            case Binding.VertexColor:
                return new VertexColorMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden);
            default:
                throw new ArgumentOutOfRangeException("binding", binding, null);
        }
    }

    // shim function to help map to old AbstractMaterialNode / MaterialSlot system
    internal static SlotValueType ConvertSandboxValueTypeToSlotValueType(SandboxType type)
    {
        if (type == Types._bool)
        {
            return SlotValueType.Boolean;
        }
        if (type.IsScalar || (type.IsVector && type.VectorDimension == 1))
        {
            return SlotValueType.Vector1;
        }
        if (type.IsVector && type.VectorDimension == 2)
        {
            return SlotValueType.Vector2;
        }
        if (type.IsVector && type.VectorDimension == 3)
        {
            return SlotValueType.Vector3;
        }
        if (type.IsVector && type.VectorDimension == 4)
        {
            return SlotValueType.Vector4;
        }
        /*            if (t == typeof(Color))
                    {
                        return SlotValueType.Vector4;
                    }
                    if (t == typeof(ColorRGBA))
                    {
                        return SlotValueType.Vector4;
                    }
                    if (t == typeof(ColorRGB))
                    {
                        return SlotValueType.Vector3;
                    }
        */
        if (type == Types._UnityTexture2D)
        {
            return SlotValueType.Texture2D;
        }
        if (type == Types._UnitySamplerState)
        {
            return SlotValueType.SamplerState;
        }
        /*
                    if (t == typeof(Texture2DArray))
                    {
                        return SlotValueType.Texture2DArray;
                    }
                    if (t == typeof(Texture3D))
                    {
                        return SlotValueType.Texture3D;
                    }
                    if (t == typeof(Cubemap))
                    {
                        return SlotValueType.Cubemap;
                    }
                    if (t == typeof(Gradient))
                    {
                        return SlotValueType.Gradient;
                    }
                    if (t == typeof(SamplerState))
                    {
                        return SlotValueType.SamplerState;
                    }
        */
        if (type.IsMatrix && type.MatrixColumns == 4)
        {
            return SlotValueType.Matrix4;
        }
        if (type.IsMatrix && type.MatrixColumns == 3)
        {
            return SlotValueType.Matrix3;
        }
        if (type.IsMatrix && type.MatrixColumns == 2)
        {
            return SlotValueType.Matrix2;
        }
        if (type == Types._dynamicVector)
        {
            return SlotValueType.DynamicVector;
        }
        if (type == Types._dynamicMatrix)
        {
            return SlotValueType.DynamicMatrix;
        }
        if (type.IsPlaceholder)
        {
            return SlotValueType.Dynamic;
        }
        throw new ArgumentException("Unsupported type " + type.Name);
    }

    internal static ConcreteSlotValueType ConvertSandboxTypeToConcreteSlotValueType(SandboxType type)
    {
        if (type == Types._bool)
        {
            return ConcreteSlotValueType.Boolean;
        }
        if (type.IsScalar || (type.IsVector && type.VectorDimension == 1))
        {
            return ConcreteSlotValueType.Vector1;
        }
        if (type.IsVector && type.VectorDimension == 2)
        {
            return ConcreteSlotValueType.Vector2;
        }
        if (type.IsVector && type.VectorDimension == 3)
        {
            return ConcreteSlotValueType.Vector3;
        }
        if (type.IsVector && type.VectorDimension == 4)
        {
            return ConcreteSlotValueType.Vector4;
        }
        /*            if (t == typeof(Color))
                    {
                        return SlotValueType.Vector4;
                    }
                    if (t == typeof(ColorRGBA))
                    {
                        return SlotValueType.Vector4;
                    }
                    if (t == typeof(ColorRGB))
                    {
                        return SlotValueType.Vector3;
                    }
        */
        if (type == Types._UnityTexture2D)
        {
            return ConcreteSlotValueType.Texture2D;
        }
        if (type == Types._UnitySamplerState)
        {
            return ConcreteSlotValueType.SamplerState;
        }
        /*
                    if (t == typeof(Texture2DArray))
                    {
                        return SlotValueType.Texture2DArray;
                    }
                    if (t == typeof(Texture3D))
                    {
                        return SlotValueType.Texture3D;
                    }
                    if (t == typeof(Cubemap))
                    {
                        return SlotValueType.Cubemap;
                    }
                    if (t == typeof(Gradient))
                    {
                        return SlotValueType.Gradient;
                    }
                    if (t == typeof(SamplerState))
                    {
                        return SlotValueType.SamplerState;
                    }
        */
        if (type.IsMatrix && type.MatrixColumns == 4)
        {
            return ConcreteSlotValueType.Matrix4;
        }
        if (type.IsMatrix && type.MatrixColumns == 3)
        {
            return ConcreteSlotValueType.Matrix3;
        }
        if (type.IsMatrix && type.MatrixColumns == 2)
        {
            return ConcreteSlotValueType.Matrix2;
        }
        throw new ArgumentException("Unsupported type " + type.Name);
    }
};
