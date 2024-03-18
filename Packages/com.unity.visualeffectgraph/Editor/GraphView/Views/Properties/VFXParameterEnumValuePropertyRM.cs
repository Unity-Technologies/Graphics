namespace UnityEditor.VFX.UI
{
    class VFXListParameterEnumValuePropertyRM : ListPropertyRM<string, StringPropertyRM>
    {
        public VFXListParameterEnumValuePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        protected override StringPropertyRM CreateField(IPropertyRMProvider provider)
        {
            return new StringPropertyRM(provider, 18);
        }

        protected override string CreateItem() => "New item";
    }
}
