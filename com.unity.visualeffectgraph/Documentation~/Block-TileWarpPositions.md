# Tile/Warp Positions

Menu Path : **Position > Tile Warp Positions**

The **Tile/Warp Positions** Block contains particles inside an [AABox](Type-AABox.md) and make these particles tile infinitely across space. A particle that exits the volume from one face, re-enters it from the opposing face, thus making the particle warp to the other side.

If you move the AABox, this Block warps particles around as the box moves, creating infinite tiling of these particles.

This Block can be useful to create infinitely tiling effects that need to stay close to the camera or the player in an application, such as rain or snow.

![](Images/Block-TileWarpPositionsMain.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)
- Any output Context

## Block properties

| **Input**  | **Type**               | **Description**                               |
| ---------- | ---------------------- | --------------------------------------------- |
| **Volume** | [AABox](Type-AABox.md) | The reference AABox volume to use for tiling. |
