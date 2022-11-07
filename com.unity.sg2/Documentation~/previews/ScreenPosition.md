## Description
The location on the screen of the current pixel.

## Output
**Out** - Mesh's screen position in selected coordinate space.

## Controls
**Mode** - Selects coordinate space of Position to output.

Default

Returns Screen Position. This mode divides Screen Position by the clip space position W component.

Raw

Returns Screen Position. This mode does not divide Screen Position by the clip space position W component. This is useful for projection.

Center

Returns Screen Position offset so position float2(0,0) is at the center of the screen.

Tiled

Returns Screen Position offset so position float2(0,0) is at the center of the screen and tiled using frac.