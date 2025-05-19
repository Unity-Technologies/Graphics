# Cellular Curl Noise

Menu Path : **Operator > Noise > Cellular Curl Noise**

The **Cellular Curl Noise** Operator allows you to sample a noise value within a specified range in two or three dimensions based on provided coordinates. Cellular curl noise uses similar math to the [Cellular Noise](Operator-Cellular-Noise.md) Operator, but with the addition of a curl function which allows it to generate a turbulent noise. This noise is incompressible (divergence-free), which means that particles cannot converge to sink points where they get stuck.

<video src="Images/Operator-CellularCurlNoiseAnimation.mp4" width="400" height="auto" autoplay="true" loop="true" title="A smoke-like simulation in movement." controls></video><br/>A smoke-like simulation in movement.

A good use case for Curl Noise is to simulate fluids or gas, without having to perform complex calculations.

## Operator settings

| **Property**   | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Dimensions** | Enum     | Specify whether the noise is two, or three dimensional. |
| **Type**       | Enum     | Specify what type of noise to use.                           |

## Operator properties

| **Input**      | **Type**                      | **Description**                                              |
| -------------- | ----------------------------- | ------------------------------------------------------------ |
| **Coordinate** | Float<br/>Vector2<br/>Vector3 | The coordinate in the noise field to sample from.<br/><video src="Images/Operator-CellularCurlNoiseCoordinate.mp4" width="100" height="auto" autoplay="true" loop="true" title="A cursor moving across the noise field, showing the different coordinates." controls></video><br/>The **Type** changes to match the number of **Dimensions**. |
| **Frequency**  | Float | The period in which Unity samples the noise. A higher frequency results in more frequent noise change.<br/><video src="Images/Operator-CellularCurlNoiseFrequency.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the frequency increases, the noise increases." controls></video> |
| **Octaves**    | Int   | The number of layers of noise. More octaves create a more varied look but are also more resource-intensive to calculate.<br/><video src="Images/Operator-CellularCurlNoiseOctaves.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the octaves increases, the noise is more varied." controls></video> |
| **Roughness**  | Float | The scaling factor Unity applies to each octave. Unity only uses roughness when **Octaves** is set to a value higher than 1.<br/><video src="Images/Operator-CellularCurlNoiseRoughness.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the roughness increases, the noise is more visibly detailed." controls></video> |
| **Lacunarity** | Float | The rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency.<br/><video src="Images/Operator-CellularCurlNoiseLacunarity.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the lacunarity increases, the noise is more visibly detailed." controls></video> |
| **Amplitude**  | Float | The magnitude of the noise. A higher value increases the range of values the **Noise** port can return.<br/><video src="Images/Operator-CellularCurlNoiseAmplitude.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the amplitude increases, the noise is more visible." controls></video> |

| **Output** | **Type** | **Description**                                |
| ---------- | -------- | ---------------------------------------------- |
| **Noise**  | Float    | The noise value at the coordinate you specify. |
