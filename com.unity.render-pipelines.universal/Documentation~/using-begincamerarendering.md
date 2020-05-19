# Using the beginCameraRendering event

The example on this page shows how to use the [beginCameraRendering](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html) event to run a custom method. 

## beginCameraRendering event overview

Unity raises a `beginCameraRendering` event before it renders each active Camera in every frame. If a Camera is inactive (for example, if the __Camera__ component checkbox is cleared on a Camera GameObject), Unity does not raise a `beginCameraRendering` event for this Camera. 

When you subscribe a method to this event, you can execute custom logic before Unity renders the Camera. Examples of custom logic include rendering extra Cameras to Render Textures, and using those Textures for effects like planar reflections or surveillance camera views.

Other events in the [RenderPipelineManager](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager.html) class provide more ways to customize URP. You can also use the principles described in this article with those events.

## beginCameraRendering event example

This example demonstrates how to subscribe a method to the `beginCameraRendering` event.
To follow the steps in this example, create a [new Unity project using the __Universal Project Template__](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@8.0/manual/creating-a-new-project-with-urp.html).

1. In the Scene, create a Cube. Name it Example Cube.
2. In your Project, create a C# script. Call it `URPCallbackExample`.
3. Copy and paste the following code into the script.
    ```C#
    using UnityEngine;
    using UnityEngine.Rendering;
    
    public class URPCallbackExample : MonoBehaviour
    {
        // Unity calls this method automatically when it enables this component
        private void OnEnable()
        {
            // Add WriteLogMessage as a delegate of the RenderPipelineManager.beginCameraRendering event
            RenderPipelineManager.beginCameraRendering += WriteLogMessage;
        }
    
        // Unity calls this method automatically when it disables this component
        private void OnDisable()
        {
            // Remove WriteLogMessage as a delegate of the  RenderPipelineManager.beginCameraRendering event
            RenderPipelineManager.beginCameraRendering -= WriteLogMessage;
        }
    
        // When this method is a delegate of RenderPipeline.beginCameraRendering event, Unity calls this method every time it raises the beginCameraRendering event
        void WriteLogMessage(ScriptableRenderContext context, Camera camera)
        {
            // Write text to the console
            Debug.Log($"Beginning rendering the camera: {camera.name}");
        }
    }
    ```
    > **NOTE**: When you subscribe to an event, your handler method (in this example, `WriteLogMessage`) must accept the parameters defined in the event delegate. In this example, the event delegate is `RenderPipeline.BeginCameraRendering`, which expects the following parameters: `<ScriptableRenderContext, Camera>`. 

4. Attach the `URPCallbackExample` script to Example Cube.

5. Select __Play__. Unity prints the message from the script in the Console window each time Unity raises the `beginCameraRendering` event.

    ![Unity prints log message in console.](Images/customizing-urp/log-message-in-console.png)

6. To raise a call to the `OnDisable()` method: In the Play mode, select Example Cube and clear the checkbox next to the script component title. Unity unsubscribes `WriteLogMessage` from the `RenderPipelineManager.beginCameraRendering` event and stops printing the message in the Console window.

    ![Deactivate the script component. Clear the checkbox next to the script component title.](Images/customizing-urp/deactivate-script-component.png)
