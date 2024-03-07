# Understanding Probe Volumes

A Probe Volume is a group of [Light Probes](https://docs.unity3d.com/Manual/LightProbes.html) that Unity places automatically based on the geometry density in your Scene, to create baked indirect lighting. You can use Probe Volumes instead of manually placing and configuring Light Probes.

## Advantages and limitations

| **Feature** | **Light Probe Groups** | **Probe Volumes** |
|---|---|---|
| Selection of surrounding probes | Per GameObject | Per pixel |
| Optimize memory use with streaming | No | Yes |
| Place probes automatically | No | Yes  |
| Place probes manually |  Yes  | No |

Probe Volumes have the following advantages:

- Unity samples surrounding probes per-pixel rather than per GameObject. This sampling approach results in better lighting consistency, and fewer seams between adjacent GameObjects.
- You can adjust Light Probe layouts across a scene, for example using a denser set of Light Probes in an interior area with more detailed lighting or geometry. Refer to [Configure the size and density of Probe Volumes](probevolumes-changedensity.md) for more information.
- Probe Volumes work well if you [work with multiple scenes](https://docs.unity3d.com/Manual/MultiSceneEditing.html). Refer to [Baking Sets](probevolumes-concept.md#baking-sets) for more information.
- Probe Volumes include [streaming](probevolumes-streaming.md) functionality to support large open worlds.

Probe Volumes have the following limitations:

- You can't adjust the locations of Light Probes inside a Probe Volume. You can use settings and overrides to try to fix visible artifacts, but it might not be possible to make sure Light Probes follow walls or are at the exact boundary between different lighting areas. Refer to [Fix issues with Probe Volumes](probevolumes-fixissues.md) for more information.
- You can't convert [Light Probe Groups](https://docs.unity3d.com/Manual/LightProbes.html) into a Probe Volume.

## How Probe Volumes work

URP automatically fills a Probe Volume with a 3D structure of 'bricks'. Each brick contains 64 Light Probes, arranged in a 4 × 4 × 4 grid.

URP uses bricks with different sizes to match the amount of geometry in different areas of your scene. For example, in areas with more geometry, URP uses small bricks with a short distance between Light Probes. The Light Probes capture lighting at a higher resolution, so lighting is more accurate.

The default Light Probe spacing is 1, 3, 9, or 27 m.

![](Images/probe-volumes/probevolumes-debug-displayprobebricks1.PNG)<br/>
In this screenshot from the Rendering Debugger, the small purple bricks contain Light Probes spaced 1 meter apart, to capture data from high-geometry areas. The large blue bricks contain Light Probes spaced 3 meters apart, to capture data from areas with less geometry.

Each pixel of a GameObject samples lighting data from the eight closest Light Probes around it.

You can do the following:

- Use the Rendering Debugger to visualize the layout of bricks and Light Probes. Refer to [Display Probe Volumes](probevolumes-showandadjust.md).
- [Configure the size and density of Probe Volumes](probevolumes-changedensity.md).
- [Add a Volume to your scene](probevolumes-fixissues.md#volume) to adjust which Light Probes GameObjects sample.

<a name="baking-sets"></a>
## Baking Sets

To store lighting from a scene in a Probe Volume, the scene must be part of a Baking Set.

A Baking Set contains the following:

- One or more scenes, which optionally include Probe Volumes.
- A single collection of settings.

By default, URP uses **Single Scene** mode, and places each scene in its own Baking Set automatically. However, only one Baking Set can be active at any time, so if you [work with multiple scenes](https://docs.unity3d.com/Manual/MultiSceneEditing.html), you must add these scenes to a single Baking Set if you want to bake them together. Refer to [Bake multiple scenes together with Baking Sets](probevolumes-usebakingsets.md) for more information.

## Additional resources

* [Light Probes](https://docs.unity3d.com/Manual/LightProbes.html)
* [Work with multiple scenes in Unity](https://docs.unity3d.com/Documentation/Manual/MultiSceneEditing.html)
