# Floor

Menu Path : **Operator > Math > Clamp > Floor**  

The **Floor** Operator rounds the input value down to the nearest integer. For example, an input of (4.1, 4, 4.8) outputs (4, 4, 4).

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types).

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The floor value of the input.<br/>The **Type** changes to match the type of **X**. |

## Operator configuration

To view the Operator's configuration, click the **cog** icon in the Operator's header. You can choose a type from all [Available Types](#available-types).

### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**