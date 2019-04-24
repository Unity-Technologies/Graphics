using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class SpliceShaderGraphsForCrossSection
    {
        [MenuItem("Edit/Render Pipeline/Splice CrossSection Tool In ShaderGraphs", priority = CoreUtils.editMenuPriority2)]
        static void CrossSectionInstallSelection()
        {
            CrossSectionShaderGraphsEditor.SpliceShaderGraphsForCrossSectionOnSelected("Splicing CrossSection Tool in ShaderGraphs", removeSplice: false, new CrossSectionEnablerConfigDefault());
        }
        [MenuItem("Edit/Render Pipeline/UnSplice CrossSection Tool In ShaderGraphs", priority = CoreUtils.editMenuPriority2)]
        static void CrossSectionUninstallSelection()
        {
            CrossSectionShaderGraphsEditor.SpliceShaderGraphsForCrossSectionOnSelected("UnSplicing CrossSection Tool in ShaderGraphs", removeSplice:true, new CrossSectionEnablerConfigDefault());
        }
        [MenuItem("Assets/Splice CrossSection Tool In ShaderGraphs")]
        private static void CrossSectionInstallSelectionContextMenu()
        {
            CrossSectionInstallSelection();
        }
        [MenuItem("Assets/UnSplice CrossSection Tool In ShaderGraphs")]
        private static void CrossSectionUnInstallSelectionContextMenu()
        {
            CrossSectionUninstallSelection();
        }
        [MenuItem("Assets/Splice CrossSection Tool In ShaderGraphs", true)]
        private static bool CrossSectionInstallSelectionContextMenuEnable()
        {
            return Selection.activeObject.GetType() == typeof(Shader);
        }
        [MenuItem("Assets/UnSplice CrossSection Tool In ShaderGraphs", true)]
        private static bool CrossSectionUnInstallSelectionContextMenuEnable()
        {
            return CrossSectionInstallSelectionContextMenuEnable();
        }
    }
}
