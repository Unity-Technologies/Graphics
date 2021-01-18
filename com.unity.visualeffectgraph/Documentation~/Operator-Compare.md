# Compare

Menu Path : **Operator > Logic > Compare**

The **Compare** Operator compares two floats based on a condition and returns the result as a boolean. It evaluates the two floats as `Left [Condition] Right` so, if **Left** is 0.5, **Right** is 1.0, and **Condition** is **Greater**, the evaluation is `0.5 Greater 1.0`, which returns false.

## Operator settings

| **Property**  | **Description**                                              |
| ------------- | ------------------------------------------------------------ |
| **Condition** | Specifies the condition this Operator uses to evaluate **Left** and **Right**. The options are:<br/>&#8226;**Equal**: Returns `true` if **Left** is equal to **Right**. Returns `false` otherwise. This condition corresponds to `==` in C#.<br/>&#8226;**Not Equals**: Returns `true` if **Left** is not equal to **Right**. Returns `false` otherwise. This condition corresponds `!=` in C#.<br/>&#8226;**Less**: Returns `true` if **Left** is less than **Right**. Returns `false` otherwise. This condition corresponds to `<` in C#.<br/>&#8226;**Less Or Equal**: Returns `true` if **Left** is less than or equal to **Right**. Returns `false` otherwise. This condition corresponds to `<=` in C#.<br/>&#8226;**Greater**: Returns `true` if **Left** is greater than **Right**. Returns `false` otherwise. This condition corresponds to `>` in C#.<br/>&#8226;**Greater Or Equal**: Returns `true` if **Left** is greater than or equal to **Right**. Returns `false` otherwise. This condition corresponds to `>=` in C#. |

## Operator properties

| **Input** | **Type** | **Description**                                |
| --------- | -------- | ---------------------------------------------- |
| **Left**  | float    | The value on the left side of the comparison.  |
| **Right** | float    | The value on the right side of the comparison. |

| **Output** | **Type** | **Description**               |
| ---------- | -------- | ----------------------------- |
| **o**      | bool     | The result of the comparison. |