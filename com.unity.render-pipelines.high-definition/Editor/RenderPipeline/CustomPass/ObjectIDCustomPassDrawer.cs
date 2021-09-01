using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Custom drawer for the draw renderers pass
    /// </summary>
    [CustomPassDrawerAttribute(typeof(ObjectIDCustomPass))]
    class ObjectIDCustomPassDrawer : DrawRenderersCustomPassDrawer
    {
        protected override void Initialize(SerializedProperty customPass)
        {
            base.Initialize(customPass);
            showMaterialOverride = false;
        }
    }
}
