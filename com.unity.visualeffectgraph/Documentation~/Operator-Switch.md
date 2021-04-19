# Switch


Menu Path : **Operator > Logic > Switch**

The **Switch** Operator compares its input to case values and outputs a value which depends on whether the input matches a case:

- If the input matches a case value, this Operator outputs the value that corresponds to the matched case.
- If the input does not match a case value, this Operator outputs a default value.

This works similarly to a [switch statement](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/switch) in C#.

## Operator settings

| **Property**          | **Type**     | **Description**                                              |
| --------------------- | ------------ | ------------------------------------------------------------ |
| **Entry Count**       | Unsigned int | The number of cases to test. The maximum value is **32**.    |
| **Custom Case Value** | bool         | (**Inspector**) When enabled, you can specify custom case integers for each case. Otherwise, this Operator uses the default natural. |

## Operator properties

| **Input**      | **Type**                                | **Description**                                              |
| -------------- | --------------------------------------- | ------------------------------------------------------------ |
| **Test Value** | int                                     | Input integer value which is going to be tested with the case entries. If you enable **Custom Case Value** and this value matches more than one case, this Operator outputs the first entry that matches. |
| **Case 0**     | int                                     | The value to test for the first case. If you assign this port, it overrides the value this Operator checks **Test Value** against. The default value for this port is **0**. This port only appears if you enable **Custom Case Value**. |
| **Value 0**    | [Configurable](#operator-configuration) | The value to output if **Test Value** matches **Case 0**.    |
| **Case 1**     | int                                     | The value to test for the first case. If you assign this port, it overrides the value this Operator checks **Test Value** against. The default value for this port is **1**. This port only appears if you enable **Custom Case Value**. |
| **Value 1**    | [Configurable](#operator-configuration) | The value to output if **Test Value** matches **Case 1**.    |
| **Case N**     | int                                     | To expose more cases, increase the **Entry Count**.          |
| **Value N**    | [Configurable](#operator-configuration) | To expose more values, increase the **Entry Count**.         |
| **Default**    | [Configurable](#operator-configuration) | The default value to output if no cases match.               |

| **Output** | **Type**                                | **Description**                                              |
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Output** | [Configurable](#operator-configuration) | The value that corresponds to the case that matches **Test Value**, or **Default** if no cases match. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Type**     | The value type this Operator uses. For the list of types this property supports, see [Available types](#available-types). |

### Available types

You can use the following types for your **Input values** and **Output** ports:

- **Bool**
- **Int**
- **Uint**
- **Float**
- **Vector2**
- **Vector3**
- **Vector4**
- **Gradient**
- **AnimationCurve**
- **Matrix**
- **OrientedBox**
- **Color**
- **Direction**
- **Position**
- **Vector**
- **Transform**
- **Circle**
- **ArcCircle**
- **Sphere**
- **ArcSphere**
- **AABox**
- **Plane**
- **Cylinder**
- **Cone**
- **ArcCone**
- **Torus**
- **ArcTorus**
- **Line**
- **Flipbook**
- **Camera**

This list does not include any type that corresponds to a buffer or texture because it is not possible to assign these types as local variables in generated HLSL code.