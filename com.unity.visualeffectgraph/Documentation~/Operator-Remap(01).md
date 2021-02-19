# Remap [-1..1] => [0..1]

Menu Path : **Operator > Math > Remap > Remap**

The **Remap [-1..1] => [0..1]** Operator linearly remaps input values from the range -1 to 1 to the range 0 to 1. This is equivalent to the operation (x * 0.5) + 0.5. For example, an input of -1 calculates (-1 * 0.5) + 0.5, and outputs 0.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available types](#available-types).

## Operator settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Clamp**   | bool     | Clamps the **Input** to the input range before the Operator remaps it. |

## Operator properties

| **Input** | **Type**                                | **Description**                    |
| --------- | --------------------------------------- | ---------------------------------- |
| Input     | [Configurable](#operator-configuration) | The value this Operator evaluates. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| Output     | Dependent | The remapped value.<br/>The **Type** changes to match the **Input** type. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header.

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Type**     | The value type for the **Input** port and Output value. For the list of types this property supports, see [Available types](#available-types). |

### Available types

You can use the following types for your **Input values** and **Output** ports:

- **Float**
- **Vector2**
- **Vector3**
- **Vector4**
- **Direction**
- **Position**
- **Vector**
