# Physical Light units

HDRP uses Physical Light Units (PLU) for its lighting. These units are based on real-life light measurements, like those you see on light bulb packaging or a photographic light meter.

## Units

<a name="Candela"></a>

#### Candela:

The base unit of [luminous intensity](Glossary.html#LuminousIntensity) in the International System of Units. For reference, a common wax candle emits light with a luminous intensity of roughly 1 candela.

<a name="Lumen"></a>

#### Lumen:

The unit of [luminous flux](Glossary.html#LuminousFlux). Measures the total quantity of visible light a source emits. A light source emitting 1 [candela](#Candela) of luminous intensity from an area of 1 steradian has a luminous flux of 1 lumen.

<a name="Lux"></a>

#### Lux (lumen per square meter):

The unit of [illuminance](Glossary.html#Illuminance). A light source that emits 1 lumen of [luminous flux](Glossary.html#LuminousFlux) onto an area of 1 square meter has an illuminance of 1 lux.

<a name="Luminance"></a>

#### Luminance (candela per square meter):

Measures the apparent brightness of light either emitted from a light source, or reflected off a surface, to the human eye. A light source that emits 1 candela of [luminous intensity](Glossary.html#LuminousIntensity) onto an area of 1 square meter has a luminance of 1 candela per square meter.

<a name="EV"></a>

#### Exposure value (EV):

A value that represents a combination of a camera's shutter speed and f-number. It is essentially a measurement of exposure such that all combinations of shutter speed and f-number that yield the same level of exposure have the same EV. HDRP Lights can use **EV<sub>100</sub>**, which is EV with a 100 International Standards Organisation (ISO) film.

## Light intensities

### Natural

Light measurements from natural sources in different conditions:

| Illuminance (lux) | Natural light level                               |
| ----------------- | ------------------------------------------------- |
| 120 000           | Very bright sunlight.                             |
| 110 000           | Bright sunlight.                                  |
| 20 000            | Blue sky at midday.                               |
| 1 000 - 2 000     | Overcast sky at midday.                           |
| < 1               | Moonlight with a clear night sky.                 |
| 0.002             | Starry night without moonlight. Includes airglow. |

### Artificial

Approximate light measurements from artificial sources:

| Luminous flux (lumen) | Source                                                       |
| --------------------- | ------------------------------------------------------------ |
| 12.57                 | Candle light.                                                |
| < 100                 | Small decorative light, such as a small LED lamp.            |
| 200 - 300             | Decorative lamp, such as a lamp that does not provide the main lighting for a bright room. |
| 400 - 800             | Ceiling lamp for a regular room.                             |
| 800 - 1 200           | Ceiling lamp for a large brightly lit room.                  |
| 1 000 - 40 000        | Bright street light.                                         |

### Indoor

Architects use these approximate values as a guide when designing rooms and buildings for functional use:

| Illuminance (lux) | Room type                  |
| ----------------- | -------------------------- |
| 150 - 300         | Bedroom.                   |
| 300 - 500         | Classroom.                 |
| 300 - 750         | Kitchen.                   |
| 300 - 500         | Kitchen Counter or Office. |
| 100 - 300         | Bathroom.                  |
| 750 lux - 1 000   | Supermarket.               |
| 30                | City street at night.      |

For more examples of indoor light levels see Archtoolbox’s web page on [Recommended Lighting Levels in Buildings](https://www.archtoolbox.com/materials-systems/electrical/recommended-lighting-levels-in-buildings.html).