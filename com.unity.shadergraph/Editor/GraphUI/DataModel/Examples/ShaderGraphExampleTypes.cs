using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine.GraphToolsFoundation.Overdrive;
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
        public static TypeHandle GetGraphType(GraphDelta.IPortReader reader)
        {
            if(reader.GetRegistryKey().Name == Registry.Types.Texture2DType.kRegistryKey.Name)
            {
                return Texture2DTypeHandle;
            }

            reader.GetField(Registry.Types.GraphType.kLength, out Registry.Types.GraphType.Length len);
            switch ((int)len)
            {
                case 1:
                    reader.GetField(Registry.Types.GraphType.kPrimitive, out Registry.Types.GraphType.Primitive prim);
                    switch(prim)
                    {
                        case Registry.Types.GraphType.Primitive.Int: return TypeHandle.Int;
                        case Registry.Types.GraphType.Primitive.Bool: return TypeHandle.Bool;
                        default: return TypeHandle.Float;
                    }
                case 2: return TypeHandle.Vector2;
                case 3: return TypeHandle.Vector3;
                default: return TypeHandle.Vector4;
            }
        }

        public static readonly TypeHandle Color = typeof(Color).GenerateTypeHandle();
        public static readonly TypeHandle AnimationClip = typeof(AnimationClip).GenerateTypeHandle();
        public static readonly TypeHandle Mesh = typeof(Mesh).GenerateTypeHandle();
        public static readonly TypeHandle Texture2DTypeHandle = typeof(Texture2D).GenerateTypeHandle();
        public static readonly TypeHandle Texture3D = typeof(Texture3D).GenerateTypeHandle();
        public static readonly TypeHandle DayOfWeek = typeof(DayOfWeek).GenerateTypeHandle();

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
            { "Texture2D", Texture2DTypeHandle },
            { "Texture3D", Texture3D },
            { "DayOfWeek", DayOfWeek },
        };

        public static IEnumerable<string> TypeHandleNames => TypeHandlesByName.Keys;
    }

    //public class GraphTypeConstant : IConstant
    //{
    //    public object ObjectValue { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    //    public object DefaultValue => throw new NotImplementedException();

    //    public Type Type => throw new NotImplementedException();
    //}

    public class FieldConstant<T> : IConstant
    {
        public GraphDelta.GraphHandler graphHandler;
        public string nodeName, portName, fieldPath;

        private GraphDelta.IFieldReader ResolveReader()
        {
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            if (nodeReader.TryGetPort(portName, out var portReader))
            {
                portReader.TryGetField(fieldPath, out var fieldReader);
                return fieldReader;
            }
            else
            {
                nodeReader.TryGetField(fieldPath, out var FieldReader);
                return FieldReader;
            }
        }

        bool IsInitialized => graphHandler != null;

        public void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName, string fieldPath)
        {
            graphHandler = handler;
            this.nodeName = nodeName;
            this.portName = portName;
            this.fieldPath = fieldPath;
        }

        public Type Type
        {
            get
            {
                if (!IsInitialized) return typeof(T);
                ResolveReader().TryGetValue(out T value);
                return value.GetType();
            }
        }

        public object ObjectValue
        {
            get
            {
                if (!IsInitialized) return default(T);
                ResolveReader().TryGetValue(out T value);
                return value;
            }
            set
            {
                if (!IsInitialized) return;
                graphHandler.GetNodeWriter(nodeName).SetPortField<T>(portName, fieldPath, (T)value);
            }
        }

        public object DefaultValue => default(T);
    }

    interface ICLDSConstant
    {
        void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName);
    }

    public class GraphTypeConstant : IConstant, ICLDSConstant
    {
        public GraphDelta.GraphHandler graphHandler;
        public string nodeName, portName;

        bool IsInitialized => nodeName != null && nodeName != "" && graphHandler != null;

        private int GetLength()
        {
            if (!IsInitialized) return -1;
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            nodeReader.TryGetPort(portName, out var portReader);
            portReader.GetField(Registry.Types.GraphType.kLength, out Registry.Types.GraphType.Length length);
            return (int)length;
        }

        private Registry.Types.GraphType.Primitive GetPrimitive()
        {
            if (!IsInitialized) return Registry.Types.GraphType.Primitive.Float;
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            nodeReader.TryGetPort(portName, out var portReader);
            portReader.GetField(Registry.Types.GraphType.kPrimitive, out Registry.Types.GraphType.Primitive prim);
            return prim;
        }

        private float gc(int i)
        {
            if (!IsInitialized) return 0;
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            nodeReader.TryGetPort(portName, out var portReader);
            portReader.GetField($"c{i}", out float value);
            return value;
        }

        private void sc(int i, float v)
        {
            if (!IsInitialized) return;
            var nodeReader = graphHandler.GetNodeWriter(nodeName);
            nodeReader.SetPortField(portName, $"c{i}", v);
        }

        public void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName)
        {
            graphHandler = handler;
            this.nodeName = nodeName;
            this.portName = portName;
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
                            case Registry.Types.GraphType.Primitive.Int: return (int)gc(0);
                            case Registry.Types.GraphType.Primitive.Bool: return gc(0) != 0;
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
                            case Registry.Types.GraphType.Primitive.Int: sc(0, (float)value); return;
                            case Registry.Types.GraphType.Primitive.Bool: sc(0, (bool)value ? 1 : 0); return;
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
                            case Registry.Types.GraphType.Primitive.Int: return typeof(int);
                            case Registry.Types.GraphType.Primitive.Bool: return typeof(bool);
                            default: return typeof(float);
                        }
                }
            }
        }

        public object DefaultValue => Activator.CreateInstance(Type);
    }
    public class Texture2DTypeConstant : IConstant, ICLDSConstant
    {
        // Most of this should be genericized, as it'll be identical across types.
        public GraphDelta.GraphHandler graphHandler;
        public string nodeName, portName;

        bool IsInitialized => nodeName != null && nodeName != "" && graphHandler != null;

        private GraphDelta.IFieldReader GetFieldReader()
        {
            if (!IsInitialized) return null;
            var nodeReader = graphHandler.GetNodeReader(nodeName);
            nodeReader.TryGetPort(portName, out var portReader);
            return (GraphDelta.IFieldReader)portReader;
        }

        private GraphDelta.IFieldWriter GetFieldWriter()
        {
            if (!IsInitialized) return null;
            var nodeWriter = graphHandler.GetNodeWriter(nodeName);
            return (GraphDelta.IFieldWriter)nodeWriter.GetPort(portName);
        }

        public void Initialize(GraphDelta.GraphHandler handler, string nodeName, string portName)
        {
            graphHandler = handler;
            this.nodeName = nodeName;
            this.portName = portName;
        }

        public object ObjectValue
        {
            get => IsInitialized ? Registry.Types.Texture2DHelpers.GetTextureAsset(GetFieldReader()) : DefaultValue;
            set
            {
                if (IsInitialized)
                    Registry.Types.Texture2DHelpers.SetTextureAsset(GetFieldWriter(), (Texture2D)value);
            }
        }

        public Type Type => typeof(Texture2D);

        public object DefaultValue => Activator.CreateInstance(Type);
    }

    //[GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.toolDefaultPriority)]
    //static class GDSExt
    //{
    //    // TODO: This should return the Port/Blackboard inline editor for the Gradient field (eg. small gradient preview, on-click to popup).
    //    public static VisualElement BuildDefaultConstantEditor(this IConstantEditorBuilder builder, Texture2DTypeConstant constant)
    //    {
    //        // return ConstantEditorExtensions.BuildInlineValueEditor(constant.ObjectValue, new FloatField(), builder.OnValueChanged);
    //        return new Label("TODO: Inline Texture 2D Editor");
    //    }
    //}
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
