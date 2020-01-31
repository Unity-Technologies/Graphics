using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Colors
{
    class PrecisionColors : ColorProviderFromStyleSheet
    {
        public override string GetTitle() => "Precision";

        public override bool AllowCustom() => false;

        public override bool ClearOnDirty() => true;
        
        protected override bool GetClassFromNode(AbstractMaterialNode node, out string ussClass)
        {
            ussClass = node.concretePrecision.ToString();

            return !string.IsNullOrEmpty(ussClass);
        }
    }
}
