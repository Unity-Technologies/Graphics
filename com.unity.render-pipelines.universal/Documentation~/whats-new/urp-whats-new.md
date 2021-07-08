# What's new in URP 12 (Unity 2021.2)

This section contains information about new features, improvements, and issues fixed in URP 12.

For a complete list of changes made in URP 12, refer to the [Changelog](../../changelog/CHANGELOG.html).

## Features

This section contains the overview of the new features in this release.

### Scene Debug View Modes

Improvements in this release bring URP's **Scene Debug View Modes** closer to parity with the options available in Built-in Render Pipeline. The Render Pipeline Debug Window is also included as a new debugging workflow for URP in this release. Users can use Debug Window to inspect the properties of materials being rendered, how the light interacts with these materials, and how shadows and LOD operations are performed to produce the final frame.

### Reflection probe blending and box projection support

![Reflection probe blending and box projection support](../Images/whats-new/urp-12/reflection-probe-blending-artistic-demo.gif)<br/>*This illustration shows the reflections implemented using reflection probes.*

Reflection probe blending and box projection support have been added to allow for better reflection quality using probes and bringing URP closer to feature parity with the Built-In Render Pipeline.

### URP Deferred Rendering Path

![URP Deferred Rendering Path](../Images/whats-new/urp-12/urp-deferred-rendering-path-art-demo.gif)<br/>*A sample scene that uses the Deferred Rendering Path.*

The URP Deferred Rendering Path uses a rendering technique where light shading is performed in screen space on a separate rendering pass after all the vertex and pixel shaders have been rendered. Deferred shading decouples scene geometry from lighting calculations, so the shading of each light is only computed for the visible pixels that it actually affects. This approach gives the ability to render a large number of lights in a scene without incurring a significant performance hit that affects forward rendering techniques.

For more information about this feature, see the page [Deferred Rendering Path](../rendering/deferred-rendering-path.md).

### URP decal system

![URP decal demo.](../Images/whats-new/urp-12/urp-decals-art-demo.gif)<br/>*A sample URP project scene using decals*.

The new decal system enables you to project decal materials into the surfaces of a Scene. Decals projected into a scene will wrap around meshes and interact with the Scene’s lighting. Decals are useful for adding extra textural detail to a Scene, especially in order to break up materials’ repetitiveness and detail patterns.

This release adds support for depth prepass, a rendering pass in which all visible opaque meshes are rendered to populate the depth buffer (without incurring fragment shading cost), which can be reused by subsequent passes. A depth prepass eliminates or significantly reduces geometry rendering overdraw. In other words, any subsequent color pass can reuse this depth buffer to produce one fragment shader invocation per pixel.

For more information about this feature, see the page [Decal Renderer Feature](../renderer-feature-decal.md).

### Light Layers

Light Layers are specific rendering layers to allow the masking of certain lights in a scene to affect certain particular meshes. In other words, much like Layer Masks, the lights assigned to a specific layer will only affect meshes assigned to the same layer.

### URP Light Cookies

The **URP Light Cookies** feature enables a technique for masking or filtering outgoing light’s intensity to produce patterned illumination. This feature can be used to change the appearance, shape, and intensity of cast light for artistic effects or to simulate complex lighting scenarios with minimal runtime performance impact.

### Converter framework: Built-in Render Pipeline to URP

A new converter framework for migrating from the Built-in Render Pipeline to URP makes the migration process more robust and supports converting elements other than Materials.

### Motion Vectors

Motion vector support provides a velocity buffer that captures and stores the per-pixel and screen-space motion of objects from one frame to another.

### URP Volume system update frequency

URP Volume system update frequency lets you to optimize the performance of your Volumes framework according to your content and target platform requirements.

### New URP package samples

New URP samples are available in the Package Manager. The samples show use cases of URP features, their configuration, and practical applications in one or more scenes.

### Lens Flare system

![Lens Flare system](../Images/whats-new/urp-12/urp-lens-flare-art-demo.png)<br/>*A sample URP scene using lens flares*.

This version introduces a new Lens Flare system. Lens Flares simulate the effect of lights refracting inside a camera lens. They are used to represent really bright lights, or, more subtly, they can add a bit more atmosphere to your Scene. The new system, similar to the one present in the Built-in Render Pipeline, allows stacking flares with an improved user interface and adds many more options.

### Enlighten Realtime GI

![Enlighten Realtime GI](../Images/whats-new/urp-12/enlighten-realtime-gi.gif)<br/>*A sample scene with Enlighten Realtime GI*.

Enlighten Realtime GI lets you to enrich your projects with more dynamic lighting effects by, for example, having moving lights that affect global illumination in scenes. Additionally, we’ve extended the platform reach of Enlighten Realtime GI to Apple Silicon, Sony PlayStation(R) 5, and Microsoft Xbox Series X|S platforms.

### SpeedTree 8 vegetation

SpeedTree 8 vegetation has been added to HDRP and URP, including support for animated vegetation using the SpeedTree wind system, created with Shader Graph.

## Improvements

This section contains the overview of the major improvements in this release.

### SSAO improvements

This release brings several SSAO improvements, including enhanced mobile platform performance and support for deferred rendering, normal maps in depth/normal buffer, unlit surfaces, and particles.

### SRP settings workflow improvements

The SRP settings workflow improvements are a series of UI/UX improvements intended to impact workflows and provide consistency between the SRP render pipelines. For this iteration, the focus was mainly on aligning the light and camera components between URP and HDRP. The changes consist of aligning header design, sub-header designs, expanders, settings order, naming, and the indentation of dependent fields. While these are mostly cosmetic changes, they have a high impact.

### URP 2D Renderer improvements

This release contains multiple URP 2D Renderer improvements:

* New SceneView Debug Modes in URP let 2D developers access the following views: Mask, Alpha channel, Overdraw or Mipmaps. The Sprite Mask feature has been adjusted to work correctly in SRP.

* The 2D Renderer can now be customized with Renderer Features which let you add custom passes.

* 2D Lights are now integrated in the Light Explorer window, and they are no longer labeled as Experimental.

* 2D shadow optimizations.

* 2D Light textures produced by the 2D Lights are now accessible via the 2D Light Texture node in Shader Graph.

* VFX Graph now supports 2D Unlit shaders.

* A new 2D URP default template has been added. It includes a set of verified 2D tools, so new projects load faster with the entire 2D toolset at your disposal.

* Sprite Atlas v2 with folder support.

* New APIs to find duplicated sprites in several atlases for a single sprite, query for MasterAtlas and IsInBuild.

* 2D Pixel Perfect's Inspector UI has a more intuitive setting display.

* 2D PSD Importer has new UX improvements, better control over the Photoshop layers, and Sprite name mapping.

* 2D Animation updates include bone colors, which can now be set in the visibility panel.

* 2D tilemap improvements.

## Issues resolved

For a complete list of issues resolved in URP 12, see the [Changelog](../../changelog/CHANGELOG.html).

## Known issues

For information on the known issues in URP 12, see the section [Known issues](../known-issues.md).
