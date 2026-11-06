# Rendering — GPU cost design

Status: partly built (voxel chunk mesh, packed vertex, texture-array skin). Items marked **[open]** are undecided. Items marked **[unverified]** were not checked against docs or a run. Every size or count marked **[estimate]** is arithmetic or a guess, not a measurement; measure on a real mobile device before relying on it.

Targets: PC and mobile. Companion to `ChunkStreaming.md`: streaming decides which data is in memory, this document decides what the GPU is asked to draw.

## 1. What costs GPU time

Reduce the work per frame, not only polygons:

1. Vertices and triangles processed.
2. Draw calls and state changes.
3. Overdraw (pixels shaded more than once).
4. Shader work per vertex and per pixel.
5. Data sent from CPU to GPU.
6. Number of things that are drawn at all.

The CPU and the world systems filter, group and prepare data; the GPU only shades what it is handed.

## 2. One render path per kind of thing

| Kind | Path |
|---|---|
| Voxel terrain and blocks | One mesh per chunk, built from prebuilt face shapes (section 3). No GameObject per block. |
| Objects (rocks, trees, decoration) | Shared mesh and material, instanced, own LOD (section 5). |
| Entities (creatures) | Own meshes; skinned meshes with animation LOD. Later step. |
| Effects | Particles and GPU effects. Later step. |

Layers, in order: world data → visibility management → renderers (voxel, object, entity, effect) → GPU. Streaming never touches meshes; renderers never touch files or generation.

## 3. Voxel renderer

- **Mesh unit:** one chunk (16^3). Only faces touching air or a transparent block exist. An empty chunk, or one buried in solid neighbours, produces no mesh and needs no renderer.
- **Faces:** chosen from the prebuilt `FaceShapeLibrary` by `FaceVariantKey` (neighbour-derived), copied into the chunk mesh. Mesh rebuild after an edit is per chunk (plus up to 3 neighbours at an edge), not per face. Patching single faces in place is **[open]**, to be decided at step 6 from a measured rebuild time.
- **Detail tier:** each prebuilt shape exists per tier (0 = flat 4 vertices, higher = rounded). The tier of a chunk follows its distance, with hysteresis so it does not flicker. Mobile caps the tier lower. Distance is enough for chunks (fixed size); screen-space size matters only for objects. Changing a chunk's tier means rebuilding it, so tiers are assigned by zone ring, not per frame. Thresholds **[open]**.
- **Texture:** one `Texture2DArray` for all blocks; a block's skin is a layer index plus a tint (`BlockSkin`). Each face maps the whole layer, so layers do not bleed under mip-mapping and tiling is free. Mobile support of texture arrays (GLES3 and later) **[unverified]**.
- **Vertex format:** 24 bytes per vertex, down from 36 as floats. Position and uv (u, v, layer) as half floats in 4-component slots, normal as 4 signed bytes, tint as 4 bytes. Chunk-local positions are 0..16, where a half float resolves 1/64 or finer, and identical inputs round identically so faces do not crack. The engine-free `ChunkMeshData` stays in floats; `ChunkMeshConverter` packs. Whether the target shader accepts this layout (half position, float3 uv) **[unverified]**: the shader is not written yet.
- **Indices:** 16-bit when a chunk has at most 65,536 vertices, 32-bit otherwise. A chunk of all-rounded faces can exceed the limit.
- **Opaque first:** transparent blocks are drawn after opaque ones; avoid stacking transparent layers and alpha-tested foliage on mobile.
- **Overdraw:** face culling already removes hidden faces. Rounded variants add vertices, not overdraw.

## 4. Draw calls and the mesh unit

One material (texture array) for all voxel chunks, so SRP Batcher or plain batching applies. With the Core of 109 columns, a few chunks per column carrying surface gives a few hundred draw calls **[estimate]**. If mobile is over budget, render one mesh per column instead of per chunk: fewer draws, coarser culling, larger rebuilds. **[open]** until measured. Do not merge the whole world: it cannot be culled.

## 5. Objects and entities (later)

- GPU instancing for repeated meshes (rocks, trees, decoration); LOD chosen by screen-space size, not only distance.
- Which instancing API to use needs a look-up against the exact Unity version (6000.4.8f1) before choosing. `DrawMeshInstancedIndirect` needs compute buffers, which older mobile GPUs lack. **[unverified]**
- Counts of rounded rocks, trees and creatures are not known. They are needed to size this. **[open]**

## 6. Visibility

- Frustum culling: one Renderer per chunk mesh, so Unity culls by bounds.
- Unity's occlusion culling is baked and static, so it does not fit a world that changes. If occlusion is needed, derive it from chunk data (for example cave visibility between chunks). Defer until a profile shows it matters. **[open]**
- GPU still shades hidden triangles before rejecting them, so cut early on the CPU where cheap.

## 7. Normal-map bevel (alternative for mobile)

Keep flat faces and fake the rounding with normals or a normal map: silhouettes stay square, vertex cost stays at tier 0. Candidate for far tiers or the mobile default. **[open]**

## 8. Measuring

- `MeshStats` totals meshes, vertices and triangles of what is built; add on build, remove on release. `FixedChunkDemo` logs it.
- In Unity: Profiler, Frame Debugger, rendering statistics (batches, SetPass calls, triangles, vertices), and the GPU profiler on a device. Reading them is a user step.
- First mobile measurement is due before tier thresholds are chosen (streaming step 4).

| Symptom | Likely cause |
|---|---|
| CPU high, GPU low | Draw calls, per-object overhead, mesh building |
| GPU high, CPU low | Shaders, vertices, overdraw, lighting |
| Both high | Too many objects, data or effects |

## 9. Not adopted

- The example LOD distance table (0-10 m, 10-30 m, ...): the source called it an example. Load radius is 5 to 7 columns (about 80 to 112 m), so no billboards.
- The source's figure of 100,000 blocks: the Core holds about 10.7 million voxels (109 columns x 98,304), of which only exposed faces are meshed.

## 10. Open items

- Tier thresholds, hysteresis, mobile cap.
- Per-chunk or per-column mesh unit; draw-call budget on a real device.
- Shader for the packed vertex and texture array (none exists yet).
- Instancing API and LOD for objects; object and entity counts.
- In-place face patching versus chunk rebuild.
- Normal-map bevel for mobile.
