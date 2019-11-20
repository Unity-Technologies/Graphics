# SpeedTree Wind Node

## Description

Performs wind animation for SpeedTree assets.  Depending on the asset and LOD, it can also generate billboard geometry as well.  This node is designed to be completely fixed function and has no inputs.  As a SpeedTree-specific node, the resulting animations are meant to replicate IDV Inc.'s own wind animation behaviors.  This also means it is dependent on the built-in core Unity integration of SpeedTree Wind properties.

## Ports

| Name               | Direction | Type    | Description              |
| :----------------- | :-------- | :------ | :----------------------- |
| OutPosition        | Output    | Vector3 | Animated position        |
| OutNormal          | Output    | Vector3 | Animated surface normal  |
| OutTangent         | Output    | Vector3 | Animated surface tangent |
| OutUV0             | Output    | Vector2 | Recomputed UVs           |
| OutAlphaMultiplier | Output    | Float   | Alpha multiplier factor  |

## Notes

This node is designed to be all-encompassing for SpeedTree v7 and SpeedTree v8 assets.  All of its specific functionality is determined by compile state as well as controls/data, some of which is embedded in the material settings, and some embedded in the imported asset itself.

As a result, this will not do anything for any Shader Graph which is not targeting a SpeedTree asset.  Likewise, the behavior will be different for SpeedTree v7 and v8 targets.  Both v7 and v8 targets will modify position, normal, and tangent.  These are geometric alterations, and should therefore be connected to Vertex fields in the Master Node.

Only in v7 asset targets will the UVs *potentially* be altered, whereas the AlphaMultiplier (which should be multiplied by texture alpha) *potentially* varies only in v8.  In both cases, this is related to billboard visualizations of the tree, which are seen at the lowest LOD.  In v7, the billboards are both generated and animated via the Wind node, and this means they must also have their UVs set on the fly.  In v8, the billboards are part of the asset, but certain planes will have their visibility changed based on camera angle, and this is controlled via alpha.

