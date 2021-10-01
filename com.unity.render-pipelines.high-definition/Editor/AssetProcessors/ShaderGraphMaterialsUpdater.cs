using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal;


namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    class ShaderGraphMaterialsUpdater
    {
        static ShaderGraphMaterialsUpdater()
        {
            GraphData.onSaveGraph += OnShaderGraphSaved;
        }

        // TODOJENNY: This entire file could be removed if we add dependencies between shadergraphs and their material
        // ideally on material import, a dependency is added towards its shader
        // so when the SG is modified/reimported, its linked materials will also be reimported
        // sss needs to active a keyword on a material
        static void OnShaderGraphSaved()
        {
            HDRenderPipeline.currentPipeline?.ResetPathTracing(); //TODORemi: check if this be called more (e.g. change from versioing tool. Check with Emannuel
            
        }
    }
}
