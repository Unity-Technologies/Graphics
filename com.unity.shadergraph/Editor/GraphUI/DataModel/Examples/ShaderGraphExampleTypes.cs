using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    /// <summary>
    /// GTF constant type for System.DayOfWeek, used to create a simple custom type w/ inline editor
    /// </summary>
    public class DayOfWeekConstant : Constant<DayOfWeek>
    {
        public static readonly List<DayOfWeek> Values = new((DayOfWeek[]) Enum.GetValues(typeof(DayOfWeek)));
        public static readonly List<string> Names = new(Enum.GetNames(typeof(DayOfWeek)));
    }

    public class GraphDataEntryConstant : StringConstant
    {
        GraphDelta.IGraphHandler graphHandler;
        public void Initialize(GraphDelta.IGraphHandler handler, string path)
        {
            graphHandler = handler;
            m_Value = path;
        }

        new object ObjectValue {
            get
            {
                var nodeName = m_Value.Substring(0, m_Value.IndexOf('.'));
                var path = m_Value.Substring(m_Value.IndexOf('.')+1);
                graphHandler.GetNodeReader(nodeName).GetField(path, out object value);
                return value;
            }

            set
            {
                var nodeName = m_Value.Substring(0, m_Value.IndexOf('.'));
                var path = m_Value.Substring(m_Value.IndexOf('.') + 1);
                var portName = path.Substring(0, m_Value.IndexOf('.'));
                path = path.Substring(m_Value.IndexOf('.') + 1);
                graphHandler.GetNodeWriter(nodeName).SetPortField(portName, path, value);
            }
        }
        new Type Type { get => this.ObjectValue.GetType(); }
    }

    public static class ShaderGraphExampleTypes
    {
        public static readonly TypeHandle GraphDataEntry = TypeHandleHelpers.GenerateCustomTypeHandle("GraphDataEntry");
        public static readonly TypeHandle Color = typeof(Color).GenerateTypeHandle();
        public static readonly TypeHandle AnimationClip = typeof(AnimationClip).GenerateTypeHandle();
        public static readonly TypeHandle Mesh = typeof(Mesh).GenerateTypeHandle();
        public static readonly TypeHandle Texture2D = typeof(Texture2D).GenerateTypeHandle();
        public static readonly TypeHandle Texture3D = typeof(Texture3D).GenerateTypeHandle();
        public static readonly TypeHandle DayOfWeek = typeof(DayOfWeek).GenerateTypeHandle();

        public static readonly Dictionary<string, TypeHandle> TypeHandlesByName = new()
        {
            {"MissingType", TypeHandle.MissingType},
            {"Unknown", TypeHandle.Unknown},
            {"ExecutionFlow", TypeHandle.ExecutionFlow},
            {"MissingPort", TypeHandle.MissingPort},
            {"Bool", TypeHandle.Bool},
            {"Void", TypeHandle.Void},
            {"Char", TypeHandle.Char},
            {"Double", TypeHandle.Double},
            {"Float", TypeHandle.Float},
            {"Int", TypeHandle.Int},
            {"UInt", TypeHandle.UInt},
            {"Long", TypeHandle.Long},
            {"Object", TypeHandle.Object},
            {"GameObject", TypeHandle.GameObject},
            {"String", TypeHandle.String},
            {"Vector2", TypeHandle.Vector2},
            {"Vector3", TypeHandle.Vector3},
            {"Vector4", TypeHandle.Vector4},
            {"Quaternion", TypeHandle.Quaternion},
            {"Color", Color},
            {"AnimationClip", AnimationClip},
            {"Mesh", Mesh},
            {"Texture2D", Texture2D},
            {"Texture3D", Texture3D},
            {"DayOfWeek", DayOfWeek},
        };

        public static IEnumerable<string> TypeHandleNames => TypeHandlesByName.Keys;
    }
}
