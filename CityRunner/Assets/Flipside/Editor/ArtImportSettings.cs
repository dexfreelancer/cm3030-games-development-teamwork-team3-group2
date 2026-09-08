using UnityEditor;
using UnityEngine;

namespace Flipside.EditorTools
{
    /// <summary>Applies consistent sprite import settings to everything under Assets/Flipside/Art.</summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Flipside/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (file.EndsWith("_inv")) file = file.Substring(0, file.Length - 4); // colour variants share settings

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = PixelsPerUnit(file);
            importer.filterMode = file == "white" ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = file == "white" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            importer.maxTextureSize = file.StartsWith("bg_") ? 4096 : 2048;
            importer.wrapMode = file.StartsWith("tile_") || file.StartsWith("bg_") || file.StartsWith("edge_") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for Tiled draw mode
            settings.spriteGenerateFallbackPhysicsShape = false;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            if (file == "robot_body") { settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(0.5f, 0.0f); }   // bottom centre (hip line)
            if (file.StartsWith("robot_leg")) { settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(0.5f, 1.0f); } // top centre (hip joint)
            importer.SetTextureSettings(settings);
        }

        /// <summary>World size is decided here so the level builder can think in units.</summary>
        static float PixelsPerUnit(string file)
        {
            switch (file)
            {
                case "white": return 8f;
                case "robot":
                case "robot_body":
                case "robot_leg_l":
                case "robot_leg_r": return 480f; // full robot is 694px tall = ~1.45 units
                case "chip": return 900f;
                case "flip_icon": return 700f;
                case "beacon_on":
                case "beacon_off": return 300f;  // ~3.4 units tall
                case "power_core": return 200f;  // ~5 units tall
                case "laser_emitter": return 800f;
                case "spikes": return 1000f;     // 4 units wide
                case "panel": return 1000f;
                case "title_art": return 100f;
                case "glow_soft": return 128f;   // 2 units across
                case "hazard_drone": return 520f; // ~1.6 units wide
                case "portal_ring": return 380f;  // ~2.4 units across
                case "switch_button": return 700f; // ~1.3 units wide
                case "noflip_icon": return 336f;
                case "arrow_right": return 160f;  // 1.6 units
                case "prop_water_tower": return 330f;  // ~3.1 units tall
                case "prop_ac_unit": return 900f;      // ~1.1 units
                case "prop_antenna": return 300f;      // ~3.4 units tall
                case "prop_pipes": return 200f;        // ~5 units wide
                case "prop_lamp": return 640f;         // ~1.6 units
                case "prop_crates": return 560f;       // ~1.4 units
                case "prop_vending": return 480f;      // ~2.1 units tall
                default:
                    if (file.StartsWith("edge_")) return 200f;  // lip strips ~0.7-1.2 units tall
                    if (file.StartsWith("key_")) return 336f;  // keycap ~1 unit tall in world space
                    if (file.StartsWith("sign_")) return 350f;
                    if (file.StartsWith("tile_")) return 128f; // 1024px = 8 units
                    if (file.StartsWith("bg_")) return 64f;    // wide layers, ~40+ units
                    return 100f;
            }
        }
    }
}
