using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    class AssetVersion : ScriptableObject
    {
        public int version;
        // General material migration uses the subasset "AssetVersion" which stores a "version" in a subasset
        // in and for the material asset itself, see MaterialPostProcessor.
        // Plugin materials (unknown to HDRP) might have their own migration to process and use its entry in
        // hdPluginSubTargetMaterialVersions. The detection and processing of a version change is started in the
        // same place at the MaterialPostProcessor.
        //
        // A dictionary is used since shaders can be switched around on a material and this way, we avoid confusing
        // different plugin material versions.
        //
        // Also note that besides materials themselves, versioning also happens for HDSubTargets (shaders) in .shadergraph
        // assets. HDSubTargets store in their SystemData chunk a "version" which holds HDRP's own "ShaderGraphVersion"
        // for its SG-based shaders, which process ShaderGraph HDSubTarget related serialized data migration common to all
        // the HDRP ShaderGraph system and its shaders, a migration which happens at HDSubTarget.Setup() time.
        public PluginMaterialVersions hdPluginSubTargetMaterialVersions = new PluginMaterialVersions();
    }
}
