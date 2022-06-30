# Ray-traced shadows

Ray-traced shadows are shadows that HDRP generates by tracing rays of light from the following [Light](Light-Component.md) sources:

- [Directional](#DirectionalLight)
- [Point](#point-and-spot-light)
- [Spot](#point-and-spot-light)
- [Rectangle](#RectangleLight)

When you enable ray-traced shadows in your HDRP Project, they replace shadow maps for opaque GameObjects.

## Using ray-traced shadows

All ray-traced shadows are screen space shadows. This means that HDRP stores them in a screen space buffer which holds the information for every pixel on the screen that is in the depth buffer (which only stores opaque GameObjects).

To use ray-traced shadows, enable screen space shadows in your HDRP Project. To do this:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Lighting section, enable Screen Space Shadows.
3. Set the value for Maximum to be the maximum number of screen space shadows you want to evaluate each frame. If there are more than this number of Lights in your Scene then HDRP only ray casts the shadows for this number of them, then uses a shadow map for the rest.

Then make sure you enable Screen Space Shadows for your Cameras. To do this:

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the **Graphics > HDRP Global Settings** tab.
2. Scroll down to **Frame Settings (Default Values)**.
3. In the **Camera** section, open the **Lighting** foldout and enable Screen Space Shadows.

Finally, to make HDRP process ray-traced shadows for your Directional, Point, or Rectangle Light:

1. Under the Shadow Map fold-out in the Shadows section, click the Enable checkbox.
2. Also in the Shadow Map foldout, enable Ray-Traced Shadows. For Directional Lights, you need to enable Screen Space Shadows to access this property.
3. To change the behavior of the shadows, edit the properties under Ray-Traced Shadows.

<a name="DirectionalLight"></a>

## Directional Light

Ray-traced shadows offer an alternative to the cascade shadow map that Directional Lights use for opaque GameObjects.

![](Images/RayTracedShadows1.png)

**Directional Light cascade shadow map**

![](Images/RayTracedShadows2.png)

**Ray-traced Directional Light shadows (Sun Angle = 0)**

![](Images/RayTracedShadows3.png)

**Ray-traced Directional Light shadows (Sun Angle = 0.53, the angle of the Sun as seen from Earth)**

Ray-traced shadows allow for transparent and transmissive GameObjects, lit by Directional Lights, to cast colored shadows.

**Note**: When you use a Directional Light with a transmissive GameObject, HDRP displays rasterized shadows on the reverse side of the GameObject.

![](Images/RayTracedShadows9.png)

**Ray-traced Directional Light shadows with colored shadows**

![](Images/RayTracedShadows10.png)

**Ray-traced Directional Light shadows without colored shadows**

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sun Angle**         | Sets the size of the Sun in the sky, in degrees. For example, the value for the Sun on Earth is 0.53°. |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. Higher values produce more accurate shadows. Increasing this value increases execution time linearly. |
| **Color Shadow**      | Allows transparent and transmissive GameObjects to cast colored shadows. A Material can only cast colored shadows when its [**Refraction Model**](Surface-Type.md#transparency-inputs) is set to **Thin**, **Box** or **Sphere**. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray-traced shadows; making them smoother. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |

<a name="PointLight"></a>

## Point And Spot Light

Ray-traced shadows offer an alternative to the shadow map that Point and Spot Lights use for opaque GameObjects. HDRP still evaluates the lighting of a Point Light as coming from a single point in space (the light is [punctual](Glossary.md#punctual-lights)), but it evaluates the shadowing as if the light was coming from the surface of a sphere. On the other side, HDRP evaluates the lighting of a Spot Light as coming from a single point in space, but it evaluates the shadowing as if the light was coming from the surface of a cone.

![](Images/RayTracedShadows4.png)

**Point Light shadow map**

![](Images/RayTracedShadows5.png)

**Ray-traced Point Light shadows (Radius = 0.001m)**

![](Images/RayTracedShadows6.png)

**Ray-traced Point Light shadows (radius = 0.5m)**

Ray-traced shadows offer the possibility of semi-transparent shadows for Point Lights cast by transparent and transmissive GameObjects.

![](Images/RayTracedShadows11.png)

**Ray-traced Point Light shadows with semi-transparent shadows**

![](Images/RayTracedShadows12.png)

**Ray-traced Point Light shadows without semi-transparent shadows**

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. A low number of samples produces a noisier image that requires more denoising. Increasing this value increases execution time linearly. |
| **Semi-Transparent Shadow**      | Allows transparent and transmissive GameObjects to cast semi-transparent shadows. |
| **Radius**            | Sets the radius of the sphere light that HDRP uses to evaluate the shadows. This increases the size of the surface that emits light. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray-traced shadows. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |
| - **Distance Based Denoising** | Specifies whether HDRP should take the distance to the occluder into account when it denoises the shadow. This increases the denoiser quality but also its resource intensity. |

<a name="RectangleLight"></a>

## Rectangle Light

Ray-traced shadows offer an alternative to the [exponential variance shadow map](Glossary.md#ExponentialVarianceShadowMap) that Rectangle Lights use for opaque GameObjects.

![](Images/RayTracedShadows7.png)
**Rectangle Light shadow map**

![](Images/RayTracedShadows8.png)
**Ray-traced Rectangle Light shadows**

**Note**: When rendering in [deferred mode](Forward-And-Deferred-Rendering.md), HDRP provides accurate ray-traced area light shadows for the [Lit](Lit-Shader.md) shader. When HDRP renders for any other shader, or for the Lit shader in forward mode, it uses an approximation to calculate ray-traced shadows for area lights. This approximation is not perfectly accurate, but does produce plausible results.

When you use a Rectangle Light with a transmissive GameObject, HDRP displays rasterized shadows on the reverse side of the GameObject

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. Increasing this values increases execution time linearly. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray-traced shadows. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |


## Limitations

#### Double-sided shadows
Ray-traced shadows do not support the **Two Sided** option for the Mesh Renderer's **Cast Shadows** property. To use double-sided shadows for a mesh, open the Mesh Renderer's Material in the Inspector and, in the **Surface Options** section, enable the **Double-Sided** property.

#### Recursive rendering

GameObjects HDRP renders using [recursive rendering](Ray-Tracing-Recursive-Rendering.md) cannot receive ray-traced shadows. If you enable both effects, HDRP renders rasterized [shadows](Shadows-in-HDRP.md) on recursively rendered GameObjects.
