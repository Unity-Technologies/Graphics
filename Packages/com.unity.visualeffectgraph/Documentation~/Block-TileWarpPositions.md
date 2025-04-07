# Tile/Warp Positions

Menu Path : **Position > Tile Warp Positions**

The **Tile/Warp Positions** Block contains particles inside an [AABox](Type-AABox.md) and make these particles tile infinitely across space. A particle that exits the volume from one face, re-enters it from the opposing face, thus making the particle warp to the other side.

If you move the AABox, this Block warps particles around as the box moves, creating infinite tiling of these particles.

This Block can be useful to create infinitely tiling effects that need to stay close to the camera or the player in an application, such as rain or snow.

![Three different states of an AABox that contains particles. When Tile/Warp is disabled, the particles are not contained to the box. When Tile/Warp is enabled, the particles are contained in the box as they warp to the other side. When moving the AABox around with the Tile/Warp volume, the particles warp as the box moves.](Images/Block-TileWarpPositionsMain.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)
- Any output Context

## Block properties

| **Input**  | **Type**               | **Description**                               |
| ---------- | ---------------------- | --------------------------------------------- |
| **Volume** | [AABox](Type-AABox.md) | The reference AABox volume to use for tiling. |
