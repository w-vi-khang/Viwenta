# Handoff

> Status: in progress
> Updated: 2026-11-07 00:40
> Branch: init/unity-project

## Checkpoint
- Doing: chunk streaming for the Viwenta voxel game; step 2 (chunk data, block table, prebuilt-face meshing, GPU-friendly output) is coded and tested but NOT committed.
- Next step: split transparent blocks into a second submesh (ChunkMeshData + ChunkMeshConverter), then write a simple shader for the texture array and packed vertex.
- Waiting on: user decision to commit step 2 (37 changed/untracked paths, including ~40 Unity .meta files that must be committed with the code).
- Files being edited: none
- Open questions: bevel rule (edge/corner "convex" test) not yet approved; tier thresholds and bevel radius; per-chunk vs per-column mesh unit; instancing API for objects; entity ownership across column edges.

## Goal
Build a voxel world for Unity 6000.4.8f1 (PC and mobile): constants and column shape services, streaming around one or more POVs, voxel/object/entity/metadata per chunk, data-driven definitions, render split from streaming. Meshes are assembled from prebuilt face shapes (made in Blender) chosen by neighbour-derived keys, not computed at runtime. Designs: Docs/Design/ChunkStreaming.md and Docs/Design/Rendering.md.

## Done (with verification results)
- Step 1 (commit 1ab380f): VoxelConstants, ColumnShapeService, asmdefs, test framework.
- Step 2 (uncommitted, working tree): Core (no UnityEngine): Direction/FaceFrame, BlockDefinition + BlockSkin(layer, tint), BlockRegistry (id 0 = air, duplicate ids rejected), ChunkData (uniform/array, Dirty/Modified), ChunkNeighborhood (26 neighbours, -16..31), FaceShape, FaceVariantKey (8-bit key from neighbours, D4 symmetry reduction), FaceShapeLibrary ((canonical key, tier) with fallback to lower tier then flat), ChunkMeshData (Uvs are u,v,layer), ChunkMeshBuilder (face culling, per-block shape override), MeshStats. Runtime asmdef: ChunkMeshConverter (24-byte packed vertex, order position/normal/colour/uv required by Unity, 16-bit indices when <= 65,536 vertices), HalfConverter, BlockDefinitionAsset, BlockRegistryAsset, FixedChunkDemo.
- Docs/Design/Rendering.md written; ChunkStreaming.md status line updated.
- `[run]` `sh <Plugin root>/scripts/run.sh verify_run --all` -> test exit 0; 89 tests, 89 passed, 0 failed (EditMode).
- Bug found by a test and fixed: Unity reorders vertex attributes (Position, Normal, Color, TexCoord0); the packed struct had UV before colour.

## Remaining
1. Commit step 2 with its .meta files (ask the user first; nothing pushed so far).
2. Transparent submesh: ChunkMeshData gets a second index list for non-opaque blocks; ChunkMeshConverter writes 2 submeshes; opaque drawn first. Today everything is in submesh 0 (gap against Rendering.md section 3).
3. Shader for texture array + packed vertex (half position, float3 uv) with a cheaper variant for far tiers; then a demo scene/material/registry asset to view FixedChunkDemo in the Editor (not verified visually yet).
4. Blender script to generate face variants (bevel radius, segments per tier) and an Editor tool to bake them into FaceShapeLibrary. Currently only the flat shape exists.
5. Steps 3-7 of ChunkStreaming.md: terrain generation, streaming around one POV, multiple POVs + priority, block edits + re-meshing, region save/load.
6. Not applied yet: occlusion culling (deferred, needs profile), per-chunk Renderer manager, mobile device measurement.

## Key decisions & reasons
- Prebuilt shapes shared by all blocks; block skin (texture array layer + tint) applied at attach time; optional per-block shape override. Avoids shapes x block types explosion.
- Face variant key comes from neighbours (edge bevelled when the side voxel and the voxel in front of it are empty; corner rounded when both edges bevelled and diagonal pair empty), so adjacent faces never disagree. Rule is mine, not yet approved by the user.
- Detail tiers (0 flat ... higher rounded) chosen by chunk distance, capped lower on mobile; rebuild on tier change, assigned by zone ring.
- Texture2DArray instead of atlas (no mip bleed, free tiling); mobile support unverified.
- Vertex packed to 24 bytes; chunk-local half floats resolve 1/64 or finer; Core data stays float so it can run on background threads.
- Rebuild a whole chunk on edit (cheap); in-place face patching deferred to step 6 pending a measured rebuild time.
- Numbers about vertex counts, draw calls and memory are estimates, not measurements.

## Related files
- Docs/Design/ChunkStreaming.md, Docs/Design/Rendering.md
- Assets/_Project/Scripts/Voxel/Core/ (all step 1 and step 2 Core files)
- Assets/_Project/Scripts/Voxel/Runtime/
- Assets/_Project/Scripts/Voxel/Tests/EditMode/ (ChunkDataTests, BlockRegistryTests, FaceVariantTests, ChunkMeshBuilderTests, ChunkMeshConverterTests, ColumnShapeServiceTests)
- .ai/project.json (verify.test runs Unity batchmode)

## Continue prompt
```text
Continue the unfinished work in .ai/handoff.md — Viwenta voxel chunk streaming, step 2 follow-ups.
Next step: split transparent blocks into a second submesh (ChunkMeshData, ChunkMeshConverter, with tests), then a simple texture-array shader; commit step 2 first if the user agrees.
Read the handoff and related files first, summarize them for me in 3 lines, then start.
Suggested model for this step: Sonnet · high (AGENTS.md §10.2).
```
