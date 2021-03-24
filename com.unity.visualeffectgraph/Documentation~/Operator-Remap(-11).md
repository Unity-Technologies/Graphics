# Remap [0..1] => [-1..1]

Menu Path : **Operator > Math > Remap > Remap**

The **Remap [0..1] => [-1..1]** Operator linearly remaps input values from the [0..1] range to the [-1..1] range. This is equivalent to the operation (x * 2) - 1. For example, an input of 0 calculates (0 * 2) -1, and outputs -1.

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
| Output     | Dependent | The remapped value The **Type** changes to match the **Input** type. |

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
