# Subpixel Anti-Aliasing

Menu Path : **Output > Subpixel Anti-Aliasing**

The **Subpixel Anti-Aliasing** Block forces the ScaleX and ScaleY to cover at least one pixel of the screen. If this Block enlarges a particle to make it fit at least one pixel, it reduces the alpha of the particle to compensate for the particle contribution.

<video src="Images/Block-SubpixelAntiAliasingExample.mp4" title="Left: A rotating sphere, with Subpixel Anti-Aliasing enabled. Its surface comprises small, white particles or dots arranged in a dynamic pattern. Right: The same sphere, with Subpixel Anti-Aliasing disabled. The sphere appears less defined. The particle distribution is sparse and scattered without forming a tight, cohesive outline." width="320" height="auto" autoplay="true" loop="true" controls></video>

## Block compatibility

This Block is compatible with the following Contexts:

- Any output Context

