---
uid: um-srp-creating-render-pipeline-asset-and-render-pipeline-instance
---

# Create a Render Pipeline Asset and Render Pipeline Instance in a custom render pipeline

If you are creating your own render pipeline based on the Scriptable Render Pipeline (SRP), your Project must contain:

* A script that inherits from [RenderPipelineAsset](xref:UnityEngine.Rendering.RenderPipelineAsset) and overrides its `CreatePipeline()` method. This script defines your Render Pipeline Asset.
* A script that inherits from [RenderPipeline](xref:UnityEngine.Rendering.RenderPipeline), and overrides its `Render()` method. This script defines your Render Pipeline Instance, and is where you write your custom rendering code.
* A Render Pipeline Asset that you have created from your [RenderPipelineAsset](xref:UnityEngine.Rendering.RenderPipelineAsset) script. This asset acts as a factory class for your Render Pipeline Instance.

Because these elements are so closely related, you should create them at the same time.

## Creating a basic Render Pipeline Asset and Render Pipeline Instance

The following example shows how to create a script for a basic custom Render Pipeline Asset that instantiates the Render Pipeline Instance, a script that defines the Render Pipeline Instance, and the Render Pipeline Asset itself.

1. Create a C# script called _ExampleRenderPipelineAsset.cs_.

2. Copy and paste the following code into the new script:

    ```lang-csharp
    using UnityEngine;
    using UnityEngine.Rendering;
    
    // The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
    [CreateAssetMenu(menuName = "Rendering/ExampleRenderPipelineAsset")]
    public class ExampleRenderPipelineAsset : RenderPipelineAsset
    {
        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
        protected override RenderPipeline CreatePipeline() {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering.
            return new ExampleRenderPipelineInstance();
        }
    }
    ```

3. Create a C# script called _ExampleRenderPipelineInstance.cs_.

4. Copy and paste the following code into the new script:


    ```lang-csharp
    using UnityEngine;
    using UnityEngine.Rendering;
    
    public class ExampleRenderPipelineInstance : RenderPipeline
    {
        public ExampleRenderPipelineInstance() {
        }
    
        protected override void Render (ScriptableRenderContext context, Camera[] cameras) {
            // This is where you can write custom rendering code. Customize this method to customize your SRP.
        }
    }
    ```

5. In the Project view, either click the add (+) button, or open the context menu and navigate to  **Create**, and then choose **Rendering** > **Example Render Pipeline Asset**. Unity creates a new Render Pipeline Asset in the Project view.

## Creating a configurable Render Pipeline Asset and Render Pipeline Instance

By default, a Render Pipeline Asset stores information about which Render Pipeline Instance to use for rendering, and the default Materials and Shaders to use in the Editor. In your `RenderPipelineAsset` script, you can extend your Render Pipeline Asset so that it stores additional data, and you can have multiple different Render Pipeline Assets with different configurations in your Project. For example, you might use a Render Pipeline Asset to hold configuration data for each different tier of hardware. The High Definition Render Pipeline (HDRP) and the Universal Render Pipeline (URP) include examples of this.

The following example shows how to create a `RenderPipelineAsset` script that defines a Render Pipeline Asset with public data that you can set for each instance using the Inspector, and a Render Pipeline Instance that receives a Render Pipeline Asset in its constructor and uses data from that Render Pipeline Asset.

1. Create a C# script called _ExampleRenderPipelineAsset.cs_.

2. Copy and paste the following code into the new script:

    ```lang-csharp
    using UnityEngine;
    using UnityEngine.Rendering;
    
    // The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
    [CreateAssetMenu(menuName = "Rendering/ExampleRenderPipelineAsset")]
    public class ExampleRenderPipelineAsset : RenderPipelineAsset
    {
        // This data can be defined in the Inspector for each Render Pipeline Asset
        public Color exampleColor;
        public string exampleString;
    
            // Unity calls this method before rendering the first frame.
           // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
        protected override RenderPipeline CreatePipeline() {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering, and pass a reference to this Render Pipeline Asset.
            // The Render Pipeline Instance can then access the configuration data defined above.
            return new ExampleRenderPipelineInstance(this);
        }
    }
    ```

3. Create a C# script called _ExampleRenderPipelineInstance.cs_.

4. Copy and paste the following code into the new script:

    ```lang-csharp
    using UnityEngine;
    using UnityEngine.Rendering;
    
    public class ExampleRenderPipelineInstance : RenderPipeline
    {
        // Use this variable to a reference to the Render Pipeline Asset that was passed to the constructor
        private ExampleRenderPipelineAsset renderPipelineAsset;
    
        // The constructor has an instance of the ExampleRenderPipelineAsset class as its parameter.
        public ExampleRenderPipelineInstance(ExampleRenderPipelineAsset asset) {
            renderPipelineAsset = asset;
        }
    
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            // This is an example of using the data from the Render Pipeline Asset.
            Debug.Log(renderPipelineAsset.exampleString);
            
            // This is where you can write custom rendering code. Customize this method to customize your SRP.
        }
    }

    ```

5. In the Project view, either click the add (+) button, or open the context menu and navigate to  **Create**, and then choose **Rendering** > **Example Render Pipeline Asset**. Unity creates a new Render Pipeline Asset in the Project view.