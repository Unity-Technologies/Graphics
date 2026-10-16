using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;
using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using static Unity.GraphAuthoring.Editor.ProviderSystem.ShaderValueUtils;

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

        // The reference name of the return value's slot.
        internal const string kReturnSlotName = "__UNITY_SHADERGRAPH_UNUSED";

        internal static MaterialSlot MakeSlotFromField(IShaderField field, int slotId, SlotType dir)
        {
            MaterialSlot slot = null;

            var presentation = field.Presentation;
            var production = field.Production;
            var editor = presentation.Editor;
            string referenceName = field.Name;
            string displayName = presentation.DisplayName;
            string typeName = field.ShaderType.Name;

            string typeTest = typeName.ToLowerInvariant();
            bool isBareResource = !typeTest.Contains("unity") && (typeTest.Contains("sampler") || typeTest.Contains("texture"));
            bool isColor = editor is ColorEditorData;
            string referable = production.Default is ReferableDefault referableValue ? referableValue.Referable : null;

            string defaultString = production.Default switch
            {
                LiteralDefault literal => literal.Text,
                ReferableDefault referableDefault => referableDefault.Option,
                _ => null,
            };
            float[] defaultValue = defaultString != null ? LazyTokenFloat(defaultString) : null;

            Enum.TryParse<Internal.UVChannel>(defaultString, true, out var defaultUVChannel);
            if (!Enum.TryParse<Internal.CoordinateSpace>(defaultString, true, out var defaultCoordinateSpace))
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;
            Enum.TryParse<ScreenSpaceType>(defaultString, true, out var defaultScreenSpaceType);

            // absolute world only works for position.
            if (defaultCoordinateSpace == Internal.CoordinateSpace.AbsoluteWorld && referable != StandardReferables.kPosition)
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;

            // Screen doesn't work for any of the Space Slots-- _why do they use this enum?_
            if (defaultCoordinateSpace == Internal.CoordinateSpace.Screen)
                defaultCoordinateSpace = Internal.CoordinateSpace.Object;

            if (referable != null && dir == SlotType.Input)
            {
                switch(referable)
                {
                    case StandardReferables.kUV:
                        slot = new UVMaterialSlot(slotId, displayName, referenceName, defaultUVChannel);
                        break;
                    case StandardReferables.kPosition:
                        slot = new PositionMaterialSlot(slotId, displayName, referenceName, defaultCoordinateSpace);
                        break;
                    case StandardReferables.kNormal:
                        slot = new NormalMaterialSlot(slotId, displayName, referenceName, defaultCoordinateSpace);
                        break;
                    case StandardReferables.kTangent:
                        slot = new TangentMaterialSlot(slotId, displayName, referenceName, defaultCoordinateSpace);
                        break;
                    case StandardReferables.kBitangent:
                        slot = new BitangentMaterialSlot(slotId, displayName, referenceName, defaultCoordinateSpace);
                        break;
                    case StandardReferables.kVertexColor:
                        slot = new VertexColorMaterialSlot(slotId, displayName, referenceName);
                        break;
                    case StandardReferables.kViewDirection:
                        slot = new ViewDirectionMaterialSlot(slotId, displayName, referenceName, defaultCoordinateSpace);
                        break;
                    case StandardReferables.kScreenPosition:
                        slot = new ScreenPositionMaterialSlot(slotId, displayName, referenceName, defaultScreenSpaceType);
                        break;
                }
            }
            if (slot == null)
            {
                if (production.IsDynamicVector)
                {
                    TryCast<Vector4>(typeName, defaultValue, out var v4, Vector4.zero);
                    slot = new DynamicVectorMaterialSlot(slotId, displayName, referenceName, dir, v4);
                }
                else if (editor is DropdownEditorData dropdown && dir == SlotType.Input)
                {
                    TryCast<float>(typeName, defaultValue, out var asIdx, 0.0f);
                    slot = new Vector1MaterialEnumSlot(slotId, displayName, referenceName, dir, new List<string>(dropdown.Options).ToArray(), asIdx);
                }
                else if (editor is RangeEditorData range && dir == SlotType.Input)
                {
                    TryCast<float>(typeName, defaultValue, out var asRng, 0);
                    slot = new Vector1MaterialRangeSlot(slotId, displayName, referenceName, dir, asRng, new Vector2(range.Min, range.Max));
                }
                else switch (typeName)
                    {
                        case "uint1":
                        case "uint":
                            TryCast<float>(typeName, defaultValue, out var asUint, 0u);
                            slot = new Vector1MaterialIntegerSlot(slotId, displayName, referenceName, dir, asUint, unsigned: true); break;
                        case "int1":
                        case "int":
                            TryCast<float>(typeName, defaultValue, out var asInt, 0);
                            slot = new Vector1MaterialIntegerSlot(slotId, displayName, referenceName, dir, asInt); break;
                        case "half":
                        case "half1":
                        case "float1":
                        case "float":
                            TryCast<float>(typeName, defaultValue, out var asFloat, 0.0f);
                            slot = new Vector1MaterialSlot(slotId, displayName, referenceName, dir, asFloat); break;
                        case "bool1":
                        case "bool":
                            TryCast<bool>(typeName, defaultValue, out var asBool, false);
                            slot = new BooleanMaterialSlot(slotId, displayName, referenceName, dir, asBool); break;
                        case "half2":
                        case "float2":
                            TryCast<Vector2>(typeName, defaultValue, out var asV2, Vector2.zero);
                            slot = new Vector2MaterialSlot(slotId, displayName, referenceName, dir, asV2); break;
                        case "half3":
                        case "float3":
                            TryCast<Vector3>(typeName, defaultValue, out var asV3, Vector3.zero);
                            slot = !isColor
                                ? new Vector3MaterialSlot(slotId, displayName, referenceName, dir, asV3)
                                : new ColorRGBMaterialSlot(slotId, displayName, referenceName, dir, new Vector4(asV3.x, asV3.y, asV3.z, 1), Internal.ColorMode.Default);
                            break;
                        case "half4":
                        case "float4":
                            TryCast<Vector4>(typeName, defaultValue, out var asV4, isColor ? new Vector4(0, 0, 0, 1) : Vector4.zero);
                            slot = isColor
                                ? new ColorRGBAMaterialSlot(slotId, displayName, referenceName, dir, asV4)
                                : new Vector4MaterialSlot(slotId, displayName, referenceName, dir, asV4);
                            break;
                        case "half2x2":
                        case "float2x2": slot = new Matrix2MaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "half3x3":
                        case "float3x3": slot = new Matrix3MaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "half4x4":
                        case "float4x4": slot = new Matrix4MaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "SamplerState":
                        case "UnitySamplerState": slot = new SamplerStateMaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "Texture2D":
                        case "UnityTexture2D":
                            slot = dir == SlotType.Input
                                    ? new Texture2DInputMaterialSlot(slotId, displayName, referenceName)
                                    : new Texture2DMaterialSlot(slotId, displayName, referenceName, dir);
                            break;
                        case "Texture2DArray":
                        case "UnityTexture2DArray":
                            slot = dir == SlotType.Input
                                    ? new Texture2DArrayInputMaterialSlot(slotId, displayName, referenceName)
                                    : new Texture2DArrayMaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "TextureCube":
                        case "UnityTextureCube":
                            slot = dir == SlotType.Input
                                    ? new CubemapInputMaterialSlot(slotId, displayName, referenceName)
                                    : new CubemapMaterialSlot(slotId, displayName, referenceName, dir); break;
                        case "Texture3D":
                        case "UnityTexture3D":
                            slot = dir == SlotType.Input
                                    ? new Texture3DInputMaterialSlot(slotId, displayName, referenceName)
                                    : new Texture3DMaterialSlot(slotId, displayName, referenceName, dir); break;
                        default:
                            slot = new ExternalMaterialSlot(slotId, displayName, referenceName, ShaderObjectUtils.QualifyName(field.ShaderType), dir, defaultString);
                            break;
                    }
            }
            slot.hideConnector = production.Access == FieldAccess.Static && dir == SlotType.Input;
            slot.hidden = production.Access == FieldAccess.Local || production.Access == FieldAccess.Linkage;
            slot.bareResource = isBareResource;

            if (editor is CustomBindingEditorData binding)
                slot.CustomBinding = binding.Label;

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

            return new ShaderType(type.Name.Replace(baseType, newType), type.Namespace);
        }

        internal static bool TryApplyLegacyPrecisionTokens(IShaderFunction func, string precisionToken, int dynamicLength, out IShaderFunction result)
        {
            List<IShaderField> fields = new();
            result = null;

            bool applyPrecision = func.Production.SupportsPrecision;
            bool anyDynamics = false;

            Debug.Assert(dynamicLength >= 0);

            foreach(var param in func.Parameters)
            {
                bool applyDynamics = param.Production.IsDynamicVector;
                anyDynamics |= applyDynamics;

                var legacyType = ApplyLegacy(param.ShaderType, applyPrecision ? precisionToken : null, applyDynamics ? dynamicLength : 0);

                var field = new ShaderField(param.Name, param.IsInput, param.IsOutput, legacyType, param.Production, param.Presentation);

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
            result = new ShaderFunction(funcName, namespaces, fields, returnType, func.FunctionBody, false, func.Dependencies,
                production: func.Production, presentation: func.Presentation);
            return true;
        }
    }
}
