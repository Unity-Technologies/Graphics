using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [Serializable]
    public struct ExposedParameter
    {
        [SerializeField]
        private string m_Name;
        private int m_Id;
        private bool m_Init;

        public static implicit operator ExposedParameter(string name)
        {
            return new ExposedParameter(name);
        }

        public static explicit operator string(ExposedParameter parameter)
        {
            return parameter.m_Name;
        }

        public static implicit operator int(ExposedParameter parameter)
        {
            if (!parameter.m_Init)
                parameter.m_Id = Shader.PropertyToID(parameter.m_Name);
            return parameter.m_Id;
        }

        public static ExposedParameter operator+(ExposedParameter self, ExposedParameter other)
        {
            return new ExposedParameter(self.m_Name + other.m_Name);
        }

        private ExposedParameter(string name)
        {
            m_Name = name;
            m_Id = 0; //Cannot be -1 because ExposedParameter is a structure, otherwise, instancing from SerializedProperty will be problematic
            m_Init = false;
        }

        public override string ToString()
        {
            return m_Name;
        }
    }
}
