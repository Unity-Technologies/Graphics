using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class ShaderGraphExampleTypes
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

        static readonly IReadOnlyDictionary<TypeHandle, ITypeDescriptor> k_UnderlyingTypes =
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
                {GradientTypeHandle, TYPE.Gradient},
                {Texture2DTypeHandle, TYPE.Texture2D},
                {Texture2DArrayTypeHandle, TYPE.Texture2DArray},
                {Texture3DTypeHandle, TYPE.Texture3D},
                {CubemapTypeHandle, TYPE.TextureCube},
                {SamplerStateTypeHandle, TYPE.SamplerState},
            };

        /// <summary>
        /// Maps this TypeHandle to the best existing ITypeDescriptor to represent its data.
        /// </summary>
        internal static ITypeDescriptor ToDescriptor(this TypeHandle typeHandle)
        {
            return k_UnderlyingTypes[typeHandle];
        }

        internal static bool IsExposable(this TypeHandle typeHandle)
        {
            var descriptor = typeHandle.ToDescriptor();
            switch (descriptor)
            {
                case ParametricTypeDescriptor {Height: GraphType.Height.One}:
                case TextureTypeDescriptor:
                case SamplerStateTypeDescriptor:
                    return true;
                default:
                    return false;
            }
        }

        // This is a sister function used with ShaderGraphStencil.GetConstantNodeValueType--
        // TypeHandles are primarily used to setup the icon that GTF will use,
        // but the TypeHandle then gets routed through GetConstantNodeValueType where a type handle is
        // mapped to an IConstant type-- it's a bit round about, but for SG's purposes, we only care about
        // having an IConstant impl for the type if it has an inline editor for the port.
        // If the IConstant return type doesn't have one setup by default,
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

    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
    static class GDSExt
    {
        public static VisualElement BuildGradientTypeConstantEditor(this IConstantEditorBuilder builder, GradientTypeConstant constant)
        {
            var editor = new GradientField();
            editor.AddToClassList("sg-gradient-constant-field");
            editor.AddStylesheet("ConstantEditors.uss");
            editor.RegisterValueChangedCallback(change => builder.OnValueChanged(change)); // I guess this is supposed to be a CSO command instead?
            return editor;
        }

        public static BaseModelPropertyField BuildGraphTypeConstantEditor(this IConstantEditorBuilder builder, GraphTypeConstant constant)
        {
            if (builder.ConstantOwner is not GraphDataPortModel graphDataPort)
                return builder.BuildDefaultConstantEditor(constant);


            // Try/Catch maybe.
            ((GraphDataNodeModel)graphDataPort.NodeModel).TryGetNodeHandler(out var nodeReader);

            var length = constant.GetLength();
            var stencil = (ShaderGraphStencil)graphDataPort.GraphModel.Stencil;
            var nodeUIDescriptor = stencil.GetUIHints(graphDataPort.owner.registryKey, nodeReader);
            var parameterUIDescriptor = nodeUIDescriptor.GetParameterInfo(constant.PortName);

            if ((int)length >= 3 && parameterUIDescriptor.UseColor)
            {
                var constantEditor = new SGModelPropertyField<Color>(
                    builder.CommandTarget as RootView,
                    builder.ConstantOwner,
                    parameterUIDescriptor.Name,
                    "",
                    parameterUIDescriptor.Tooltip);

                void OnValueChanged(ChangeEvent<Color> change)
                {
                    Vector4 vector4Value = (Vector4)change.newValue;
                    Vector3 vector3Value = vector4Value;
                    builder.CommandTarget.Dispatch(new UpdateConstantValueCommand(constant, (int)length == 3 ? vector3Value : vector4Value, builder.ConstantOwner));
                }

                if (constantEditor.PropertyField is ColorField colorField)
                {
                    colorField.showAlpha = (int)length == 4;
                    colorField.RegisterValueChangedCallback(OnValueChanged);
                }

                return constantEditor;
            }

            return builder.BuildDefaultConstantEditor(constant);
        }
    }
}

//    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
//    static class GDSExt
//    {
//        public static VisualElement BuildDefaultConstantEditor(this IConstantEditorBuilder builder, GraphTypeConstant constant)
//        {
//            VisualElement container = new VisualElement();
//            container.Add(ConstantEditorExtensions.BuildDefaultConstantEditor(builder, constant));



//            Action<IChangeEvent> myValueChanged = evt =>
//            {
//                if (evt != null) // Enum editor sends null
//                {
//                    var p = evt.GetType().GetProperty("newValue");
//                    var newValue = p.GetValue(evt);
//                    builder.CommandDispatcher.Dispatch(new UpdateConstantValueCommand(constant.lengthConstant, newValue, null));
//                    var reg = ((ShaderGraphStencil)((ShaderGraphModel)builder.PortModel.GraphModel).Stencil).GetRegistry();
//                    constant.graphHandler.ReconcretizeNode(constant.nodeName, reg);
//                    builder.PortModel.NodeModel.OnCreateNode();
//                }
//            };
//            var build = new ConstantEditorBuilder(myValueChanged, builder.CommandDispatcher, false, null);
//            container.Add(ConstantEditorExtensions.BuildDefaultConstantEditor(build, constant.lengthConstant));

//            return container;
//        }
//    }
//}


/*
class GraphStruct : Constant<List<IConstant>>
{
    // This isn't safe-- List is no good here.
    public GraphStruct() { ObjectValue = new List<IConstant>(); }
}


[GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
static class GDSExt
{
    public static VisualElement BuildDefaultConstantEditor(this IConstantEditorBuilder builder, GraphStruct constant)
    {
        VisualElement container = new VisualElement();

        foreach (var member in (List<IConstant>)constant.ObjectValue)
        {
            Action<IChangeEvent> myValueChanged = evt =>
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p.GetValue(evt);
                    builder.CommandDispatcher.Dispatch(new UpdateConstantValueCommand(member, newValue, null));
                }
            };
            var build = new ConstantEditorBuilder(myValueChanged, builder.CommandDispatcher, false, null);
            container.Add(ConstantEditorExtensions.BuildDefaultConstantEditor(build, member));
        }
        return container;
    }
}



[SearcherItem(typeof(ShaderGraphStencil), SearcherContext.Graph, "Testing Node")]
class TestNodeModel : NodeModel
{
    protected override void OnDefineNode()
    {
        var port = AddInputPort("MyPort", PortType.Data, ShaderGraphExampleTypes.GraphStruct, initializationCallback:
            (IConstant c) =>
            {
                var d = c as GraphStruct;
                d.ObjectValue = new List<IConstant>();
                ((List<IConstant>)d.ObjectValue).Add(new Vector3Constant());
                ((List<IConstant>)d.ObjectValue).Add(new BooleanConstant());
            });
    }
}
*/
