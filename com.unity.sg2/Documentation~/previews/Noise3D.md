## Description
Creates 3d noise based on the selected options.

## Inputs
**Position** - The 3d seed for the noise pattern.
**Octaves** - The number of times to repeat the noise algorithm. More octaves creates more detail and more expensive noise.
**Lacunarity** - The scale adjustment between each octave.
**Gain** - The contrast adjustment between each octave.
**Offset** - Controls the brightess or height of the multifractal ridges
**Gradient Volume** - The 3d texture to use for each octave instead of using math.
**Noise Hash Texture** - The texture to use as a hash instead of using math for Value Noise.
**Rotate Octaves** - When true, each octave is rotated slightly to create better variation.

## Output
**Out** - A smooth, non-tiling noise pattern using the selected options

## Controls
**Noise Type** - This dropdown offers three different selections:

1. Base Pattern - the base noise type: Value Noise, Gradient Noise, or Worley Noise. These are in order of least expensive to most.
2. Calculation Method - select whether to use Math or Texture for the base pattern.
3. Fractal Type - select Fractal Brownian Motion, Turbulance, or Ridged Multifractal. These are in order of least expensive to most.
