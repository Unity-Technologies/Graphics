// using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    enum ConversionType
    {
        Position,
        Direction,
        Normal
    }

    internal struct SpaceTransform
    {
        public CoordinateSpace from;
        public CoordinateSpace to;
        public ConversionType type;
        public bool normalize;
        public int version;

        public const int kLatestVersion = 2;

        public SpaceTransform(CoordinateSpace from, CoordinateSpace to, ConversionType type, bool normalize = false, int version = kLatestVersion)
        {
            this.from = from;
            this.to = to;
            this.type = type;
            this.normalize = normalize;
            this.version = version;
        }

        internal string NormalizeString()
        {
            return normalize ? "true" : "false";
        }

        // non-identity transforms involving tangent space require the full tangent frame
        public NeededCoordinateSpace RequiresNormal =>
            (((from == CoordinateSpace.Tangent) || (to == CoordinateSpace.Tangent)) && (from != to)) ?
                NeededCoordinateSpace.World : NeededCoordinateSpace.None;
        public NeededCoordinateSpace RequiresTangent =>
            (((from == CoordinateSpace.Tangent) || (to == CoordinateSpace.Tangent)) && (from != to)) ?
                NeededCoordinateSpace.World : NeededCoordinateSpace.None;
        public NeededCoordinateSpace RequiresBitangent =>
            (((from == CoordinateSpace.Tangent) || (to == CoordinateSpace.Tangent)) && (from != to)) ?
                NeededCoordinateSpace.World : NeededCoordinateSpace.None;

        // non-identity position transforms involving tangent space require the world position
        public NeededCoordinateSpace RequiresPosition =>
            ((type == ConversionType.Position) && ((from == CoordinateSpace.Tangent) || (to == CoordinateSpace.Tangent)) && (from != to)) ?
                NeededCoordinateSpace.World : NeededCoordinateSpace.None;

        public IEnumerable<NeededTransform> RequiresTransform =>
            SpaceTransformUtil.ComputeTransformRequirement(this);
    };

    static class SpaceTransformUtil
    {
        delegate void TransformFunction(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb);

        public static string GenerateTangentTransform(ShaderStringBuilder sb, CoordinateSpace tangentTransformSpace)
        {
            sb.AddLine("$precision3x3 tangentTransform = $precision3x3(IN.",
                tangentTransformSpace.ToString(), "SpaceTangent, IN.",
                tangentTransformSpace.ToString(), "SpaceBiTangent, IN.",
                tangentTransformSpace.ToString(), "SpaceNormal);");
            return "tangentTransform";
        }

        public static void Identity(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            // identity didn't normalize before version 2
            if ((xform.version > 1) && xform.normalize && (xform.type != ConversionType.Position))
                sb.AddLine(outputVariable, " = SafeNormalize(", inputValue, ");");
            else
                sb.AddLine(outputVariable, " = ", inputValue, ";");
        }

        private static void ViaWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            // should never be calling this if one of the spaces is already world space (silly, and could lead to infinite recursions)
            if ((xform.from == CoordinateSpace.World) || (xform.to == CoordinateSpace.World))
                return;

            // this breaks the transform into two parts: (from->world) and (world->to)
            var toWorld = new SpaceTransform()
            {
                from = xform.from,
                to = CoordinateSpace.World,
                type = xform.type,
                normalize = false,
                version = xform.version
            };

            var fromWorld = new SpaceTransform()
            {
                from = CoordinateSpace.World,
                to = xform.to,
                type = xform.type,
                normalize = xform.normalize,
                version = xform.version
            };

            // Apply Versioning Hacks to match old (incorrect) versions
            if (xform.version <= 1)
            {
                if (xform.type == ConversionType.Direction)
                {
                    switch (xform.from)
                    {
                        case CoordinateSpace.AbsoluteWorld:
                            if ((xform.to == CoordinateSpace.Object) || (xform.to == CoordinateSpace.View))
                            {
                                // these transforms were wrong in v0, but correct in v1, so here we
                                // pretend it is a later version to disable the v1 versioning in the AbsWorldToWorld transform
                                if (xform.version == 1)
                                    toWorld.version = 2;
                            }
                            break;
                        case CoordinateSpace.View:
                            if ((xform.to == CoordinateSpace.Tangent) || (xform.to == CoordinateSpace.AbsoluteWorld))
                            {
                                // these transforms erroneously used the position view-to-world transform
                                toWorld.type = ConversionType.Position;
                            }
                            break;
                        case CoordinateSpace.Tangent:
                            if ((xform.to == CoordinateSpace.Object) || (xform.to == CoordinateSpace.View) || (xform.to == CoordinateSpace.AbsoluteWorld))
                            {
                                // manually version to 2, to remove normalization (while keeping Normal type)
                                toWorld.type = ConversionType.Normal;
                                toWorld.version = 2;
                            }
                            break;
                    }
                }
            }

            using (sb.BlockScope())
            {
                sb.AddLine("// Converting ", xform.type.ToString(), " from ", xform.from.ToString(), " to ", xform.to.ToString(), " via world space");
                sb.AddLine("float3 world;");
                GenerateTransformCodeStatement(toWorld, inputValue, "world", sb);
                GenerateTransformCodeStatement(fromWorld, "world", outputVariable, sb);
            }
        }

        public static void WorldToObject(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = TransformWorldToObject(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(outputVariable, " = TransformWorldToObjectDir(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(outputVariable, " = TransformWorldToObjectNormal(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        public static void WorldToTangent(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            if (xform.version <= 1)
            {
                // prior to version 2, all transform were normalized, and all transforms were Normal transforms
                xform.normalize = true;
                xform.type = ConversionType.Normal;
            }

            using (sb.BlockScope())
            {
                string tangentTransform = GenerateTangentTransform(sb, xform.from);

                switch (xform.type)
                {
                    case ConversionType.Position:
                        sb.AddLine(outputVariable, " = TransformWorldToTangentDir(", inputValue, " - IN.WorldSpacePosition, ", tangentTransform, ", false);");
                        break;
                    case ConversionType.Direction:
                        sb.AddLine(outputVariable, " = TransformWorldToTangentDir(", inputValue, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                    case ConversionType.Normal:
                        sb.AddLine(outputVariable, " = TransformWorldToTangent(", inputValue, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                }
            }
        }

        public static void WorldToView(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = TransformWorldToView(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = false;
                    sb.AddLine(outputVariable, " = TransformWorldToViewDir(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(outputVariable, " = TransformWorldToViewNormal(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        public static void WorldToAbsoluteWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            // prior to version 2 always used Position transform
            if (xform.version <= 1)
                xform.type = ConversionType.Position;

            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = GetAbsolutePositionWS(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                case ConversionType.Normal:
                    // both normal and direction are unchanged
                    if (xform.normalize)
                        sb.AddLine(outputVariable, " = SafeNormalize(", inputValue, ");");
                    else
                        sb.AddLine(outputVariable, " = ", inputValue, ";");
                    break;
            }
        }

        public static void ObjectToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = TransformObjectToWorld(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(outputVariable, " = TransformObjectToWorldDir(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(outputVariable, " = TransformObjectToWorldNormal(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        public static void ObjectToAbsoluteWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    ViaWorld(xform, inputValue, outputVariable, sb);
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(outputVariable, " = TransformObjectToWorldDir(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(outputVariable, " = TransformObjectToWorldNormal(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        public static void TangentToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            // prior to version 2 all transforms were Normal, and directional transforms were normalized
            if (xform.version <= 1)
            {
                if (xform.type != ConversionType.Position)
                    xform.normalize = true;
                xform.type = ConversionType.Normal;
            }

            using (sb.BlockScope())
            {
                string tangentTransform = GenerateTangentTransform(sb, CoordinateSpace.World);
                switch (xform.type)
                {
                    case ConversionType.Position:
                        sb.AddLine(outputVariable, " = TransformTangentToWorldDir(", inputValue, ", ", tangentTransform, ", false).xyz + IN.WorldSpacePosition;");
                        break;
                    case ConversionType.Direction:
                        sb.AddLine(outputVariable, " = TransformTangentToWorldDir(", inputValue, ", ", tangentTransform, ", ", xform.NormalizeString(), ").xyz;");
                        break;
                    case ConversionType.Normal:
                        sb.AddLine(outputVariable, " = TransformTangentToWorld(", inputValue, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                }
            }
        }

        public static void ViewToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = TransformViewToWorld(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = false;
                    sb.AddLine(outputVariable, " = TransformViewToWorldDir(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(outputVariable, " = TransformViewToWorldNormal(", inputValue, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        public static void AbsoluteWorldToWorld(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            // prior to version 2, always used position transform
            if (xform.version <= 1)
                xform.type = ConversionType.Position;

            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(outputVariable, " = GetCameraRelativePositionWS(", inputValue, ");");
                    break;
                case ConversionType.Direction:
                case ConversionType.Normal:
                    // both normal and direction are unchanged
                    if (xform.normalize)
                        sb.AddLine(outputVariable, " = SafeNormalize(", inputValue, ");");
                    else
                        sb.AddLine(outputVariable, " = ", inputValue, ";");
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

        internal static IEnumerable<NeededTransform> ComputeTransformRequirement(SpaceTransform xform)
        {
            var func = k_TransformFunctions[(int)xform.from, (int)xform.to];
            if (func == ViaWorld || func == ObjectToAbsoluteWorld)
            {
                yield return new NeededTransform(xform.from.ToNeededCoordinateSpace(), NeededCoordinateSpace.World);
                yield return new NeededTransform(NeededCoordinateSpace.World, xform.to.ToNeededCoordinateSpace());
            }
            else
            {
                yield return new NeededTransform(xform.from.ToNeededCoordinateSpace(), xform.to.ToNeededCoordinateSpace());
            }
        }

        public static void GenerateTransformCodeStatement(SpaceTransform xform, string inputValue, string outputVariable, ShaderStringBuilder sb)
        {
            var func = k_TransformFunctions[(int)xform.from, (int)xform.to];
            func(xform, inputValue, outputVariable, sb);
        }
    }
}
