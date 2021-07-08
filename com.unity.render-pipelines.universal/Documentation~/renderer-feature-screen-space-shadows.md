# Screen Space Shadows Renderer Feature

With the Screen Space Shadows Renderer Feature, Unity can resolve main light shadows in screen space before drawing objects. It needs one more additional render target, but can prevent from multiple accesses to cascade shadow maps in forward rendering.
![Show screen space shadows](Images/ssshadows/ssshadows-result.png)<br/>*Screen Space Shadows in URP Template Scene*

![Show screen space shadows texture](Images/ssshadows/ssshadows-shadow-texture.png)<br/>*Screen Space Shadows TextureOnly*

After enabling this renderer feature, you can check this pass in frame debugger.
![Show main light shadows in frame debugger](Images/ssshadows/ssshadows-framedebugger.png)<br/>*Screen Space Shadows pass in frame debugger*

Compare casting shadows on opaque objects with screen space shadow texture or cascade shadow maps.
![Cast Shadows using screen space shadow stexture](Images/ssshadows/ssshadows-cast-shadow-using-screenspace.png)<br/>*Shadows from screen space shadow texture*

![Cast shadows using cascade shadowmaps](Images/ssshadows/ssshadows-cast-shadow-using-cascades.png)<br/>*Shadows from cascade shadow maps* 

## Adding the Screen Space Shadows Renderer Feature to a Renderer

It provides the Screen Space Shadows as Renderer Feature.

To resolve main light shadows with screen space:

1. In the Project window, select the Renderer that URP asset is using.

![Select Renderer](Images/ssshadows/ssshadows-select-renderer.png)<br/>*The inspector window shows the Renderer properties.*

2. In the Inspector window, select Add Renderer Feature. In the list, select Screen Space Shadows.

![Select Renderer Feature](Images/ssshadows/ssshadows-select-renderer-feature.png)<br/>*Unity adds the Screen Space Shadow to the Renderer.*

![Add Screen Space Shadows Renderer Feature](Images/ssshadows/ssshadows-renderer-feature-added.png)

## No Properties

## Implementation details

The Screen Space Shadows Renderer Feature needs depth texture before drawing opaque objects, and will invoke depth prepass.
It resolves main light shadows in screen space prior to 'DrawOpaqueObjects' pass and works on only opaque objects.

If you added this renderer feature and used transparent objects, only opaque objects would be shadowed with screen space shadows, but
transparent objects would be shadowed by cascade shadow maps.
![Won't cast shadows on transparent from screen space shadow texture](Images/ssshadows/ssshadows-cast-shadow-totransparent.png)
