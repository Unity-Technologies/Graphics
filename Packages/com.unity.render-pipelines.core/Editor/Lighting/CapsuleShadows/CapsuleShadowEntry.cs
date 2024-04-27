using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    public class CapsuleShadowEntry
    {
        private VisualElement rootElement;
        private Label nameLabel;
        private Button interactionButton;

        public CapsuleModel Data { get; private set; }
        private CapsuleShadowsInspector parentInspector;

        public void SetVisualElement(VisualElement root)
        {
            rootElement = root;
            nameLabel = rootElement.Q<Label>("nameLB");
            interactionButton = root.Q<Button>("deleteBT");
            interactionButton.clicked += OnInteractionPressed;
        }

        private void OnInteractionPressed()
        {
            if (Data.m_Occluder != null)
            {
                parentInspector.DeleteCapsule(Data.m_Occluder);
            }
            else
            {
                parentInspector.AddCapsule(Data);
            }
        }

        public void SetData(CapsuleModel data, CapsuleShadowsInspector parent)
        {
            Data = data;
            parentInspector = parent;

            nameLabel.SetEnabled(data.m_Occluder != null);
            if (data.m_Occluder != null)
            {
                nameLabel.text = CapsuleOccluderManager.instance.IsOccluderIgnored(data.m_Occluder)
                    ? $"{data.ToString()} (IGNORED)"
                    : data.ToString();
            }
            else
            {
                nameLabel.text = data.ToString();
            }

            interactionButton.text = data.m_Occluder == null ? "Add" : "Delete";
        }

        public void Dispose()
        {
            Data = null;
            parentInspector = null;

            nameLabel.text = "";
            interactionButton.clicked -= OnInteractionPressed;
        }
    }
}
