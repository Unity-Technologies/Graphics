# Forward and Deferred rendering

The High Definition Render Pipeline (HDRP) allows you to render Lit Materials using either Forward or Deferred rendering. You can configure your Unity Project to only use one of these modes, or allow it to use both and switch at runtime on a per-[Camera](HDRP-Camera.md) basis.

## Using Forward or Deferred rendering

Before you use forward or deferred rendering in your Unity Project, you must make sure your [HDRP Asset](HDRP-Asset.md) supports them. To do this:

1. Select your HDRP Asset in the Project window to view it in the Inspector.
2. Under the **Rendering** section, use the **Lit Shader Mode** drop-down to select the rendering mode that HDRP supports.

![](Images/ForwardAndDeferred1.png)

You can choose between three rendering modes:

| **Lit Shader Mode** | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Forward**         | HDRP calculates the lighting in a single pass when rendering each individual GameObject. |
| **Deferred**        | HDRP renders the Material properties of every GameObject visible on screen into a GBuffer. HDRP then processes the lighting for every pixel in the frame. |
| **Both**            | Use the [Frame Settings](Frame-Settings.md) to change between **Forward** and **Deferred** rendering mode on a per Camera basis. Selecting this increases Project [build time](#BuildTime). |

If you select **Both**, you can set a rendering mode for all Cameras to use by default, and also override this default rendering mode at runtime for a specific Camera. For example, you can use Forward mode for a Planar Reflection Probe and then render your main Camera using Deferred mode.

To set the default rendering mode:

1. Select your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Default Frame Settings For Camera > Rendering** and select the rendering mode you want to be the default from the **Lit Shader Mode** drop-down.

To edit the rendering mode for a specific Camera:

1. Select the Camera in the Hierarchy window to view it in the Inspector, and then enable the **Custom Frame Settings** checkbox.
2. In the **Rendering** section, enable the override checkbox to the left of the **Lit Shader Mode** property. This allows you to change the **Lit Shader Mode** for this Camera, and indicates to HDRP that it should use the new value.
3. Select the rendering mode you want this Camera to use from the **Lit Shader Mode** drop-down.

![](Images/ForwardAndDeferred2.png)

## Selecting a Lit Shader Mode

To decide whether to use Forward or Deferred mode, consider the level of quality and performance you want for your Project. Deferred rendering in HDRP is faster in most scenarios, such as a Scene with various Materials and multiple local Lights. Some scenarios, like those with a single Directional Light in a Scene, can be faster in Forward rendering mode. If performance is not so important for your Project, use Forward rendering mode for better quality rendering.

HDRP enforces Forward rendering for the following types of Shaders:

- Fabric
- Hair
- AxF
- StackLit
- Unlit
- Lit Shader with a Transparent Surface Type

If you set the **Lit Shader Mode** to **Deferred** in your HDRP Asset, HDRP uses deferred rendering to render all Materials that use an Opaque Lit Material.

Forward and Deferred rendering both implement the same features, but the quality can differ between them. This means that HDRP works with all features for whichever **Lit Shader Mode** you select. For example, Screen Space Reflection, Screen Space Ambient Occlusion, Decals, and Contact Shadows work with a Deferred or Forward **Lit Shader Mode**. Although feature parity is core to HDRP, the quality and accuracy of these effects may vary between **Lit Shader Modes** due to technical restraints.

## Differences between Forward and Deferred rendering in HDRP

### Visual differences

- Normal shadow bias: In Forward mode, HDRP uses the geometric normal (the vertex normal) of the Material for shadow bias, and Deferred mode uses the pixel normal. This means Forward mode produces less shadow artifacts than Deferred mode.
- Emissive Color: In Deferred mode, Ambient Occlusion affects Emissive Color due to technical constraints. This is not the Case in Forward mode.
- Ambient Occlusion: In Deferred mode, HDRP applies Ambient Occlusion on Lightmaps and Light Probes as well as the Screen Space Ambient Occlusion effect. This results in incorrect darkening. In Forward mode, HDRP applies the minimum amount of Ambient Occlusion and Screen Space Ambient Occlusion to Lightmaps and Lightprobes as well as Screen Space Global Illumination and Ray-trace global Illumination. This results in correct darkening.
- Material Quality: In Deferred mode, HDRP compresses Material properties, such as normals or tangents, in the GBuffer. This results in compression artifacts. In Forward mode, there is no compression, so there are no compression artifacts.

### Technical differences

- For Materials that use Forward mode, HDRP always renders a depth prepass, which outputs a depth and a normal buffer. This is optional in Deferred Mode, if you are not using Decals.
- In Forward mode, HDRP updates normal buffers after the decal DBuffer pass. HDRP uses the normal buffer for Screen Space Reflection and other effects.

<a name="BuildTime"></a>

## Build time

The build time for an HDRP Project may be faster when using either Forward or Deferred rendering. The downside of choosing a **Lit Shader Mode** of **Both** is that it increases the build time for the Player substantially because Unity builds two sets of Shaders for each Material, one for Forward and one for Deferred. If you use a specific rendering mode for everything in your Project, you should use that rendering mode instead of **Both**, to reduce build time. This also reduces the memory size that HDRP allocates for Shaders.
