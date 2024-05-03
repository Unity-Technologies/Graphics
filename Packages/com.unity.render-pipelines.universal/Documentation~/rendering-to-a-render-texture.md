# Render a camera's output to a Render Texture

In the Universal Render Pipeline (URP), a Camera can render to the screen or to a [Render Texture](https://docs.unity3d.com/Manual/class-RenderTexture.html). Rendering to a screen is the default and is the most common use case, but rendering to a Render Texture allows you to create effects such as CCTV camera monitors.

If you have a Camera that is rendering to a Render Texture, you must have a second Camera that then renders that Render Texture to the screen. In URP, all Cameras that render to Render Textures perform their render loops before all Cameras that render to the screen. This ensures that the Render Textures are ready to render to the screen. For more information on Camera rendering order in URP, refer to [Rendering order and overdraw](cameras-advanced.md).

## Render to a Render Texture that renders to the screen

1. Create a Render Texture Asset in your project. To do this select **Assets** > **Create** > **Render Texture**.
2. Create a Quad game object in your scene.
3. Create a material in your Project. 
4. In the Inspector, drag the Render Texture to the material's **Base Map** field.
5. In the Scene view, drag the material on to the quad.
6. Create a camera in your scene.
7. Select the Base Camera and in the Inspector, drag the Render Texture on to the **Output Texture** property.
8. Create another camera in your scene.
9. Place the quad within the view of the new Base Camera.

The first Camera now renders its view to the Render Texture. The second Camera renders the scene including the Render Texture to the screen.

You can set the output target for a camera in a script by setting the `targetTexture` property of the camera:

```c#
myCamera.targetTexture = myRenderTexture;
```
