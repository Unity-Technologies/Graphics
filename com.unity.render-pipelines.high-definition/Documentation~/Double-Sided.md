# Double sided

This setting controls whether The High Definition Render Pipeline (HDRP) renders both faces of the geometry of GameObjects using this Material, or just the front face.

 ![](Images/DoubleSided1.png)

This setting is disabled by default.  Enable it if you want HDRP to render both faces of your geometry. When disabled, HDRP [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back-facing polygons of your geometry and only renders front-facing polygons.

## Properties

### Surface Options

| **Property**    | Description                                                  |
| --------------- | ------------------------------------------------------------ |
| **Normal Mode** | Use the drop-down to select the mode that HDRP uses to calculate the normals for the back facing geometry.<br />&#8226; **Flip**: The normal of the back face is 180° of the front facing normal. This also applies to the Material which means that it looks the same on both sides of the geometry.<br />&#8226; **Mirror**: The normal of the back face mirrors the front facing normal. This also applies to the Material which means that it inverts on the back face. This is useful when you want to keep the same shapes on both sides of the geometry, for example, for leaves.<br />&#8226; **None**: The normal of the back face is the same as the front face. |

