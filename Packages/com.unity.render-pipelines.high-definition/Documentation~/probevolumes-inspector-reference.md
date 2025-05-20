# Adaptive Probe Volume Inspector reference

Select an Adaptive Probe Volume and open the Inspector to view its properties.

| **Property** | **Description** |
|---|---|
| **Mode**              | This property has the following options:<ul><li>**Global**: HDRP sizes this Adaptive Probe Volume to include all renderers in the scene or Baking Set that have **Contribute Global Illumination** enabled in their [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html). HDRP recalculates the volume size every time you save or generate lighting.</li><li>**Scene**: HDRP sizes this Adaptive Probe Volume to include all renderers in the same scene as this Adaptive Probe Volume. HDRP recalculates the volume size every time you save or generate lighting.</li><li>**Local**: Set the size of this Adaptive Probe Volume manually.</li></ul> |
| **Size**              | Set the size of this Adaptive Probe Volume. This setting only appears when you set **Mode** to **Local**.                                                 |
| **Subdivision Override** | **Override Probe Spacing**: Override the Probe Spacing set in the **Baking Set** for this Adaptive Probe Volume. This cannot exceed the **Min Probe Spacing** and **Max Probe Spacing** values in the [Adaptive Probe Volumes panel in the Lighting window](probevolumes-lighting-panel-reference.md). |
| **Geometry Settings** | This property has the following options:<ul><li>**Override Renderer Filters**: Enable filtering by Layer which GameObjects HDRP considers when it generates probe positions. Use this to exclude certain GameObjects from contributing to Adaptive Probe Volume lighting.</li><li>**Layer Mask**: Filter by Layer which GameObjects HDRP considers when it generates probe positions.</li><li>**Min Renderer Size**: The smallest [Renderer](https://docs.unity3d.com/ScriptReference/Renderer.html) size HDRP considers when it generates probe positions.</li><li>**Fill Empty Spaces**: Enable HDRP filling the empty space between and around Renderers with bricks. Bricks in empty spaces always use the **Max Probe Spacing** value. </li></ul>  |


## Size gizmo
<a name ="size-gizmo"></a>
To resize the Adaptive Probe Volume, use one of the handles of the box gizmo in the Scene view. You can't resize an Adaptive Probe Volume by changing the Transform component of the GameObject, or using the scale gizmo.

In this screenshot, a red box indicates the box gizmo handles.

![Inside a cube, a red box highlights the box gizmo handles.](Images/ProbeVolume-Size-gizmo.png)<br/>
The resize handles for Adaptive Probe Volumes.
