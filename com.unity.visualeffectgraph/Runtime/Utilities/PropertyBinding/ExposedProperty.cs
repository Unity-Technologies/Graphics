using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    /// <summary>
    /// An utility Class that Accelerates caching of both string properties and their integer values based on Shader.PropertyToID()
    /// </summary>
    [Serializable]
    public class ExposedProperty
    {
        [SerializeField]
        private string m_Name;
#if !UNITY_EDITOR
        private int m_Id;
#endif

        /// <summary>
        /// Creates a new ExposedProperty from a string.
        /// </summary>
        /// <param name="name">the string desired name</param>
        public static implicit operator ExposedProperty(string name)
        {
            return new ExposedProperty(name);
        }

        /// <summary>
        /// Converts automatically an ExposedProperty to a string
        /// </summary>
        /// <param name="parameter"></param>
        public static explicit operator string(ExposedProperty parameter)
        {
            return parameter.m_Name;
        }

        /// <summary>
        /// Converts automatically an ExposedProperty to an int (based on Shader.PropertyToID())
        /// </summary>
        /// <param name="parameter"></param>
        public static implicit operator int(ExposedProperty parameter)
        {
#if UNITY_EDITOR
            //In Editor, m_Id cached cannot be used for several reasons :
            // - m_Name is modified thought a SerializedProperty
            // -ExposedParameter are stored in array, when we modify it, m_Id is reset to zero
            // -Undo /Redo is restoring m_Name
            // Could be resolved modifying directly object reference in inspector, but for Undo/Redo, we have to invalid everything
            //In Runtime, there isn't any undo/redo and SerializedObject is only available in UnityEditor namespace
            return Shader.PropertyToID(parameter.m_Name);
#else
            if (parameter.m_Id == 0 && !string.IsNullOrEmpty(parameter.m_Name))
                throw new InvalidOperationException("Unexpected constructor has been called");

            if (parameter.m_Id == -1)
                parameter.m_Id = Shader.PropertyToID(parameter.m_Name);

            return parameter.m_Id;
#endif
        }

        /// <summary>
        /// Implicit string concatenation Operator
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static ExposedProperty operator+(ExposedProperty self, ExposedProperty other)
        {
            return new ExposedProperty(self.m_Name + other.m_Name);
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ExposedProperty()
        {
#if !UNITY_EDITOR
            m_Id = -1;
#endif
        }

        private ExposedProperty(string name)
        {
            m_Name = name;
#if !UNITY_EDITOR
            m_Id = -1;
#endif
        }

        /// <summary>
        /// The string value of this ExposedProperty
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_Name;
        }
    }
}
