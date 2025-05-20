# Cellular Noise

Menu Path : **Operator > Noise > Cellular Noise**

The **Cellular Noise** Operator allows you to specify coordinates to sample a noise value within a specified range in one, two, or three dimensions. Cellular noise (also known as “Worley noise”) creates distinct cell-like patterns. To do this, it scatters random points then calculates the distance between the nearest ones.

<video src="Images/Operator-CellularNoiseAnimation.mp4" width="400" height="auto" autoplay="true" loop="true" title="Three cubes representing one, two, and three dimensional noise with cell-like patterns." controls></video><br/>Three cubes representing one, two, and three dimensional noise with cell-like patterns.

You can use this Operator to introduce variety to your particle attributes. A common use case is to use each particle’s position as a coordinate to sample the noise to output a new color, velocity, or position value.

## Operator settings

| **Property**   | **Type** | **Description**                                              |
| -------------- | -------- | ------------------------------------------------------------ |
| **Dimensions** | Enum     | Specify whether the noise is one, two, or three dimensional. |
| **Type**       | Enum     | Specify what type of noise to use.                           |

## Operator properties

| **Input**      | **Type**                      | **Description**                                              |
| -------------- | ----------------------------- | ------------------------------------------------------------ |
| **Coordinate** | Float<br/>Vector2<br/>Vector3 | The coordinate in the noise field to sample from. The **Type** changes to match the number of **Dimensions**.<br/><video src="Images/Operator-CellularNoiseCoordinate.mp4" width="100" height="auto" autoplay="true" loop="true" title="A cursor moving across the noise field, showing the different coordinates." controls></video><br/>|
| **Frequency**  | Float | The period in which Unity samples the noise. A higher frequency results in more frequent noise change.<br/><video src="Images/Operator-CellularNoiseFrequency.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the frequency increases, the noise increases." controls></video> |
| **Octaves**    | Int   | The number of layers of noise. More octaves create a more varied look but are also more resource-intensive to calculate.<br/><video src="Images/Operator-CellularNoiseOctaves.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the octaves increases, the noise is more varied." controls></video> |
| **Roughness**  | Float | The scaling factor Unity applies to each octave. Unity only uses roughness when **Octaves** is set to a value higher than 1.<br/><video src="Images/Operator-CellularNoiseRoughness.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the roughness increases, the noise is more visibly detailed." controls></video> |
| **Lacunarity** | Float | The rate of change of the frequency for each successive octave. A lacunarity value of 1 results in each octave having the same frequency.<br/><video src="Images/Operator-CellularNoiseLacunarity.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the value of the lacunarity increases, the noise is more visibly detailed." controls></video> |
| **Range**      | Vector2 | The range within which Unity calculates the noise. The noise stays between the X and Y value you specify here, where X is the minimum and Y is the maximum.<br/><video src="Images/Operator-CellularNoiseRange.mp4" width="100" height="auto" autoplay="true" loop="true" title="As the range increases, the noise is more visible." controls></video> |


| **Output**      | **Type**                      | **Description**                                              |
| --------------- | ----------------------------- | ------------------------------------------------------------ |
| **Noise**       | Float                         | The noise value at the coordinate you specify.               |
| **Derivatives** | Float<br/>Vector2<br/>Vector3 | The rate of change of the noise for every dimension.<br/><br/>The **Type** changes to match the number of **Dimensions**. |
