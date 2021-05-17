# The SDF Bake Tool window

The SDF Bake Tool window generates [Signed Distance Field](sdf-in-vfx-graph.md) (SDF) assets from a Mesh or a Prefab that contains Meshes. The SDF Bake Tool window works in the Unity Editor. To bake an SDF at runtime, see [SDF Bake Tool API](sdf-bake-tool-api.md).

To open the SDF Bake Tool window, select **Window** > **Visual Effects** > **Utilities** > **SDF Bake Tool**.

## Working with the SDF Bake Tool window

In the Unity Editor, in the [Visual Effect Graph window](VisualEffectGraphWindow.md), blocks and operators, such as [Collide With Signed Distance Field](Block-CollideWithSignedDistanceField.md), take an SDF as an input.

![](Images/sdf-update-particle-context.png)

*A screenshot of the Update Particle context.*

To create an SDF asset to use in the Unity Editor, you can use the SDF Bake Tool window:

1. Open the SDF Bake Tool window (menu: **Window** > **Visual Effects** > **Utilities** > **SDF Bake Tool**)<br/>![](Images/sdf-bake-tool-window.png)<br/>*The SDF Bake Tool window previewing a Mesh asset and its SDF representation.*
2. Choose an asset to generate an SDF representation for. If you want to generate an SDF to represent a single Mesh asset, set **Model Source** to **Mesh**. If you want to generate an SDF that represents multiple Meshes, set **Model Source** to **Prefab**. This mode generates an SDF that represents the combination of every Mesh in a Prefab's hierarchy.
3. By default, the SDF Bake Tool sets the bounds of the [baking box](sdf-bake-tool.md#baking-box) to be equal to the bounding box of the geometry. To scale the baking box, use **Box Size**. To move the baking box, use **Box Center**.
4. Choose a **Maximal Resolution** for the resulting SDF texture. The **Maximal Resolution** corresponds to the resolution along the longest side of the box.
5. To preview the result with the current properties, select **Bake Mesh**. If **Preview Object** is set to **None** or **Mesh**, the SDF Bake Tool window does not display the resulting SDF texture. To display the texture, set **Preview Object** to either **Mesh And Texture** or **Texture**. If you want the preview to update in real time as you change values, enable **Live Update**. Note that baking an SDF is a resource-intensive operation. Enabling **Live Update** can cause slowdowns or instabilities depending on the capabilities of your computer.
6. If the input Mesh does not explicitly separate its interior from its exterior (for example if it contains holes, self-intersections, open boundaries, or self-containing geometry), the resulting SDF can misclassify some regions. To help mitigate these artifacts, there are Additional Properties](#properties) that you can manually expose.
7. Save the SDF as an asset in your project: select **Save SDF**. The SDF Bake tool window saves the SDF that is currently visible in the preview.

You can now use the resulting SDF asset as an input for several blocks and operators. For a list of blocks and operators that use SDF assets, see [SDFs in the Visual Effect Graph](sdf-in-vfx-graph.md).

To make it easier to iterate over signed distance fields, the SDF Bake Tool window uses an asset to store its property values. This enables you to save and restore the settings you used to create a particular SDF texture asset. The window exposes this asset in the **Settings Asset** property. To save the current property values to the asset, click **Save Settings**. To restore the settings from another asset, use one of the following methods:
* Assign the asset to the **Settings Asset** property.
* With the SDF Bake Tool window open, select the asset in the Project window.
* In the Project window, double-click the asset. If the SDF Bake Tool window isn't open, this opens the window and assigns the asset.

Note: To use the SDF asset with the [Collide With Signed Distance Field](Block-CollideWithSignedDistanceField.md) block. In the block, set the **Size** of the **Field Transform** to match the **Box Size** that you used in the SDF Bake Tool.

## Properties

The SDF Bake Tool window includes default properties, which should suit most use cases, and additional properties that further tweak the baking process. The additional properties are invisible by default. To show them:

1. To the right of the window's header, select the **More** menu (&#8942;).
2. Enable **Show Additional Properties**.

![](Images/sdf-bake-tool-additional-properties.png)

*The SDF Bake Tool window and the context menu that includes the additional properties toggle.*

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Settings Asset**      | Specifies the settings asset that stores the properties in this window. To save the current settings, click **Save Settings**. To restore the settings from another asset, either assign the asset to this property, select the asset while the window is open, or double-click the asset while the window is closed. |
| **Maximal resolution**  | The resolution of the largest side of the resulting 3D texture. |
| **Box Center**          | The center of the [baking box](sdf-bake-tool.md#baking-box). |
| **Desired Box Size**    | The desired per-axis size of the [baking box](sdf-bake-tool.md#baking-box). |
| **Actual Box Size**     | The actual per-axis size of the [baking box](sdf-bake-tool.md#baking-box). This might slightly differ from the Desired Box Size, to ensure that the voxels in the texture are cubic.|
| **Live Update**         | Indicates whether to update the preview in real time. If you enable this property, the SDF Bake Tool rebakes the Mesh every time a property in this window changes. This can be a resource-intensive operation if you set the **Maximal Resolution** or **Sign Passes Count** to a high value. |
| **Baking Parameters**   | When the input geometry does not explicitly separate an inside from the outside, for example, because of holes or self-intersection, the baking process can produce unwanted results. The properties in this **Baking Parameters** section of the Inspector can help to mitigate these cases.<br/><br/>The properties in this section only appear if you [show additional properties](#properties). |
| - **Sign Passes Count** | The number of neighboring texels that the SDF Bake Tool uses to calculate whether the current texel is inside or outside the Mesh. Increasing this value reduces artifacts caused by geometry that do not explicitly separate an inside from the outside (for example because of holes or self-intersection).<br/><br/>This property only appears if you [show additional properties](#properties). |
| - **In/Out Threshold**  | The threshold from which the SDF Bake Tool considers voxels to be outside. To separate the insides of the geometry from the outside, each texel in the texture has a score that determines whether or not it is outside. Low values for this property mean the SDF Bake Tool considers more points as inside. High values for this property mean the SDF Bake Tool considers more points to be outside.<br/><br/>This property only appears if you [show additional properties](#properties). |
| - **Surface Offset**    | Scales the surface of the SDF. Positive values enlarge the surface of the SDF and negative values shrink the surface of the SDF.<br/><br/>This property only appears if you [show additional properties](#properties). |
| **Fit Padding**         | Per-axis padding to apply when using the **Fit Box to Mesh** and **Fit Cube to Mesh** buttons.  <br/><br/>This property only appears if you [show additional properties](#properties). |
| **Fit Box to Mesh**     | Sets the center and size of the [baking box](sdf-bake-tool.md#baking-box) so that it fits the bounds of the Mesh. To add padding around the Mesh, use the **Fit Padding** property. |
| **Fit Cube to Mesh**    | Sets the center and size of the [baking box](sdf-bake-tool.md#baking-box) so that it is the smallest possible cube that contains bounds of the Mesh. The cube's side length thus matches the longest side of the Mesh. To add padding around the Mesh, use the **Fit Padding** property. |
| **Model Source**        | Specifies the source of the input Mesh. The options are:<br/>&#8226; **Mesh**: Directly uses a Mesh asset as the input Mesh.<br/>&#8226; **Mesh Prefab**: Uses all the Meshes within a Prefab as the input. This mode creates one SDF that represents the combination of all the Meshes in the Prefab's hierarchy. |
| **Mesh**                | The Mesh to generate an SDF for.<br/><br/>This property only appears if you set **Model Source** to **Mesh**. |
| **Mesh Prefab**         | The Prefab to generate an SDF for. The SDF Bake tool creates an SDF that represents the combination of all the Meshes in this Prefab's hierarchy. The Prefab must contain at least one Mesh Filter or Skinned Mesh Renderer that references a Mesh. For Skinned Mesh Renderers, the SDF Bake Tool uses the Mesh in its rest pose.<br/><br/>This property only appears if you set **Model Source** to **Mesh Prefab**. |
| **Preview Object**      | Specifies how the SDF Bake Tool displays a preview of the SDF. If you want the preview to include the output SDF texture, you need to bake the input Mesh at least once. If you preview the output SDF texture, this is the exact texture the SDF Bake Tool saves when you select **Save SDF**. The options for this property are:<br/>&#8226; **Mesh And Texture**: Displays both the input Mesh and the output SDF texture side-by-side. <br/>&#8226; **Mesh**: Displays a preview of the input Mesh.<br/>&#8226; **Texture**: Displays a preview of the output SDF texture.<br/>&#8226; **None**: Does not show a preview. |
