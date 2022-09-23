using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    static class ShaderGraphExampleTypes
    {
        public static readonly TypeHandle Color = typeof(Color).GenerateTypeHandle();
        public static readonly TypeHandle AnimationClip = typeof(AnimationClip).GenerateTypeHandle();
        public static readonly TypeHandle Mesh = typeof(Mesh).GenerateTypeHandle();
        public static readonly TypeHandle Texture2DTypeHandle = typeof(Texture2D).GenerateTypeHandle();
        public static readonly TypeHandle Texture3DTypeHandle = typeof(Texture3D).GenerateTypeHandle();
        public static readonly TypeHandle Texture2DArrayTypeHandle = typeof(Texture2DArray).GenerateTypeHandle();
        public static readonly TypeHandle CubemapTypeHandle = typeof(Cubemap).GenerateTypeHandle();
        public static readonly TypeHandle GradientTypeHandle = typeof(Gradient).GenerateTypeHandle();
        public static readonly TypeHandle SamplerStateTypeHandle = typeof(SamplerStateData).GenerateTypeHandle();
        public static readonly TypeHandle Matrix2 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix 2");
        public static readonly TypeHandle Matrix3 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix 3");
        public static readonly TypeHandle Matrix4 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix 4");

        static readonly IReadOnlyDictionary<TypeHandle, ITypeDescriptor> k_BackingTypeDescriptors =
            new Dictionary<TypeHandle, ITypeDescriptor>
            {
                {TypeHandle.Int, TYPE.Int},
                {TypeHandle.Float, TYPE.Float},
                {TypeHandle.Bool, TYPE.Bool},
                {TypeHandle.Vector2, TYPE.Vec2},
                {TypeHandle.Vector3, TYPE.Vec3},
                {TypeHandle.Vector4, TYPE.Vec4},
                {Color, TYPE.Vec4},
                {Matrix2, TYPE.Mat2},
                {Matrix3, TYPE.Mat3},
                {Matrix4, TYPE.Mat4},
                // {GradientTypeHandle, TYPE.Gradient}, todo: https://jira.unity3d.com/browse/GSG-1290
                {Texture2DTypeHandle, TYPE.Texture2D},
                {Texture2DArrayTypeHandle, TYPE.Texture2DArray},
                {Texture3DTypeHandle, TYPE.Texture3D},
                {CubemapTypeHandle, TYPE.TextureCube},
                {SamplerStateTypeHandle, TYPE.SamplerState},
            };

        public static IEnumerable<TypeHandle> AllUiTypes => k_BackingTypeDescriptors.Keys;

        // TODO: Should eventually exclude virtual textures
        public static IEnumerable<TypeHandle> SubgraphOutputTypes => AllUiTypes.Where(t => t != Color);

        public static IEnumerable<TypeHandle> BlackboardTypes => AllUiTypes;

        /// <summary>
        /// Maps this TypeHandle to the best existing ITypeDescriptor to represent its data.
        /// </summary>
        internal static ITypeDescriptor GetBackingDescriptor(this TypeHandle typeHandle)
        {
            return k_BackingTypeDescriptors[typeHandle];
        }

        // This is a sister function used with ShaderGraphStencil.GetConstantNodeValueType--
        // TypeHandles are primarily used to setup the icon that GTF will use,
        // but the TypeHandle then gets routed through GetConstantNodeValueType where a type handle is
        // mapped to an Constant type-- it's a bit round about, but for SG's purposes, we only care about
        // having an Constant impl for the type if it has an inline editor for the port.
        // If the Constant return type doesn't have one setup by default,
        // It can be expressed such as:
        //    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
        //    static class GDSExt
        //    {
        //        public static VisualElement BuildDefaultConstantEditor(this IConstantEditorBuilder builder, GraphTypeConstant constant)
        //
        public static TypeHandle GetGraphType(PortHandler reader) // TODO: Get rid of this.
        {
            var field = reader.GetTypeField();

            var key = field.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName);

            if (key.Name == GraphType.kRegistryKey.Name)
            {
                var len = GraphTypeHelpers.GetLength(field);
                var height = GraphTypeHelpers.GetHeight(field);

                switch ((int)len)
                {
                    case 1:
                        var prim = GraphTypeHelpers.GetPrimitive(field);
                        switch (prim)
                        {
                            case GraphType.Primitive.Int: return TypeHandle.Int;
                            case GraphType.Primitive.Bool: return TypeHandle.Bool;
                            default: return TypeHandle.Float;
                        }
                    case 2 when height is GraphType.Height.Two: return Matrix2;
                    case 2: return TypeHandle.Vector2;
                    case 3 when height is GraphType.Height.Three: return Matrix3;
                    case 3: return TypeHandle.Vector3;
                    case 4 when height is GraphType.Height.Four: return Matrix4;
                    case 4: return TypeHandle.Vector4;
                }
            }
            else if (key.Name == GradientType.kRegistryKey.Name)
                return GradientTypeHandle;

            else if (key.Name == BaseTextureType.kRegistryKey.Name)
            {
                switch (BaseTextureType.GetTextureType(field))
                {
                    case BaseTextureType.TextureType.Texture3D: return Texture3DTypeHandle;
                    case BaseTextureType.TextureType.CubeMap: return CubemapTypeHandle;
                    case BaseTextureType.TextureType.Texture2DArray: return Texture2DArrayTypeHandle;
                    case BaseTextureType.TextureType.Texture2D:
                    default: return Texture2DTypeHandle;
                }
            }

            else if (key.Name == SamplerStateType.kRegistryKey.Name)
                return SamplerStateTypeHandle;

            return TypeHandle.Unknown;
        }

        public static GraphType.Height GetGraphTypeHeight(TypeHandle th)
        {
            if (th == Matrix4) return GraphType.Height.Four;
            if (th == Matrix3) return GraphType.Height.Three;
            if (th == Matrix2) return GraphType.Height.Two;

            return GraphType.Height.One;
        }

        public static GraphType.Length GetGraphTypeLength(TypeHandle th)
        {
            if (th == Matrix4 || th == TypeHandle.Vector4 || th == Color) return GraphType.Length.Four;
            if (th == Matrix3 || th == TypeHandle.Vector3) return GraphType.Length.Three;
            if (th == Matrix2 || th == TypeHandle.Vector2) return GraphType.Length.Two;

            return GraphType.Length.One;
        }

        public static GraphType.Primitive GetGraphTypePrimitive(TypeHandle th)
        {
            if (th == TypeHandle.Bool) return GraphType.Primitive.Bool;
            if (th == TypeHandle.Int) return GraphType.Primitive.Int;

            return GraphType.Primitive.Float;
        }
    }
}
