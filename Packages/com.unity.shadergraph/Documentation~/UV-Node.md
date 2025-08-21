# UV node

The UV node outputs the vertex or fragment UV coordinates of a mesh.

UV coordinates usually have two channels, but the UV node outputs four channels so you can use the remaining two channels, for example to store custom mesh data.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **Out** | Output | Vector 4 | None | The u and v coordinates from the mesh in the first two channels, and two extra channels. |

## Controls

| **Name** | **Type** | **Options** | **Description** |
|:------------ |:-------------|:-----|:---|
| **Channel** | Dropdown | **UV0**, **UV1**, **UV2**, **UV3**, **UV4**, **UV5**, **UV6**, **UV7** | Selects the coordinate set to output. |
