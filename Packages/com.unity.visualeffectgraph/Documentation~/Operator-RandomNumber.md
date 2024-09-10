# Random Number

Menu Path : **Operator > Random > Random Number**

The **Random Number** Operator allows you to generate pseudo-random floating-point numbers within a particular range.

You can define the scope of the Operator to generate random values on a per-particle, per-particle strip, or per-component level. You can also make the random number constant based on a seed. In this case, each time the Operator generates a new number from a particular seed, the result is the same (even over frames) with respect to the scope. So two constant **Random Number** Operators with the same scope and seed generate the same random number.

Note that every random number this Operator generates also depends on the global seed in the Visual Effect component. Running the same effect with the same seed allows for deterministic behavior in random number generations.

<video src="Images/Operator-RandomNumberExample.mp4" width="auto" height="auto" autoplay="true" loop="true" controls></video>

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Seed**     | Enum     | Defines the scope of the random number. The options are: <br/>&#8226;**Per Particle**: The Operator generates a different number every time.<br/>&#8226;**Per VFX Component**: The Operator generates a random number every frame and uses it for every particle in the same component.<br/>&#8226;**Per Particle Strip**: The Operator generates the same number every time based on the value in the **Seed** input port. If you use this option, the Operator implicitly enables **Constant** and does not allow you to disable it. |
| **Constant** | boolean  | Specifies whether the generated random number is constant or not.<br/>When enabled, the Operator generates the same number every time based on the **Seed** Operator property.<br/>This setting only appears if you set **Seed** to **Per Particle** or **Per Component**. If you set **Seed** to **Per Particle Strip**, the Operator implicitly enables this setting and does not allow you to disable it. |

| **Output** | **Type**                                | **Description**                       |
| ---------- | --------------------------------------- | ------------------------------------- |
| **Output** | [Configurable](#operator-configuration) | The output random per axis dimension. |



## Operator properties

| **Input**            | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| **Min**              | float    | The minimum value of the generated random number             |
| **Max**              | float    | The maximum value of the generated random number             |
| **Seed**             | uint     | Specifies a seed that the Operator uses to generate random values.<br/>This property only appears if you enable **Constant**.<br />If output type has more than one dimension and **Independent Seed** is enabled then there will be one **Seed** for every single channel. If two seed are equals, the result random value will be identical in this mode. |
| **Independent Seed** | bool     | When enabled, you can customize Seed per channel, otherwise Seed is randomly generated for each channel.<br />This property only appears if output type has more than one dimension. |

| **Output** | **Type** | **Description**                                          |
| ---------- | -------- | -------------------------------------------------------- |
| **r**      | float    | The generated random number between **Min** and **Max**. |

## Operator configuration

To view the Node's configuration, click the **cog** icon in the Node's header.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Type**     | The value type this Operator uses. For the list of types this property supports, see [Available types](#available-types). |



### Available types

You can use the following types:

- **Float**
- **Vector2**
- **Vector3**
- **Vector4**
- **Color**
- **Bool**
- **Int**
- **Uint**
