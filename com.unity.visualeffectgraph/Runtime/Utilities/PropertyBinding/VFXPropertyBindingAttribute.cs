using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.VFX.Utility
{
    [AttributeUsage(AttributeTargets.Field)]
    public class VFXPropertyBindingAttribute : PropertyAttribute
    {
        public string[] EditorTypes;

        public VFXPropertyBindingAttribute(params string[] editorTypes)
        {
            EditorTypes = editorTypes;
        }
    }
}
