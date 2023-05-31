using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Custom Pass that draws Object IDs
    /// </summary>
    [Serializable]
    public class ObjectIDCustomPass : DrawRenderersCustomPass
    {
        static readonly int k_ObjectColor = Shader.PropertyToID("ObjectColor");

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);

            AssignObjectIDs();

            overrideMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaderGraphs.objectIDPS);
            overrideMaterialPassName = "ForwardOnly";
        }

        /// <summary>
        /// Used to assign an ObjectID (in the form of a color) to every renderer in the scene.
        /// If a scene uses dynamic objects or procedural object placement, then the user script should call
        /// this function to assign Object IDs to the new objects.
        /// </summary>
        public virtual void AssignObjectIDs()
        {
            var sceneCount = SceneManager.sceneCount;
            var rendererList = new List<Renderer>();
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var rootGameObjects = scene.GetRootGameObjects();
                foreach (var rootGameObject in rootGameObjects)
                {
                    rendererList.AddRange(rootGameObject.GetComponentsInChildren<Renderer>());
                }
            }

            var renderListCount = rendererList.Count;
            for (var i = 0; i < renderListCount; i++)
            {
                var renderer = rendererList[i];
                var propertyBlock = new MaterialPropertyBlock();
                //it could be just i / renderListCount but I wanted to have more separation between colors
                var hue = (float) ( i * 3 % renderListCount) / renderListCount;
                propertyBlock.SetColor(k_ObjectColor, Color.HSVToRGB(hue, 0.7f, 1.0f));
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
