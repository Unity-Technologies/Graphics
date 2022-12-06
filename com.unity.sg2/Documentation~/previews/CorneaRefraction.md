## Description
Calculates the refraction of the view ray in object space to return the object space position.

## Input
**Position OS** -  Position of the fragment to shade in object space.
**View Direction OS** -  Direction of the incident ray in object space.
**Cornea Normal OS** -  The normal of the eye surface in object space.
**Cornea IOR** -  The index of refraction of the eye (1.333 by default).
**Iris Plane Offset** -  Distance between the end of the cornea and the iris plane.

## Output
**Refracted Position OS** - Position of the refracted point on the iris plane in object space.