# How to add a Render Pass to a Renderer at runtime
To add a Render Pass you need to retrive the scriptable renderer you want to add the Render Pass to and use the function:
'''c++
scriptableRenderer.EnqueuePass(renderPass);
'''
This will have to be done for each frame since the queue is cleared each iteration.

## Example
Here is a small example how to add a Render Pass at Runtime:
```c++
public class RunPassAtRuntime : MonoBehaviour
{
    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            [...]
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            [...]
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            [...]
        }
    }
    CustomRenderPass renderPass;
    void Start()
    {
        renderPass = new CustomRenderPass();
    }
    void Update()
    {
        ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).scriptableRenderer.EnqueuePass(renderPass);
    }
}

A larger code example can be downloaded in: 'Package Manager -> Universal RP -> Samples -> RenderPass Samples'
![Samples located in Universal RP package](Images/urp-download-samples.png)
```