# Set up ray tracing

The High Definition Render Pipeline (HDRP) includes preview ray tracing support from Unity 2019.3. Ray tracing allows you to access data that's not on screen. For example, you can use it to request position data, normal data, or lighting data, and then use this data to compute quantities that are hard to approximate using classic rasterization techniques.

For information about the hardware ray tracing requires, refer to [Ray tracing hardware requirements](raytracing-requirements.md)

## Integrate ray tracing into your HDRP Project

Before you use ray tracing features in your HDRP Project, you need to set up your HDRP Project for ray tracing support. HDRP only supports ray tracing using the DirectX 12 API, so ray tracing only works in the Unity Editor or the Windows Unity Player when they render with DirectX 12. You need to change the default graphics API of your HDRP project from DirectX 11 to DirectX 12.

There are two ways to do this:

* [Use the Render Pipeline Wizard](#WizardSetup)

* [Manual setup](#ManualSetup)

Once you have completed one of these, move onto [Final setup](#final-setup).

<a name="WizardSetup"></a>

### Render Pipeline Wizard setup

You can use the [Render Pipeline Wizard](Render-Pipeline-Wizard.md) to set up ray tracing in your HDRP Project.

1. To open the HDRP Wizard, go to **Window** > **Rendering** > **HDRP Wizard**.

2. Select the **HDRP + DXR** tab.

3. Click the **Fix All** button.

To enable ray tracing for specific effects, enable the ray tracing features in the [HDRP Asset](#ManualSetup-EnableAssetFeatures).

For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

<a name="ManualSetup"></a>

### Manual setup

To set up ray tracing manually, you need to:

1. [Make your HDRP project use DirectX 12](#ManualSetup-EnablingDX12).
2. [Disable static batching on your HDRP project](#ManualSetup-DisablingStaticBatching).
3. [Enable and configure ray tracing in your HDRP Asset](#ManualSetup-EnablingRayTracing).
4. [Ensure ray tracing resources are properly assigned](#ManualSetup-RayTracingResources).
5. (Optional) [Enable ray-traced effects in your HDRP Asset](#ManualSetup-EnableAssetFeatures).

<a name="ManualSetup-EnablingDX12"></a>

#### Upgrade to DirectX 12

In Unity 6, DirectX 12 is enabled by default.

To enable DirectX 12 manually:

1. Go to **Edit** > **Project Settings** > **Player** > **Other Settings**.

1. In the **Rendering** section, disable **Auto Graphics API for Windows**.

	This exposes the Graphics APIs for Windows section.

1. Make sure **Direct3D12** is at the top of the list.

	If you made any changes to the rendering settings, you might be prompted to restart the Unity Editor.

<a name="ManualSetup-DisablingStaticBatching"></a>

#### Disable static batching

Next, you need to disable static batching, because HDRP doesn't support this feature with ray tracing in **Play Mode**. To do this:

1. Open the Project Settings window (menu:  **Edit** > **Project Settings**), then select the **Player** tab.
2. Select the **Other Settings** drop-down, then in the **Rendering** section, disable **Static Batching**.

<a name="ManualSetup-EnablingRayTracing"></a>

#### Enable ray tracing in the HDRP Asset 

Now that Unity is running in DirectX 12, and you have disabled [static batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), enable and configure ray tracing in your [HDRP Asset](HDRP-Asset.md). The previous steps configured Unity to support ray tracing; the following step enables it in your HDRP Unity Project.

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the **Rendering** section, enable **Realtime Ray Tracing**. This triggers a recompilation, which makes ray tracing available in your HDRP Project.

<a name="ManualSetup-RayTracingResources"></a>

#### Verify ray tracing resources

To verify that HDRP has assigned ray tracing resources:

1. Open the Project Settings window (menu: **Edit** > **Project Settings**), then select the **HDRP Default Settings** tab.
2. Find the **Render Pipeline Resources** field and make sure there is a Render Pipeline Resources Asset assigned to it.

<a name="ManualSetup-EnableAssetFeatures"></a>

#### Enable ray-traced effects in the HDRP Asset (Optional)

HDRP uses ray tracing to replace certain rasterized effects. To use a ray tracing effect in your Project, you must first enable the rasterized version of the effect. The four effects that require you to modify your HDRP Asset  are:

* **Screen Space Shadows**
* **Screen Space Reflections**
*  **Transparent Screen Space Reflections**
* **Screen Space Global Illumination**

To enable the above effects in your HDRP Unity Project:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. Go to **Lighting** > **Reflections** and enable **Screen Space Reflection**.
3. After enabling **Screen Space Reflections**, go to **Lighting** > **Reflections** and enable **Transparent Screen Space Reflection**.
4. Go to **Lighting** > **Shadows** and enable **Screen Space Shadows**.
5. Go to **Lighting** > **Lighting** and enable **Screen Space Global Illumination**.

Your HDRP Project now fully supports ray tracing. For information on how to set up ray tracing for your Scene, see [final setup](#final-setup).

### Final setup

Now that your HDRP Project supports ray tracing, there are steps you must complete to use it in your Scene.

1. [Frame Settings validation](#frame-settings)
2. [Build settings validation](#build-settings)
3. [Scene validation](#scene-validation)


#### Frame Settings

To make HDRP calculate ray tracing effects for [Cameras](hdrp-camera-component-reference.md) in your Scene, make sure your Cameras use [Frame Settings](Frame-Settings.md) that have ray tracing enabled. You can enable ray tracing for all Cameras by default, or you can enable ray tracing for specific Cameras in your Scene.

To enable ray tracing by default:

1. From the main menu, select **Edit** &gt; **Project Settings**.
2. In the **Project Settings** window, go to the **Pipeline Specific Settings** section, then select the **HDRP** tab.
3. Under **Frame Settings (Default Values)** &gt; **Camera** &gt; **Rendering**, enable **Ray Tracing**.

To enable ray tracing for a specific camera:

1. Select the camera in the scene or **Hierarchy** window to view it in the **Inspector** window.
2. In the **Rendering** section, enable **Custom Frame Settings**. This exposes frame settings for this camera only.
3. Use the foldout (triangle) to expand **Rendering**, then enable **Ray Tracing**.

#### Build settings

To build your Project to a Unity Player, ray tracing requires that the build uses 64 bits architecture. To set your build to use 64 bits architecture:

1. Open the Build Settings window (menu: **File > Build Settings**).
2. From the **Architecture** drop-down, select **x86_64**.

#### Scene validation

To check whether it's possible to use ray tracing in a Scene, HDRP includes a menu option that validates each GameObject in the Scene. If you don't setup GameObjects correctly, this process throws warnings in the Console window. For the list of things this option checks for, see [Menu items](Menu-Items.md#other). To use it:
1. Click **Edit** > **Render Pipeline** > **HD Render Pipeline**  > **Check Scene Content for Ray Tracing**.
2. In the Console window (menu: **Window > General > Console**), check if there are any warnings.

<a name="RayTracingMeshes"></a>

## Ray tracing and Meshes

HDRP changes how it handles Meshes in your scene when you integrate a ray traced effect into your project.

When you enable ray tracing, HDRP automatically creates a ray tracing acceleration structure. This structure allows Unity to calculate ray tracing for Meshes in your scene efficiently in real time.

As a result, ray tracing can change how some Meshes appear in your scene in the following ways:

- If your Mesh has a Material assigned that doesn't have the HDRenderPipeline tag, HDRP doesn't add it to the acceleration structure and doesn't apply any ray traced effects to the mesh as a result.
- If a Mesh has a combination of Materials that are single and double-sided, HDRP flags all Materials you have assigned to this mesh as double-sided.

To include a GameObject in ray tracing effects, adjust the Ray Tracing settings in the GameObject's [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html#ray-tracing).

## Ray tracing light culling
Ray tracing requires HDRP to cull lights differently to how it culls lights for rasterization. With rasterization, only lights that affect the current frustum matter. Since ray tracing uses off-screen data for effects such as reflection, HDRP needs to consider lights that affect off screen geometry. For this reason, HDRP defines a range around the camera where it gathers light. To control this range, use the [Light Cluster](Ray-Tracing-Light-Cluster.md) Volume override. It's important to set a range that accurately represents the environment scale. A higher range makes HDRP include lights further away, but it also increases the resource intensity of light culling for ray tracing.

## Ray tracing mode
HDRP includes two ray tracing modes that define how it evaluates certain ray-traced effects. The modes are:

* **Performance**: This mode targets real-time applications. If you select this mode, ray-traced effects include presets that you can change to balance performance with quality.
* **Quality**: This mode targets technical demos and applications that want the best quality results.

HDRP exposes different properties for some ray-traced effects based on the ray tracing mode you use..

You can change which ray tracing mode HDRP uses on either a Project level or effect level. To change the ray tracing mode for your entire Project:

1. Click on your [HDRP Asset](HDRP-Asset.md) in the Project window to view it in the Inspector.
2. In the **Rendering** section, enable the **Realtime Raytracing** checkbox, open the **Supported Ray Tracing Mode** drop-down and select a ray tracing mode from open.

If you select the **Both** option, you can change the ray tracing mode for each ray-traced effect. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component that includes a ray-traced effect.
2. In the Inspector for the ray-traced effect, change the **Mode** property to use the ray tracing mode you want the effect to use. This changes the properties available in the Inspector.


## Ray tracing project

You can find a ray tracing project that contains all the effects mentioned above in the [Small Office Ray Tracing sample project](https://github.com/Unity-Technologies/SmallOfficeRayTracing).
This Project is already set up with ray tracing support.

## Limitations
### Platform support

HDRP supports ray tracing for DirectX 12 and specific console platforms. Consult console-specific documentation for more information.

### Feature compatibility

HDRP ray tracing in Unity isn't compatible with the following features:

- Vertex animation, for example wind deformation of vegetation.
- Emissive [Decals](decals.md). To disable emission, go to the [Decal Material Inspector window](decal-material-inspector-reference.md) and disable **Affect Emissive**.
- Ray tracing is not compatible with the detail meshes and trees in the [Terrain system](https://docs.unity3d.com/Manual/script-Terrain.html). It is compatible with terrain geometry. To include detailed meshes and trees in ray traced reflections, use [mixed tracing](Override-Screen-Space-Reflection.md#mixed-tracing).
- Volumetric [fog](create-a-local-fog-effect.md).
- [Tessellation](Tessellation.md).
- Per-pixel displacement techniques such as parallax occlusion mapping, depth offset, and non-terrain height maps.
- MSAA.
- [Graphics.DrawMesh](https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html) or [Graphics.RenderMesh](https://docs.unity3d.com/ScriptReference/Graphics.RenderMesh.html), because rasterization and ray tracing are different ways of generating an image.
- [Orthographic projection](hdrp-camera-component-reference.md). If you enable orthographic projection mode, you might experience rendering problems with transparent materials, volumetrics, and planar reflections.
- Ray Traced and Screen Space effects. These don't appear recursively in [Ray Traced Reflections](Ray-Traced-Reflections.md), [Ray Traced Global Illumination](Ray-Traced-Global-Illumination.md) or [Recursive Ray Tracing](Ray-Tracing-Recursive-Rendering.md). This means, for example, you can't see [Screen Space Global Illumination](Override-Screen-Space-GI.md) in [ray-traced reflections](Ray-Traced-Reflections.md).
- Fully accurate shadow culling. You might see missing shadows in ray-traced effects. You can use **Extend Shadow Culling** to improve accuracy. See [Ray Tracing Settings](Ray-Tracing-Settings.md) for more information.
- Data-Oriented Technology Stack (DOTS)
- Entity Component System (ECS)
- Water

#### Reflection Probes

Although ray-traced rendering results include data from [Reflection Probes](Reflection-Probe.md), Reflection Probes do not capture geometry that HDRP renders with ray tracing.

### Unsupported shader graph nodes for ray tracing

When building your custom shaders using shader graph, some nodes are incompatible with ray tracing. You need either to avoid using them or provide an alternative behavior using the [ray tracing shader node](SGNode-Raytracing-Quality.md). Here is the list of the incompatible nodes:
- DDX, DDY and DDXY nodes, and NormalFromHeight nodes.
- All the nodes under **Inputs** > **Geometry** (Position, View Direction, Normal, etc.) in View Space mode.
Furthermore, Shader Graphs that use [Custom Interpolators](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Custom-Interpolators.html) aren't supported in ray tracing.

### Unsupported features of path tracing

For information about unsupported features of path tracing, see [Path tracing limitations](path-tracing-limitations.md).
