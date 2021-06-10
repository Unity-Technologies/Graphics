# Random Number

Menu Path : **Operator > Random > Random Number**

The **Random Number** Operator allows you to generate pseudo-random floating-point numbers within a particular range.

You can define the scope of the Operator to generate random values on a per-particle, per-particle strip, or per-component level. You can also make the random number constant based on a seed. In this case, each time the Operator generates a new number from a particular seed, the result is the same (even over frames) with respect to the scope. So two constant **Random Number** Operators with the same scope and seed generate the same random number.

Note that every random number this Operator generates also depends on the global seed in the Visual Effect component. Running the same effect with the same seed allows for deterministic behavior in random number generations.

## Operator settings

| **Property** | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Seed**     | Enum     | Defines the scope of the random number. The options are: <br/>&#8226;**Per Particle**: The Operator generates a different number every time.<br/>&#8226;**Per Component**: The Operator generates a random number every frame and uses it for every particle in the same component.<br/>&#8226;**Per Particle Strip**: The Operator generates the same number every time based on the value in the **Seed** input port. If you use this option, the Operator implicitly enables **Constant** and does not allow you to disable it. |
| **Constant** | boolean  | Specifies whether the generated random number is constant or not.<br/>When enabled, the Operator generates the same number every time based on the **Seed** Operator property.<br/>This setting only appears if you set **Seed** to **Per Particle** or **Per Component**. If you set **Seed** to **Per Particle Strip**, the Operator implicitly enables this setting and does not allow you to disable it. |

## Operator properties

| **Input** | **Type** | **Description**                                              |
| --------- | -------- | ------------------------------------------------------------ |
| **Min**   | float    | The minimum value of the generated random number             |
| **Max**   | float    | The maximum value of the generated random number             |
| **Seed**  | uint     | Specifies a seed that the Operator uses to generate random values.<br/>This property only appears is you enable **Constant**. |

| **Output** | **Type** | **Description**                                          |
| ---------- | -------- | -------------------------------------------------------- |
| **r**      | float    | The generated random number between **Min** and **Max**. |
