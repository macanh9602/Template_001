# AMZG Outer Wall Helper

Reusable Unity helper for creating outer wall meshes for grid and polygon-based games.

## Install

Use either approach:

- Copy this `OuterWall` folder into a Unity project's `Packages` folder or `Assets` folder.
- Add it as a local package from Unity Package Manager using this folder path.

## Main Component

Add `OuterWallGenerator` to a GameObject. The component automatically requires a `MeshFilter` and `MeshRenderer`.

Namespace:

```csharp
using AMZG.Helpers.OuterWall;
```

## Source Modes

- `ManualPoints`: Uses child/helper transforms as editable polygon points.
- `GridRect`: Builds a rectangular grid outline from width, height, and cell size.
- `GridMask`: Builds the largest outer contour from occupied grid cells.
- `PathPoints`: Uses serialized `Vector2` path points directly.

## Options

- `height`: Vertical wall height.
- `thickness`: Wall band thickness. Set to `0` for solid prism extrusion like the original FreeGroundCreator tool.
- `cornerRadius`: Radius for rounded corners.
- `cornerSegments`: Arc resolution for rounded corners.
- `generateTop`: Creates the wall top cap.
- `generateBottom`: Creates bottom faces.
- `generateCollider`: Adds or updates a `MeshCollider`.
- `uvScale`: Controls generated UV density.
- `rebuildInEditor`: Rebuilds automatically when inspector values change.
- `material`: Optional material assignment.

## Editor Controls

The custom inspector provides:

- `Build`
- `Clear Points`
- `Add Point` for `ManualPoints`
- Scene handles for manual point editing
- Scene grid preview for `GridRect` and `GridMask`

## Notes

This helper was extracted from the FreeGroundCreator research in WoolLoop and renamed into the `AMZG.Helpers.OuterWall` namespace so it can coexist with older project scripts.

For very concave shapes, lower `thickness` if the inner offset polygon becomes invalid.
