# Perlin Curl Noise

Menu Path : **Operator > Noise > Perlin Curl Noise**

The **Perlin Curl Noise** Operator allows you to sample a noise value within a specified range in two or three dimensions based on provided coordinates. Perlin curl noise uses similar math to the [Perlin Noise](Operator-Perlin-Noise.md) Operator, but with the addition of a curl function which allows it to generate a turbulent noise. This resulting noise is incompressible (divergence-free), which means that particles cannot converge to sink points where they get stuck.

<video src="Images/Operator-PerlinCurlNoiseAnimation.mp4" title="Particles being influenced by dynamic, swirling noise patterns based on Perlin noise, creating smooth, chaotic motion." width="320" height="auto" autoplay="true" loop="true" controls></video>

A good use case for Curl Noise is emulating fluid or gas simulation, without having to perform complex calculations.

## Operator settings

| **Property**   | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Dimensions** | Enum     | Specify whether the noise is two, or three dimensional. |
| **Type**       | Enum     | Specify what type of noise to use.                           |

## Operator properties

| **Input**      | **Type**            | **Description**                                              |
| -------------- | ------------------- | ------------------------------------------------------------ |
| **Coordinate** | FloatVector2Vector3 | The coordinate in the noise field to sample from.<br/><video src="Images/Operator-PerlinCurlNoiseCoordinate.mp4" title="Coordinate operator example" width="320" height="auto" autoplay="true" loop="true" controls></video><br/>The **Type** changes to match the number of **Dimensions**. |
| **Frequency**  | Float               | The period in which Unity samples the noise. A higher frequency results in more frequent noise change.<br/><video src="Images/Operator-PerlinCurlNoiseFrequency.mp4" title="Frequency operator example" width="320" height="auto" autoplay="true" loop="true" controls></video> |
| **Octaves**    | Int                 | The number of layers of noise. More octaves create a more varied look but are also more resource-intensive to calculate.<br/><video src="Images/Operator-PerlinCurlNoiseOctaves.mp4" title="Octavesoperator example" width="320" height="auto" autoplay="true" loop="true" controls></video> |
| **Roughness**  | Float               | The scaling factor Unity applies to each octave. Unity only uses roughness when **Octaves** is set to a value higher than 1.<br/><video src="Images/Operator-PerlinCurlNoiseRoughness.mp4" title="Roughnessoperator example" width="320" height="auto" autoplay="true" loop="true" controls></video> |
| **Lacunarity** | Float               | The rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency.<br/><video src="Images/Operator-PerlinCurlNoiseLacunarity.mp4" title="Lacunarity operator example" width="320" height="auto" autoplay="true" loop="true" controls></video> |
| **Amplitude**  | Float               | The magnitude of the noise. A higher value increases the range of values the **Noise** port can return.<br/><video src="Images/Operator-PerlinCurlNoiseAmplitude.mp4" title="Amplitude operator example" width="320" height="auto" autoplay="true" loop="true" controls></video> |

| **Output** | **Type** | **Description**                                |
| ---------- | -------- | ---------------------------------------------- |
| **Noise**  | Float, Vector2, or Vector3 | The noise value at the coordinate you specify. |
