## Description
Changes the length of the input vector to 1.

## Inputs
**In** - A vector to normalize.

## Output
**Out** - In / Length(In).

## Controls
**Mode** - Safe Normalize ensures that you don't get a NaN result if the input vector is of zero length. This is a slightly more expensive opperation and should not be needed if you provide the Normalize with proper data.