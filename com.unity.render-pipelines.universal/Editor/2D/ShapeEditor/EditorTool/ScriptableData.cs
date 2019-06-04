using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class ScriptableData<T> : ScriptableObject
    {
        [SerializeField]
        private T m_Data;
        public UnityObject owner { get; set; }
        public int index { get; set; }

        public T data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }
    }
}
