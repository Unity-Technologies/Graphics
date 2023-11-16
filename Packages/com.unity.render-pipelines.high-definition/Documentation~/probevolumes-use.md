# Use Probe Volumes

This page provides the basic workflow you need to use Probe Volumes in your project.

This feature is experimental, so it's not ready for production use.

## Add and bake a Probe Volume

### Enable Probe Volumes

1. Open the **Edit** menu and select **Project Settings** > **Quality** > **HDRP**.
2. Expand **Lighting** > **Light Probe Lighting**.
3. For **Light Probe System**, select **Probe Volumes**.
4. Select **Graphics** > **HDRP Global Settings**.
5. Go to **Frame Settings**.
6. Expand **Camera** > **Lighting** and enable **Probe Volumes**.

To make sure Reflection Probes also capture lighting data from Probe Volumes, you should also do the following:

1. Expand **Realtime Reflection** > **Lighting** and enable **Probe Volumes**.
2. Expand **Baked or Custom Reflection** > **Lighting** and enable **Probe Volumes**.

### Add a Probe Volume to the Scene

1. Open the **GameObject** menu and select **Light** > **Probe Volumes (Experimental)** > **Probe Volume**.
2. In the Inspector for the Probe Volume, enable **Global** to make this Probe Volume cover your entire Scene.

### Adjust your Light and Mesh Renderer settings

1. To include a Light in a Probe Volume's baked lighting data, open the Inspector for the Light then set the **Light Mode** to **Mixed** or **Baked**.
2. To include an object in a Probe Volume's baked lighting data, open the Inspector for the object and enable **Contribute Global Illumination**.
3. To make an object receive baked lighting, open the Inspector for the object and set **Receive Global Illumination** to **Light Probes**. 

### Bake your lighting

1. Open the **Window** menu and select **Rendering** > **Probe Volume Settings**.
2. Select the Baking Set that contains your Scene. By default this is **New Baking Set**.
3. Select the Lighting Scenario that stores the baking result, for example the default **New Lighting Scenario**.
4. Select the arrow on the **Generate Lighting** button and select **Bake active Scene**.

You can change baking settings in the Lighting window's [Lightmapping Settings](https://docs.unity3d.com/Documentation/Manual/class-LightingSettings.html#LightmappingSettings).

To bake multiple Scenes in a Baking Set, select the arrow on the **Generate Lighting** button and select **Bake loaded scenes** or **Bake active scenes**.

## Configure a Probe Volume

You can use the following to configure a Probe Volume:

- Use the settings in the [Probe Volume Inspector](probevolumes-settings.md#probe-volume-properties) to change the Probe Volume size and Light Probe density.
- Use the settings in [Probe Volume Settings](probevolumes-settings.md#probe-volume-settings) to change the Probe Volume size, Light Probe density, and Light Probe behaviour in all the Probe Volumes in a Baking Set.
- Add a [Probe Volume Touchup Component](probevolumes-settings.md#probe-volume-touchup-component) to a Probe Volume, to make Light Probes invalid in a small area and fix lighting issues.
- Add a [Volume](Volumes.md) to your Scene with a [Probe Volume Options](probevolumes-settings.md#probe-volumes-options-override) override, to change the way HDRP samples Probe Volume data when the Camera is inside the Volume. This doesn't affect baking.

For more information, see the following:

- [Adjust Probe Volume size](probevolumes-showandadjust.md#adjust-probe-volume-size).
- [Fix issues with Probe Volumes](probevolumes-fixissues.md).

## Configure Baking Sets

To access [Baking Sets](probevolumes-concept.md#baking-sets) properties, open **Window** > **Rendering** > **Probe Volume Settings**. 

See [Settings and properties related to Probe Volumes](probevolumes-settings.md#probe-volume-settings).

### Add and remove Scenes

When you add a Probe Volume to a Scene, Unity automatically adds the Scene to the default Baking Set.

To add or remove a Scene, select a Baking Set, then use **+** and **-** to add or remove.

You can add more than one Scene to a Baking Set, for example if you [load multiples Scenes at the same time](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html) in an open world game. Each Scene can be in only one Baking Set.

### Move a Scene between Baking Sets

1. Select the Baking Set you want to move a Scene to.
2. In the **Scenes** section, use the **+** to select the Scene you want to move.
3. In the popup message, select **Yes** to move the Scene.

### Load a Scene

Unity doesn't automatically load the Scenes in a Baking Set when you select the Scene in the **Scenes** list. To load a Scene, select **Load All Scenes In Set**.

When you load multiple Scenes together, the lighting might be too bright because HDRP combines light from all the Scenes. See [Set up multiple Scenes](https://docs.unity3d.com/Manual/setupmultiplescenes.html) for more information on loading and unloading Scenes.

You can load multiple Scenes together only if they belong to the same Baking Set.

<a name="scenarios"></a>
### Add a Lighting Scenario

You can use multiple Lighting Scenarios to store baking results for different Scene setups, and switch between them at runtime. For example, you can use one Lighting Scenario for when a lamp is off, and one for when it's on.

To create a new Lighting Scenario and store baking results inside, do the following:

1. Select a Baking Set.
2. In the **Lighting Scenarios** section, select **+** to add a Lighting Scenario. The Lighting Scenario displays **Active Scenario**.
3. Select **Generate Lighting**. HDRP stores the baking results in the Lighting Scenario.

To store baking results in a different Lighting Scenario, select the Lighting Scenario so it displays **Active Scenario**.

If you switch between Lighting Scenarios at runtime, HDRP changes only the Probe Volume's baked indirect lighting. You might still need to use scripts to move geometry or change direct lighting. You can use the following to debug Lighting Scenarios:

- Use the [Rendering Debugger](Render-Pipeline-Debug-Window.md#ProbeVolume) to preview transitions between Lighting Scenarios.
- In the [Probe Volume Settings](probevolumes-settings.md#probe-volume-settings), enable **Freeze Placement** to use existing baked data when you bake, for example if you've moved geometry since the last bake but you want to keep the same Light Probe positions.

## Additional resources

- [Display and adjust Probe Volumes](probevolumes-showandadjust.md)
- [Fix issues with Probe Volumes](probevolumes-fixissues.md)
- [Work with multiple Scenes in Unity](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html)
- [Probe Volumes settings and properties](probevolumes-settings.md)
