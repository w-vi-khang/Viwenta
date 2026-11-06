# Chunk Streaming — Design

Status: design only, no code yet. Items marked **[open]** are undecided. Items marked **[unverified]** were not checked against docs or a run.

## 1. Constants service (`VoxelConstants`)

A static class of `const` values, free of `UnityEngine` dependencies, callable from every assembly and thread.

| Name | Value | Note |
|---|---|---|
| `ChunkSize` | 16 | 2^4, so local index can be `x \| z<<4 \| y<<8` and world→chunk uses bit shifts |
| `ColumnChunkCount` | 24 | chunks stacked in one column |
| `ColumnHeightBlocks` | 384 | derived |
| `VoxelsPerChunk` | 4096 | derived |
| `VoxelsPerColumn` | 98304 | derived |

Structural constants (above) are compile-time and are written into every save header; changing one requires a migration. Tunables (radius, budgets, weights) are data-driven (section 8), never hardcoded.

## 2. Column shape service (`ColumnShapeService`)

Pure, stateless, thread-safe, no `UnityEngine` use. Input: center `(cx, cz)` and radius `R`. Offsets are relative to the center and cached per `R`; the center is added at use time.

Definitions (offsets `x, z` relative to the center):

- `Shape`: `x² + z² <= R²` and `|x² − z²| != R²` (the second term removes the four single-cell axis tips `(±R,0)`, `(0,±R)`).
- `InsideBorder`: cells of `Shape` with `x² + z² >= (R−1)²`.
- `Core`: `Shape − InsideBorder`.
- `OutsideBorder`: cells adjacent to `InsideBorder` that are not in `Shape`. Adjacency is the plus shape (4 directions), diagonals excluded.

Verified by a run (Python, not committed):

| R | Disc | Shape | Core | InsideBorder | OutsideBorder | Core cells missing a 4-neighbour in Shape |
|---|---|---|---|---|---|---|
| 5 | 81 | 77 | 45 | 32 | 32 | 0 |
| 6 | 113 | 109 | 69 | 40 | 36 | 0 |
| 7 | 149 | 145 | 109 | 36 | 40 | 0 |

R=5 shape (C = Core, I = InsideBorder, O = OutsideBorder):

```
....OOOOOOO....
...OIIIIIIIO...
..OIICCCCCIIO..
..OICCCCCCCIO..
..OICCCCCCCIO..
..OICCCCCCCIO..
..OICCCCCCCIO..
..OICCCCCCCIO..
..OIICCCCCIIO..
...OIIIIIIIO...
....OOOOOOO....
```

Also provides a diff between two shapes (same or different `R`, different centers): columns to load and columns to release. The service is generic ("a disc of cells around a point") and reusable for other systems.

Radius by altitude: `R` grows from 5 to 7 as the POV's Y rises. It is a step function of Y with thresholds and hysteresis in config. **[open]** Threshold values to be tuned.

Known limit: with 4-neighbour borders, vertex ambient occlusion that samples diagonal columns is not guaranteed to find data at `Core` edges at R=7 **[unverified for this formula]**. Face culling needs only 4 neighbours and is unaffected.

## 3. Zones

| Zone | Voxel data | Mesh and draw | Purpose |
|---|---|---|---|
| Core | yes | yes | Visible area |
| InsideBorder | yes | no | Provides neighbour blocks so Core can decide visible faces |
| OutsideBorder | yes (as needed) | no | Padding for InsideBorder: consistent generation across column edges |
| Beyond | no | no | Not loaded |

## 4. Data model

The unit of loading, saving and priority is the **chunk column** (16 x 384 x 16, 24 chunks). The unit of meshing is the **chunk** (16^3).

Each chunk holds:

- **Voxel:** compact block-ID array (for example `ushort`) referencing a block definition table. Fully empty or single-type chunks store a flag instead of an array.
- **Object:** placed things with their own shape and data (chests, trees, furniture).
- **Entity:** dynamic things (mobs, dropped items).
- **Metadata:** format version, `dirty`, `modified`, lifecycle state, per-voxel auxiliary data.

**[open]** Which column owns an entity standing on a column edge, and how it is restored when its column unloads. **[open]** Whether objects and entities share a file with voxels.

## 5. Pipeline

```
1. Read user data        -> POV position, seed, version, saved modifications
2. Locate POV column     -> (cx, cz) = floor(position / 16)
3. Radius by altitude    -> ColumnShapeService -> Core / InsideBorder / OutsideBorder
4. Load data             -> from file if present, else generate
5. Analyse voxels        -> solid / empty / transparent, visible faces (not drawn yet)
6. Build render structure (separate from streaming)
7. Mesh and upload       -> Core only
```

Step 5 produces an intermediate structure (visible faces per chunk), not a Unity `Mesh`, so it can be computed off the main thread.

Two layers, communicating by events or queues:

- **Streaming (data):** which columns are in memory and each chunk's state. Knows nothing about GPU or meshes.
- **Render:** consumes the visible-face structure; owns meshes, GameObjects, pooling, show/hide. Knows nothing about files or generation.

## 6. Multiple POVs and priority

- A POV is an abstraction: position, facing, `R`. A player, an observer camera or anything else can be one.
- The set of needed columns is the union over all POVs. A column is released only when no POV needs it (reference count by POV). A column's zone follows the strongest zone any POV assigns it.
- Priority of a pending column for one POV:
  - closer to the POV first;
  - in front of the POV first (dot product of facing and direction to the column);
  - Core before InsideBorder before OutsideBorder;
  - columns in memory but missing meshes before columns not loaded.
- A column's priority is its maximum across POVs. The queue is re-sorted when a POV moves or turns past a threshold, not every frame. Weights are config.

## 7. Threading

| Work | Where |
|---|---|
| File read, decompression | Background |
| Terrain generation | Background (Job System + Burst if installed **[unverified]**) |
| Voxel analysis, visible faces | Background |
| Shape computation, priority sort | Background or main (light) |
| `Mesh`, GameObject, collider creation, show/hide | Main thread only, with a per-frame ms budget |
| File write | Background, atomic (temp file then rename) |

Rules: background work never touches `UnityEngine` objects; concurrency and per-frame budgets are capped; every background task carries a cancellation token and is re-checked on return ("is this column still needed?"); results return to the main thread through a thread-safe queue.

## 8. Data-driven definitions

- **Blocks:** ID, name, texture, solid or transparent, shape, physics properties.
- **Streaming:** `R` min and max, altitude-to-`R` table, concurrency, per-frame budgets.
- **Priority:** weights for distance, facing, zone.
- **Generation:** seed, noise parameters, biomes, structure list.
- **Objects and entities:** type definitions, defaults, spawn rules.

IDs are stable and never reused after removal, because saves store IDs.

## 9. Open items

- Entity ownership across column edges (section 4).
- Object and entity storage layout (section 4).
- Structures spanning several columns: proposed deterministic generation from world coordinates, details undecided.
- Altitude thresholds for `R` (section 2).
- Vertex AO at R=7 (section 2).
- Greedy meshing, propagated lighting, LOD: out of scope for this document.

## 10. Suggested build order

1. `VoxelConstants` and `ColumnShapeService` with a check that reproduces the table in section 2.
2. Chunk data and block definitions; mesh one fixed chunk.
3. Deterministic terrain generation; show a small area.
4. Streaming around one POV (load/unload, pooling, budgets).
5. Multiple POVs and priority.
6. Block edits and re-meshing.
7. Save and load by region, with a version.
