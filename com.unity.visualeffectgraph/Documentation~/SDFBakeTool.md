<div style="border: solid 1px #999; border-radius:12px; background-color:#EEE; padding: 8px; padding-left:14px; color: #555; font-size:14px;"><b>Draft:</b> The content on this page is complete, but it has not been reviewed yet.</div>

# SDF Bake Tool
The SDF Bake Tool is an Utility Window that enables generating [Signed Distance Fields](SignedDistanceFields.md) Assets from a Mesh or a Prefab containing meshes.

## Opening the Tool

The SDF Bake Tool Window is accessible through the menu :

**Window > Visual Effects > Utilities > SDF Bake Tool**

## Using the Tool

## Settings
The window displays the following Baking properties :

* **Maximal resolution** (uint) : The resolution of the largest side of the baked texture.
* **Box Center** (Vector3): The center of the Axis-Aligned box that will contain the object to bake.
* **Desired Box Size** (Vector3): The desired size of each side of the Axis-Aligned box that will contain the object to bake.
* **Actual Box Size** (Vector3): The actual size of each side of the Axis-Aligned box that will contain the object to bake This might slightly differ from the Desired Box Size, to ensure that the voxels in the texture are cubic.
* **Live Update** (bool): When this box is ticked, every setting change will trigger a new bake, that you can visualize in real-time. Warning: This can get demanding for your machine if the resolution you are using and/or the [Sign Passes Count]() is high.
* **Fit Box to Mesh** : Sets the baking box to the actual bounding box of the mesh. Some padding can be added, see [Fit Padding]().
* **Fit Cube to Mesh** : Sets the baking box to the smallest cube containing the mesh. Some padding can be added, see [Fit Padding]().
* **Model Source** (Enum) : Controls whether is object to bake is a Mesh or a Prefab containing meshes.
  * Mesh
  * Mesh Prefab
* **Mesh** (Mesh) :(only when Model Source is set to Mesh) The mesh of which you want to compute the SDF.
* **Mesh Prefab** (Prefab) :(only when Model Source is set to Mesh Prefab) The set of meshes of which you want to compute the SDF. The prefab needs to contain at least one mesh in its hierarchy.


## Advanced Settings
By clicking the cogwheel on the top-right of the window, you can toggle the **Advanced settings** :
* **Baking parameters**: When the provided geometry does not not unambiguously separate an inside from the outside, for example because of holes or self-intersection, the baking can result in unwanted classifications. Sign Passes Count and In/Out Threshold help mitigate these cases.
  * **Sign Passes Count**(uint): In general, increasing this value reduces artefacts caused by ambiguous geometry.
  * **In/Out Threshold**(float): Each voxel in the texture is assigned a score of "outsideness". This parameters controls what is the threshold from which voxels are considered outside. Low values will consider more points as inside and vice versa.
  * **Surface Offset**(float): Allows to enlarge (positive values) or shrink (negative values) the surface of the SDF.
* **Fit Padding** (Vector3): Some padding to apply when using the Fit Box to Mesh and Fit Cube to Mesh buttons.



## Limitations and Caveats

Signed Distance Fields generated with the [SDF Bake Tool](SDFBakeTool.md) are normalized, i.e. the underlying surface is scaled such that the largest side of the texture is of length 1.
It might be required to increase the parameter **Scale** of the SDF preview to get a better visualisation.
