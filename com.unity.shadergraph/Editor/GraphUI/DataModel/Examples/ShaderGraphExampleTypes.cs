using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// GTF constant type for System.DayOfWeek, used to create a simple custom type w/ inline editor
    /// </summary>
    public class DayOfWeekConstant : Constant<DayOfWeek>
    {
        public static readonly List<DayOfWeek> Values = new((DayOfWeek[])Enum.GetValues(typeof(DayOfWeek)));
        public static readonly List<string> Names = new(Enum.GetNames(typeof(DayOfWeek)));
    }
    public static class ShaderGraphExampleTypes
    {

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
        public static TypeHandle GetGraphType(PortHandler reader)
        {
            var field = reader.GetTypeField();

            var key = field.GetMetadata<RegistryKey>(GraphDelta.GraphDelta.kRegistryKeyName);

            if (key.Name == GraphType.kRegistryKey.Name)
            {
                var len = GraphTypeHelpers.GetLength(field);
                switch ((int)len)
                {
                    case 1:
                        var prim = GraphTypeHelpers.GetPrimitive(field);
                            switch(prim)
                            {
                                case GraphType.Primitive.Int: return TypeHandle.Int;
                                case GraphType.Primitive.Bool: return TypeHandle.Bool;
                                default: return TypeHandle.Float;
                            }
                    case 2: return TypeHandle.Vector2;
                    case 3: return TypeHandle.Vector3;
                    case 4: return TypeHandle.Vector4;
                }
            }
            else if (key.Name == GradientType.kRegistryKey.Name)
                return GradientTypeHandle;

            return TypeHandle.Unknown;
        }

        public static readonly TypeHandle Color = typeof(Color).GenerateTypeHandle();
        public static readonly TypeHandle AnimationClip = typeof(AnimationClip).GenerateTypeHandle();
        public static readonly TypeHandle Mesh = typeof(Mesh).GenerateTypeHandle();
        public static readonly TypeHandle Texture2D = typeof(Texture2D).GenerateTypeHandle();
        public static readonly TypeHandle Texture3D = typeof(Texture3D).GenerateTypeHandle();
        public static readonly TypeHandle DayOfWeek = typeof(DayOfWeek).GenerateTypeHandle();
        public static readonly TypeHandle GradientTypeHandle = typeof(Gradient).GenerateTypeHandle();

        public static readonly TypeHandle Matrix2 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix2");
        public static readonly TypeHandle Matrix3 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix3");
        public static readonly TypeHandle Matrix4 = TypeHandleHelpers.GenerateCustomTypeHandle("Matrix4");


        public static readonly Dictionary<string, TypeHandle> TypeHandlesByName = new()
        {
            { "MissingType", TypeHandle.MissingType },
            { "Unknown", TypeHandle.Unknown },
            { "ExecutionFlow", TypeHandle.ExecutionFlow },
            { "MissingPort", TypeHandle.MissingPort },
            { "Bool", TypeHandle.Bool },
            { "Void", TypeHandle.Void },
            { "Char", TypeHandle.Char },
            { "Double", TypeHandle.Double },
            { "Float", TypeHandle.Float },
            { "Int", TypeHandle.Int },
            { "UInt", TypeHandle.UInt },
            { "Long", TypeHandle.Long },
            { "Object", TypeHandle.Object },
            { "GameObject", TypeHandle.GameObject },
            { "String", TypeHandle.String },
            { "Vector2", TypeHandle.Vector2 },
            { "Vector3", TypeHandle.Vector3 },
            { "Vector4", TypeHandle.Vector4 },
            { "Quaternion", TypeHandle.Quaternion },
            { "Color", Color },
            { "AnimationClip", AnimationClip },
            { "Mesh", Mesh },
            { "Texture2D", Texture2D },
            { "Texture3D", Texture3D },
            { "DayOfWeek", DayOfWeek },
            { "Gradient", GradientTypeHandle },
            { "Matrix2", Matrix2 },
            { "Matrix3", Matrix3 },
            { "Matrix4", Matrix4 },
        };

        public static IEnumerable<string> TypeHandleNames => TypeHandlesByName.Keys;
    }

    interface ICLDSConstant
    {
        public string NodeName { get; }
        public string PortName { get; }

        void Initialize(GraphHandler handler, string nodeName, string portName);
    }

    public class GraphTypeConstant : IConstant, ICLDSConstant
    {
        public GraphHandler graphHandler;
        public string nodeName, portName;

        public string NodeName => nodeName;
        public string PortName => portName;

        bool IsInitialized => nodeName != null && nodeName != "" && graphHandler != null;

        internal int GetLength()
        {
            if (!IsInitialized) return -1;
            var nodeReader = graphHandler.GetNode(nodeName);
            var portReader = nodeReader.GetPort(portName);
            var field = portReader.GetTypeField().GetSubField<GraphType.Length>(GraphType.kLength);
            return (int)field.GetData();
        }

        private GraphType.Primitive GetPrimitive()
        {
            if (!IsInitialized) return GraphType.Primitive.Float;
            var nodeReader = graphHandler.GetNode(nodeName);
            var portReader = nodeReader.GetPort(portName);
            var field = portReader.GetTypeField().GetSubField<GraphType.Primitive>(GraphType.kPrimitive);
            return field.GetData();
        }

        private float gc(int i)
        {
            if (!IsInitialized) return 0;
            var nodeReader = graphHandler.GetNode(nodeName);
            var port = nodeReader.GetPort(portName);
            return port.GetTypeField().GetSubField<float>($"c{i}").GetData();
        }

        private void sc(int i, float v)
        {
            if (!IsInitialized) return;
            var node = graphHandler.GetNode(nodeName);
            var port = node.GetPort(portName);
            var field = port.GetTypeField().GetSubField<float>($"c{i}");
            field.SetData(v);
        }

        public void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName)
        {
            if (IsInitialized) return;
            graphHandler = handler;
            this.nodeName = nodeName;
            this.portName = portName;
        }

        public void Initialize(TypeHandle constantTypeHandle)
        {

        }

        public IConstant Clone()
        {
            return null;
        }

        public TypeHandle GetTypeHandle()
        {
            switch (GetLength())
            {
                case 1:
                    switch (GetPrimitive())
                    {
                        case GraphType.Primitive.Int: return TypeHandle.Int;
                        case GraphType.Primitive.Bool: return TypeHandle.Bool;
                        default: return TypeHandle.Float;
                    }
                case 2: return TypeHandle.Vector2;
                case 3: return TypeHandle.Vector3;
                default: return TypeHandle.Vector4;
            }
        }

        public object ObjectValue
        {
            get
            {
                switch (GetLength())
                {
                    case 1:
                        switch(GetPrimitive())
                        {
                            case GraphType.Primitive.Int: return (int)gc(0);
                            case GraphType.Primitive.Bool: return gc(0) != 0;
                            default: return gc(0);
                        }
                    case 2: return new Vector2(gc(0), gc(1));
                    case 3: return new Vector3(gc(0), gc(1), gc(2));
                    case 4: return new Vector4(gc(0), gc(1), gc(2), gc(3));
                    default: return 0;
                }
            }
            set
            {
                switch (GetLength())
                {
                    default:
                        switch (GetPrimitive())
                        {
                            case GraphType.Primitive.Int: sc(0, (float)value); return;
                            case GraphType.Primitive.Bool: sc(0, (bool)value ? 1 : 0); return;
                            default: sc(0, (float)value); return;
                        }
                    case 2: var v2 = (Vector2)value; sc(0, v2.x); sc(1, v2.y); return;
                    case 3: var v3 = (Vector3)value; sc(0, v3.x); sc(1, v3.y); sc(2, v3.z); return;
                    case 4: var v4 = (Vector4)value; sc(0, v4.x); sc(1, v4.y); sc(2, v4.z); sc(3, v4.w); return;
                }
            }
        }
        public Type Type
        {
            get
            {
                switch (GetLength())
                {
                    case 2: return typeof(Vector2);
                    case 3: return typeof(Vector3);
                    case 4: return typeof(Vector4);
                    default:
                        switch (GetPrimitive())
                        {
                            case GraphType.Primitive.Int: return typeof(int);
                            case GraphType.Primitive.Bool: return typeof(bool);
                            default: return typeof(float);
                        }
                }
            }
        }

        public object DefaultValue => Activator.CreateInstance(Type);
    }

    public class GradientTypeConstant : IConstant, ICLDSConstant
    {
        // Most of this should be genericized, as it'll be identical across types.
        public GraphDelta.GraphHandler graphHandler;
        public string nodeName, portName;

        public string NodeName => nodeName;
        public string PortName => portName;

        bool IsInitialized => nodeName != null && nodeName != "" && graphHandler != null;

        private GraphDelta.FieldHandler GetFieldReader()
        {
            if (!IsInitialized) return null;
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            var portReader = nodeReader.GetPort(portName);
            return portReader.GetTypeField();
        }

        private GraphDelta.FieldHandler GetFieldWriter()
        {
            return GetFieldReader();
        }

        public void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName)
        {
            graphHandler = handler;
            this.nodeName = nodeName;
            this.portName = portName;
        }

        public void Initialize(TypeHandle constantTypeHandle)
        {

        }

        public IConstant Clone()
        {
            return null;
        }

        public TypeHandle GetTypeHandle() => ShaderGraphExampleTypes.GradientTypeHandle;

        public object ObjectValue
        {
            get => IsInitialized ? GradientTypeHelpers.GetGradient(GetFieldReader()) : DefaultValue;
            set
            {
                if (IsInitialized)
                    GradientTypeHelpers.SetGradient(GetFieldWriter(), (Gradient)value);
            }
        }

        public Type Type => typeof(Gradient);

        public object DefaultValue => Activator.CreateInstance(Type);
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

        public static VisualElement BuildGraphTypeConstantEditor(this IConstantEditorBuilder builder, GraphTypeConstant constant)
        {
            if (builder.ConstantOwner is not GraphDataPortModel graphDataPort)
                return builder.BuildDefaultConstantEditor(constant);

            var length = constant.GetLength();
            var stencil = (ShaderGraphStencil)graphDataPort.GraphModel.Stencil;
            var uiHints = stencil.GetUIHints(graphDataPort.graphDataNodeModel.registryKey);

            if (length >= 3 && uiHints.ContainsKey(constant.portName + ".UseColor"))
            {
                var editor = new ColorField();
                editor.showAlpha = length == 4;

                editor.AddToClassList("sg-color-constant-field");
                editor.AddStylesheet("ConstantEditors.uss");

                // IConstantEditorBuilder will cast an incoming ChangeEvent's type to the constant's underlying type,
                // so we have to supply it with the right ChangeEvent type.

                void OnValueChangedVec4(ChangeEvent<Color> change)
                {
                    // Implicit conversion from Color to Vector4
                    using var changeEvent = ChangeEvent<Vector4>.GetPooled(change.previousValue, change.newValue);
                    builder.OnValueChanged(changeEvent);
                }

                void OnValueChangedVec3(ChangeEvent<Color> change)
                {
                    // Implicit conversion from Vector4 to Vector3
                    using var changeEvent = ChangeEvent<Vector3>.GetPooled((Vector4)change.previousValue, (Vector4)change.newValue);
                    builder.OnValueChanged(changeEvent);
                }

                editor.RegisterValueChangedCallback(length == 4 ? OnValueChangedVec4 : OnValueChangedVec3);

                return editor;
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
