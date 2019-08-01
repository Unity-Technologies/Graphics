using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class VectorShaderProperty : AbstractShaderProperty<Vector4>, ISplattableShaderProperty
    {
        public override bool isExposable => true;
        public override bool isRenamable => true;

        [SerializeField]
        bool m_Splat = false;

        public bool splat
        {
            get => m_Splat;
            set => m_Splat = value;
        }
        
        public override string GetPropertyBlockString()
        {
            return $"{hideTagString}{this.PerSplatString()}{referenceName}(\"{displayName}\", Vector) = ({NodeUtils.FloatToShaderValue(value.x)}, {NodeUtils.FloatToShaderValue(value.y)}, {NodeUtils.FloatToShaderValue(value.z)}, {NodeUtils.FloatToShaderValue(value.w)})";
        }
    }
}
