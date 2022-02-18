# Inverse Lerp

Menu Path : **Operator > Math > Arithmetic > Inverse Lerp**

The **Inverse Lerp** Operator calculates a fraction which represents how far through a value is between two border values. Whereas the [Lerp Operator](Operator-Lerp.md) takes a fraction and outputs a blend between the two values, this Operator takes a blended value and outputs the fraction. For example, if **a** and **b** are the two border values, **t** is the fraction, and **value** is the blend between **a** and **b**:

* Lerp (**a**, **b**, **t**) = **value**.
* Inverse Lerp (**a**, **b**, **value**) = **t**.

This Operator accepts a number of input values of various types. For the list of types this Operator can use, see [Available Types](#available-types). The **X** and **Y** input are always of the same type. The **S** input is either a float or a vector of the same size as **X** and **Y**.

## Operator properties

| **Input** | **Type**                                | **Description**                        |
| --------- | --------------------------------------- | -------------------------------------- |
| **X**     | [Configurable](#operator-configuration) | The value to interpolate from.         |
| **Y**     | [Configurable](#operator-configuration) | The value to interpolate to.           |
| **S**     | [Configurable](#operator-configuration) | A value for the inverse interpolation. |

| **Output** | **Type**  | **Description**                                              |
| ---------- | --------- | ------------------------------------------------------------ |
| **Out**    | Dependent | The inverse of the linear interpolation of **S** between **X** and **Y**.<br/>The **Type** changes to match the type of **X** and **Y**. |

## Operator configuration

To view the Operator’s configuration, click the **cog** icon in the Operator’s header. **X** and **Y** must be of the same type among [Available Types](#available-types). **S** is either a float or the same type as **X** and **Y**. If **S** is a vector type Unity calculates the interpolation value by value.

If S is between X and Y then the result is between 0 and 1.



### Available types

You can use the following types for your **input** ports:

- **float**
- **Vector**
- **Vector2**
- **Vector3**
- **Vector4**
- **Position**
- **Direction**
