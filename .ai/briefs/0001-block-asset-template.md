# Brief 0001 — Block asset template (per-face skins, texture baker)

> Status: awaiting review
> Level: Large
> Review: full
> Executor model: Sonnet · medium
> Updated: 2026-11-07 07:20

## Goal
Make block art replaceable without touching data: a block references its face textures (Top / Side / Bottom) as asset files instead of a raw texture-array layer number, and an Editor baker packs those files into the block `Texture2DArray`. All art stays placeholder; this brief is logic and tooling only (no shader, no material, no art files).

## Context to read
- `Assets/_Project/Scripts/Voxel/Core/BlockDefinition.cs:1-53` — `BlockSkin` (keep as is) and `BlockDefinition`. Current members:
  ```csharp
  public BlockSkin Skin { get; }                                   // line 37
  public BlockDefinition(ushort id, string name, bool isOpaque, BlockSkin skin, FaceShape faceShapeOverride = null) // line 42
  ```
- `Assets/_Project/Scripts/Voxel/Core/ChunkMeshBuilder.cs:56` — the only reader of `block.Skin`:
  ```csharp
  AppendFace(output, shape, symmetry, direction, x, y, z, block.Skin, !block.IsOpaque);
  ```
- `Assets/_Project/Scripts/Voxel/Core/Direction.cs:3-11` — `enum Direction : byte { PosX, NegX, PosY, NegY, PosZ, NegZ }`.
- `Assets/_Project/Scripts/Voxel/Runtime/BlockDefinitionAsset.cs` (whole file, 26 lines) — fields `id`, `blockName`, `isOpaque`, `textureLayer`, `tint`; method `public BlockDefinition ToDefinition()`. Tint packing to copy: `(uint)(tint.r | (tint.g << 8) | (tint.b << 16) | (tint.a << 24))`.
- `Assets/_Project/Scripts/Voxel/Runtime/BlockRegistryAsset.cs` (whole file, 24 lines) — `public BlockRegistry Build()`; empty slot throws `InvalidOperationException($"Block registry '{name}' has an empty slot.")`.
- `Assets/_Project/Scripts/Voxel/Runtime/FixedChunkDemo.cs:26` — calls `registryAsset.Build()` (signature stays the same).
- `Assets/_Project/Scripts/Voxel/Tests/EditMode/ChunkMeshBuilderTests.cs:15-21` (SetUp registering stone/glass with the 1-skin constructor) and `:176-188` (`Build_AppliesSkinLayerAndTint_AndMapsFaceToWholeLayer`) — must keep passing unchanged.
- `Assets/_Project/Scripts/Voxel/Tests/EditMode/Viwenta.Voxel.Tests.EditMode.asmdef` — Editor-only test assembly; `references` currently `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Viwenta.Voxel.Core`, `Viwenta.Voxel.Runtime`.
- `Assets/_Project/Scripts/Voxel/Runtime/Viwenta.Voxel.Runtime.asmdef` — copy its shape for the new Editor asmdef.
- `Docs/Design/Rendering.md:36` — the "Texture" bullet to update.
- No `BlockDefinitionAsset` / `BlockRegistryAsset` `.asset` file exists in the repo yet, so removing serialized fields needs no data migration.

## Settled decisions (do not change)
- Identity is separate from look: block ID never changes; textures are swappable. Layer numbers are computed when the registry is built and are **never stored** in block data.
- Three face slots per block: Top (`PosY`), Bottom (`NegY`), Side (the other four). An empty Side or Bottom slot reuses the whole Top slot (texture and tint). Top is required.
- Each slot has its own tint (e.g. biome tint on grass top only).
- One source PNG per face texture under `Assets/_Project/Art/Blocks/`; all sources are square, the same size (`faceSize`, default 32, power of two, 4..512), sRGB, with mipmaps.
- Layer order = sources sorted by `AssetDatabase.GetAssetPath` then by `name`, ordinal compare, duplicates removed. Deterministic between bakes.
- The baked `Texture2DArray` is stored as a sub-asset of the `BlockTextureLibrary` asset, named `<library name>_Array`. Format `TextureFormat.RGBA32`, mip chain on, sRGB (`linear: false`), `wrapMode = Repeat`, `filterMode` from the library (default `Point`).
- Editor code namespace is `Viwenta.Voxel.EditorTools` (not `...Editor`, to avoid clashing with `UnityEditor.Editor`).
- The old 1-skin `BlockDefinition` constructor stays and applies the skin to all three slots, so existing tests and callers keep working. The `Skin` property is removed (only `ChunkMeshBuilder.cs:56` uses it).
- No shader, material, PNG, scene or `.asset` file is created in this brief.

## Scope
- May edit/create:
  - `Assets/_Project/Scripts/Voxel/Core/BlockDefinition.cs`, `Core/ChunkMeshBuilder.cs`
  - `Assets/_Project/Scripts/Voxel/Runtime/BlockDefinitionAsset.cs`, `Runtime/BlockRegistryAsset.cs`, new `Runtime/BlockTextureLibrary.cs`
  - new folder `Assets/_Project/Scripts/Voxel/Editor/` with `Viwenta.Voxel.Editor.asmdef`, `BlockTextureBaker.cs`, `BlockTextureImportRules.cs`
  - `Assets/_Project/Scripts/Voxel/Tests/EditMode/Viwenta.Voxel.Tests.EditMode.asmdef` (add one reference), new `Tests/EditMode/BlockSkinTests.cs`, new `Tests/EditMode/BlockTextureBakerTests.cs`
  - new `Docs/Design/AssetTemplates.md`; `Docs/Design/Rendering.md` line 36 only
  - the `.meta` files Unity generates for the new files/folders
  - this brief (Result / Test results / Blockers sections, Status line)
- Must not edit: any other file; `FixedChunkDemo.cs`; existing test files; `ProjectSettings/`; `Packages/`.

## Steps
1. **Core — per-face skins.** `Core/BlockDefinition.cs`:
   - Replace `public BlockSkin Skin { get; }` with three properties `public BlockSkin Top { get; }`, `public BlockSkin Side { get; }`, `public BlockSkin Bottom { get; }`, each with a one-line `<summary>` (Top: "Skin of the face looking up (+Y).", Side: "Skin of the four side faces.", Bottom: "Skin of the face looking down (-Y).").
   - Add constructor `public BlockDefinition(ushort id, string name, bool isOpaque, BlockSkin top, BlockSkin side, BlockSkin bottom, FaceShape faceShapeOverride = null)` with the same name check as today.
   - Keep the existing constructor; make it chain: `: this(id, name, isOpaque, skin, skin, skin, faceShapeOverride)`; add `<summary>` "Uses <paramref name="skin"/> on every face."
   - Add `public BlockSkin SkinFor(Direction direction)`: `PosY` → `Top`, `NegY` → `Bottom`, otherwise `Side`. `<summary>` "Skin of the face looking along <paramref name="direction"/>."
   - Done when: the file compiles and `Skin` no longer exists.
2. **Core — builder uses the face skin.** `Core/ChunkMeshBuilder.cs:56`: replace `block.Skin` with `block.SkinFor(direction)`. Done when: no other change in that file.
3. **Runtime — `BlockTextureLibrary`.** New `Runtime/BlockTextureLibrary.cs`, namespace `Viwenta.Voxel`:
   ```csharp
   /// <summary>
   /// The block texture array and which source texture sits on which layer. Filled by the baker
   /// (Viwenta > Voxel > Bake Block Textures); layers are looked up when a registry is built, never stored in block data.
   /// </summary>
   [CreateAssetMenu(menuName = "Viwenta/Voxel/Block Texture Library", fileName = "BlockTextures")]
   public sealed class BlockTextureLibrary : ScriptableObject
   {
       public const int MinFaceSize = 4;
       public const int MaxFaceSize = 512;

       [Tooltip("Width and height in pixels of every block face texture. Power of two.")]
       [SerializeField] int faceSize = 32;
       [SerializeField] FilterMode filterMode = FilterMode.Point;
       [Tooltip("Written by the baker: layer i of the array is layers[i].")]
       [SerializeField] Texture2D[] layers = System.Array.Empty<Texture2D>();
       [Tooltip("Written by the baker; stored inside this asset.")]
       [SerializeField] Texture2DArray array;

       public int FaceSize => faceSize;
       public FilterMode FilterMode => filterMode;
       public Texture2DArray Array => array;
       public IReadOnlyList<Texture2D> Layers => layers;

       /// <summary>Layer of <paramref name="texture"/> in the baked array; false when it was not baked.</summary>
       public bool TryGetLayer(Texture2D texture, out ushort layer) { /* linear search over layers, reference equality */ }

       /// <summary>Called by the baker only. Replaces the layer list and the array reference.</summary>
       public void SetBakedData(Texture2D[] bakedLayers, Texture2DArray bakedArray) { /* null checks → ArgumentNullException; assign */ }
   }
   ```
   `TryGetLayer` with a null texture returns false. Done when: compiles.
4. **Runtime — `BlockDefinitionAsset` references textures.** Rewrite `Runtime/BlockDefinitionAsset.cs`:
   - Keep `id`, `blockName`, `isOpaque` fields and tooltips; remove `textureLayer` and `tint`.
   - Add a nested serializable class and three fields:
     ```csharp
     [Serializable]
     public sealed class FaceSkin
     {
         [Tooltip("Face texture under Assets/_Project/Art/Blocks/. Leave empty on Side/Bottom to reuse Top.")]
         public Texture2D texture;
         public Color32 tint = new Color32(255, 255, 255, 255);
     }

     [SerializeField] FaceSkin top = new FaceSkin();
     [SerializeField] FaceSkin side = new FaceSkin();
     [SerializeField] FaceSkin bottom = new FaceSkin();
     ```
   - `public IEnumerable<Texture2D> ReferencedTextures()` — yields the non-null textures of top, side, bottom (in that order).
   - Replace `ToDefinition()` with `public BlockDefinition ToDefinition(BlockTextureLibrary textures)`:
     - `textures == null` → `ArgumentNullException(nameof(textures))`.
     - `top.texture == null` → `InvalidOperationException($"Block '{blockName}' (id {id}) has no top texture.")`.
     - Resolve each slot (Side/Bottom with null texture → use the Top slot); for a texture not in the library → `InvalidOperationException($"Block '{blockName}' (id {id}): {slotName} texture '{texture.name}' is not in block texture library '{textures.name}'. Run Viwenta > Voxel > Bake Block Textures.")` where `slotName` is `top`, `side` or `bottom`.
     - Build `new BlockSkin(layer, packedTint)` per slot (tint packing as in Context) and return the 3-skin `BlockDefinition`.
   - Done when: compiles; no `textureLayer` left in the repo (`grep -rn textureLayer Assets` is empty).
5. **Runtime — registry asset carries the library.** `Runtime/BlockRegistryAsset.cs`:
   - Add `[SerializeField] BlockTextureLibrary textures;` with tooltip "Texture library the block textures are baked into."
   - Add `public BlockTextureLibrary Textures => textures;` and `public IReadOnlyList<BlockDefinitionAsset> Blocks => blocks;`.
   - In `Build()`, before the loop: `if (textures == null) throw new System.InvalidOperationException($"Block registry '{name}' has no block texture library.");` and call `block.ToDefinition(textures)`. Update the `<summary>` to "Builds the engine-free registry. Throws when two blocks share an ID, a slot is empty, or a block texture is not baked."
6. **Editor asmdef.** New `Assets/_Project/Scripts/Voxel/Editor/Viwenta.Voxel.Editor.asmdef`: same shape as the Runtime asmdef, with `"name": "Viwenta.Voxel.Editor"`, `"rootNamespace": "Viwenta.Voxel.EditorTools"`, `"references": ["Viwenta.Voxel.Core", "Viwenta.Voxel.Runtime"]`, `"includePlatforms": ["Editor"]`, `"autoReferenced": false`.
7. **Editor — import rules.** New `Editor/BlockTextureImportRules.cs`, `sealed class BlockTextureImportRules : AssetPostprocessor`, namespace `Viwenta.Voxel.EditorTools`:
   - `public const string Folder = "Assets/_Project/Art/Blocks/";`
   - `public static bool Applies(string assetPath)` → `assetPath != null && assetPath.StartsWith(Folder, StringComparison.Ordinal)`.
   - `void OnPreprocessTexture()`: when `Applies(assetPath)`, cast `assetImporter` to `TextureImporter` and set `textureType = Default`, `textureShape = Texture2D`, `sRGBTexture = true`, `alphaSource = FromInput`, `isReadable = true`, `mipmapEnabled = true`, `npotScale = None`, `textureCompression = Uncompressed`, `wrapMode = Repeat`, `filterMode = Point`.
   - `<summary>` "Applies the block texture mold (see Docs/Design/AssetTemplates.md) to every texture under Assets/_Project/Art/Blocks/, so the baker can read it."
8. **Editor — baker.** New `Editor/BlockTextureBaker.cs`, `public static class BlockTextureBaker`, namespace `Viwenta.Voxel.EditorTools`:
   - `public static List<Texture2D> CollectSources(BlockRegistryAsset registry)` — null → `ArgumentNullException`; gathers `ReferencedTextures()` of every non-null block, removes duplicates, sorts by (`AssetDatabase.GetAssetPath(t)`, `t.name`) with `StringComparer.Ordinal`.
   - `public static Texture2DArray BuildArray(IReadOnlyList<Texture2D> sources, int faceSize, FilterMode filterMode)`; validation, each failure throws `ArgumentException` (or `ArgumentOutOfRangeException` for size) with these exact messages:
     - `faceSize` not a power of two or outside `MinFaceSize..MaxFaceSize` → `$"Face size {faceSize} must be a power of two between {BlockTextureLibrary.MinFaceSize} and {BlockTextureLibrary.MaxFaceSize}."`
     - `sources` null or empty → `"No block textures to bake."`
     - a null entry → `$"Block texture {i} is missing."`
     - wrong size → `$"Block texture '{t.name}' is {t.width}x{t.height}, expected {faceSize}x{faceSize}."`
     - not readable → `$"Block texture '{t.name}' is not readable; put it under {BlockTextureImportRules.Folder} so the import rules apply."`
     - mip count differs from the full chain → `$"Block texture '{t.name}' has {t.mipmapCount} mip levels, expected {expectedMips}; enable Generate Mip Maps."` where `int expectedMips = 1 + (int)Math.Round(Math.Log(faceSize, 2));` (8 → 4, 32 → 6).
     - Validate every source first (all checks above), then build: `new Texture2DArray(faceSize, faceSize, sources.Count, TextureFormat.RGBA32, true, false)`, set `wrapMode = TextureWrapMode.Repeat`, `filterMode = filterMode`; for each layer `i` and mip `m` in `0..expectedMips-1`: `array.SetPixels32(sources[i].GetPixels32(m), i, m)`; then `array.Apply(false, false)`.
   - `public static void Bake(BlockRegistryAsset registry)`: `registry.Textures == null` → `InvalidOperationException($"Block registry '{registry.name}' has no block texture library.")`; collect sources; build the array with the library's `FaceSize` / `FilterMode`; name it `$"{library.name}_Array"`; remove the previous array sub-asset if any (`AssetDatabase.RemoveObjectFromAsset`), `AssetDatabase.AddObjectToAsset(array, library)`, `library.SetBakedData(sources.ToArray(), array)`, `EditorUtility.SetDirty(library)`, `AssetDatabase.SaveAssets()`; log `Debug.Log($"Baked {sources.Count} block textures into '{library.name}'.", library)`.
   - `[MenuItem("Viwenta/Voxel/Bake Block Textures")] static void BakeAll()`: for every `AssetDatabase.FindAssets("t:BlockRegistryAsset")`, call `Bake`; catch `Exception` per registry and `Debug.LogError($"Baking block textures of '{path}' failed: {e.Message}")` then continue (do not swallow silently). No registry found → `Debug.LogWarning("No BlockRegistryAsset found to bake.")`.
9. **Tests asmdef.** Add `"Viwenta.Voxel.Editor"` to `references` in `Tests/EditMode/Viwenta.Voxel.Tests.EditMode.asmdef`. Nothing else changes in that file.
10. **Tests — `Tests/EditMode/BlockSkinTests.cs`** (namespace `Viwenta.Voxel.Tests`). Build test textures in memory: `new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "..." }` filled with `SetPixels32` + `Apply(true)`; destroy everything created in `[TearDown]` with `Object.DestroyImmediate`. Set private serialized fields of ScriptableObjects through `UnityEditor.SerializedObject` / `FindProperty(...)` + `ApplyModifiedPropertiesWithoutUndo()` (e.g. `top.texture` is `FindProperty("top").FindPropertyRelative("texture").objectReferenceValue`). Cases listed under "Parts to test" (rows marked BlockSkinTests).
11. **Tests — `Tests/EditMode/BlockTextureBakerTests.cs`.** Same helpers (a small private factory in each file is fine). Cases listed under "Parts to test" (rows marked BlockTextureBakerTests). Do not create files on disk; `Bake` (which writes assets) is not unit-tested.
12. **Docs.** Create `Docs/Design/AssetTemplates.md` with exactly the content in the block at the end of this brief. In `Docs/Design/Rendering.md:36` replace the words
   > a block's skin is a layer index plus a tint (`BlockSkin`)

   with
   > a block has three skins, Top, Side and Bottom, each a texture layer plus a tint (`BlockSkin`); layers are resolved from texture assets when the registry is built (see AssetTemplates.md)

   Change nothing else in Rendering.md.
13. **Verify and commit.** Run the commands below. Commit everything in scope, including the `.meta` files Unity generated for the new `Editor` folder, `.asmdef` and `.cs` files (files under `Docs/` get no `.meta`), as one commit: `Add per-face block skins and block texture baker`, ending with the co-author line the session gives you. Do not push.

### Content of `Docs/Design/AssetTemplates.md` (copy verbatim)
```markdown
# Asset templates

Status: blockout. All art in the project is placeholder; these templates fix the parts that code and data rely on, so the art can be redrawn or extended later without changing code, block data or saves. There is no art bible yet: style, palette and resolution choices below are **[open]** unless stated.

## Principle: identity is separate from look

- A block is identified by its ID (`BlockDefinitionAsset.id`), stored in saves and never reused.
- Its look is swappable: face textures, tints and (later) face shapes. Replacing a look never changes the ID or any other data.
- Texture array layer numbers are computed when the registry is built and are never stored, so adding, removing or reordering textures cannot break a block.

## Block face textures

| Item | Rule |
|---|---|
| Location | `Assets/_Project/Art/Blocks/` (import rules are applied automatically there) |
| File name | `blk_<block name>.png`, or `blk_<block name>_top.png`, `_side.png`, `_bottom.png` when the faces differ |
| Size | Square, equal to the library face size (default 32x32 **[open]**), power of two |
| Colour | sRGB, RGBA. Opaque blocks: alpha 255 everywhere. Transparent blocks: alpha carries the transparency |
| Tiling | Must tile seamlessly: each face maps the whole texture and neighbouring blocks repeat it |
| Import | Set by `BlockTextureImportRules`: readable, uncompressed source, mipmaps, repeat, point filter. Do not change them by hand |
| Slots | Each block has Top, Side and Bottom. Side and Bottom may stay empty to reuse Top |
| Tint | Per slot, multiplied with the texture. Keep textures mostly neutral where a tint (e.g. biome colour) will apply |

## How to replace or add block art

1. Draw the PNG to the template above and save it in `Assets/_Project/Art/Blocks/`, overwriting the placeholder of the same name to replace it.
2. To add a block or a face, assign the texture to the slot of the `BlockDefinitionAsset`.
3. Run **Viwenta > Voxel > Bake Block Textures**. It rejects a texture of the wrong size or without mipmaps and names the file.
4. Check in the Editor at near, middle and far distance. No code or data changes are needed.

## Face shapes (later)

Face shapes are prebuilt meshes made in Blender and chosen by `FaceVariantKey`. Their template is the `FaceShape` format: a face of one voxel in local (u, w, h) coordinates, u and w in 0..1, h along the face normal, triangles wound outward. Bevel radius and detail tiers are **[open]**; the Blender export and bake tool are not built yet.
```

## Done criteria
- `grep -rn "\.Skin\b\|textureLayer" Assets/_Project/Scripts` returns nothing.
- `sh "<Plugin root>/scripts/run.sh" verify_run --all` exits 0; the results XML shows `failed="0"` and contains every new test name below as `Passed`; the total equals 97 + the number of new tests.
- `Assets/_Project/Scripts/Voxel/Editor/` contains the asmdef and both `.cs` files, each with a `.meta`.
- `Docs/Design/AssetTemplates.md` matches the block above; `Rendering.md` differs only on line 36.
- One commit on branch `init/unity-project`, nothing pushed.

## Tests after completion

### Parts to test
- `BlockDefinition.SkinFor` (BlockSkinTests) · `SkinFor_MapsUpToTopDownToBottomAndOthersToSide`: build with 3 distinct skins, check all 6 directions · PosY→Top, NegY→Bottom, the 4 others→Side.
- 1-skin constructor (BlockSkinTests) · `SingleSkinConstructor_UsesSkinOnEveryFace` · Top, Side, Bottom all equal the given skin.
- Builder per-face layer (BlockSkinTests) · `Build_GrassLikeBlock_UsesTopSideAndBottomLayers`: one block with layers 1/2/3, built with `ChunkMeshBuilder.Build` · vertices whose normal y > 0.5 have uv layer 1, y < -0.5 layer 3, others layer 2.
- `ToDefinition` resolves layers (BlockSkinTests) · `ToDefinition_ResolvesLayersFromLibraryAndFallsBackToTop`: library baked data set via `SetBakedData` with 2 textures; block top = texture B, side empty, bottom = texture A, side tint distinct · Top and Side layer = index of B, Bottom layer = index of A, Side tint = Top tint (whole slot reused).
- `ToDefinition` errors (BlockSkinTests) · `ToDefinition_NoTopTexture_ThrowsWithBlockName` and `ToDefinition_TextureNotBaked_ThrowsWithSlotAndTextureName` and `ToDefinition_NullLibrary_Throws` · `InvalidOperationException` whose message contains the block name / `side` (or the slot used) and the texture name; `ArgumentNullException` for null.
- Registry asset (BlockSkinTests) · `RegistryBuild_NoLibrary_ThrowsWithRegistryName` · `InvalidOperationException` containing the registry name.
- `BuildArray` happy path (BlockTextureBakerTests) · `BuildArray_CopiesEveryLayerAndMip`: 2 textures 8x8 with different solid colours · depth 2, `mipmapCount` equals the sources', `GetPixels32(i, 0)[0]` and the last mip equal each source colour, `filterMode` as passed.
- `BuildArray` errors (BlockTextureBakerTests) · `BuildArray_WrongSize_ThrowsWithTextureName`, `BuildArray_Empty_Throws`, `BuildArray_NullEntry_Throws`, `BuildArray_FaceSizeNotPowerOfTwo_Throws` (e.g. 24), `BuildArray_NoMipmaps_Throws` (texture made with `mipChain: false`) · exception type and message per step 8.
- `CollectSources` (BlockTextureBakerTests) · `CollectSources_RemovesDuplicatesAndOrdersByName`: registry with 2 blocks sharing a texture, in-memory textures named "b" and "a" · result is [a, b] (empty asset paths, so name order), no duplicates; null registry → `ArgumentNullException`.
- Import rule path (BlockTextureBakerTests) · `ImportRules_AppliesOnlyUnderBlocksFolder` · true for `Assets/_Project/Art/Blocks/blk_stone.png`, false for `Assets/_Project/Art/blk_stone.png`, `assets/_project/art/blocks/x.png` and null.
- Regression · all 97 existing tests, in particular `Build_AppliesSkinLayerAndTint_AndMapsFaceToWholeLayer` and `Build_TransparentBlock_GoesToTransparentIndices` · still pass unchanged.
- Compile of the whole project including the Editor asmdef · Unity batchmode run · no `error CS` in `.ai/.state/unity-test.log`.

### Commands the executor runs
1. `grep -rn "\.Skin\b\|textureLayer" Assets/_Project/Scripts` → no output.
2. `sh "<Plugin root>/scripts/run.sh" verify_run --all` (Plugin root is in "Session status") → `exit 0`.
3. `grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' .ai/.state/unity-test-results.xml | head -1` and `grep -c 'error CS' .ai/.state/unity-test.log` → failed 0, error count 0.
4. `git status --short` → only files in Scope (plus their `.meta`) before committing; clean after.

### User tests
None (no art, scene or shader in this brief; visual checks come with the shader brief).

## Stop and hand back to Opus when
- The brief is unclear or contradicts the real code.
- A decision is needed that falls under AGENTS.md §3.4 or is not in "Settled decisions".
- The same failure is fixed twice and still fails.
- A file outside "Scope" must change.
- A Unity API named here does not exist or behaves differently in 6000.4.8f1 (e.g. `SetPixels32` on `Texture2DArray`, `TextureImporter` members): record the exact compiler/runtime message in Blockers.

---

## Result
Base commit: 8e15358
Run with: Sonnet · medium
Commit: 30714c5 (branch init/unity-project, not pushed). Changed: Core/BlockDefinition.cs, Core/ChunkMeshBuilder.cs, Runtime/BlockDefinitionAsset.cs, Runtime/BlockRegistryAsset.cs, new Runtime/BlockTextureLibrary.cs, new Editor/ (asmdef, BlockTextureBaker.cs, BlockTextureImportRules.cs), Tests asmdef (+1 reference), new BlockSkinTests.cs and BlockTextureBakerTests.cs, new Docs/Design/AssetTemplates.md, Rendering.md line 36; plus .meta files.

## Test results
- Commands: (1) `grep -rn "\.Skin\b\|textureLayer" Assets/_Project/Scripts` -> no output. (2) `verify_run --all` -> exit 0. (3) results XML `total="113" passed="113" failed="0"` (97 + 16 new); `grep -c 'error CS'` log -> 0. (4) git status showed only scope files + .meta before commit.
- All 16 new tests Passed: SkinFor_MapsUpToTopDownToBottomAndOthersToSide, SingleSkinConstructor_UsesSkinOnEveryFace, Build_GrassLikeBlock_UsesTopSideAndBottomLayers, ToDefinition_ResolvesLayersFromLibraryAndFallsBackToTop, ToDefinition_NoTopTexture_ThrowsWithBlockName, ToDefinition_TextureNotBaked_ThrowsWithSlotAndTextureName, ToDefinition_NullLibrary_Throws, RegistryBuild_NoLibrary_ThrowsWithRegistryName, BuildArray_CopiesEveryLayerAndMip, BuildArray_WrongSize/Empty/NullEntry/FaceSizeNotPowerOfTwo/NoMipmaps, CollectSources_RemovesDuplicatesAndOrdersByName, ImportRules_AppliesOnlyUnderBlocksFolder.
- Regression: all 97 existing tests Passed unchanged. Compile incl. Editor asmdef: pass (0 `error CS`).
- Note: the Unity log printed ".state is not a valid directory name" twice (harmless; exit 0). `Bake`/`BakeAll` (write assets) are not unit-tested, per brief.
- User tests: none.

## Blockers

---

## Review
