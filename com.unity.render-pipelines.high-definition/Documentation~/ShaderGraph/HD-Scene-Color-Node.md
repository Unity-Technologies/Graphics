# HD Scene Color Node

Do the same thing than the [Scene Color Node](Scene-Color-Node.md) but let you access the mips of the color buffer. 

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV     | Input | Vector 4 | Screen Position | Normalized screen coordinates |
| Lod | Input | float | None | The mip level used to sample the color buffer | 
| Output | Output      |    Vector 3 | None | Output value |

## Exposure

There is an exposure toggle to specify if you want the exposed camera color or not, by default it's disabled because in most cases the exposure is applied to the object where you display the camera color so it avoids to have double exposure.

Note: the sampler used to do the sampling on the color buffer is in trilinear clamp mode so it will interpolate nicely between the lods.