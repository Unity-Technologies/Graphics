using UnityEditor.ShaderFoundry;

// TODO: Remove hardcoded float precision
// TODO: Once this works, see how it can be simplified to better fit in SG2

namespace UnityEditor.ShaderGraph.GraphDelta
{
    enum CoordinateSpace
    {
        Object,
        View,
        World,
        Tangent,
        AbsoluteWorld
    }

    enum ConversionType
    {
        Position,
        Direction,
        Normal
    }

    struct SpaceTransform
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
    }

    struct TransformValues
    {
        public string Input, WorldTangent, WorldBiTangent, WorldNormal, WorldPosition;
        public string OutputVariable;
    }

    static class SpaceTransformUtils
    {
        delegate void TransformFunction(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb);

        static string GenerateTangentTransform(ShaderFunction.Builder sb, string tangentVec, string biTangentVec, string normalVec)
        {
            const string name = "tangentTransform";

            var scalarType = ShaderType.Scalar(sb.Container, "float"); // TODO: precision?
            var matrixType = ShaderType.Matrix(sb.Container, scalarType, 3, 3);
            var value = $"{{{tangentVec}, {biTangentVec}, {normalVec}}}";

            sb.DeclareVariable(matrixType, name, value);
            return name;
        }

        static void Identity(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            // identity didn't normalize before version 2
            if ((xform.version > 1) && xform.normalize && (xform.type != ConversionType.Position))
                sb.AddLine(transformValues.OutputVariable, " = SafeNormalize(", transformValues.Input, ");");
            else
                sb.AddLine(transformValues.OutputVariable, " = ", transformValues.Input, ";");
        }

        static void ViaWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
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
                var scalarType = ShaderType.Scalar(sb.Container, "float"); // TODO: precision?
                var vectorType = ShaderType.Vector(sb.Container, scalarType, 3);

                sb.AddLine("// Converting ", xform.type.ToString(), " from ", xform.from.ToString(), " to ", xform.to.ToString(), " via world space");
                sb.DeclareVariable(vectorType, "world");

                var toWorldInfo = transformValues;
                toWorldInfo.OutputVariable = "world";
                GenerateTransform(toWorld, toWorldInfo, sb);

                var fromWorldInfo = transformValues;
                fromWorldInfo.Input = "world";
                GenerateTransform(fromWorld, fromWorldInfo, sb);
            }
        }

        static void WorldToObject(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToObject(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToObjectDir(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToObjectNormal(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        static void WorldToTangent(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            if (xform.version <= 1)
            {
                // prior to version 2, all transform were normalized, and all transforms were Normal transforms
                xform.normalize = true;
                xform.type = ConversionType.Normal;
            }

            using (sb.BlockScope())
            {
                string tangentTransform = GenerateTangentTransform(sb, transformValues.WorldTangent, transformValues.WorldBiTangent, transformValues.WorldNormal);

                switch (xform.type)
                {
                    case ConversionType.Position:
                        sb.AddLine(transformValues.OutputVariable, " = TransformWorldToTangentDir(", transformValues.Input, " - ", transformValues.WorldPosition, ", ", tangentTransform, ", false);");
                        break;
                    case ConversionType.Direction:
                        sb.AddLine(transformValues.OutputVariable, " = TransformWorldToTangentDir(", transformValues.Input, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                    case ConversionType.Normal:
                        sb.AddLine(transformValues.OutputVariable, " = TransformWorldToTangent(", transformValues.Input, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                }
            }
        }

        static void WorldToView(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToView(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = false;
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToViewDir(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(transformValues.OutputVariable, " = TransformWorldToViewNormal(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        static void WorldToAbsoluteWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            // prior to version 2 always used Position transform
            if (xform.version <= 1)
                xform.type = ConversionType.Position;

            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = GetAbsolutePositionWS(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                case ConversionType.Normal:
                    // both normal and direction are unchanged
                    if (xform.normalize)
                        sb.AddLine(transformValues.OutputVariable, " = SafeNormalize(", transformValues.Input, ");");
                    else
                        sb.AddLine(transformValues.OutputVariable, " = ", transformValues.Input, ";");
                    break;
            }
        }

        static void ObjectToWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = TransformObjectToWorld(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(transformValues.OutputVariable, " = TransformObjectToWorldDir(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(transformValues.OutputVariable, " = TransformObjectToWorldNormal(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        static void ObjectToAbsoluteWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    ViaWorld(xform, transformValues, sb);
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = true;
                    sb.AddLine(transformValues.OutputVariable, " = TransformObjectToWorldDir(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(transformValues.OutputVariable, " = TransformObjectToWorldNormal(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        static void TangentToWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
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
                string tangentTransform = GenerateTangentTransform(sb, transformValues.WorldTangent, transformValues.WorldBiTangent, transformValues.WorldNormal);
                switch (xform.type)
                {
                    case ConversionType.Position:
                        sb.AddLine(transformValues.OutputVariable, " = TransformTangentToWorldDir(", transformValues.Input, ", ", tangentTransform, ", false).xyz + ", transformValues.WorldPosition, ";");
                        break;
                    case ConversionType.Direction:
                        sb.AddLine(transformValues.OutputVariable, " = TransformTangentToWorldDir(", transformValues.Input, ", ", tangentTransform, ", ", xform.NormalizeString(), ").xyz;");
                        break;
                    case ConversionType.Normal:
                        sb.AddLine(transformValues.OutputVariable, " = TransformTangentToWorld(", transformValues.Input, ", ", tangentTransform, ", ", xform.NormalizeString(), ");");
                        break;
                }
            }
        }

        static void ViewToWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = TransformViewToWorld(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                    if (xform.version <= 1)
                        xform.normalize = false;
                    sb.AddLine(transformValues.OutputVariable, " = TransformViewToWorldDir(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
                case ConversionType.Normal:
                    sb.AddLine(transformValues.OutputVariable, " = TransformViewToWorldNormal(", transformValues.Input, ", ", xform.NormalizeString(), ");");
                    break;
            }
        }

        static void AbsoluteWorldToWorld(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            // prior to version 2, always used position transform
            if (xform.version <= 1)
                xform.type = ConversionType.Position;

            switch (xform.type)
            {
                case ConversionType.Position:
                    sb.AddLine(transformValues.OutputVariable, " = GetCameraRelativePositionWS(", transformValues.Input, ");");
                    break;
                case ConversionType.Direction:
                case ConversionType.Normal:
                    // both normal and direction are unchanged
                    if (xform.normalize)
                        sb.AddLine(transformValues.OutputVariable, " = SafeNormalize(", transformValues.Input, ");");
                    else
                        sb.AddLine(transformValues.OutputVariable, " = ", transformValues.Input, ";");
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

        public static void GenerateTransform(SpaceTransform xform, TransformValues transformValues, ShaderFunction.Builder sb)
        {
            var func = k_TransformFunctions[(int)xform.from, (int)xform.to];
            func(xform, transformValues, sb);
        }
    }
}
