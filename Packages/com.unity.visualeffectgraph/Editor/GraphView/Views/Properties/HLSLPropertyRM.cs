namespace UnityEditor.VFX.UI
{
    class HLSLPropertyRM : SimplePropertyRM<string>
    {
        public HLSLPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override float GetPreferredControlWidth() => 60;

        public override ValueControl<string> CreateField()
        {
            var hlslModel = provider is VFXSettingController settingController && settingController.owner is VFXModel model
                ? model
                : null;

            return new VFXTextEditorField(hlslModel);
        }
    }
}
