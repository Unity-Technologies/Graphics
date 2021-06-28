using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace GtfPlayground
{
    public static class PlaygroundTypes
    {
        public static readonly TypeHandle MissingType = TypeHandle.MissingType;
        public static readonly TypeHandle Unknown = TypeHandle.Unknown;
        public static readonly TypeHandle ExecutionFlow = TypeHandle.ExecutionFlow;
        public static readonly TypeHandle MissingPort = TypeHandle.MissingPort;
        public static readonly TypeHandle Bool = TypeHandle.Bool;
        public static readonly TypeHandle Void = TypeHandle.Void;
        public static readonly TypeHandle Char = TypeHandle.Char;
        public static readonly TypeHandle Double = TypeHandle.Double;
        public static readonly TypeHandle Float = TypeHandle.Float;
        public static readonly TypeHandle Int = TypeHandle.Int;
        public static readonly TypeHandle UInt = TypeHandle.UInt;
        public static readonly TypeHandle Long = TypeHandle.Long;
        public static readonly TypeHandle Object = TypeHandle.Object;
        public static readonly TypeHandle GameObject = TypeHandle.GameObject;
        public static readonly TypeHandle String = TypeHandle.String;
        public static readonly TypeHandle Vector2 = TypeHandle.Vector2;
        public static readonly TypeHandle Vector3 = TypeHandle.Vector3;
        public static readonly TypeHandle Vector4 = TypeHandle.Vector4;
        public static readonly TypeHandle Quaternion = TypeHandle.Quaternion;
        public static readonly TypeHandle Color = typeof(Color).GenerateTypeHandle();
        public static readonly TypeHandle AnimationClip = typeof(AnimationClip).GenerateTypeHandle();
        public static readonly TypeHandle Mesh = typeof(Mesh).GenerateTypeHandle();
        public static readonly TypeHandle Texture2D = typeof(Texture2D).GenerateTypeHandle();
        public static readonly TypeHandle Texture3D = typeof(Texture3D).GenerateTypeHandle();

        public static readonly Dictionary<string, TypeHandle> TypeHandlesByName = new()
        {
            {"MissingType", MissingType},
            {"Unknown", Unknown},
            {"ExecutionFlow", ExecutionFlow},
            {"MissingPort", MissingPort},
            {"Bool", Bool},
            {"Void", Void},
            {"Char", Char},
            {"Double", Double},
            {"Float", Float},
            {"Int", Int},
            {"UInt", UInt},
            {"Long", Long},
            {"Object", Object},
            {"GameObject", GameObject},
            {"String", String},
            {"Vector2", Vector2},
            {"Vector3", Vector3},
            {"Vector4", Vector4},
            {"Quaternion", Quaternion},
            {"Color", Color},
            {"AnimationClip", AnimationClip},
            {"Mesh", Mesh},
            {"Texture2D", Texture2D},
            {"Texture3D", Texture3D},
        };

        public static IEnumerable<string> TypeHandleNames => TypeHandlesByName.Keys;
    }
}
