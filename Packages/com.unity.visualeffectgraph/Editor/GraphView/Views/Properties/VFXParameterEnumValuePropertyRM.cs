using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXListParameterEnumValuePropertyRM : ListPropertyRM<string, StringPropertyRM>
    {
        const string k_DefaultItemName = "New item";
        const int k_MaxItemNameLength = VFXParameterController.kMaxExposedNameLength;

        public VFXListParameterEnumValuePropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
        }

        public override bool IsCompatible(IPropertyRMProvider provider) => GetPropertyType(provider) == typeof(UintPropertyRM);

        protected override StringPropertyRM CreateField(IPropertyRMProvider provider)
        {
            return new StringPropertyRM(provider, 18);
        }

        protected override string CreateItem() => k_DefaultItemName;

        protected override string ValidateItem(List<string> list, int index, string item)
        {
            var previousName = index < list.Count ? list[index] : null;
            if (previousName != null && previousName.Length > k_MaxItemNameLength && item != previousName)
            {
                if (!EditorUtility.DisplayDialog("Name is too long", "The name exceeds the character count limit. Do you want to proceed with renaming and truncate it, or cancel renaming?", "Rename", "Cancel"))
                    return previousName;

                item = TruncateName(item);
            }

            return MakeNameUnique(list, index, item);
        }

        internal static string TruncateName(string name)
        {
            return name != null && name.Length > k_MaxItemNameLength ? name.Substring(0, k_MaxItemNameLength) : name;
        }

        internal static string MakeNameUnique(IReadOnlyList<string> names, int index, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = k_DefaultItemName;

            var otherNames = new HashSet<string>(names.Where((_, i) => i != index), StringComparer.OrdinalIgnoreCase);
            if (!otherNames.Contains(name))
                return name;

            return VFXParameterController.MakeNameUnique(name, otherNames, true, k_MaxItemNameLength);
        }
    }
}
