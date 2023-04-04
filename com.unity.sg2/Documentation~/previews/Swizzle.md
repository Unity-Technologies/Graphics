## Description
Swaps, duplicates, or reorders the channels of a vector

## Input
**In** - A vector to swizzle.

## Outputs
**Out** - The new float or vector formed by the swizzle

## Controls
**Mask** - The letters represent the channels of the input. How they're arrange represents the new channels of the output.  

Some examples:
ZZZ - copies the third input channel to all three channels and outputs a vec3
XZY - swaps the second and third channels
X - returns only the first channel as a float

The letters RGBA can also be used instead of XYZW to represent the channels.
