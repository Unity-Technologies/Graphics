# Branch


Menu Path : **Operator > Logic > Branch**

The **Branch** Operator evaluates a predicate and returns the value assigned to **True** if the predicate is `true` and returns the value assigned to **False** otherwise.

## Operator properties

| **Input**     | **Type**                                | **Description**                                              |
| ------------- | --------------------------------------- | ------------------------------------------------------------ |
| **Predicate** | bool                                    | The boolean to test. If `true`, this Operator outputs the value assigned to **True**. If `false`, this Operator outputs the value assigned to **False**. |
| **True**      | [Configurable](#operator-configuration) | The value to output if **Predicate** is `true`.              |
| **False**     | [Configurable](#operator-configuration) | The value to output if **Predicate** is `false`.             |

| **Output** | **Type**                                | **Description**                                              |
| ---------- | --------------------------------------- | ------------------------------------------------------------ |
| **Output** | [Configurable](#operator-configuration) | The output value based on the **Predicate**. If **Predicate** is `true`, this is the value assigned to **True**. If **Predicate** is `false`, this is the value assigned to **False**. |

## Operator configuration

To view the Node's configuration, click the **cog** icon in the Node's header.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Type**     | The value type for the **True** and **False** ports as well as the output value. For the list of types this property supports, see [Available types](#available-types). |

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
