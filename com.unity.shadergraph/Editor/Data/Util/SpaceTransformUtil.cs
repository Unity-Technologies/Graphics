// using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class SpaceTransformUtil
    {
        public const int kLatestVersion = 1;

        public struct SpaceTransform
        {
            public CoordinateSpace from;
            public CoordinateSpace to;
            public ConversionType type;
            // public bool normalize;
        };

        delegate void TransformFunction(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion);

        public static string GenerateTangentTransform(ShaderStringBuilder sb, CoordinateSpace tangentTransformSpace)
        {
            sb.AppendLine("$precision3x3 tangentTransform = $precision3x3(IN.", tangentTransformSpace, "SpaceTangent, IN.", tangentTransformSpace, "SpaceBiTangent, IN.", tangentTransformSpace, "SpaceNormal);");
            return "tangentTransform";
        }

        public static string GenerateTransposeTangentTransform(ShaderStringBuilder sb, CoordinateSpace tangentTransformSpace = CoordinateSpace.World)
        {
            var tangentTransform = GenerateTangentTransform(sb, tangentTransformSpace);
            sb.AppendLine("$precision3x3 transposeTangentTransform = transpose(tangentTransform);");
            return "transposeTangentTransform";
        }

        public static void Identity(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            // if (xform.normalize && (xform.type != ConversionType.Position))
            //    sb.AppendLine(outputVariable, " = SafeNormalize(", inputValue, ");");
            // else
            sb.AppendLine(outputVariable, " = ", inputValue, ";");
        }

        private static void ViaWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            // should never be calling this if one of the spaces is already world space (silly, and could lead to infinite recursions)
            if ((xform.from == CoordinateSpace.World) || (xform.to == CoordinateSpace.World))
                return;

            // this breaks the transform into two parts: (from->world) and (world->to)
            var fromToWorld = new SpaceTransform()
            {
                from = xform.from,
                to = CoordinateSpace.World,
                type = xform.type
            };

            var worldToTo = new SpaceTransform()
            {
                from = CoordinateSpace.World,
                to = xform.to,
                type = xform.type
            };

            using (sb.BlockScope())
            {
                sb.AppendLine("// Converting ", xform.from, " to ", xform.to, " via world space");
                sb.AppendLine("float3 world;");
                GenerateTransformCodeStatement(fromToWorld, inputValue, "world", sb, version);
                GenerateTransformCodeStatement(worldToTo, "world", outputVariable, sb, version);
            }
        }

        public static void WorldToObject(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = TransformWorldToObject(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    // float3 TransformWorldToObjectDir(float3 dirWS, bool doNormalize = true)
                    sb.AppendLine(outputVariable, " = TransformWorldToObjectDir(", inputValue, ");");
                    break;
            }
        }

        public static void WorldToTangent(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            using (sb.BlockScope())
            {
                string tangentTransform = GenerateTangentTransform(sb, xform.from);
                // TransformWorldToTangent ALWAYS normalizes
                sb.AppendLine(outputVariable, " = TransformWorldToTangent(", inputValue, ", ", tangentTransform, ")");
                // NOTE ^ this is a direction transform ONLY... but probably have to version it to make it transform position correctly..
            }
        }

        public static void WorldToView(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = TransformWorldToView(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    // real3 TransformWorldToViewDir(real3 dirWS, bool doNormalize = false)
                    sb.AppendLine(outputVariable, " = TransformWorldToViewDir(", inputValue, ");");
                    break;
            }
        }

        public static void WorldToAbsoluteWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = GetAbsolutePositionWS(", inputValue, ");");
                    break;
                case ConversionType.Direction:  // BEHAVIOR CHANGE -- VERSION NEEDED?
                    // both normal and direction are unchanged
                    sb.AppendLine(outputVariable, " = ", inputValue, ";");
                    break;
            }
        }

        public static void ObjectToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = TransformObjectToWorld(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    // float3 TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)
                    sb.AppendLine(outputVariable, " = TransformObjectToWorldDir(", inputValue, ");");
                    break;
            }
        }

        public static void ObjectToAbsoluteWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    ViaWorld(xform, inputValue, outputVariable, sb, version);
                    break;
                case ConversionType.Direction:
                    // float3 TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)
                    sb.AppendLine(outputVariable, " = TransformObjectToWorldDir(", inputValue, ");");
                    break;
            }
        }

        public static void TangentToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            using (sb.BlockScope())
            {
                string transposeTangentTransform = GenerateTransposeTangentTransform(sb, xform.from);
                switch (xform.type)
                {
                    case ConversionType.Position:
                        sb.AppendLine(outputVariable, " = mul(", transposeTangentTransform, ", ", inputValue, ").xyz;");
                        break;
                    case ConversionType.Direction:
                        sb.AppendLine(outputVariable, " = normalize(mul(", transposeTangentTransform, ", ", inputValue, ").xyz);");
                        break;
                }
            }
        }

        public static void ViewToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = mul(UNITY_MATRIX_I_V, $precision4(", inputValue, ", 1)).xyz;");
                    break;
                case ConversionType.Direction:
                    sb.AppendLine(outputVariable, " = mul(UNITY_MATRIX_I_V, $precision4(", inputValue, ", 0)).xyz;");
                    break;
            }
        }

        public static void AbsoluteWorldToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AppendLine(outputVariable, " = GetCameraRelativePositionWS(", inputValue, ");");
                    break;
                case ConversionType.Direction:  // BEHAVIOR CHANGE -- VERSION NEEDED?
                    // both normal and direction are unchanged
                    sb.AppendLine(outputVariable, " = ", inputValue, ";");
                    break;
            }
        }

        static readonly TransformFunction[,] k_TransformFunctions = new TransformFunction[5, 5]   // [from, to]
        {
            {   // from CoordinateSpace.Object
                Identity,               // to CoordinateSpace.Object
                ViaWorld,               // to CoordinateSpace.View
                ObjectToWorld,          // to CoordinateSpace.World
                ViaWorld,               // to CoordinateSpace.Tangent
                ObjectToAbsoluteWorld,  // to CoordinateSpace.AbsoluteWorld
            },
            {   // from CoordinateSpace.View
                ViaWorld,               // to CoordinateSpace.Object
                Identity,               // to CoordinateSpace.View
                ViewToWorld,            // to CoordinateSpace.World
                ViaWorld,               // to CoordinateSpace.Tangent
                ViaWorld,               // to CoordinateSpace.AbsoluteWorld
            },
            {   // from CoordinateSpace.World
                WorldToObject,          // to CoordinateSpace.Object
                WorldToView,            // to CoordinateSpace.View
                Identity,               // to CoordinateSpace.World
                WorldToTangent,         // to CoordinateSpace.Tangent
                WorldToAbsoluteWorld,   // to CoordinateSpace.AbsoluteWorld
            },
            {   // from CoordinateSpace.Tangent
                ViaWorld,               // to CoordinateSpace.Object
                ViaWorld,               // to CoordinateSpace.View
                TangentToWorld,         // to CoordinateSpace.World
                Identity,               // to CoordinateSpace.Tangent
                ViaWorld,               // to CoordinateSpace.AbsoluteWorld
            },
            {   // from CoordinateSpace.AbsoluteWorld
                ViaWorld,               // to CoordinateSpace.Object
                ViaWorld,               // to CoordinateSpace.View
                AbsoluteWorldToWorld,   // to CoordinateSpace.World
                ViaWorld,               // to CoordinateSpace.Tangent
                Identity,               // to CoordinateSpace.AbsoluteWorld
            }
        };

        public static void GenerateTransformCodeStatement(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb, int version = kLatestVersion)
        {
            var func = k_TransformFunctions[(int)xform.from, (int)xform.to];
            func(xform, inputValue, outputVariable, sb, version);
        }
    }
}
