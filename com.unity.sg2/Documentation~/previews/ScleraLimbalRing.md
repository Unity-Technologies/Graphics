## Description
Calculates the intensity of the Sclera ring, a darkening feature around the outside of the iros of eyes

## Input
**Position OS** - Position of the current fragment to shade in object space.
**ViewDirectionOS** - a normalized vector from camera to position.
**Iris Radius** -  The radius of the Iris in the used model. For the default model, this value should be 0.225
**Limbal Ring Size** - the width of the limbal ring
**Limbal Ring Fade** - the opacity if the limbal ring
**Limbal Ring Intensity** - the strength of the limbal ring

## Output
**Limbal Ring Factor** - Intensity of the limbal ring (blackscale).