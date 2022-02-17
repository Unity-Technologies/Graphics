using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// An ObjectPart is a node part that displays an object field for selecting assets like textures.
    /// </summary>
    public class ObjectPart : BaseModelUIPart
    {
        const string k_AssetPartTemplate = "NodeFieldParts/ObjectPart";
        const string k_ObjectFieldName = "sg-object-field";

        public ObjectPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        VisualElement m_Root;
        ObjectField m_ObjectField;
        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement {name = PartName};
            GraphElementHelper.LoadTemplate(m_Root, k_AssetPartTemplate);

            m_ObjectField = m_Root.Q<ObjectField>(k_ObjectFieldName);

            // TODO: Get the required type from the node
            m_ObjectField.objectType = typeof(Texture2D);

            m_ObjectField.RegisterValueChangedCallback(change =>
            {
                // TODO: Write asset to field
                Debug.Log($"Asset is now {change.newValue}");
            });

            parent.Add(m_Root);
        }

        protected override void UpdatePartFromModel()
        {
            // TODO: Reconstruct asset reference from field and show it in the UI
            // m_ObjectField.value = ...
        }
    }
}
