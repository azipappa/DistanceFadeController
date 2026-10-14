# Usage / 使用方法

Select a single scene avatar root with VRCAvatarDescriptor. Use the Hierarchy context menu `DistanceFadeController > Add Controller`.
The installed object is a connected prefab instance; edits are per-avatar overrides, and insertion supports Undo.

Each override checkbox controls one shader property. Disable the top-level override to leave all source material references unchanged. Disable fade with parameter override enabled to set strength to zero.

Start and end distances may be reversed, but cannot be equal. Strength and backface force are 0–1. Fade mode 0 is vertex distance and 1 is object position. Missing optional shader properties are skipped with warnings.

Object filters include descendants. Renderer and material filters are additional matching targets. Inactive outfits are included. Unsupported shaders are skipped; a generic shader with only `_DistanceFade` is not accepted.

Diagnostics count current renderer assignments; build-time additions and animation materials are resolved during NDMF processing. There is no destructive scene-material preview. To inspect final material assignments, use NDMF Manual Bake on a copy.

Only one enabled controller is supported per avatar. Do not animate the override properties themselves unless the animation is intended to take precedence. Unresolved or unsupported material swap bindings produce detailed warnings.

Delete the prefab to remove all overrides from later builds.
