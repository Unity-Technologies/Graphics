# Forward and Deferred rendering

The High Definition Render Pipeline (HDRP) allows you to render Lit Materials using either Forward or Deferred rendering. You can configure your Unity Project to only use one of these modes, or allow it to use both and switch at runtime on a per-[Camera](hdrp-camera-component-reference.md) basis.

## Use Forward or Deferred rendering

Before you use forward or deferred rendering in your Unity Project, you must make sure your [HDRP Asset](HDRP-Asset.md) supports them.

To set the default support for forward or deferred rendering in your Project:

1. Select your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Rendering** > **Lit Shader Mode**.
3. Select one of the following Lit Shader modes:

| **Lit Shader Mode** | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Forward**         | HDRP calculates the lighting in a single pass when rendering each individual GameObject. |
| **Deferred**        | HDRP renders the Material properties of every GameObject visible on screen into a GBuffer. HDRP then processes the lighting for every pixel in the frame. |
| **Both**            | Use the [Frame Settings](Frame-Settings.md) to change between **Forward** and **Deferred** rendering mode on a per Camera basis. Selecting this increases Project [build time](#BuildTime). |

If you select **Both**, you can set the rendering mode at runtime for each Camera individually. For example, you can use Forward mode for a Planar Reflection Probe and then render your main Camera using Deferred mode.

To override the rendering mode for a specific Camera:

1. Select the Camera in the Hierarchy window to view it in the Inspector
2. Go to **Rendering** and enable **Custom Frame Settings**.
3. Go to the **Rendering** section and enable **Lit Shader Mode**. You can use this to change the **Lit Shader Mode** for this Camera.
4. Select the rendering mode you want this Camera to use.

## Selecting a Lit Shader Mode

To decide if you want to use Forward or Deferred mode, consider the level of quality and performance you want for your Project. Deferred rendering in HDRP is faster in most scenarios, such as a Scene with various Materials and multiple local Lights. Some scenarios, like those with a single Directional Light in a Scene, can be faster in Forward rendering mode. If quality is more important than performance for your Project, use Forward rendering mode for better quality rendering.

HDRP enforces Forward rendering for the following types of Shaders:

- Fabric
- Hair
- AxF
- StackLit
- Unlit
- Lit Shader with a Transparent Surface Type

If you set the **Lit Shader Mode** to **Deferred** in your HDRP Asset, HDRP uses deferred rendering to render all Materials that use an Opaque Lit Material.

Forward and Deferred rendering both implement the same features, but the quality can differ between them. This means that HDRP works with all features for whichever **Lit Shader Mode** you select. For example, Screen Space Reflection, Screen Space Ambient Occlusion, Decals, and Contact Shadows work with a Deferred or Forward **Lit Shader Mode**. Although feature parity is core to HDRP, the quality and accuracy of these effects may vary between **Lit Shader Modes** due to technical constraints.

## Differences between Forward and Deferred rendering in HDRP

| **Feature** | **Forward Rendering** | **Deferred** |
|---|---|---|
| **Normal shadow bias** | HDRP uses the geometric normal (the vertex normal) of the Material for shadow bias, so Forward Rendering produces fewer shadow artifacts. | HDRP uses the pixel normal of the Material for shadow bias, so Deferred Rendering produces more shadow artifacts. |
| **Emissive Color** | Ambient Occlusion doesn't affect Emissive Color. | Ambient Occlusion affects Emissive Color due to technical constraints. |
| **Ambient Occlusion** | HDRP applies the minimum amount of Ambient Occlusion and Screen Space Ambient Occlusion to Lightmaps, Lightprobes, and Screen Space Global Illumination and Ray-traced global Illumination. This results in correct darkening. | HDRP applies Ambient Occlusion on Lightmaps, Light Probes, and the Screen Space Ambient Occlusion effect. This results in incorrect darkening. |
| **Material Quality** | There is no compression, so there are no compression artifacts. | HDRP compresses Material properties, such as normals or tangents, in the GBuffer. This results in compression artifacts. |
| **Depth Prepass** | HDRP always renders a depth prepass, which outputs a depth and a normal buffer. | A depth prepass is optional in Deferred mode if you aren't using Decals. |
| **Normal Buffer** | HDRP updates normal buffers after the decal DBuffer pass. HDRP uses the normal buffer for Screen Space Reflection and other effects. | N/A |

<a name="BuildTime"></a>

## Build time

The build time for an HDRP Project may be faster when using either Forward or Deferred rendering. The downside of choosing a **Lit Shader Mode** of **Both** is that it increases the build time for the Player substantially because Unity builds two sets of Shaders for each Material, one for Forward and one for Deferred. If you use a specific rendering mode for everything in your Project, you should use that rendering mode instead of **Both**, to reduce build time. This also reduces the memory size that HDRP allocates for Shaders.
