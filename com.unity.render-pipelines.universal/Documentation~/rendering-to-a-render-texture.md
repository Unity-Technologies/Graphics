# Rendering to a Render Texture
In the Universal Render Pipeline (URP), a Camera can render to the screen or to a [Render Texture](https://docs.unity3d.com/Manual/class-RenderTexture.html). Rendering to a screen is the default and is the most common use case, but rendering to a Render Texture allows you to create effects such as CCTV camera monitors.

If you have a Camera that is rendering to a Render Texture, you must have a second Camera that then renders that Render Texture to the screen. In URP, all Cameras that render to Render Textures perform their render loops before all Cameras that render to the screen. This ensures that the Render Textures are ready to render to the screen. For more information on Camera rendering order in URP, see [Rendering order and overdraw](cameras-advanced.md).

## Rendering to a Render Texture, and then rendering that Render Texture to the screen

![Rendering to a Render Texture in URP](Images/camera-inspector-output-target.png)

Create a Render Texture Asset in your Project using **Assets** > **Create** > **Render Texture**.
Create a Quad in your Scene.
Create a Material in your Project, and select it. In the Inspector, drag the Render Texture to the Material's **Base Map** field.
In the Scene view, drag the Material on to the Quad.
Create a Camera in your Scene. Its **Render Mode** defaults to **Base**, making it a Base Camera.
Select the Base Camera.
In the Inspector, scroll to the Output section.
Set the Camera’s  **Output Target** to **Texture**, and drag the Render Texture on to the **Texture** field.
Create another Camera in your Scene. Its **Render Mode** defaults to **Base**, making it a Base Camera.
Place the Quad within the view of the new Base Camera.

The first Camera renders its view to the Render Texture. The second Camera renders the Scene including the Render Texture to the screen.

You can set the Output Target for a Camera in a script by setting the `cameraOutput` property of the Camera's  [Universal Additional Camera Data](xref:UnityEngine.Rendering.Universal.UniversalAdditionalCameraData) component, like this:

```
myUniversalAdditionalCameraData.cameraOutput = CameraOutput.Texture;
myCamera.targetTexture = myRenderTexture;
```
