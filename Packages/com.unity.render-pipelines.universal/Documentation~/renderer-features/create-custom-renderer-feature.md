# How to create a custom Renderer Feature

This section describes how to create a custom Renderer Feature for a URP Renderer.

This section assumes the following:

* The **Scriptable Render Pipeline Settings** property refers to a URP asset (**Project Settings** > **Graphics** > **Scriptable Render Pipeline Settings**).

This article contains the following sections:

* [Create example Scene and GameObjects.](#example-scene)

* [Create a scriptable Renderer Feature and add it to the Universal Renderer.](#scriptable-renderer-feature)

* [Create and enqueue the scriptable Render Pass.](#scriptable-render-pass)

* [Implement rendering commands in the Execute method.](#execute-method)

* [Implement the example-specific Material and rendering code.](#example-specific-material)

* [Change the order of the render passes](#order-of-passes)

* [Complete code for this example](#complete-code)

## <a name="example-scene"></a>Create example Scene and GameObjects

To follow the steps in this section, create a new Scene with the following GameObjects:

1. Create a plane.

2. Create a new Material and assign it the `Universal Render Pipeline/Lit` shader. Set the base color to grey (for example, `#6A6A6A`). Call the Material `Plane`.

3. Create a Point Light and place it above the plane.

Your Scene should look like the following illustration:

![Example Scene](../Images/customizing-urp/custom-renderer-feature/sample-scene.png)

## <a name="scriptable-renderer-feature"></a>Create a scriptable Renderer Feature and add it to the Universal Renderer

This part shows how to create a scriptable Renderer Feature and implement the methods that let you configure and inject `ScriptableRenderPass` instances into the scriptable Renderer.

1. Create a new C# script. Call the script `LensFlareRendererFeature.cs`.

2. Open the script, remove all the code from the `LensFlareRendererFeature` class that Unity created. Add the following `using` directive.

    ```C#
    using UnityEngine.Rendering.Universal;
    ```

3. The `LensFlareRendererFeature` class must inherit from the `ScriptableRendererFeature` class.

    ```C#
    public class LensFlareRendererFeature : ScriptableRendererFeature
    ```

4. The class must implement the following methods:

    * `Create`: Unity calls this method on the following events:

        * When the Renderer Feature loads the first time.

        * When you enable or disable the Renderer Feature.

        * When you change a property in the inspector of the Renderer Feature.

    * `AddRenderPasses`: Unity calls this method every frame, once for each Camera. This method lets you inject `ScriptableRenderPass` instances into the scriptable Renderer.

Now you have the custom `LensFlareRendererFeature` Renderer Feature with its main methods.

Below is the complete code for this part.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensFlareRendererFeature : ScriptableRendererFeature
{
    public override void Create()
    { }

    public override void AddRenderPasses(ScriptableRenderer renderer,
    ref RenderingData renderingData)
    { }
}
```

Add the Renderer Feature you created to the the Universal Renderer asset. [Follow this link to read how to add a Renderer Feature to a Renderer](../urp-renderer-feature-how-to-add.md).

![Add the Lens Flare Renderer Feature to the Universal Renderer.](../Images/customizing-urp/custom-renderer-feature/add-new-renderer-feature.png)<br/>*Add the Lens Flare Renderer Feature to the Universal Renderer.*

## <a name="scriptable-render-pass"></a>Create and enqueue the scriptable Render Pass

This part shows how to create a scriptable Render Pass and and enqueue its instance into the scriptable Renderer.

1. In the `LensFlareRendererFeature` class, declare the `LensFlarePass` class that inherits from `ScriptableRenderPass`.

    ```C#
    class LensFlarePass : ScriptableRenderPass
    { }
    ```

2. In `LensFlarePass`, add the `Execute` method.

    Unity runs the `Execute` method every frame. In this method, you can implement your custom rendering functionality.

    ```C#
    public override void Execute(ScriptableRenderContext context,
    ref RenderingData renderingData)
    { }
    ```

3. In the `LensFlareRendererFeature` class, declare a private `LensFlarePass` field.

    ```C#
    private LensFlarePass _lensFlarePass;
    ```

4. In the `Create` method, instantiate the `_lensFlarePass` object:

    ```C#
    _lensFlarePass = new LensFlarePass(FlareSettings);
    ```

5. In the `AddRenderPasses` method, use the `EnqueuePass` method of the `renderer` object to enqueue `_lensFlarePass` in the rendering queue.

    ```C#
    renderer.EnqueuePass(_lensFlarePass);
    ```

Now your custom `LensFlareRendererFeature` Renderer Feature is executing the `Execute` method inside the custom `LensFlarePass` pass.

Below is the complete code for this part.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensFlareRendererFeature : ScriptableRendererFeature
{
    class LensFlarePass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Debug.Log(message: "The Execute() method runs.");
        }
    }

    private LensFlarePass _lensFlarePass;

    public override void Create()
    {
        _lensFlarePass = new LensFlarePass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_lensFlarePass);
    }
}
```

## <a name="execute-method"></a>Implement rendering commands in the Execute method

This part shows how to implement custom logic in the Execute method.

1. Create a [CommandBuffer](https://docs.unity3d.com/2021.2/Documentation/ScriptReference/Rendering.CommandBuffer.html) type object. This object holds the list of rendering commands to execute.

    In the `Execute` method, add the following line:

    ```C#
    CommandBuffer cmd = CommandBufferPool.Get(name: "LensFlarePass");
    ```

    The method `CommandBufferPool.Get(name: "LensFlarePass")` gets the new command buffer and assigns a name to it.

2. Add the line that executes the command buffer and the line that releases it.

    In the `Execute` method, add the following lines after the command buffer declaration:

    ```C#
    context.ExecuteCommandBuffer(cmd);
    CommandBufferPool.Release(cmd);
    ```

    Now the boilerplate part is ready and we can proceed to implementing the custom rendering logic.

The following steps implement the custom rendering logic.

In this example, the Renderer Feature draws lens flares as a texture on a Quad. The implementation requires a Material and a mesh (Quad).

1. In the `LensFlarePass` class, declare two private fields: `Material` and `Mesh`:

    ```C#
    private Material _material;
    private Mesh _mesh;
    ```

    Then declare the constructor that takes those variables as arguments:

    ```C#
    public LensFlarePass(Material material, Mesh mesh)
    {
        _material = material;
        _mesh = mesh;
    }
    ```

2. Now the `LensFlarePass` class expects two arguments. To initialize the class with the arguments, add the following public fields in the `LensFlareRendererFeature` class:

    ```C#
    public Material material;
    public Mesh mesh;
    ```

    And add the arguments to the `LensFlarePass` declaration in the `Create` method:

    ```C#
    _lensFlarePass = new LensFlarePass(material, mesh);
    ```

3. In the `Execute` method, use the `DrawMesh` method of the `cmd` object. The method takes the `_material` and the `_mesh` fields as arguments. Add the following line between the `cmd` object declaration and the command `context.ExecuteCommandBuffer(cmd)`.

    ```C#
    cmd.DrawMesh(_mesh, Matrix4x4.identity, _material);
    ```

    To ensure that Unity does call the `DrawMesh` method with `null` arguments, in the `AddRenderPasses` method, wrap the `EnqueuePass` call in the null check condition:

    ```C#
    if (material != null && mesh != null)
    {
        renderer.EnqueuePass(_lensFlarePass);
    }
    ```

Now the `LensFlarePass` class has the following basic logic in the `Execute` method:

1. Get the new command buffer and assign it the name `LensFlarePass`.

2. Add rendering commands.

3. Execute the command buffer.

4. Release the buffer.

> **NOTE:** Unity does not enqueue the `LensFlarePass` pass yet, because the `Material` and the `Mesh` properties are null.

Below is the complete code for this part.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensFlareRendererFeature : ScriptableRendererFeature
{
    class LensFlarePass : ScriptableRenderPass
    {
        private Material _material;
        private Mesh _mesh;

        public LensFlarePass(Material material, Mesh mesh)
        {
            _material = material;
            _mesh = mesh;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "LensFlarePass");
            cmd.DrawMesh(_mesh, Matrix4x4.identity, _material);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private LensFlarePass _lensFlarePass;
    public Material material;
    public Mesh mesh;

    public override void Create()
    {
        _lensFlarePass = new LensFlarePass(material, mesh);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (material != null && mesh != null)
        {
            renderer.EnqueuePass(_lensFlarePass);
        }
    }
}
```

## <a name="example-specific-material"></a>Implement the example-specific Material and rendering code

This section shows how to create a Material for the lens flare effect and how to implement the code to render flares at the positions of Lights.

1. Create a new Material, and assign it the `Universal Render Pipeline/Unlit` shader. Call the Material `LensFlare`.

1. For demonstration purpose, change the base color of the Material to red.

2. In the Universal Renderer, in `Lens Flare Renderer Feature`, select the `LensFlare` Material in the Material property, and the `Quad` mesh in the Mesh property.

    ![](../Images/customizing-urp/custom-renderer-feature/select-mesh-and-material.png)

3. The Renderer Feature draws the quad in the Scene, but at this point it's just black. This is because the `Universal Render Pipeline/Unlit` shader has multiple passes, and one of them paints the quad black. To change this behavior, use the `cmd.DrawMesh` method overload that accepts the `shaderPass` argument, and specify shader pass 0:

    ```C#
    cmd.DrawMesh(_mesh, Matrix4x4.identity, _material, 0, 0);
    ```

The following steps show the changes that are specific to the effect implementation in this example. They are for illustrative purposes.

1. Add the following lines in the `Execute` method. Place them after the `cmd` object declaration. These lines ensure that Unity draws the quad with the flare in the following way:

    <ul>
        <li>In the screen space.</li>
        <li>With the correct aspect ratio.</li>
        <li>For each Light, in the center of the Light.</li>
    </ul>

    ```C#
    // Get the Camera data from the renderingData argument.
    Camera camera = renderingData.cameraData.camera;
    // Set the projection matrix so that Unity draws the quad in screen space
    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
    // Add the scale variable, use the Camera aspect ratio for the y coordinate
    Vector3 scale = new Vector3(1, camera.aspect, 1);
    // Draw a quad for each Light, at the screen space position of the Light.
    foreach (VisibleLight visibleLight in renderingData.lightData.visibleLights)
    {
        Light light = visibleLight.light;
        // Convert the position of each Light from world to viewport point.
        Vector3 position =
            camera.WorldToViewportPoint(light.transform.position) * 2 - Vector3.one;
        // Set the z coordinate of the quads to 0 so that Uniy draws them on the same plane.
        position.z = 0;
        // Change the Matrix4x4 argument in the cmd.DrawMesh method to use the position and
        // the scale variables.
        cmd.DrawMesh(_mesh, Matrix4x4.TRS(position, Quaternion.identity, scale),
            _material, 0, 0);
    }
    ```

    Now Unity draws a quad in the center of each Light.

    ![](../Images/customizing-urp/custom-renderer-feature/quad-in-screen-space-on-light.png)

2. To visualize the lens flare, make the following changes to the `LensFlare` Material.

    Add the following texture to the base map:<br/>![Lens flare texture.](../Images/customizing-urp/custom-renderer-feature/lens-flare-texture.png)

    Set the color to white.

    Set `Surface Type` to `Transparent`.

    Set `Blending Mode` to `Additive`.

Now Unity draws the lens flare texture on the quad, but a part of the flare is not visible:

![](../Images/customizing-urp/custom-renderer-feature/skybox-after-lens-flare.png)

This is because Unity draws the skybox after the `LensFlarePass` render pass.

## <a name="order-of-passes"></a>Change the order of the render passes

To see the order in which Unity draws the render passes, open the **Frame Debugger** (**Window** > **Analysis** > **Frame&#160;Debugger**).

![](../Images/customizing-urp/custom-renderer-feature/frame-debug-view.png)

To enqueue the `LensFlarePass` pass after the skybox pass, use the `renderPassEvent` property of `LensFlarePass`. Assign the property the `AfterRenderingSkybox` event from the `RenderPassEvent` enum.

Make the following changes in the `Create` method:

```C#
public override void Create()
{
    _lensFlarePass = new LensFlarePass(material, mesh);
    // Draw the lens flare effect after the skybox.
    _lensFlarePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
}
```

Now Unity draws the lens flare on top of the skybox.

![](../Images/customizing-urp/custom-renderer-feature/final-lens-flare-view.png)

## <a name="complete-code"></a>Complete code for this example

Below is the complete code for this example.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensFlareRendererFeature : ScriptableRendererFeature
{
    class LensFlarePass : ScriptableRenderPass
    {
        private Material _material;
        private Mesh _mesh;

        public LensFlarePass(Material material, Mesh mesh)
        {
            _material = material;
            _mesh = mesh;
        }

        public override void Execute(ScriptableRenderContext context,
            ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "LensFlarePass");
            // Get the Camera data from the renderingData argument.
            Camera camera = renderingData.cameraData.camera;
            // Set the projection matrix so that Unity draws the quad in screen space.
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // Add the scale variable, use the Camera aspect ratio for the y coordinate
            Vector3 scale = new Vector3(1, camera.aspect, 1);

            // Draw a quad for each Light, at the screen space position of the Light.
            foreach (VisibleLight visibleLight in renderingData.lightData.visibleLights)
            {
                Light light = visibleLight.light;
                // Convert the position of each Light from world to viewport point.
                Vector3 position =
                    camera.WorldToViewportPoint(light.transform.position) * 2 - Vector3.one;
                // Set the z coordinate of the quads to 0 so that Uniy draws them on the same
                // plane.
                position.z = 0;
                // Change the Matrix4x4 argument in the cmd.DrawMesh method to use
                // the position and the scale variables.
                cmd.DrawMesh(_mesh, Matrix4x4.TRS(position, Quaternion.identity, scale),
                    _material, 0, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private LensFlarePass _lensFlarePass;
    public Material material;
    public Mesh mesh;

    public override void Create()
    {
        _lensFlarePass = new LensFlarePass(material, mesh);
        // Draw the lens flare effect after the skybox.
        _lensFlarePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material != null && mesh != null)
        {
            renderer.EnqueuePass(_lensFlarePass);
        }
    }
}
```
