// How the table kit (Resources/Kit, unity/art/table_kit) comes in: sprites, the 9-slice borders from kit.json, and
// size caps that keep the web download small (cards 256, chips and UI 128, the table plates 2048), no mipmaps.
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    public class KitImport : AssetPostprocessor
    {
        static readonly (string file, int border)[] Borders =
        {
            ("pill.png", 63), ("pill_outline.png", 63), ("rounded_24.png", 26), ("rounded_12.png", 14),
            ("seat_plate.png", 24), ("card_shadow.png", 50), ("glow.png", 80),
        };

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Dream/Resources/Kit/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite; t.spriteImportMode = SpriteImportMode.Single; t.mipmapEnabled = false;
            t.alphaIsTransparency = true; t.textureCompression = TextureImporterCompression.Compressed; t.wrapMode = TextureWrapMode.Clamp;
            t.maxTextureSize = assetPath.Contains("/cards/") ? 256 : assetPath.Contains("/chips/") ? 128 : assetPath.Contains("/ui/") ? 256 : 2048;
            foreach (var (file, b) in Borders)
                if (assetPath.EndsWith("/ui/" + file)) t.spriteBorder = new Vector4(b, b, b, b);
        }
    }
}
