using System;
using System.Collections.Generic;
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

        public override IEnumerable<(string cbName, string line)> GetPropertyDeclarationStrings()
        {
            if (splat)
            {
                for (int i = 0; i < 4; ++i)
                    yield return ("UnitySplatMaterials", $"{concreteShaderValueType.ToShaderString(concretePrecision)} {referenceName}{i}");
            }
            else
            {
                yield return (s_UnityPerMaterialCbName, $"{concreteShaderValueType.ToShaderString(concretePrecision)} {referenceName}");
            }
        }
    }
}
