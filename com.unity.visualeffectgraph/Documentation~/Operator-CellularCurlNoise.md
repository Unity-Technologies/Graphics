# Cellular Curl Noise

Menu Path : **Operator > Noise > Cellular Curl Noise**

The **Cellular Curl Noise** Operator allows you to sample a noise value within a specified range in two or three dimensions based on provided coordinates. Cellular curl noise uses similar math to the [Cellular Noise](Operator-Cellular-Noise.md) Operator, but with the addition of a curl function which allows it to generate a turbulent noise. This noise is incompressible (divergence-free), which means that particles cannot converge to sink points where they get stuck.

![](Images/Operator-CellularCurlNoiseAnimation.gif)

A good use case for Curl Noise is to simulate fluids or gas, without having to perform complex calculations.

## Operator settings

| **Property**   | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Dimensions** | Enum     | Specify whether the noise is two, or three dimensional. |
| **Type**       | Enum     | Specify what type of noise to use.                           |

## Operator properties

| **Input**      | **Type**                      | **Description**                                              |
| -------------- | ----------------------------- | ------------------------------------------------------------ |
| **Coordinate** | Float<br/>Vector2<br/>Vector3 | The coordinate in the noise field to sample from.<br/>![](Images/Operator-CellularCurlNoiseCoordinate.gif)<br/>The **Type** changes to match the number of **Dimensions**. |
| **Frequency**  | Float                         | The period in which Unity samples the noise. A higher frequency results in more frequent noise change.<br/>![](Images/Operator-CellularCurlNoiseFrequency.gif) |
| **Octaves**    | Int                           | The number of layers of noise. More octaves create a more varied look but are also more resource-intensive to calculate.<br/>![](Images/Operator-CellularCurlNoiseOctaves.gif) |
| **Roughness**  | Float                         | The scaling factor Unity applies to each octave. Unity only uses roughness when **Octaves** is set to a value higher than 1.<br/>![](Images/Operator-CellularCurlNoiseRoughness.gif) |
| **Lacunarity** | Float                         | The rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency.<br/>![](Images/Operator-CellularCurlNoiseLacunarity.gif) |
| **Amplitude**  | Float                         | The magnitude of the noise. A higher value increases the range of values the **Noise** port can return.<br/>![](Images/Operator-CellularCurlNoiseAmplitude.gif) |

| **Output** | **Type** | **Description**                                |
| ---------- | -------- | ---------------------------------------------- |
| **Noise**  | Float    | The noise value at the coordinate you specify. |