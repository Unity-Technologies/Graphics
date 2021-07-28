using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

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

    public static class ShaderGraphExampleTypes
    {
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
