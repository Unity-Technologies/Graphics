## Description
creates a flipbook, or texture sheet animation

## Inputs
**UV** - the input UV coordinates
**Width** - the number of horizontal tiles in the atlas texture
**Height** - the number of vertical tiles in the atlas texture
**Tile** - index of the current tile where 0 is the first and 1 is the last

## Output
**Out** - UVs for sampling the atlas texture
(only available when Flip Frames is selected)
**UV0** - UVs for the first atlas texture sample
(only available when Blend Frames is selected)
**UV1** - UVs for the second atlas texture sample
(only available when Blend Frames is selected)
**Blend** - the T input of a Lerp node to blend between the 2 atlas samples
(only available when Blend Frames is selected)

## Controls
**Invert X** - inverts the horizontal axis of the UVs
**Invert Y** - inverts the vertical axis of the UVs
**Mode** - choose to flip between the frames or blend. Blending is more expensive since it requires the atlas texture to be sampled twice, but it also allows the flipbook framerate to be lower without popping - which means similar types of effects can be achieved with fewer frames.