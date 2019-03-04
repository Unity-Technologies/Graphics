# Static Lighting Sky

The Static Lighting Sky component allows you to select the type of sky that the High Definition Render Pipeline (HDRP) uses in the light baking process.

When you create a **Scene Settings** GameObject (Menu: **GameObject > Rendering > Scene Settings**), HDRP attaches this component to the new GameObject by default . 

To manually add a Static Lighting Sky component to any GameObject; 

1. Click on the GameObject in the Scene or Hierarchy to view it in the Inspector. 
2. Click **Add Component** and type **Static Lighting Sky** into the search bar. 
3. Select **Static Lighting Sky** in the drop-down to add a Static Lighting Sky component to the GameObject.

To select the [sky type](Sky-Overview.html), you must specify a [Volume Profile](Volume-Profile.html) in the **Profile** field. The Volume Profile must contain at least one sky type Volume override, for example, **Exponential Sky**. When you specify a Volume Profile, you can use the **Static Lighting Sky** drop-down to select the type of sky to use for the light baking process. The drop-down only exposes sky types that the **Profile** includes as Volume overrides.

## Properties

![](Images/StaticLightingSky1.png)

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Profile**             | Assign a [Volume Profile](Volume-Profile.html) that includes sky [Volume overrides](Volume-Components.html). |
| **Static Lighting Sky** | Use the drop-down to select which type of sky to use for the light baking process. The drop-down exposes sky types that the **Profile** includes as Volume overrides. For example, if the **Profile** includes the **Procedural Sky** Volume override, you can select **Procedural Sky** from this drop-down. |

## Details

Changes to the Static Lighting Sky component only affect baked lightmaps and Light Probes during the baking process. There should only be one Static Lighting Sky enabled in your Scene. If there is more than one, HDRP uses the Static Lighting Sky component you activated last for the light baking process.