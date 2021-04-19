# Age Over Lifetime

Menu Path : **Operator > Attribute > Age over Lifetime** 

The **Age Over Lifetime** Operator returns the age of a particle relative to its lifetime, as a value between 0.0 and 1.0.

```
t = age / lifetime
```

## Operator properties

| **Output** | **Type** | **Description**                                   |
| ---------- | -------- | ------------------------------------------------- |
| **t**      | float    | The age of the particle relative to its lifetime. |

## Details

If the system you use this Operator in does not include either age or lifetime, Unity uses the default attribute values instead.

- Age defaults to **0**.
- Lifetime defaults to **1**.