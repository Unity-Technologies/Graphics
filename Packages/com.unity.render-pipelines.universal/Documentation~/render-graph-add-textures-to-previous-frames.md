---
uid: urp-render-graph-add-textures-to-previous-frames
---
# Add data to previous frames

To add your own texture to the camera history and read the data in later frames, create a camera history type to store the texture between frames.

## Create a camera history type

Follow these steps:

1. Create a class that inherits from `CameraHistoryItem`. For example:

    ```csharp
    public class ExampleHistoryType : CameraHistoryItem {
        ...
    }
    ```

2. In the class, add an id for the camera history system. For example:

    ```csharp
        private int uniqueId;
    ```

    The id represents the complete history of one texture, including the current and previous frames. 

    You can also add any other data you need, for example a texture descriptor you need to store between frames.

3. Override the `OnCreate` method. In the method, call the `OnCreate` method of the parent class, and generate the unique id. For example:

    ```csharp
    public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
    {
        // Call the OnCreate method of the parent class
        base.OnCreate(owner, typeId);

        // Generate the unique id
        uniqueId = MakeId(0);
    }
    ```

4. Create public properties for the current and previous textures, so that render passes can access them. For example:

    ```csharp
    public RTHandle currentTexture => GetCurrentFrameRT(uniqueId);
    public RTHandle previousTexture => GetPreviousFrameRT(uniqueId);
    ```

5. Allocate memory for the texture. For example:

    ```csharp
    // Allocate 2 textures using a texture descriptor, assign them to the uniqueId, and give them a name.
    AllocHistoryFrameRT(uniqueId, 2, ref textureDescriptor, "ExampleHistoryTexture");
    ```

You might also need to reallocate memory each frame if a render pass writes a texture with a different size or format.

## Write to the texture

To write to the texture you created, follow these steps:

1. To request access to the texture in a `ScriptableRenderPass` class, use the `RequestAccess` API with your camera history type. For example:

    ```csharp
    cameraData.historyManager.RequestAccess<ExampleHistoryType>();
    ```

2. Get the texture for the current frame for writing, and convert it to a handle the render graph system can use. For example:

    ```csharp
    // Get the textures 
    RawColorHistory history = cameraData.historyManager.GetHistoryForWrite<ExampleHistoryType>();

    // Get the texture for the current frame, using the unique id
    RTHandle historyTexture = history?.currentTexture(theUniqueid);

    // Convert the texture into a handle the render graph system can use
    historyTexture = renderGraph.ImportTexture(historyTexture);
    ```

You can then write to the texture in your render pass. For more information, refer to [Using textures](working-with-textures.md).

## Read from the texture

To read from the texture, use the `RequestAccess` API with the camera history type you created.

You must write to the texture before you read from it.

For more information, refer to [Get data from previous frames](render-graph-get-previous-frames.md).

## Example

The following is an example of a camera history type.

```csharp
public class ExampleHistoryType : CameraHistoryItem
{
    private int m_Id;
    
    // Add a descriptor for the size and format of the texture.
    private RenderTextureDescriptor m_Descriptor;

    // Add a hash key to track changes to the descriptor.
    private Hash128 m_DescKey;
    
    public override void OnCreate(BufferedRTHandleSystem owner, uint typeId)
    {
        base.OnCreate(owner, typeId);
        m_Id = MakeId(0);
    }
    
    public RTHandle currentTexture => return GetCurrentFrameRT(m_Id);
    public RTHandle previousTexture => return GetPreviousFrameRT(m_Id);

    // The render pass calls the Update method every frame, to initialize, update, or dispose of the textures.
    public void Update(RenderTextureDescriptor textureDescriptor)
    {
        // Dispose of the textures if the memory needs to be reallocated.
        if (m_DescKey != Hash128.Compute(ref textureDescriptor))
            ReleaseHistoryFrameRT(m_Id);

        // Allocate the memory for the textures if it's not already allocated.
        if (currentTexture == null)
        {
            AllocHistoryFrameRT(m_Id, 2, ref textureDescriptor, "HistoryTexture");
    
            // Store the descriptor and hash key for future changes.
            m_Descriptor = textureDescriptor;
            m_DescKey = Hash128.Compute(ref textureDescriptor);
        }
    }
}
```

The following is an example of a render pass that writes to the texture.

```csharp
class WriteToHistoryTexture : ScriptableRenderPass
{
    private class PassData
    {
        internal Material material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var cameraData = frameData.Get<UniversalCameraData>();

        cameraData.historyManager.RequestAccess<ExampleHistory>();
        var history = cameraData.historyManager.GetHistoryForWrite<ExampleHistory>();

        if (history != null)
        {
            // Call the Update method of the camera history type.
            history.Update(cameraData.cameraTargetDescriptor);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Write to history texture", out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                RTHandle historyTexture = history?.currentTexture(multipassId);

                // Set the render graph to render to the history texture.
                builder.SetRenderAttachment(renderGraph.ImportTexture(historyTexture), 0, AccessFlags.Write);

                passData.material = m_Material;

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    // Draw a triangle to the history texture
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                });
            }
        }
    }
}
```

