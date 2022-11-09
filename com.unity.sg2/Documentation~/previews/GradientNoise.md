## Description
Creates a smooth, non-tiling noise pattern using a gradient lattice.

## Inputs
**UV** - The coordinates used to create the noise
**Scale** - The size of the noise

## Output
**Out** - A smooth, non-tiling noise pattern using a gradient lattice

## Controls
**Hash Type** - The formula used for calculating hash from the input coordinates.  Legacy was the original formula used, but it breaks down with high input values.  The Deterministic option is both cheaper to calculate and does not break up with high input values.