# Use Probe Volumes

This page provides the basic workflow you need to use Probe Volumes in your project.

## Add and bake a Probe Volume

### Enable Probe Volumes

1. Open the **Edit** menu and select **Project Settings** > **Quality** > **HDRP**.
2. Expand **Lighting** > **Light Probe Lighting**.
3. Set **Light Probe System** to **Probe Volumes**.
4. Select **Graphics** > **HDRP Global Settings**.
5. Go to **Frame Settings**.
6. Expand **Camera** > **Lighting** and enable **Probe Volumes**.

To make sure Reflection Probes also capture lighting data from Probe Volumes, you should also do the following:

1. Expand **Realtime Reflection** > **Lighting** and enable **Probe Volumes**.
2. Expand **Baked or Custom Reflection** > **Lighting** and enable **Probe Volumes**.

### Add a Probe Volume to the Scene

1. Open the **GameObject** menu and select **Light** > **Probe Volumes** > **Probe Volume**.
2. In the Inspector for the Probe Volume, set **Mode** to **Global** to make this Probe Volume cover your entire Scene.

### Adjust your Light and Mesh Renderer settings

1. To include a Light in a Probe Volume's baked lighting data, open the Inspector for the Light then set the **Light Mode** to **Mixed** or **Baked**.
2. To include an object in a Probe Volume's baked lighting data, open the Inspector for the object and enable **Contribute Global Illumination**.
3. To make an object receive baked lighting, open the Inspector for the object and set **Receive Global Illumination** to **Light Probes**. 

### Bake your lighting

1. Open the **Window** menu and select **Rendering** > **Lighting**.
2. Select the **Probe Volumes** tab.
3. Set **Baking Mode** to **Single Scene**.
4. Select **Generate Lighting**.

If no scene in the Baking Set contains a Probe Volume, Unity asks if you want to create a Probe Volume automatically.

You can change baking settings in the Lighting window's [Lightmapping Settings](https://docs.unity3d.com/Documentation/Manual/class-LightingSettings.html#LightmappingSettings).

## Configure a Probe Volume

You can use the following to configure a Probe Volume:

- Use [Baking Set properties](probevolumes-settings.md#pv-tab) to change the probe spacing and behaviour in all the Probe Volumes in a Baking Set.
- Use the settings in the [Probe Volume Inspector](probevolumes-settings.md#probe-volume-properties) to change the Probe Volume size and probe density.
- Add a [Probe Adjustment Volume](probevolumes-settings.md#probe-adjustment-volume) to the scene, to make probes invalid in a small area or fix other lighting issues.
- Add a [Volume](Volumes.md) to your Scene with a [Probe Volume Options](probevolumes-settings.md#probe-volumes-options-override) override, to change the way HDRP samples Probe Volume data when the Camera is inside the Volume. This doesn't affect baking.

For more information, see the following:

- [Adjust Probe Volume size](probevolumes-showandadjust.md#adjust-probe-volume-size).
- [Fix issues with Probe Volumes](probevolumes-fixissues.md).

## Use Baking Sets

To configure [Baking Set](probevolumes-concept.md#baking-sets) properties, open **Window** > **Rendering** > **Lighting** > **Probe Volumes**. See [Baking Set properties](probevolumes-settings.md#pv-tab) for more information.

If you [load multiple scenes simultaneously](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html) in your project, for example if you load multiples scenes at the same time in an open world game, you must do the following to place the scenes in a single Baking Set and bake them together:

1. Set **Baking Mode** to **Baking Sets (Advanced)**.
2. Select an existing Baking Set asset, or select **New** to create a new Baking Set.
3. Select the scenes.
4. Under **Scenes in Baking Set**, select **+** to add the scenes. 

To remove a Scene from a Baking Set, select the Scene in the **Scenes in Baking Set** list, then select **-**.

When you select **Generate Lighting**, HDRP bakes the lighting for all the scenes in the Baking Set.

For faster iteration times, disable **Bake** next to a scene to stop Unity baking the scene. This results in incomplete data, but can help reduce baking time when you're iterating on a part of the world.

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

Probe Volumes split the baked data into multiple parts:

- The shared data, which contains mainly the scene subdivision information and probe placement.
- The per scenario data, which contains the probe lighting information.

As a result, HDRP doesn't need to duplicate baked data on disk when you use multiple **Lighting Scenarios**, but this requires that all Lighting Scenarios use the same probe placement, and therefore that the geometry doesn't change between bakes of all Lighting Scenarios.

### Keep probe positions the same in different Lighting Scenarios

If you need to make changes to the static geometry for your Lighting Scenarios, for example one Lighting Scenario where a door is open and one where the door is closed, you can do the following to stop HDRP recomputing probe positions when baking.

1. Bake one Lighting Scenario.
2. Switch to another Lighting Scenario.
3. Change your scene lighting or geometry.
4. Set **Probe Positions** to **Don't Recalculate**.
5. Select **Generate Lighting** to recompute only the indirect lighting, and skip the probe placement computations.

If you switch between Lighting Scenarios at runtime, HDRP changes only the Probe Volume's baked indirect lighting. You might still need to use scripts to move geometry or change direct lighting.

You can use the use the [Rendering Debugger](Render-Pipeline-Debug-Window.md#ProbeVolume) to preview transitions between Lighting Scenarios.

## Additional resources

- [Display and adjust Probe Volumes](probevolumes-showandadjust.md)
- [Fix issues with Probe Volumes](probevolumes-fixissues.md)
- [Work with multiple Scenes in Unity](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html)
- [Probe Volumes settings and properties](probevolumes-settings.md)
