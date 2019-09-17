using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    abstract class MasterNode : AbstractMaterialNode, IMasterNode, IHasSettings
    {
        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public VisualElement CreateSettingsElement()
        {
            var container = new VisualElement();
            var commonSettingsElement = CreateCommonSettingsElement();
            if (commonSettingsElement != null)
                container.Add(commonSettingsElement);

            return container;
        }

        public virtual void ProcessPreviewMaterial(Material Material) {}

        protected virtual VisualElement CreateCommonSettingsElement()
        {
            return null;
        }
    }
}
