# Verification / 検証記録

Date: 2026-10-14 (JST).

## Environment

| Dependency | Tested version |
| --- | --- |
| Unity | 2022.3.22f1 |
| VRChat Avatars SDK | 3.10.5 |
| lilToon | 2.3.4 |
| NDMF | 1.15.0-beta.1 |
| Modular Avatar | 1.19.0-alpha.1 |

Editor tests run in `VerificationProject~` using the installed dependency packages and the source package by local reference. This avoids closing the user's existing Unity editor.

## Automated coverage

- Prefab instance insertion, duplicate prevention, invalid selection, Undo, serialized prefab override.
- Original material values retained, copies cached, settings snapshot independence.
- Parameters, color-only override, mode, rim, fade disable, invalid values, no overrides.
- Independent distance (XYZ) and backface (W) overrides in all four ON/OFF combinations, retaining original values for disabled items.
- Normal, Cutout, Transparent, Multi shader compatibility and Multi `_FADING_ON` keyword.
- Standard, Lite and unrelated `_DistanceFade` shader rejection.
- Full NDMF build of inactive renderer and Material Swap animation.
- Object exclusion and include-only filters.
- Material-only include targeting a swap material absent from renderer assignments; original `.mat` file contents unchanged.
- BlendTree and AnimatorOverrideController Material Swap.
- Disabled or deleted controller behavior.
- MA Merge Armature and MA Object Toggle with inactive outfit, including target-scope preservation after merging.

Result: **32 passed, 0 failed, 0 skipped** in Unity 2022.3.22f1, including the independent backface override. The latest result is `Logs/DistanceFade-backface-tests.xml` in the development project. The temporary verification project was removed after testing; the result XML and log are retained.

## Remaining manual verification

- Real VRChat SDK Build & Test/upload and the visible fade effect in VRChat.
- User-facing Inspector layout in Japanese and English environments.
- NDMF editor preview rendering. This release provides diagnostics; an independent live preview is not implemented.
- Interaction with later optimization plugins and custom lilToon derivatives.
- Hosted VPM repository installation/update in VCC. The package ZIP and VPM metadata are prepared, but no repository/release URL is published.
- Lowest supported dependency versions; automated tests use the installed versions above.

These manual checks are required before claiming every v1.0.0 completion criterion is satisfied.
