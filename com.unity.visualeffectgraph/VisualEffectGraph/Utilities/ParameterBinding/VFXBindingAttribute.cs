using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field)]
public class VFXBindingAttribute : PropertyAttribute
{
    public string[] EditorTypes;

    public VFXBindingAttribute(params string[] editorTypes)
    {
        EditorTypes = editorTypes;
    }
}

