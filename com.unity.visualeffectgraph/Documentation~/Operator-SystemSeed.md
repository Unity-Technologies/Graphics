# System Seed

Menu Path : **Operator > BuiltIn > System Seed**

The **System Seed** Operator outputs the internal Visual Effect system seed. The Visual Effect Graph uses this value to initialize a random number generator per component. The system seed is generally constant but it can regenerate, if you enable **Reseed on Play** in the Visual Effect component, when a new play event triggers.

## Operator properties

| **Output**     | **Type** | **Description**          |
| -------------- | -------- | ------------------------ |
| **systemSeed** | uint     | The current system seed. |
