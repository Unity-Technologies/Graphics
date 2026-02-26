using UnityEngine;
using UnityEditor.Graphing;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal static class Hints
    {
        internal static class Common
        {
            internal const string kDisplayName = "sg:DisplayName";

            // Not yet implemented.
            internal const string kTooltip = "sg:Tooltip"; // SG doesn't have tooltips
        }
        internal static class Func
        {
            internal const string kProviderKey = "sg:ProviderKey";

            internal const string kReturnDisplayName = "sg:ReturnDisplayName";

            internal const string kSearchTerms = "sg:SearchTerms";
            internal const string kSearchName = "sg:SearchName";
            internal const string kCategory = "sg:SearchCategory";

            // Not yet implemented.
            internal const string kGroupKey = "sg:GroupKey";
            internal const string kReturnTooltip = "sg:ReturnTooltip";
            internal const string kDocumentationLink = "sg:HelpURL";

            // ProviderKey associated hints, Not yet implemented.
            internal const string kDeprecated = "sg:Deprecated";
            internal const string kObsolete = "sg:Obsolete";
            internal const string kVersion = "sg:Version";
        }

        internal static class Param
        {
            internal const string kStatic = "sg:Static";
            internal const string kLocal = "sg:Local";
            internal const string kLiteral = "sg:Literal";
            internal const string kColor = "sg:Color";
            internal const string kRange = "sg:Range";
            internal const string kDropdown = "sg:Dropdown";
            internal const string kDefault = "sg:Default";

            // Not yet implemented.
            internal const string kSetting = "sg:Setting";
            internal const string kLinkage = "sg:Linkage";
            internal const string kPrecision = "sg:Precision";
            internal const string kDynamic = "sg:Dynamic";
            internal const string kReferable = "sg:Referable";
            internal const string kExternal = "sg:External";

            // Hard coded referables, not yet implemented.
            internal const string kUV = "sg:ref:UV";
            internal const string kPosition = "sg:ref:Position";
            internal const string kNormal = "sg:ref:Normal";
            internal const string kBitangent = "sg:ref:Bitangent";
            internal const string kTangent = "sg:ref:Tangent";
            internal const string kViewDirection = "sg:ref:ViewDirection";
            internal const string kScreenPosition = "sg:ref:ScreenPosition";
            internal const string kVertexColor = "sg:VertexColor";
        }
    }

    internal static class HeaderUtils
    {
        internal static string ToShaderType(this MaterialSlot slot)
        {
            if (slot is ExternalMaterialSlot eslot)
                return eslot.TypeName;

            return slot.concreteValueType.ToShaderString();
        }

        internal static bool IsNumeric(string typeName)
        {
            return TryParseTypeInfo(typeName, out _, out _, out _, out _, out _, out _, out _);
        }

        internal static bool TryParseTypeInfo(
            string type,
            out string prim,
            out bool isScalar,
            out bool isVector,
            out bool isMatrix,
            out int rows,
            out int cols,
            out int length)
        {
            prim = null;
            isScalar = false;
            isVector = false;
            isMatrix = false;
            rows = -1;
            cols = -1;
            length = -1;

            // TODO: Array support isn't established yet, if we support multidim this wouldn't work.
            if (type.Contains('['))
                return false;


            if (type.StartsWith("uint")) prim = "uint";
            if (type.StartsWith("int")) prim = "int";
            if (type.StartsWith("bool")) prim = "bool";
            if (type.StartsWith("half")) prim = "half";
            if (type.StartsWith("float")) prim = "float";

            if (prim == null)
                return false;

            if (type == prim)
                return isScalar = true;

            var remainder = type.Substring(prim.Length);

            if (remainder.Length == 1)
            {
                return isVector = int.TryParse(remainder, out rows) && rows >= 1 && rows <= 4;
            }
            if (remainder.Length == 3)
            {
                return isMatrix
                    = int.TryParse(remainder.Substring(0, 1), out rows) && rows >= 1 && rows <= 4
                    && int.TryParse(remainder.Substring(2, 1), out cols) && cols >= 1 && cols <= 4;
            }
            return false;
        }

        internal static bool TryCast<T>(string shaderType, float[] values, out T value, T fallback = default)
        {
            try
            {
                return Cast<T>(shaderType, values, out value, fallback);
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static bool Cast<T>(string shaderType, float[] values, out T value, T fallback = default)
        {
            object obj = default;
            value = fallback;

            if (values == null || values.Length == 0)
                return false;

            float[] v = new float[16];
            for (int i = 0; i < values.Length; ++i)
                v[i] = values[i];

            if (!TryParseTypeInfo(shaderType, out string prim, out bool isScalar, out bool isVector, out bool isMatrix, out int rows, out int cols, out int length))
                return false;

            if (isScalar || isVector && rows == 1 || isMatrix && rows == 1 && cols == 1)
            {
                var fv = v[0];

                switch (prim)
                {
                    case "bool": obj = fv != 0; break;
                    case "uint": obj = fv; break;
                    case "int": obj = fv; break;
                    case "float":
                    case "half": obj = fv; break;
                    default: return false;
                }
            }
            if (isVector)
            {
                switch (rows)
                {
                    case 2: obj = new Vector2(v[0], v[1]); break;
                    case 3: obj = new Vector3(v[0], v[1], v[2]); break;
                    case 4: obj = new Vector4(v[0], v[1], v[2], v[3]); break;
                    default: return false;
                }

            }
            if (isMatrix)
            {
                Matrix4x4 m = new();
                for (int i = 0; i < cols * rows; ++i)
                {
                    // TODO: Validate with GTK
                    m[i % rows + rows * (i / rows)] = v[i];
                }
            }

            value = (T)obj;
            return true;
        }

        internal static MaterialSlot MakeSlotFromParameter(ParameterHeader header, int slotId, SlotType dir)
        {
            MaterialSlot slot;

            if (header.isDropdown)
            {
                HeaderUtils.TryCast<float>(header.typeName, header.defaultValue, out var asIdx, 0.0f);
                slot = new Vector1MaterialEnumSlot(slotId, header.displayName, header.referenceName, dir, header.options, asIdx);
            }
            else if (header.isSlider)
            {
                HeaderUtils.TryCast<float>(header.typeName, header.defaultValue, out var asRng, 0);
                slot = new Vector1MaterialRangeSlot(slotId, header.displayName, header.referenceName, dir, asRng, new Vector2(header.sliderMin, header.sliderMax));
            }
            else switch (header.typeName)
                {
                    case "uint1":
                    case "uint":
                        HeaderUtils.TryCast<float>(header.typeName, header.defaultValue, out var asUint, 0u);
                        slot = new Vector1MaterialIntegerSlot(slotId, header.displayName, header.referenceName, dir, asUint, unsigned: true); break;
                    case "int1":
                    case "int":
                        HeaderUtils.TryCast<float>(header.typeName, header.defaultValue, out var asInt, 0);
                        slot = new Vector1MaterialIntegerSlot(slotId, header.displayName, header.referenceName, dir, asInt); break;
                    case "half":
                    case "half1":
                    case "float1":
                    case "float":
                        HeaderUtils.TryCast<float>(header.typeName, header.defaultValue, out var asFloat, 0.0f);
                        slot = new Vector1MaterialSlot(slotId, header.displayName, header.referenceName, dir, asFloat); break;
                    case "bool1":
                    case "bool":
                        HeaderUtils.TryCast<bool>(header.typeName, header.defaultValue, out var asBool, false);
                        slot = new BooleanMaterialSlot(slotId, header.displayName, header.referenceName, dir, asBool); break;
                    case "half2":
                    case "float2":
                        HeaderUtils.TryCast<Vector2>(header.typeName, header.defaultValue, out var asV2, Vector2.zero);
                        slot = new Vector2MaterialSlot(slotId, header.displayName, header.referenceName, dir, asV2); break;
                    case "half3":
                    case "float3":
                        HeaderUtils.TryCast<Vector3>(header.typeName, header.defaultValue, out var asV3, Vector3.zero);
                        slot = !header.isColor
                            ? new Vector3MaterialSlot(slotId, header.displayName, header.referenceName, dir, asV3)
                            : new ColorRGBMaterialSlot(slotId, header.displayName, header.referenceName, dir, new Vector4(asV3.x, asV3.y, asV3.z, 1), Internal.ColorMode.Default);
                        break;
                    case "half4":
                    case "float4":
                        HeaderUtils.TryCast<Vector4>(header.typeName, header.defaultValue, out var asV4, header.isColor ? new Vector4(0, 0, 0, 1) : Vector4.zero);

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

            slot.hideConnector = header.isStatic && header.isInput;
            slot.hidden = header.isLocal;
            slot.bareResource = header.isBareResource;
            return slot;
        }

        internal static float[] LazyTokenFloat(string arg)
        {
            var tokens = LazyTokenize(arg);
            List<float> asfloat = new();

            foreach (var token in tokens)
                if (float.TryParse(token, out float r))
                    asfloat.Add(r);

            return asfloat.ToArray();
        }

        internal static string[] LazyTokenString(string arg)
        {
            List<string> tokens = new(LazyTokenize(arg));
            return tokens.ToArray();
        }

        private static IEnumerable<string> LazyTokenize(string arg)
        {
            foreach (var e in arg.Split(','))
            {
                var result = e.Trim();
                if (!string.IsNullOrWhiteSpace(result))
                    yield return e.Trim();
            }
        }
    }
}
