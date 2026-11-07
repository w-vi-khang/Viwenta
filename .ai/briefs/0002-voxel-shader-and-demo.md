# Brief 0002 — URP voxel shader, placeholder blocks and demo scene

> Status: todo
> Level: Large
> Review: full + user check
> Executor model: Sonnet · high
> Updated: 2026-11-07 07:20

## Goal
Make a voxel chunk visible in the Editor: a URP shader that reads the packed chunk vertex and the block `Texture2DArray`, an opaque and a transparent material (submesh 0 and 1), placeholder block textures made to the template of `Docs/Design/AssetTemplates.md`, and a `VoxelDemo` scene showing `FixedChunkDemo` with grass, dirt, stone and glass. Art is placeholder; the structure (template, baker, two materials) is what matters.

## Context to read
- `Docs/Design/AssetTemplates.md` — block texture template (location, file names, size 32, alpha rule, slots).
- `Docs/Design/Rendering.md:31-41` — voxel renderer section (opaque first, texture array, vertex format).
- `Assets/_Project/Scripts/Voxel/Runtime/ChunkMeshConverter.cs:20-35` — vertex layout the shader receives:
  ```csharp
  new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float16, 4),
  new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.SNorm8, 4),
  new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
  new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float16, 4),
  ```
  TexCoord0 = (u, v, layer, 0); Color = block tint; submesh 0 = opaque, submesh 1 = transparent (`OpaqueSubMesh`, `TransparentSubMesh` constants).
- `Assets/_Project/Scripts/Voxel/Runtime/FixedChunkDemo.cs` (whole file, 50 lines) — fields at lines 12-16 (`registryAsset`, `blockId`, `tier`), hill loop in `Start()`.
- `Assets/_Project/Scripts/Voxel/Runtime/BlockDefinitionAsset.cs` — serialized fields `id`, `blockName`, `isOpaque`, `top`/`side`/`bottom` (`FaceSkin { Texture2D texture; Color32 tint; }`).
- `Assets/_Project/Scripts/Voxel/Runtime/BlockRegistryAsset.cs` — serialized `blocks` (array) and `textures` (`BlockTextureLibrary`).
- `Assets/_Project/Scripts/Voxel/Runtime/BlockTextureLibrary.cs` — serialized `faceSize` (32), `filterMode`; `Array` property.
- `Assets/_Project/Scripts/Voxel/Editor/BlockTextureBaker.cs` — `public static void Bake(BlockRegistryAsset registry)`.
- `Assets/_Project/Scripts/Voxel/Editor/BlockTextureImportRules.cs` — applies import settings to `Assets/_Project/Art/Blocks/`.
- URP 17.4 reference shaders (read-only), folder `Library/PackageCache/com.unity.render-pipelines.universal@13e5115b98bf/Shaders/`: `SimpleLit.shader` (pass tags, pragmas), `ShadowCasterPass.hlsl` (lines 11-50: `_LightDirection`, `_LightPosition`, `ApplyShadowBias`, clip-space clamp), `DepthOnlyPass.hlsl`, `DepthNormalsPass.hlsl` (lines 51-76: `outNormalWS`, `_GBUFFER_NORMALS_OCT` branch). Mirror their code where this brief says "as in".

## Settled decisions (do not change)
- One shader `Viwenta/VoxelChunk` for both materials; opaque vs transparent is chosen by material properties and render queue, not by a second shader.
- Lighting: Lambert from the main light × main light shadow attenuation, plus ambient `SampleSH(normalWS)`, times albedo; fog via `MixFog`. No specular, no additional lights, no normal map, no lightmaps. Main light shadows received and cast (the user did not opt out of shadows).
- Albedo = `SAMPLE_TEXTURE2D_ARRAY(_BlockTextures, sampler_BlockTextures, uv.xy, round(uv.z))` × vertex colour × `_BaseColor`. Alpha = product of the three alphas.
- SRP Batcher compatible: every material property except textures is in `CBUFFER_START(UnityPerMaterial)`, declared once in a shared `HLSLINCLUDE` block.
- Transparent material: `_SrcBlend = SrcAlpha (5)`, `_DstBlend = OneMinusSrcAlpha (10)`, `_ZWrite = 0`, render queue 3000, override tag `RenderType = Transparent`, passes `ShadowCaster`, `DepthOnly`, `DepthNormals` disabled with `SetShaderPassEnabled`. Opaque material: `_SrcBlend = One (1)`, `_DstBlend = Zero (0)`, `_ZWrite = 1`, queue 2000, all passes enabled.
- Shader location `Assets/_Project/Art/Shaders/VoxelChunk.shader`; materials in `Assets/_Project/Materials/`; block data assets in `Assets/_Project/Settings/Blocks/`; placeholder PNGs in `Assets/_Project/Art/Blocks/`; scene `Assets/_Project/Scenes/VoxelDemo.unity`.
- Demo blocks (IDs are stable): 1 `stone` (opaque, all faces `blk_stone`), 2 `dirt` (opaque, `blk_dirt`), 3 `grass` (opaque, top `blk_grass_top`, side `blk_grass_side`, bottom `blk_dirt`), 4 `glass` (not opaque, `blk_glass`). All tints white.
- Face texture orientation: on side faces texture "up" (v = 1) is world +Y and u grows to the right of a viewer outside the face; this replaces the current uv = (u, w) of the face frame, which turns NegX and PosZ textures by 90°. Only shapes without their own `Uvs` are affected. Table (p = vertex position minus the voxel's integer corner, so each component is 0..1): PosX (pz, py); NegX (1 - pz, py); PosZ (1 - px, py); NegZ (px, py); PosY (px, pz); NegY (px, 1 - pz).
- Placeholder textures are generated, not drawn, and are temporary art (status blockout). The generator script is NOT committed.
- One-shot asset creation runs through a temporary Editor script that is deleted afterwards (same approach the project used for the URP setup); only its outputs are committed.

## Scope
- May edit/create:
  - new `Assets/_Project/Art/Shaders/VoxelChunk.shader`
  - new `Assets/_Project/Art/Blocks/blk_stone.png`, `blk_dirt.png`, `blk_grass_top.png`, `blk_grass_side.png`, `blk_glass.png`
  - new `Assets/_Project/Settings/Blocks/` (`BlockTextures.asset`, `Block_Stone.asset`, `Block_Dirt.asset`, `Block_Grass.asset`, `Block_Glass.asset`, `DemoBlockRegistry.asset`)
  - new `Assets/_Project/Materials/VoxelOpaque.mat`, `VoxelTransparent.mat`
  - new `Assets/_Project/Scenes/VoxelDemo.unity`
  - `Assets/_Project/Scripts/Voxel/Runtime/FixedChunkDemo.cs`
  - `Assets/_Project/Scripts/Voxel/Core/ChunkMeshBuilder.cs` (uv lines only, step 3b)
  - new `Assets/_Project/Scripts/Voxel/Tests/EditMode/FaceUvTests.cs`
  - new `Assets/_Project/Scripts/Voxel/Tests/EditMode/VoxelShaderTests.cs`, new `Tests/EditMode/FixedChunkDemoTests.cs`
  - `Docs/Design/Rendering.md` line 83 only (open items list)
  - `.meta` files Unity generates for the above (and for new folders)
  - this brief (Result / Test results / Blockers, Status line)
  - temporary files that are deleted before commit: the generator script (in the session scratchpad) and `Assets/_Project/Editor/TempVoxelDemoSetup.cs` (+ `.meta`, + the `Assets/_Project/Editor` folder and its `.meta` if they did not exist)
- Must not edit: everything else, in particular other scripts in `Scripts/Voxel/`, `ProjectSettings/`, `Packages/`, `Docs/Design/AssetTemplates.md`.

## Steps
1. **Shader.** Create `Assets/_Project/Art/Shaders/VoxelChunk.shader`:
   - `Shader "Viwenta/VoxelChunk"`, Properties:
     ```
     [NoScaleOffset] _BlockTextures ("Block Textures", 2DArray) = "" {}
     _BaseColor ("Tint", Color) = (1,1,1,1)
     [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
     [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
     [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
     ```
   - SubShader tags `"RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry"`; `#pragma target 3.5` in every pass (texture arrays need it).
   - `HLSLINCLUDE` with `#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"`, `TEXTURE2D_ARRAY(_BlockTextures); SAMPLER(sampler_BlockTextures);`, `CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END`.
   - Vertex input struct for all passes: `float3 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float3 uv : TEXCOORD0;` (the GPU converts half / snorm / unorm to float).
   - Pass `ForwardLit`, `Tags { "LightMode" = "UniversalForward" }`, `Blend [_SrcBlend] [_DstBlend]`, `ZWrite [_ZWrite]`, `Cull Back`. Pragmas: `multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN`, `multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH`, `multi_compile_fog`. Include `.../ShaderLibrary/Lighting.hlsl`. Vertex: world position, `TransformWorldToHClip`, `TransformObjectToWorldNormal`, fog factor `ComputeFogFactor(positionCS.z)`, pass uv and colour. Fragment: albedo per Settled decisions; `Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));` `half3 lighting = mainLight.color * (saturate(dot(normalWS, mainLight.direction)) * mainLight.shadowAttenuation) + SampleSH(normalWS);` `color.rgb = MixFog(albedo.rgb * lighting, fogFactor);` return `half4(color.rgb, albedo.a)`. Normalize `normalWS` in the fragment.
   - Pass `ShadowCaster` (`"LightMode" = "ShadowCaster"`, `ZWrite On`, `ZTest LEqual`, `ColorMask 0`, `Cull Back`): `#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW`; include `.../ShaderLibrary/Shadows.hlsl`; vertex computes the clip position with `ApplyShadowBias` and the near-plane clamp **as in** `ShadowCasterPass.hlsl:11-50`; fragment returns 0.
   - Pass `DepthOnly` (`"LightMode" = "DepthOnly"`, `ZWrite On`, `ColorMask R`): position only, fragment returns `input.positionCS.z` as in `DepthOnlyPass.hlsl`.
   - Pass `DepthNormals` (`"LightMode" = "DepthNormals"`, `ZWrite On`): `#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT`; output `outNormalWS` **as in** `DepthNormalsPass.hlsl:51-76` from the interpolated normal.
   - `FallBack "Hidden/Universal Render Pipeline/FallbackError"`.
   - Done when: Unity imports it with no shader error (checked by the test in step 6).
2. **Placeholder textures.** Write a Python 3 script in the session scratchpad (standard library only: `zlib`, `struct`; PIL is not installed) that writes 32×32 RGBA 8-bit PNGs into `Assets/_Project/Art/Blocks/`. Use a fixed `random.Random(1234)` so output is reproducible. Colours (each pixel = base ± up to 12 per channel noise, clamped):
   - `blk_stone.png`: base (128,128,128), alpha 255.
   - `blk_dirt.png`: base (121,85,58), alpha 255.
   - `blk_grass_top.png`: base (86,150,60), alpha 255.
   - `blk_grass_side.png`: rows y = 0..5 counted from the TOP of the image use grass (86,150,60), other rows dirt (121,85,58); alpha 255. (PNG row 0 is the top of the image.)
   - `blk_glass.png`: base (200,230,240) without noise; alpha 230 on the 2-pixel border, alpha 60 inside.
   Done when: 5 files exist, each 32×32. Do not commit the script.
3. **FixedChunkDemo.** In `Runtime/FixedChunkDemo.cs`:
   - Replace the field `blockId` (lines 13-14) with:
     ```csharp
     [Tooltip("Block ID of the top layer of the hill.")]
     [SerializeField] ushort surfaceBlockId = 3;
     [Tooltip("Block ID under the top layer.")]
     [SerializeField] ushort fillBlockId = 2;
     [Tooltip("Block ID of the transparent wall on the hill; 0 for none.")]
     [SerializeField] ushort glassBlockId = 4;
     ```
   - Move the hill loop into `public static ChunkData BuildHill(ushort surfaceId, ushort fillId, ushort glassId)` with this rule: per column the height is computed as today; `y == height - 1` → `surfaceId`, `y < height - 1` → `fillId`; then, when `glassId != 0`, for `x` in 2..5 at `z = 2`, set `glassId` at `y = height` and `y = height + 1` of that column (skip any `y >= ChunkSize`). `<summary>`: "Builds the demo hill: a fill core, a surface top layer and a short transparent wall."
   - `Start()` calls `BuildHill(surfaceBlockId, fillBlockId, glassBlockId)`.
   - After assigning the mesh, when `GetComponent<MeshRenderer>().sharedMaterials.Length < 2` → `Debug.LogWarning("FixedChunkDemo: the MeshRenderer needs 2 materials (opaque, transparent); transparent faces will not be drawn.", this);`.
   - Update the class `<summary>` second sentence to: "Add it to an empty GameObject, assign a registry, and give the MeshRenderer two materials: opaque (submesh 0) then transparent (submesh 1)."
3b. **Face uv orientation.** `Core/ChunkMeshBuilder.cs`, in `AppendFace`, the lines `float tu = shape.Uvs != null ? shape.Uvs[2 * i] : u;` and `float tv = shape.Uvs != null ? shape.Uvs[2 * i + 1] : w;`: keep the shape-uv branch; otherwise compute `px, py, pz` = the three position values just added minus `vx, vy, vz`, and pick (tu, tv) from the table in Settled decisions with a `switch (direction)`. Update the method's `<summary>` sentence about uvs to: "Uvs come from the shape, or else from the vertex position on the face so that texture up is world up on side faces; the third component is the skin layer." Done when: existing tests still pass and step 7b passes.
4. **Temporary setup script.** Create `Assets/_Project/Editor/TempVoxelDemoSetup.cs` (no asmdef → Assembly-CSharp-Editor, can see `Viwenta.Voxel` and `Viwenta.Voxel.EditorTools`) with `public static void Run()` that, in order:
   1. `AssetDatabase.ImportAsset` each PNG with `ImportAssetOptions.ForceUpdate` (import rules apply).
   2. Creates folder `Assets/_Project/Settings/Blocks` if missing; creates `BlockTextures.asset` (`BlockTextureLibrary`, defaults), the 4 `BlockDefinitionAsset`s and `DemoBlockRegistry.asset` per Settled decisions, setting private fields through `SerializedObject` / `FindProperty` (`"id"`, `"blockName"`, `"isOpaque"`, `"top"`→`"texture"`, `"side"`→`"texture"`, `"bottom"`→`"texture"`, `"blocks"`, `"textures"`) and `ApplyModifiedPropertiesWithoutUndo()`.
   3. `BlockTextureBaker.Bake(registry)`.
   4. Creates `VoxelOpaque.mat` and `VoxelTransparent.mat` with `Shader.Find("Viwenta/VoxelChunk")` (throw `InvalidOperationException("Shader Viwenta/VoxelChunk not found.")` if null), sets `_BlockTextures` to the library's `Array`, and the blend / ZWrite / queue / tag / pass settings from Settled decisions.
   5. Creates a new scene (`EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single)` — gives camera + directional light), positions the camera at (8, 18, -14) looking at (8, 4, 8), adds GameObject `FixedChunk` at the origin with `MeshFilter`, `MeshRenderer` (`sharedMaterials = { opaque, transparent }`) and `FixedChunkDemo` (`registryAsset` = the registry via `SerializedObject`), saves to `Assets/_Project/Scenes/VoxelDemo.unity`.
   6. `AssetDatabase.SaveAssets()`; `Debug.Log("TempVoxelDemoSetup: done.")`.
   Run it: `"/Applications/Unity/Hub/Editor/6000.4.8f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath "$(pwd)" -executeMethod TempVoxelDemoSetup.Run -logFile <scratchpad>/demo-setup.log` (another project may be open in the Editor; this project must not be). Check the log has `TempVoxelDemoSetup: done.` and no `error CS` / `Exception`. Then delete the script, its `.meta`, and the `Assets/_Project/Editor` folder + `.meta` (it does not exist in the repo today).
5. **Docs.** `Docs/Design/Rendering.md:83`: replace `- Shader for the packed vertex and texture array (none exists yet).` with `- Shader \`Viwenta/VoxelChunk\` exists (main light, SH ambient, shadows, fog); a cheaper variant for far tiers is still open.` (the backticks around the shader name are literal Markdown).
6. **Tests — `Tests/EditMode/VoxelShaderTests.cs`** (namespace `Viwenta.Voxel.Tests`; loads the committed assets with `AssetDatabase.LoadAssetAtPath`, read-only; does not modify them):
   - `Shader_CompilesWithoutErrors`: `Shader.Find("Viwenta/VoxelChunk")` not null and `UnityEditor.ShaderUtil.ShaderHasError(shader)` false.
   - `Shader_HasForwardShadowAndDepthPasses`: the set of `LightMode` tags over all passes (via `shader.FindPassTagValue(subshader 0, pass, new ShaderTagId("LightMode"))` and `shader.GetPassCountInSubshader(0)`) contains `UniversalForward`, `ShadowCaster`, `DepthOnly`, `DepthNormals`.
   - `Materials_OpaqueAndTransparentAreConfigured`: opaque renderQueue 2000, `_ZWrite` 1, ShadowCaster enabled; transparent renderQueue 3000, `_ZWrite` 0, `_SrcBlend` 5, `_DstBlend` 10, `GetShaderPassEnabled("ShadowCaster")` false, `GetTag("RenderType", false)` = "Transparent"; both have a non-null `_BlockTextures`.
   - `DemoRegistry_GrassUsesDistinctTopSideBottomLayers`: `DemoBlockRegistry.asset` `.Build()`; block 3 has `Top.Layer`, `Side.Layer` distinct and `Bottom.Layer` equal to block 2's `Top.Layer`; block 4 `IsOpaque` false.
7. **Tests — `Tests/EditMode/FixedChunkDemoTests.cs`** (engine-free registry built in code with the 1-skin constructor, ids 2/3/4, 4 non-opaque):
   - `BuildHill_TopLayerIsSurfaceAndBelowIsFill`: column (7,·,7): highest non-air block = surface id, the one below = fill id.
   - `BuildHill_GlassWall_GoesToTransparentSubMesh`: mesh built with `ChunkMeshBuilder.Build` has `TransparentIndices.Count > 0`; with `glassId = 0` it has 0 and the chunk contains no id 4.
7b. **Tests — `Tests/EditMode/FaceUvTests.cs`:** `AppendFace_FlatFaces_TextureUpIsWorldUpAndUGrowsToViewerRight`: for each of the 6 directions, `ChunkMeshBuilder.AppendFace(mesh, FaceShape.Flat, 0, direction, 0, 0, 0, BlockSkin.Default)` on a fresh mesh; for every vertex, (u, v) equals the table value computed from its position; additionally for the 4 side directions the vertex with position y = 1 has v = 1.
8. **Verify and commit.** Run the commands below. One commit: `Add URP voxel shader, placeholder blocks and demo scene`, with the co-author line your session gives. Not pushed.

## Done criteria
- `verify_run --all` exits 0; results XML `failed="0"`, total = 113 + the 7 new tests, all new names `Passed`.
- `grep -c 'error CS' .ai/.state/unity-test.log` → 0; the setup log contains `TempVoxelDemoSetup: done.`
- `git ls-files Assets/_Project/Editor` is empty and the generator script is not in the repo.
- Every new asset in Scope has its `.meta`; `git status --short` clean after the commit.
- `BlockTextures.asset` contains a sub-asset named `BlockTextures_Array` (`grep -c 'BlockTextures_Array' Assets/_Project/Settings/Blocks/BlockTextures.asset` ≥ 1).

## Tests after completion

### Parts to test
- Shader compile · `Shader_CompilesWithoutErrors` · pass.
- Shader passes · `Shader_HasForwardShadowAndDepthPasses` · pass.
- Materials (opaque/transparent settings, texture assigned) · `Materials_OpaqueAndTransparentAreConfigured` · pass.
- Placeholder assets + bake through the template (grass 3 slots, dirt reuse, glass non-opaque) · `DemoRegistry_GrassUsesDistinctTopSideBottomLayers` · pass.
- `FixedChunkDemo.BuildHill` layers · `BuildHill_TopLayerIsSurfaceAndBelowIsFill` · pass.
- Face uv orientation (all 6 directions) · `AppendFace_FlatFaces_TextureUpIsWorldUpAndUGrowsToViewerRight` · pass; existing `Build_FlatFace_CoversTheFullUvSquare` and `AppendFace_*` tests still pass.
- Glass to submesh 1 / no glass case · `BuildHill_GlassWall_GoesToTransparentSubMesh` · pass.
- Placeholder PNG format · `file Assets/_Project/Art/Blocks/*.png` · each "32 x 32, 8-bit/color RGBA".
- Import rules applied to the PNGs · `grep -h 'isReadable\|sRGBTexture\|filterMode\|enableMipMap' Assets/_Project/Art/Blocks/*.png.meta | sort | uniq -c` · each of the 5 has `isReadable: 1`, `sRGBTexture: 1`, `enableMipMap: 1`, `filterMode: 0`.
- Regression · all 113 existing tests · pass unchanged.
- Demo scene in the Editor · user check (below).

### Commands the executor runs
1. Generator script → `ls Assets/_Project/Art/Blocks/*.png | wc -l` → 5.
2. Setup batchmode command (step 4) → `grep -c 'TempVoxelDemoSetup: done.' <log>` → 1; `grep -c 'error CS\|Exception' <log>` → report the number and quote any line found.
3. `sh "<Plugin root>/scripts/run.sh" verify_run --all` → exit 0.
4. `grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' .ai/.state/unity-test-results.xml | head -1`; `grep -c 'error CS' .ai/.state/unity-test.log`.
5. The two checks in "Parts to test" for PNG format and `.meta` import settings.
6. `git status --short` before and after the commit.

### User tests
Open `Assets/_Project/Scenes/VoxelDemo.unity` in the Unity Editor and press Play:
- The hill shows green grass on top, side faces with the green band at the top on all four sides (none sideways), dirt underneath; no pink (missing shader) surfaces.
- A short wall of light-blue glass stands near one corner; terrain is visible through it; the glass casts no shadow.
- The hill casts and receives shadows from the directional light.
- The Console has no errors and shows the `FixedChunkDemo: ... meshes, ... vertices` line, without the "needs 2 materials" warning.

## Stop and hand back to Opus when
- The brief is unclear or contradicts the real code.
- A decision is needed that falls under AGENTS.md §3.4 or is not in "Settled decisions".
- The same failure is fixed twice and still fails.
- A file outside "Scope" must change.
- A URP include, macro, keyword or pass structure named here does not exist in URP 17.4 or the shader still has errors after mirroring the reference files twice: quote the exact error in Blockers.

---

## Result

## Test results

## Blockers

---

## Review
