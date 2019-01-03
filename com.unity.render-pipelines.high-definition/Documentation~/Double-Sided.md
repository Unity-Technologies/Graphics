# Double sided

This setting controls whether HDRP renders both faces of the geometry of GameObjects using this Material, or just the front face.

 ![](Images/DoubleSided1.png)

This setting is disabled by default.  Enable it if you want HDRP to render both faces of your geometry. When disabled, HDRP [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back-facing polygons of your geometry and only renders front-facing polygons.

 