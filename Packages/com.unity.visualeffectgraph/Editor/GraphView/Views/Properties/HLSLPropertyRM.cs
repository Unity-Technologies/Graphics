namespace UnityEditor.VFX.UI
{
    class HLSLPropertyRM : SimplePropertyRM<string>
    {
        public HLSLPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth() => 60;
        protected override void UpdateIndeterminate() { }

        public override void SetValue(object obj)
        {
            if (obj is string code)
            {
                base.SetValue(code);
            }
        }

        public override ValueControl<string> CreateField()
        {
            return new VFXTextEditorField(provider);
        }
    }
}
