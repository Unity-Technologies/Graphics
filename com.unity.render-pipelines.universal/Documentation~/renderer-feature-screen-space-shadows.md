# Screen Space Shadows Renderer Feature

The Screen Space Shadows [Renderer Feature](urp-renderer-feature.md) calculates screen-space shadows for opaque objects affected by the main directional light and draws them in the scene. To render screen-space shadows, URP requires an additional render target. This increases the amount of memory your application requires, but if your project uses forward rendering, screen-space shadows can benefit the runtime resource intensity. This is because if you use screen-space shadows, URP doesn't need to access the cascade shadow maps multiple times.
![Show screen space shadows](Images/ssshadows/ssshadows-result.png)<br/>*Screen-space shadows in a sample Scene.*

![Show screen space shadows texture](Images/ssshadows/ssshadows-shadow-texture.png)<br/>*The screen-space shadows texture for the above image.*

## Enabling screen-space shadows

To add screen space shadows to your project, [add the Screen Space Shadows Renderer Feature ](urp-renderer-feature-how-to-add.md) to the URP Renderer.

## Viewing screen-space shadows in the Frame Debugger

After you enable this Renderer Feature, URP renders screen-space shadows in your scene. To distinguish between shadow map shadows and screen-space shadows, you can view the render passes that draws the shadows in the [Frame Debugger](https://docs.unity3d.com/Manual/FrameDebugger.html).
![Show main light shadows in frame debugger](Images/ssshadows/ssshadows-framedebugger.png)<br/>*Screen Space Shadows pass in frame debugger.*

You can compare shadows cast on opaque objects from the screen-space shadow texture or the cascade shadow maps.
![Cast Shadows using screen space shadow stexture](Images/ssshadows/ssshadows-cast-shadow-using-screenspace.png)<br/>*The Frame Debugger shows the screen-space shadows texture.*

![Cast shadows using cascade shadowmaps](Images/ssshadows/ssshadows-cast-shadow-using-cascades.png)<br/>*The Frame Debugger shows shadows from a shadow map.*

## **Requirements and compatibility**

This Renderer Feature uses a depth texture and invokes a depth prepass before it draws opaque objects. It calculates the shadows in screen space before the `DrawOpaqueObjects` render pass. URP doesn't calculate or apply screen-space shadows for transparent objects; it uses [shadow maps](Shadows-in-URP.md) for transparent objects instead. ![Won't cast shadows on transparent from screen space shadow texture](Images/ssshadows/ssshadows-cast-shadow-totransparent.png)*<br/>The Frame Debugger showing that Unity uses shadow maps for transparent objects and screen-space shadows for opaque objects.*
