using System;

using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    interface ITextProvider
    {
        event Action titleChanged;
        event Action textChanged;
        VFXModel model { get; }
        string title { get; }
        string text { get; set; }
    }

    class VFXTextEditorField : ValueControl<string>
    {
        private readonly VFXModel m_Model;

        public VFXTextEditorField(IPropertyRMProvider provider) : base(ObjectNames.NicifyVariableName(provider.name))
        {
            m_Model = provider is VFXSettingController { owner: VFXModel m } ? m : null;
            var editButton = new Button(OnEditText);
            editButton.AddToClassList("propertyrm-button");
            editButton.text = "Edit";
            editButton.style.marginLeft = 0;
            editButton.style.marginRight = 0;
            editButton.tooltip = "Open HLSL code editor window";
            Add(editButton);
        }

        public VFXModel model => m_Model;

        protected override void ValueToGUI(bool force) { }

        private void OnEditText()
        {
            var textEditor = EditorWindow.GetWindow<VFXTextEditor>();
            textEditor.Show(m_Model);
        }
    }

    class VFXHLSLTextProvider : ITextProvider, IDisposable
    {
        private readonly VFXModel m_Model;
        private string m_Text;
        private string m_Title;

        public VFXHLSLTextProvider(VFXModel model)
        {
            m_Model = model;
            m_Title = GetTitle();
            m_Text = ((IHLSLCodeHolder)model).sourceCode;
            m_Model.onInvalidateDelegate += OnInvalidate;
        }

        private string GetTitle()
        {
            var hlslCodeHolder = (IHLSLCodeHolder)m_Model;
            if (hlslCodeHolder.HasShaderFile())
            {
                return hlslCodeHolder.shaderFile.name;
            }

            return m_Model.name;
        }

        public event Action titleChanged;
        public event Action textChanged;

        public VFXModel model => m_Model;

        public string title => m_Title;

        public string text
        {
            get => m_Text;
            set
            {
                m_Text = value;
                ((IHLSLCodeHolder)m_Model).sourceCode = value;
            }
        }

        public void Dispose()
        {
            m_Model.onInvalidateDelegate -= OnInvalidate;
        }

        private void OnInvalidate(VFXModel vfxModel, VFXModel.InvalidationCause cause)
        {
            if (cause == VFXModel.InvalidationCause.kSettingChanged)
            {
                var newTitle = GetTitle();
                if (newTitle != m_Title)
                {
                    m_Title = newTitle;
                    titleChanged?.Invoke();
                }

                var source = ((IHLSLCodeHolder)model).sourceCode;
                if (source != m_Text)
                {
                    m_Text = source;
                    textChanged?.Invoke();
                }
            }
        }
    }
}
