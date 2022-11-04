## Description
Calculates the partial derivative of the input in relation to the screen-space y-coordinate.

## Inputs
**In** - input value

## Output
**Out** - the difference between the value of the current pixel and the vertical neighboring pixel

## Controls
**Standard** - the standard partial derivative - typically behaves the same as Coarse
**Coarse** - a low precision derivative where each pixel quad has the same value
**Fine** - a high precision derivative where each pixel can have a unique value