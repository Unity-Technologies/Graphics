# Probe Volumes

*NOTE: This is temporary documentation and the feature is experimental. More detailed documentation will be provided as soon as the feature is out of Experimental. Changes might be happening before a final version is released*

Probe Volumes is a feature that is meant to be an evolution of the light probe groups in Unity for probe-based global illumination.  This system provides per pixel lighting.

The system provides automatic placement of probes and distributes them in a hierarchical fashion around scene geometry marked to contribute to lighting via Light Probes.  All the lighting data will be placed in units (bricks) of 4x4x4 probes distributed in an axis aligned structure, with the highest density is placed close to geometry and having less density moving away from the geometry.
The last level of the hierarchy is called a "Cell" and it is the loading unit and eventually the streaming unit.

Even if enlighten is active, this system will **not** provide real time GI.



## How to Enable Probe Volume feature

This experimental feature will need to be enabled in several places (this will be simplified in the out-of-experimental version).

Firstly, it needs to be enabled at a project-level in the **HDRP Global Settings > Miscellaneous > Probe Volumes (Experimental)**. Then the HDRP asset in use it needs to be enabled under **Lighting > Probe Volumes (Experimental)**. Finally, for each relevant camera, the feature needs to be enabled in its frame settings **Lighting > Probe Volume**.



## How to add Light Probes to a Scene

As a first step you need to add a Probe Volume to the scene (**Light > Probe Volume  (Experimental)**).  This will add a volume to your scene. The area covered by this volume is the area that will be filled with probes, this volume it is just a marker to inform the baker where to put the probes and does not hold any data.

![Probe-Volume](Images/Probe-Volume.png)



| **Field Name**              | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| Global                      | If this is set, the probe volume will automatically expand to cover all the static game objects in the scene when baking starts. If this option is set, there is no need to define a specific area. |
| Size                        | The size of the area covered by the Probe Volume.            |
| Override Subdivision Levels | It is possible to change how the area covered by the Probe Volume is subdivided by changing the maximum and minimum subdivision levels. The overrides are enabled only if this checkbox is set. |
| Highest Subdivision Level   | Overrides the highest subdivision level used by the system. This determines how finely a probe volume is subdivided, lower values means larger minimum distance between probes. |
| Lowest Subdivision Level    | Overrides the lowest subdivision level used by the system. This determines how coarsely a probe volume is allowed to be subdivided, higher values means smaller maximum distance between probes. |
| Object Layer Mask           | Control which layers will be used to select the meshes for the probe placement algorithm. |
| Geometry Distance Offset    | Affects the minimum distance at which the subdivision system will place probes near the geometry. |
| Fit to Scene                | A button that will cause the probe volume that will automatically fit to cover all the static game objects in the scene. |
| Fit to Selection            | A button that will cause the probe volume that will automatically fit to cover all the static game objects selected in the hierarchy. |

## How to bake and customize baking for Probe Volumes

To define the characteristics of the baking of the probe volumes system the Probe Volume Settings window needs to be used (**Window > Rendering > Probe Volume Settings (Experimental)**).

![Probe-Volumes-Baking-Window](Images/Probe-Volumes-Baking-Window.png)

Note that the baking still happens via the lightmapper and it must configured as the lightmapper is usally configured.

#### Baking sets

A Baking set is a set of scenes that share the same Probe volume settings and set of scenes that could be loaded together. A scene can only be part of a single baking set and scenes belonging to separate baking sets are not compatible and should not be loaded together.

If two scenes belonging to two different baking sets are loaded together, the lighting data of one of the two scenes will not be loaded or used.

You can create any number of Baking set, by default every scene containing at least a Probe Volume component will be added to the Default baking set.

#### Probe Volume Settings

This tab of the window will show the settings for the selected Baking Set.

###### Scenes List

First section allows you to add or remove scenes to a bake setting. By pressing the + button you can select among the list of available scenes in the project. The first scene in the list is the one that will contribute the lighting settings for baking (the settings used to configure the lightmapper), this will likely change in the near future.
An icon will be showed next to each scene containing a probe volume in it.

###### Probe Volume Profile

This section specifies how the hierarchical structure of the probe volume system is defined.

| **Field Name**              | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| Simplification levels       | Defines how many levels of subdivisions will be defined for the system. Each simplification is a power of 3 of the previous level. For example if minimum distance of probes is set to 1 meter and we have 3 simplification levels, it will lead with bricks that have probes spaced at: 1 meter -> 3 meters -> 9 meters -> 27 meters. |
| Min Distance Between Probes | The minimum distance between probes in the system.           |

An info box will help in understanding what is the distance of probes that will be represented in the system.

###### Dilation Settings

*This settings are fairly advanced and we suggest keeping the defaults for now.*

Given the axis aligned regular structure in which probes are going to be laid out, some probes are likely to be end up inside geometry and therefore containing invalid data. To obviate the issue, data from neighboring valid probes are dilated inside invalid data that would otherwise be completely black.

| **Field Name**              | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| Enable Dilation             | Whether to enable dilation after the baking.                 |
| Dilation Distance           | The distance used to pick neighboring probes to dilate into the invalid probe. |
| Dilation Validity Threshold | The validity threshold used to identify invalid probes. The more a probe sees back-faces when backing the lower the validity will be for that given probe. |
| Dilation Iteration Count    | The number of times the dilation process takes place.        |
| Squared Distance Weighting  | Whether to weight neighboring probe contribution using squared distance rather than linear distance. |

###### Virtual Offset

*This settings are fairly advanced and we suggest keeping the defaults for now. Moreover this will be changed a lot very soon.*

Virtual offset is a complimentary solution to the same problem of probes ending up inside geometry. To solve this problem virtual offset will virtually push the position at which lighting is captured outside the geometry and then the data baked this way is placed again in the regular structure.

| **Field Name**    | Description                                                  |
| ----------------- | ------------------------------------------------------------ |
| Search multiplier | A multiplier to be applied on the distance between two probes to derive the search distance out of geometry. |
| Bias out geometry | Determines how much a probe is pushed out of the geometry on top of the distance to closest hit. |

## Probe Volume Sampling Options

It is possible to tweak the sampling of light probe data with a Volume component called **Probe Volumes Options**.

![Probe-Volumes-Options](Images/Probe-Volumes-Options.png)

| **Field Name**                     | Description                                                  |
| ---------------------------------- | ------------------------------------------------------------ |
| Normal Bias                        | The position that is used to sample the probe volumes data structure is biased alongside the normal of the object that is sampling the lighting. This can help with leaking issues. In meters. |
| View Bias                          | Similar to normal bias, however the bias is applied along the view vector. In meters. |
| Scale Bias With Min Probe Distance | Scales the biases with the minimum distance between probes. Can be used to maintain a consistent look as the settings change on the settings panel. |
| Sampling Noise                     | Adds a bit of noise to the world space position used to sample the lighting. This is used to break some harsh seams lines can be observed when close pixels are sampling from different subdivision levels. |

These options can help for leaking situations.

## Debug views

The probe volume systems comes with several debug views that can be found in the Rendering Debugger (**Window > Analysis > Rendering Debugger > Probe Volume**)

![Probe-Volumes-Debug](Images/Probe-Volumes-Debug.png)

###### Subdivision Visualization

| **Field Name**  | Description                                                  |
| --------------- | ------------------------------------------------------------ |
| Display Cells   | Displays loaded cells. As a reminder, cells are the loading and streaming unit. |
| Display Bricks  | Display the hierarchical structure with each brick of a given hierarchy level is color coded differently. |
| Realtime Update | Runs the placement algorithm at real time, it can be useful to visualize the structure before the baking happens. **This only refers to the placement and not the content of lighting.** |

![Probe-Volumes-Brick-Debug.png](Images/Probe-Volumes-Brick-Debug.png.jpg)

​                                                                                                             *Display Bricks visualization*

###### Probe Visualization

![Probe-Volumes-Probe-Debug-Panel](Images/Probe-Volumes-Probe-Debug-Panel.png)

######

| **Field Name**              | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| Display Probes              | Display spheres at probe locations displaying the content of the lighting of that probe. |
| Probe Shading Mode          | Determines the visualization mode of probes. The options are:<br />   -  SH: Displays the lighting data of the probe.<br />   -  Validity: Displays how much a probe is considered as a gradient between valid (green) and invalid (red). See discussion on Dilation for more information about validity. <br />    - Validity over dilation threshold: Green if a probe is considered valid for dilation purposes and red otherwise. |
| Probe Size                  | Size of the debug sphere mesh used to display the probe.     |
| Probe Exposure Compensation | An exposure compensation to be applied to the debug data displayed on the probe. |
| Culling Distance            | Define a culling distance for the debug spheres defined from camera position. |
| Max subdivision displayed   | Cull probes based on their subdivision levels.               |

![Probe-Volumes-Probes-Debug](Images/Probe-Volumes-Probes-Debug.jpg)

​                                                                                                             *Display Probes visualization*
