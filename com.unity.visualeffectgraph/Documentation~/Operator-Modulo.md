# Modulo

Menu Path : **Operator > Math > Arithmetic > Modulo**

The **Modulo** Operator divides the first input by the second input and calculates the remainder. For example, an input of 4.5 % 1.5 outputs 0, an input of 8 % 3 outputs 2, and an input of (4, 8, 4) % (2, 3, 3) outputs (0, 2, 1).

This Operator accepts a number of input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The first and second input are always of the same type.

## Operator properties

| **Input** | **Type**                                | **Description**                                              |
| --------- | --------------------------------------- | ------------------------------------------------------------ |
| **A**     | [Configurable](#operator-configuration) | The dividend. The Operator divides this value by **B** and returns the remainder. |
| **B**     | [Configurable](#operator-configuration) | The Divisor. The Operator divides **A** by this value and returns the remainder. |

| **Output** | **Type**    | **Description**       |
| ---------- | ----------- | --------------------- |
| **Out**    | Output port | The Modulo of A by B. |

## Operator configuration

To view the **Modulo** Operator’s configuration, click the **cog** icon in the Operator’s header. You can choose a type among all [Available Types](#available-types).



### Available types

You can use the following types for your **input** ports:

- **float**
- **int**
- **uint**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
