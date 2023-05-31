namespace UnityEditor.VFX
{
    /// <summary>
    /// This class lets you manage Visual Effect templates
    /// </summary>
    public static class VFXTemplateHelper
    {
        /// <summary>
        /// This method gets template information for any Visual Effect asset.
        /// </summary>
        /// <param name="vfxPath">The path to a Visual Effect asset.</param>
        /// <param name="template">The structure that contains template information.</param>
        /// <returns>Returns true if the Visual Effect asset has template information, otherwise it returns false.</returns>
        public static bool TryGetTemplate(string vfxPath, out VFXTemplateDescriptor template)
        {
            var importer = (VisualEffectImporter)AssetImporter.GetAtPath(vfxPath);
            var nativeTemplate = importer.templateProperty;

            if (!string.IsNullOrEmpty(nativeTemplate.name))
            {
                template = new VFXTemplateDescriptor
                {
                    name = nativeTemplate.name,
                    category = nativeTemplate.category,
                    description = nativeTemplate.description,
                    icon = nativeTemplate.icon,
                    thumbnail = nativeTemplate.thumbnail,
                };

                return true;
            }

            template = default;
            return false;
        }

        /// <summary>
        /// This method creates or updates a Visual Effect asset template.
        /// </summary>
        /// <param name="vfxPath">The path to the existing Visual Effect asset.</param>
        /// <param name="template">The structure that contains all template information.</param>
        /// <returns>Returns true if the template is created, otherwise it returns false.</returns>
        public static bool TrySetTemplate(string vfxPath, VFXTemplateDescriptor template)
        {
            if (string.IsNullOrEmpty(vfxPath))
                return false;

            if (AssetDatabase.AssetPathExists(vfxPath))
            {
                var importer = (VisualEffectImporter)AssetImporter.GetAtPath(vfxPath);
                var nativeTemplate = new VFXTemplate
                {
                    name = template.name,
                    category = template.category,
                    description = template.description,
                    icon = template.icon,
                    thumbnail = template.thumbnail,
                };
                importer.templateProperty = nativeTemplate;
                return true;
            }

            return false;
        }
    }
}
