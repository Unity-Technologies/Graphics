# Turbulence

Menu Path : **Force > Turbulence**

The **Turbulence** Block generates a noise field which it applies to the particle’s velocity. This Block is useful for adding natural-looking movement to particles. For more information on the types of noise, see the [Value Curl](Operator-ValueCurlNoise.md), [Perlin Curl](Operator-PerlinCurlNoise.md), and [Cellular Curl](Operator-CellularCurlNoise.md) noise Operators.

![](Images/Block-TurbulenceExample.gif)

## Block compatibility

This Block is compatible with the following Contexts:

- [Update](Context-Update.md)

## Block settings

| **Setting**    | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Mode**       | Enum     | The mode this Block uses to apply the force to the particles. The options are:<br/>&#8226; **Absolute**: Applies the force to the particle as an absolute value.<br/>&#8226; **Relative**: Applies the force relative to the particle's velocity. |
| **Noise Type** | Enum     | The type of noise this Block uses to generate the turbulence pattern. The options are: <br/>&#8226; **Value**: Uses [Value Curl Noise](Operator-ValueCurlNoise.md) to generate the turbulence.<br/>&#8226; **Perlin**: Uses [Perlin Curl Noise](Operator-PerlinCurlNoise.md) to generate the turbulence.<br/>&#8226; **Cellular**: Uses [Cellular Curl Noise](Operator-CellularCurlNoise.md) to generate the turbulence. |

## Block properties

| **Input**           | **Type**                       | **Description**                                              |
| ------------------- | ------------------------------ | ------------------------------------------------------------ |
| **Field Transform** | [Transform](Type-Transform.md) | The transform with which to position, rotate, or scale the turbulence field. |
| **Intensity**       | Float                          | The intensity of the turbulence. Higher values result in an increased particle velocity. |
| **Drag**            | Float                          | The drag coefficient. Higher drag leads to a stronger force influence over the particle velocity.<br/>This property only appears if you set **Mode** to **Relative**. |
| **Frequency**       | Float                          | The period in which Unity samples the noise. A higher frequency results in more frequent noise change. |
| **Octaves**         | Uint (Slider)                  | The number of layers of noise. More octaves create a more varied look but are also more resource-intensive to calculate. |
| **Roughness**       | Float (Slider)                 | The scaling factor Unity applies to each octave. Unity only uses roughness when **Octaves** is set to a value higher than 1. |
| **Lacunarity**      | Float                          | The rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency. |
