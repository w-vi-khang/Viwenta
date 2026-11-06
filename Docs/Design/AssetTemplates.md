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
