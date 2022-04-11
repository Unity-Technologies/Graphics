using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
#if PROCEDURAL_VT_IN_GRAPH
    [SGPropertyDrawer(typeof(ProceduralVirtualTextureNode))]
    class ProceduralVirtualTextureNodePropertyDrawer : AbstractMaterialNodePropertyDrawer
    {
        // Use the same HLSLDeclarationStrings as used by the ShaderInputPropertyDrawer, for consistency
        static string[] allHLSLDeclarationStrings = new string[]
        {
            "Do Not Declare",       // HLSLDeclaration.DoNotDeclare
            "Global",               // HLSLDeclaration.Global
            "Per Material",         // HLSLDeclaration.UnityPerMaterial
            "Hybrid Per Instance",  // HLSLDeclaration.HybridPerInstance
        };

        internal override void AddCustomNodeProperties(VisualElement parentElement, AbstractMaterialNode nodeBase, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            var node = nodeBase as ProceduralVirtualTextureNode;

            var hlslDecls = Enum.GetValues(typeof(HLSLDeclaration));
            var allowedDecls = new List<HLSLDeclaration>();
            for (int i = 0; i < hlslDecls.Length; i++)
            {
                HLSLDeclaration decl = (HLSLDeclaration)hlslDecls.GetValue(i);
                var allowed = node.AllowHLSLDeclaration(decl);
                if (allowed)
                    allowedDecls.Add(decl);
            }

            var propertyRow = new PropertyRow(new Label("Shader Declaration"));
            var popupField = new PopupField<HLSLDeclaration>(
                    allowedDecls,
                    node.shaderDeclaration,
                    (h => allHLSLDeclarationStrings[(int)h]),
                    (h => allHLSLDeclarationStrings[(int)h]));
            popupField.RegisterValueChangedCallback(
                evt =>
                {
                    if (node.shaderDeclaration == evt.newValue)
                        return;

                    setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo("Change PVT shader declaration");
                    node.shaderDeclaration = (UnityEditor.ShaderGraph.Internal.HLSLDeclaration)evt.newValue;
                    updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            propertyRow.Add(popupField);
            parentElement.Add(propertyRow);
        }
    }
#endif // PROCEDURAL_VT_IN_GRAPH
}
