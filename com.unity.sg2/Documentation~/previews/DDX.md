## Description
Calculates the partial derivative of the input in relation to the screen-space x-coordinate.

## Inputs
**In** - Input value.

## Output
**Out** - The difference between the value of the current pixel and the horizontal neighboring pixel.

## Controls
**Standard** - The standard partial derivative - typically behaves the same as Coarse.
**Coarse** - A low precision derivative where each pixel quad has the same value.
**Fine** - A high precision derivative where each pixel can have a unique value.