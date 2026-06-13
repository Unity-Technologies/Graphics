using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using Hints = Unity.GraphAuthoring.Editor.ProviderSystem.Hints;
using static Unity.GraphAuthoring.Editor.ProviderSystem.HintUtils;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal static class HeaderUtils
    {
        internal static string ToShaderType(this MaterialSlot slot)
        {
            if (slot is ExternalMaterialSlot eslot)
                return eslot.TypeName;

            return slot.concreteValueType.ToShaderString();
        }

        internal static MaterialSlot MakeSlotFromParameter(ParameterHeader header, int slotId, SlotType dir)
        {
            MaterialSlot slot = null;

            Enum.TryParse<Internal.UVChannel>(header.defaultString, true, out var defaultUVChannel);
            if (!Enum.TryParse<Internal.CoordinateSpace>(header.defaultString, true, out var defaultCoordinateSpace))
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;
            Enum.TryParse<ScreenSpaceType>(header.defaultString, true, out var defaultScreenSpaceType);

            // absolute world only works for position.
            if (defaultCoordinateSpace == Internal.CoordinateSpace.AbsoluteWorld && header.Referable != Hints.Param.Ref.kPosition)
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;

            // Screen doesn't work for any of the Space Slots-- _why do they use this enum?_
            if (defaultCoordinateSpace == Internal.CoordinateSpace.Screen)
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;

            if (header.isReferable && dir == SlotType.Input)
            {
                switch(header.Referable)
                {
                    case Hints.Param.Ref.kUV:
                        slot = new UVMaterialSlot(slotId, header.displayName, header.referenceName, defaultUVChannel);
                        break;
                    case Hints.Param.Ref.kPosition:
                        slot = new PositionMaterialSlot(slotId, header.displayName, header.referenceName, defaultCoordinateSpace);
                        break;
                    case Hints.Param.Ref.kNormal:
                        slot = new NormalMaterialSlot(slotId, header.displayName, header.referenceName, defaultCoordinateSpace);
                        break;
                    case Hints.Param.Ref.kTangent:
                        slot = new TangentMaterialSlot(slotId, header.displayName, header.referenceName, defaultCoordinateSpace);
                        break;
                    case Hints.Param.Ref.kBitangent:
                        slot = new BitangentMaterialSlot(slotId, header.displayName, header.referenceName, defaultCoordinateSpace);
                        break;
                    case Hints.Param.Ref.kVertexColor:
                        slot = new VertexColorMaterialSlot(slotId, header.displayName, header.referenceName);
                        break;
                    case Hints.Param.Ref.kViewDirection:
                        slot = new ViewDirectionMaterialSlot(slotId, header.displayName, header.referenceName, defaultCoordinateSpace);
                        break;
                    case Hints.Param.Ref.kScreenPosition:
                        slot = new ScreenPositionMaterialSlot(slotId, header.displayName, header.referenceName, defaultScreenSpaceType);
                        break;
                }
            }
            if (slot == null)
            {
                if (header.isDynamic)
                {
                    TryCast<Vector4>(header.typeName, header.defaultValue, out var v4, Vector4.zero);
                    slot = new DynamicVectorMaterialSlot(slotId, header.displayName, header.referenceName, dir, v4);
                }
                else if (header.isDropdown && dir == SlotType.Input)
                {
                    TryCast<float>(header.typeName, header.defaultValue, out var asIdx, 0.0f);
                    slot = new Vector1MaterialEnumSlot(slotId, header.displayName, header.referenceName, dir, header.options, asIdx);
                }
                else if (header.isSlider && dir == SlotType.Input)
                {
                    TryCast<float>(header.typeName, header.defaultValue, out var asRng, 0);
                    slot = new Vector1MaterialRangeSlot(slotId, header.displayName, header.referenceName, dir, asRng, new Vector2(header.sliderMin, header.sliderMax));
                }
                else switch (header.typeName)
                    {
                        case "uint1":
                        case "uint":
                            TryCast<float>(header.typeName, header.defaultValue, out var asUint, 0u);
                            slot = new Vector1MaterialIntegerSlot(slotId, header.displayName, header.referenceName, dir, asUint, unsigned: true); break;
                        case "int1":
                        case "int":
                            TryCast<float>(header.typeName, header.defaultValue, out var asInt, 0);
                            slot = new Vector1MaterialIntegerSlot(slotId, header.displayName, header.referenceName, dir, asInt); break;
                        case "half":
                        case "half1":
                        case "float1":
                        case "float":
                            TryCast<float>(header.typeName, header.defaultValue, out var asFloat, 0.0f);
                            slot = new Vector1MaterialSlot(slotId, header.displayName, header.referenceName, dir, asFloat); break;
                        case "bool1":
                        case "bool":
                            TryCast<bool>(header.typeName, header.defaultValue, out var asBool, false);
                            slot = new BooleanMaterialSlot(slotId, header.displayName, header.referenceName, dir, asBool); break;
                        case "half2":
                        case "float2":
                            TryCast<Vector2>(header.typeName, header.defaultValue, out var asV2, Vector2.zero);
                            slot = new Vector2MaterialSlot(slotId, header.displayName, header.referenceName, dir, asV2); break;
                        case "half3":
                        case "float3":
                            TryCast<Vector3>(header.typeName, header.defaultValue, out var asV3, Vector3.zero);
                            slot = !header.isColor
                                ? new Vector3MaterialSlot(slotId, header.displayName, header.referenceName, dir, asV3)
                                : new ColorRGBMaterialSlot(slotId, header.displayName, header.referenceName, dir, new Vector4(asV3.x, asV3.y, asV3.z, 1), Internal.ColorMode.Default);
                            break;
                        case "half4":
                        case "float4":
                            TryCast<Vector4>(header.typeName, header.defaultValue, out var asV4, header.isColor ? new Vector4(0, 0, 0, 1) : Vector4.zero);
                            slot = header.isColor
                                ? new ColorRGBAMaterialSlot(slotId, header.displayName, header.referenceName, dir, asV4)
                                : new Vector4MaterialSlot(slotId, header.displayName, header.referenceName, dir, asV4);
                            break;
                        case "half2x2":
                        case "float2x2": slot = new Matrix2MaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "half3x3":
                        case "float3x3": slot = new Matrix3MaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "half4x4":
                        case "float4x4": slot = new Matrix4MaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "SamplerState":
                        case "UnitySamplerState": slot = new SamplerStateMaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "Texture2D":
                        case "UnityTexture2D":
                            slot = dir == SlotType.Input
                                    ? new Texture2DInputMaterialSlot(slotId, header.displayName, header.referenceName)
                                    : new Texture2DMaterialSlot(slotId, header.displayName, header.referenceName, dir);
                            break;
                        case "Texture2DArray":
                        case "UnityTexture2DArray":
                            slot = dir == SlotType.Input
                                    ? new Texture2DArrayInputMaterialSlot(slotId, header.displayName, header.referenceName)
                                    : new Texture2DArrayMaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "TextureCube":
                        case "UnityTextureCube":
                            slot = dir == SlotType.Input
                                    ? new CubemapInputMaterialSlot(slotId, header.displayName, header.referenceName)
                                    : new CubemapMaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        case "Texture3D":
                        case "UnityTexture3D":
                            slot = dir == SlotType.Input
                                    ? new Texture3DInputMaterialSlot(slotId, header.displayName, header.referenceName)
                                    : new Texture3DMaterialSlot(slotId, header.displayName, header.referenceName, dir); break;
                        default:
                            slot = new ExternalMaterialSlot(slotId, header.displayName, header.referenceName, header.externalQualifiedTypeName, dir, header.defaultString);
                            break;
                    }
            }
            slot.hideConnector = header.isStatic && dir == SlotType.Input;
            slot.hidden = header.isLocal;
            slot.bareResource = header.isBareResource;

            if (header.hasCustomBinding)
                slot.CustomBinding = header.customBinding;

            return slot;
        }

        private static IShaderType ApplyLegacy(IShaderType type, string precisionToken, int dynamicLength)
        {
            var baseType = type.Name.Split('[')[0];

            var newType = baseType;

            // Dynamic hint currently only supports float, float2, float3, float4 and half, half2, half3, half4
            if (dynamicLength >= 1 && dynamicLength <= 4)
            {
                string halfType = $"half{(dynamicLength == 1 ? null : dynamicLength)}";
                string floatType = $"float{(dynamicLength == 1 ? null : dynamicLength)}";
                newType = System.Text.RegularExpressions.Regex.Replace(newType, "half[1-4]|half", halfType);
                newType = System.Text.RegularExpressions.Regex.Replace(newType, "float[1-4]|float", floatType);
            }

            if (precisionToken != null)
                newType = newType.Replace("float", precisionToken).Replace("half", precisionToken);

            return new ShaderType(type.Name.Replace(baseType, newType));
        }

        internal static bool TryApplyLegacy(IShaderFunction func, FunctionHeader funcHeader, IReadOnlyDictionary<string, ParameterHeader> paramHeaders, string precisionToken, int dynamicLength, out IShaderFunction result)
        {
            List<IShaderField> fields = new();
            result = null;

            bool applyPrecision = funcHeader.allowPrecision;
            bool anyDynamics = false;

            Debug.Assert(dynamicLength >= 0);

            foreach(var param in func.Parameters)
            {
                bool applyDynamics = paramHeaders.TryGetValue(param.Name, out var paramHeader) && paramHeader.isDynamic;
                anyDynamics |= applyDynamics;

                var legacyType = ApplyLegacy(param.ShaderType, applyPrecision ? precisionToken : null, applyDynamics ? dynamicLength : 0);

                var field = new ShaderField(param.Name, param.IsInput, param.IsOutput, legacyType, param.Hints);

                fields.Add(field);
            }

            if (!anyDynamics && !applyPrecision)
            {
                result = func;
                return false;
            }

            List<string> namespaces = new();
            namespaces.AddRange(func.Namespace);
            if (applyPrecision || anyDynamics)
                namespaces.Add("unity_sg_generated");

            string funcName = $"{func.Name}{(applyPrecision ? $"_{precisionToken}" : null)}{(anyDynamics ? dynamicLength : null)}";

            // Dynamic typing isn't supported on return values.
            IShaderType returnType = ApplyLegacy(func.ReturnType, applyPrecision ? precisionToken : null, 0);
            result = new ShaderFunction(funcName, namespaces, fields, returnType, func.FunctionBody, func.Hints);
            return true;
        }
    }
}
