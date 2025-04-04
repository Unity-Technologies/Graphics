using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UnityEditor.VFX
{
    /// <summary>
    /// This structure provides all useful information for a VFX template
    /// </summary>
    public struct VFXTemplateDescriptor
    {
        /// <summary>
        /// Name of the template which will be displayed in the template window
        /// </summary>
        public string name;
        /// <summary>
        /// Category is used to group templates together in the template window
        /// </summary>
        public string category;
        /// <summary>
        /// Give some description to your template so that we know what it's doing
        /// </summary>
        public string description;
        /// <summary>
        /// This icon is displayed next to the name in the template window
        /// </summary>
        public Texture2D icon;
        /// <summary>
        /// Thumbnail is displayed with the description in the details panel of the template window
        /// </summary>
        public Texture2D thumbnail;

        /// <summary>
        /// Same as the name, inherited from the interface ITemplateDescriptor
        /// </summary>
        public string header => name;
    }

    /// <summary>
    /// Helper class to create or update a Visual Effect asset template
    /// </summary>
    public static class VFXTemplateHelper
    {
        /// <summary>
        /// This method gets template information for any Visual Effect asset.
        /// </summary>
        /// <param name="vfxPath">The path to a Visual Effect asset.</param>
        /// <param name="vfxTemplateDescriptor">The structure that contains template information.</param>
        /// <returns>Returns true if the Visual Effect asset has template information, otherwise it returns false.</returns>
        public static bool TryGetTemplate(string vfxPath, out VFXTemplateDescriptor vfxTemplateDescriptor)
        {
            if (VFXTemplateHelperInternal.TryGetTemplateStatic(vfxPath, out var graphViewTemplate))
            {
                vfxTemplateDescriptor = new VFXTemplateDescriptor
                {
                    name = graphViewTemplate.name,
                    category = graphViewTemplate.category,
                    description = graphViewTemplate.description,
                    icon = graphViewTemplate.icon,
                    thumbnail = graphViewTemplate.thumbnail,
                };
                return true;
            }

            vfxTemplateDescriptor = default;
            return false;
        }

        /// <summary>
        /// This method creates or updates a Visual Effect asset template.
        /// </summary>
        /// <param name="vfxPath">The path to the existing Visual Effect asset.</param>
        /// <param name="vfxTemplateDescriptor">The structure that contains all template information.</param>
        /// <returns>Returns true if the template is created, otherwise it returns false.</returns>
        public static bool TrySetTemplate(string vfxPath, VFXTemplateDescriptor vfxTemplateDescriptor)
        {
            return VFXTemplateHelperInternal.TrySetTemplateStatic(vfxPath, new GraphViewTemplateDescriptor
            {
                name = vfxTemplateDescriptor.name,
                category = vfxTemplateDescriptor.category,
                description = vfxTemplateDescriptor.description,
                icon = vfxTemplateDescriptor.icon,
                thumbnail = vfxTemplateDescriptor.thumbnail,
            });
        }
    }
}
