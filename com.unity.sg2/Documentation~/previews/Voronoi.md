## Description
Creates a cell noise pattern using ranomly-placed points as cell centers.

## Inputs
**UV** - The coordinates used to create the noise
**Angle Offset** - offset value for cell center points
**Cell Density** - scale of generated cells


## Output
**Out** - A cell noise pattern using ranomly-placed points as cell centers.

## Controls
**Hash Type** - the formula used for calculating hash from the input coordinates.  Legacy was the original formula used, but it breaks down with high input values.  The Deterministic option is both cheaper to calculate and does not break down with high input values.