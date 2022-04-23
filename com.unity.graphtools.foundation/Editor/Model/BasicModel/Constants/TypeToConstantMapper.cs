using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Helper class to find a suitable <see cref="IConstant"/> type for a <see cref="TypeHandle"/>.
    /// </summary>
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public static class TypeToConstantMapper
    {
        static Dictionary<TypeHandle, Type> s_TypeToConstantTypeCache;

        /// <summary>
        /// Maps <see cref="TypeHandle"/> to a type of <see cref="IConstant"/>.
        /// </summary>
        public static Type GetConstantType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantTypeCache == null)
            {
                s_TypeToConstantTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { TypeHandle.Bool, typeof(BooleanConstant) },
                    { TypeHandle.Double, typeof(DoubleConstant) },
                    { TypeHandle.Float, typeof(FloatConstant) },
                    { TypeHandle.Int, typeof(IntConstant) },
                    { TypeHandle.Quaternion, typeof(QuaternionConstant) },
                    { TypeHandle.String, typeof(StringConstant) },
                    { TypeHandle.Vector2, typeof(Vector2Constant) },
                    { TypeHandle.Vector3, typeof(Vector3Constant) },
                    { TypeHandle.Vector4, typeof(Vector4Constant) },
                    { typeof(Color).GenerateTypeHandle(), typeof(ColorConstant) },
                    { typeof(AnimationClip).GenerateTypeHandle(), typeof(AnimationClipConstant) },
                    { typeof(Mesh).GenerateTypeHandle(), typeof(MeshConstant) },
                    { typeof(Texture2D).GenerateTypeHandle(), typeof(Texture2DConstant) },
                    { typeof(Texture3D).GenerateTypeHandle(), typeof(Texture3DConstant) },
                };
            }

            if (s_TypeToConstantTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve();
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstant);

            return null;
        }
    }
}
