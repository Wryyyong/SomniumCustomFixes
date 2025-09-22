# SomniumCustomFixes
A MelonLoader plugin written to help with increasing the visual quality in games within the _AI: The Somnium Files_ series.

The development of this mod was inspired by ["AISomniumFiles2Fix"](https://codeberg.org/Lyall/AISomniumFiles2Fix), updated for the latest MelonLoader and Il2CppInterop versions, and features a lot more flexibility with its configuration.

## Supported titles
- [_AI: THE SOMNIUM FILES - nirvanA Initiative_](https://store.steampowered.com/app/1449200)
- [_No Sleep For Kaname Date - From AI: THE SOMNIUM FILES_](https://store.steampowered.com/app/2752180)

## Features
- Visual quality upgrades:
  - Automatically enables supported antialiasing on applicable `UniversalAdditionalCameraData` objects:
    - Fast-Approximate AntiAliasing (FXAA)
    - Subpixel Morphological Anti-Aliasing (SMAA)
    - **[_AINS_ only]** Temporal Anti-Aliasing (TAA)
  - Force-enabling anisotropic filtering on textures, and increasing texture filtering quality in general
  - Increasing the resolution and quality of generated shadowmaps to a configurable value, ranging from a resolution of 256<sup>2</sup> to 4096<sup>2</sup> (_AINI_)/8192<sup>2</sup> (_AINS_)
  - Greatly increasing the distance at which character models fade between levels-of-detail (LoDs)
  - Other, smaller miscellaneous adjustments
- Optional stylistic toggles:
  - Enabling/disabling the outlines around character models
- Miscellaneous toggles:
  - Enabling/disabling whether the mouse cursor appears
- Most of these additions are user-configurable, and are stored in their own configuration file--See the [Example configuration file](#example-configuration-file) section for an example

#### Planned additions for future releases
- Implementing the fixes for ultrawide displays from the original mod
  - Cannot implement until either Il2CppInterop fixes how it interacts with nullable parameters, or I find a decent workaround
- Further visual quality improvements and additional preferences to adjust them
- BepInEx support

## Visual comparisons of quality improvements

<center><i><b>AI: THE SOMNIUM FILES - nirvanA Initiative</b></i></center>

| Without mod | With mod, default settings |
| :-: | :-: |
| ![](__RepoAssets/AINI_Example1_Before.png) | ![](__RepoAssets/AINI_Example1_After.png) |
| ![](__RepoAssets/AINI_Example2_Before.png) | ![](__RepoAssets/AINI_Example2_After.png) |

<center><i><b>No Sleep For Kaname Date - From AI: THE SOMNIUM FILES</b></i></center>

| Without mod | With mod, default settings |
| :-: | :-: |
| ![](__RepoAssets/AINS_Example1_Before.png) | ![](__RepoAssets/AINS_Example1_After.png) |
| ![](__RepoAssets/AINS_Example2_Before.png) | ![](__RepoAssets/AINS_Example2_After.png) |

## Example configuration file
Instead of storing its preferences in the shared `UserData/MelonPreferences.cfg` file, this mod uses its own configuration file: `UserData/SomniumCustomFixes.ini`.

The following is an example configuration file for the _AINS_ edition of the mod, all default settings:
```ini
[Debugging]
LogVerbose = false

[Miscellaneous]
DisableMouseCursor = false

[StylisticSettings]
RenderCharacterModelOutlines = true

[QualitySettings]
# Anisotropic filtering makes Textures look better when viewed at a shallow angle.
# Possible values:
# - "Disable"
# - "Enable"
# - "ForceEnable"
AnisotropicFilteringMode = "ForceEnable"
# Defines the anisotropic filtering level of textures.
# Possible values: 0-16
# Has certain effects when AnisotropicFilteringMode is set to "ForceEnable":
# - If set to 0, Unity does not apply anisotropic filtering.
# - If set between 1-9, Unity sets the value to 9.
AnisotropicFilteringLevel = 16
# Sets how textures are filtered.
# Possible values:
# - "Point"
# - "Bilinear"
# - "Trilinear"
TextureFilteringMode = "Trilinear"
# The resolution to render shadows at, through the Universal Rendering Pipeline.
# Possible values:
# - "_256"
# - "_512"
# - "_1024"
# - "_2048"
# - "_4096"
# - "_8192"
URP_ShadowResolution = "_8192"
# The type of antialiasing to set UniversalAdditionalCameraData instances to use.
# Possible values:
# - "None"
# - "FastApproximateAntialiasing"
# - "SubpixelMorphologicalAntiAliasing"
# - "TemporalAntiAliasing"
AntialiasingMode = "TemporalAntiAliasing"
# The level of quality to use for Subpixel Morphological Anti-Aliasing.
# Has no effect unless AntialiasingMode is set to "SubpixelMorphologicalAntiAliasing".
# Possible values:
# - "Low"
# - "Medium"
# - "High"
SMAAQuality = "High"
# The level of quality to use for Temporal Anti-Aliasing.
# Has no effect unless AntialiasingMode is set to "TemporalAntiAliasing".
# Possible values:
# - "VeryLow"
# - "Low"
# - "Medium"
# - "High"
# - "VeryHigh"
TAAQuality = "VeryHigh"


```
