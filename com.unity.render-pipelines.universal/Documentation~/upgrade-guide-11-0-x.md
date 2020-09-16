# Upgrading to version 11.0.x of the Universal Render Pipeline

This page describes how to upgrade from an older version of the Universal Render Pipeline (URP) to version 11.0.x.

## Upgrading from URP 7.2.x and later releases

1. URP 11.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.

### Shadow Normal Bias

In 11.0.x the formula used to apply Shadow Normal Bias has been slightly fix in order to work better with punctual lights.
As a result, to match exactly shadow outlines from earlier revisions, the parameter might to be adjusted in some scenes. Typically, using 1.4 instead of 1.0 for a Directional light is usually enough.

## Upgrading from URP 7.0.x-7.1.x

1. Upgrade to URP 7.2.0 first. Refer to [Upgrading to version 7.2.0 of the Universal Render Pipeline](upgrade-guide-7-2-0).

2. URP 8.x.x does not support the package Post-Processing Stack v2. If your Project uses the package Post-Processing Stack v2, migrate the effects that use that package first.
