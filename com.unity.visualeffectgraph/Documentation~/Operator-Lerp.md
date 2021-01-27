# Lerp

Menu Path : **Operator > Math > Arithmetic > Lerp**

The **Lerp** Operator calculates the linear interpolation of a value between two border values.

This Operator accepts input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **X** and **Y** input are always of the same type. The **S** input is either a float or a vector of the same size as **X** and **Y**. If **S** is between 0 and 1 then the result is between **X** and **Y**.

## Operator properties

| **Input** | **Type**                                | **Description**                |
| --------- | --------------------------------------- | ------------------------------ |
| **X**     | [Configurable](#operator-configuration) | The value to interpolate from. |
| **Y**     | [Configurable](#operator-configuration) | The value to interpolate to.   |
| **S**     | [Configurable](#operator-configuration) | A value for the interpolation. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The linear interpolation of S between X and Y.<br/>The **Type** changes to match the type of **X** and **Y**. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. **X** and **Y** must be the same type among [Available Types](#available-types). If **S** is a vector type, Unity calculates the interpolation value by value.



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
