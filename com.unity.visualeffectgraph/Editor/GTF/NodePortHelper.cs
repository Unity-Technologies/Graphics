using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.VFX
{
    static class NodePortHelper
    {
        private static readonly Dictionary<Type, TypeHandle> s_TypeHandleCache = new();

        public static void AddPort(this NodeModel node, PortDirection direction, string name, Type type)
        {
            Action<string, TypeHandle> function = direction == PortDirection.Input
                ? (n, th) => node.AddDataInputPort(n, th, GUID.Generate().ToString())
                : (n, th) => node.AddDataOutputPort(n, th, GUID.Generate().ToString(), options: PortModelOptions.NoEmbeddedConstant);

            switch (type.Name)
            {
                case nameof(AABox):
                    function("Center", TypeHandle.Vector3);
                    function("Size", TypeHandle.Vector3);
                    break;
                case nameof(OrientedBox):
                    function("Center", TypeHandle.Vector3);
                    function("Angles", TypeHandle.Vector3);
                    function("Size", TypeHandle.Vector3);
                    break;
                case nameof(Vector2):
                    function(null, TypeHandle.Vector2);
                    break;
                case nameof(Vector):
                case nameof(Vector3):
                    function(null, TypeHandle.Vector3);
                    break;
                case nameof(Vector4):
                    function(null, TypeHandle.Vector4);
                    break;
                case nameof(Transform):
                    function("Position", TypeHandle.Vector3);
                    function("Angles", TypeHandle.Vector3);
                    function("Scale", TypeHandle.Vector3);
                    break;
                default:
                    function(name, GetTypeHandle(type));
                    break;
            }
        }

        private static TypeHandle GetTypeHandle(Type type)
        {
            if (!s_TypeHandleCache.TryGetValue(type, out var handle))
            {
                handle = type.GenerateTypeHandle();
                s_TypeHandleCache[type] = handle;
            }

            return handle;
        }
    }
}
