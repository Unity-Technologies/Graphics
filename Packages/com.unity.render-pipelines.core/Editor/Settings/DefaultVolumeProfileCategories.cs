using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class DefaultVolumeProfileCategories
    {
        const string k_MainCategoryName = "Main";

        // Declare some known SRP Volume categories in order to affect their order. Empty categories won't be displayed.
        static readonly string[] s_DefaultCategoryNames = { k_MainCategoryName, "Sky", "Lighting", "Shadowing", "Post-processing" };

        public Dictionary<string, List<VolumeComponentEditor>> categories { get; } = new();

        public DefaultVolumeProfileCategories(VolumeProfile profile)
        {
            var volumeComponentTypeList = VolumeManager.instance.GetVolumeComponentsForDisplay(GraphicsSettings.currentRenderPipelineAssetType);

            foreach (var defaultCategory in s_DefaultCategoryNames)
                categories.Add(defaultCategory, new());

            var components = profile.components;
            foreach (var component in components)
            {
                foreach (var (path, type) in volumeComponentTypeList)
                {
                    if (type == component.GetType())
                    {
                        var editor = (VolumeComponentEditor) Editor.CreateEditor(component);
                        editor.SetVolumeProfile(profile);
                        editor.enableOverrides = false;
                        editor.categoryTitle = ToCategoryName(path);
                        editor.Init();

                        if (!categories.ContainsKey(editor.categoryTitle))
                            categories.Add(editor.categoryTitle, new ());

                        categories[editor.categoryTitle].Add(editor);

                        break;
                    }
                }
            }

            foreach (var (_, categoryEditors) in categories)
            {
                categoryEditors.Sort((a, b) =>
                    a.GetDisplayTitle().text.CompareTo(b.GetDisplayTitle().text));
            }
        }

        static string ToCategoryName(string volumeComponentPath)
        {
            var parts = volumeComponentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return k_MainCategoryName;
            return volumeComponentPath.Substring(0, volumeComponentPath.LastIndexOf('/')).Replace("/", " / ");
        }

        public void Destroy()
        {
            foreach (var (_, categoryEditors) in categories)
                foreach (var editor in categoryEditors)
                    CoreUtils.Destroy(editor);
        }
    }
}
