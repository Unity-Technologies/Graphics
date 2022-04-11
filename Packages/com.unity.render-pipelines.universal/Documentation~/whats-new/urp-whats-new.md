# What's new in URP 12 (Unity 2021.2)

This section contains information about new features, improvements, and issues fixed in URP 12.

For a complete list of changes made in URP 12, refer to the [Changelog](../../changelog/CHANGELOG.html).

## Features

This section contains the overview of the new features in this release.

### Scene Debug View Modes

![Rendering Debugger](../Images/whats-new/urp-12/rendering-debugger.png)

Improvements in this release bring URP's **Scene Debug View Modes** closer to parity with the options available in Built-in Render Pipeline. The Render Pipeline Debug Window is also included as a new debugging workflow for URP in this release. Users can use Debug Window to inspect the properties of materials being rendered, how the light interacts with these materials, and how shadows and LOD operations are performed to produce the final frame.

### Reflection probe blending and box projection support

![Reflection probe blending and box projection support](../Images/whats-new/urp-12/reflection-probe-blending-artistic-demo.gif)<br/>*This illustration shows the reflections implemented using reflection probes.*

Reflection probe blending and box projection support have been added to allow for better reflection quality using probes and bringing URP closer to feature parity with the Built-In Render Pipeline.

For more information on reflection probes in URP, see the page [Reflection probes](../lighting/reflection-probes.md).

### URP Deferred Rendering Path

![URP Deferred Rendering Path](../Images/whats-new/urp-12/urp-deferred-rendering-path.png)<br/>*A sample scene that uses the Deferred Rendering Path.*

The URP Deferred Rendering Path uses a rendering technique where light shading is performed in screen space on a separate rendering pass after all the vertex and pixel shaders have been rendered. Deferred shading decouples scene geometry from lighting calculations, so the shading of each light is only computed for the visible pixels that it actually affects. With this approach, Unity can efficiently render a far greater amount of lights in a scene compared to per-object forward rendering.

For more information about this feature, see the page [Deferred Rendering Path](../rendering/deferred-rendering-path.md).

### URP decal system

![Decal Projector in the Scene.](../Images/whats-new/urp-12/urp-decal.png)<br/>*Decal Projector in the Scene.*

The new decal system enables you to project decal materials into the surfaces of a Scene. Decals projected into a scene will wrap around meshes and interact with the Scene’s lighting. Decals are useful for adding extra textural detail to a Scene, especially in order to break up materials’ repetitiveness and detail patterns.

For more information about this feature, see the page [Decal Renderer Feature](../renderer-feature-decal.md).

### Depth prepass (Depth Priming Mode)

This release adds support for depth prepass, a rendering pass in which all visible opaque meshes are rendered to populate the depth buffer (without incurring fragment shading cost). Any subsequent color pass can reuse this depth buffer. A depth prepass eliminates or significantly reduces geometry rendering overdraw.

To enable the depth prepass, set the **Depth Priming Mode** to Auto or Forced (URP Asset > Rendering > Rendering Path, Forward > Depth Priming Mode).

![Depth Priming Mode property.](../Images/whats-new/urp-12/urp-asset-depth-priming-mode.png)<br/>*The Depth Priming Mode property*.

### URP Light Cookies

![Light Cookie sample](../Images/whats-new/urp-12/light-cookie-sample-1.png)

The **URP Light Cookies** feature enables a technique for masking or filtering outgoing light’s intensity to produce patterned illumination. This feature can be used to change the appearance, shape, and intensity of cast light for artistic effects or to simulate complex lighting scenarios with minimal runtime performance impact.

### Render Pipeline Converter

A new converter framework for migrating from the Built-in Render Pipeline to URP makes the migration process more robust and supports converting elements other than Materials.

To open the Render Pipeline Converter window, select **Window** > **Rendering** > **Render Pipeline Converter**

![Render Pipeline Converter](../Images/whats-new/urp-12/render-pipeline-converter-ui.png)

For more information, see the page [Render Pipeline Converter](../features/rp-converter.md).

### Motion Vectors

Motion vector support provides a velocity buffer that captures and stores the per-pixel and screen-space motion of objects from one frame to another.

### URP Volume system update frequency

![Volume update modes](../Images/whats-new/urp-12/volume-update-modes.png)

URP Volume system update frequency lets you to optimize the performance of your Volumes framework according to your content and target platform requirements.

### URP Global Settings

![URP Global Settings](../Images/whats-new/urp-12/urp-global-settings.png)

The URP Global Settings section lets you define project-wide settings for URP. In this release, URP Global Settings contain the names of Light layers.

### Light Layers

Light Layers let you mask certain lights in a Scene to affect particular meshes. The lights assigned to a specific layer only affect meshes assigned to the same layer.

For more information, see the page [Light layers](../lighting/light-layers.md).

### New URP package samples

New URP samples are available in the Package Manager. The samples show use cases of URP features, their configuration, and practical applications in one or more scenes.

> **Note**: in the current URP version, there is a known issue that prevents the rendering effects from working correctly. [Follow this link to read the description of the issue and how to fix it](../known-issues.md#urp-samples-known-issue-1).

### Lens Flare system

![Lens Flare system](../Images/whats-new/urp-12/urp-lens-flare-art-demo.png)<br/>*A sample URP scene using lens flares*.

This version introduces a new Lens Flare system. Lens Flares simulate the effect of lights refracting inside a camera lens. They are used to represent really bright lights, or, more subtly, they can add a bit more atmosphere to your Scene. The new system, similar to the one present in the Built-in Render Pipeline, allows stacking flares with an improved user interface and adds many more options.

### Enlighten Realtime GI

![Enlighten Realtime GI](../Images/whats-new/urp-12/enlighten-realtime-gi.png)<br/>*A sample scene with Enlighten Realtime GI*.

Enlighten Realtime GI lets you enrich your projects with more dynamic lighting effects by, for example, having moving lights that affect global illumination in scenes. We've extended the platform reach of Enlighten Realtime GI to Apple Silicon, Sony PlayStation(R) 5, and Microsoft Xbox Series X|S platforms.

### SpeedTree 8 vegetation

This release adds support for SpeedTree 8 vegetation to URP, including support for animated vegetation using the SpeedTree wind system. URP uses Shader Graphs to support SpeedTree 8, for more information see the page [SpeedTree Sub Graph Assets](https://docs.unity3d.com/Packages/com.unity.shadergraph@12.0/manual/SpeedTree8-SubGraphAssets.html).

### Shader Graph: Override Material properties

In this release, Shader Graph stacks that have URP as a target have the **Allow Material Override** property. The property is available for Lit and Unlit Material types.

When enabled, this property lets you override certain surface properties on Materials. Before this release, those properties were set in a Shader Graph.

![Allow Material Override property.](../Images/whats-new/urp-12/allow-material-override.png)

## Improvements

This section contains the overview of the major improvements in this release.

### SSAO improvements

This release brings multiple SSAO improvements:

* SSAO supports the Deferred Rendering Path.

* Normal maps contribute to the effect.

* SSAO supports Particle Systems and surfaces with Unlit shaders.

* Performance is improved.

### SRP settings workflow improvements

The SRP settings workflow improvements are a series of UI/UX improvements intended to impact workflows and provide consistency between the SRP render pipelines. For this iteration, the focus was mainly on aligning the light and camera components between URP and HDRP. The changes consist of aligning header design, sub-header designs, expanders, settings order, naming, and the indentation of dependent fields. While these are mostly cosmetic changes, they have a high impact.

### More optimal handling of the depth buffer with MSAA enabled

Previously, with MSAA enabled, Unity executed an extra depth prepass to populate the depth buffer.
In this release, with MSAA enabled, Unity doesn't execute the extra depth prepass and reuses the  depth texture from the opaque pass instead (**Note**: this is valid for all but GLES3 platforms).

### SwapBuffer

In this release, Universal Renderer can manage and operate on multiple Camera color buffers in the backend.

You can now use the new [ScriptableRenderPass.Blit](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@12.0/api/UnityEngine.Rendering.Universal.ScriptableRenderPass.html) method in your Scriptable Renderer Feature to apply effects to the color buffer without managing and handling Camera color buffers yourself. You can use the method to write effects that read and write to the Camera color buffer.

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
