## Description
Calculates a blend of values between input **A** and input **B** using the value of input **T**.

## Inputs
**A** - Out will be this value when T is zero.
**B** - Out will be this value when T is one.
**T** - The blend value.  Will return A when this is 0 and B when this is 1.


## Output
**Out** - A * (1-T) + B * T.