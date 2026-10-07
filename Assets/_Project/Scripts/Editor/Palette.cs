using System.IO;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Colours of the modelled art (bees, hive, scenery) as swatches of one small texture, so they
    /// all share one material and one draw setup: a mesh picks its colours through UVs
    /// (<see cref="LowPolyBuilder.Swatch"/>). The texture is rewritten in place (same asset, same
    /// references) whenever a colour here no longer matches it.
    /// </summary>
    internal static class Palette
    {
        public const int Worker = 0;
        public const int Forager = 1;
        public const int Golden = 2;
        public const int Stripe = 3;
        public const int Eye = 4;
        public const int Wing = 5;
        public const int Pollen = 6;
        public const int HiveLight = 7;
        public const int HiveDark = 8;
        public const int Door = 9;
        public const int Wood = 10;
        public const int Honey = 11;
        public const int GoldenWing = 12;
        public const int Cheek = 13;
        public const int White = 14;
        public const int MeadowLight = 16;
        public const int MeadowMid = 17;
        public const int MeadowDark = 18;
        public const int HillFar = 19;
        public const int LeafLight = 20;
        public const int LeafMid = 21;
        public const int LeafDark = 22;
        public const int Pine = 23;
        public const int Trunk = 24;
        public const int Rock = 25;
        public const int RockDark = 26;
        public const int Dirt = 27;
        public const int Shade = 28;
        public const int ButterflyWing = 15;
        public const int ButterflySpot = 29;
        public const int Dew = 30;
        public const int DewLight = 31;

        const int Columns = 8;
        const int Rows = 4;
        const int SwatchPixels = 4;
        const string TexturePath = EditorAssets.Root + "/Textures/Palette.png";

        static readonly Color[] Colors =
        {
            Hex(0xFFD11A), // Worker body
            Hex(0xFF8C1A), // Forager body
            Hex(0xFFE673), // Golden body
            Hex(0x3A2A12), // stripes, antennae, smile
            Hex(0x1A1410), // eyes
            Hex(0xEBF5FF), // wings
            Hex(0xFFD94D), // pollen baskets
            Hex(0xEDAD33), // hive
            Hex(0xC98A24), // hive grooves
            Hex(0x40260D), // hive door
            Hex(0x996B40), // wood
            Hex(0xF59E1B), // honey drip
            Hex(0xFFF1C2), // Golden bee wings
            Hex(0xFF9E7A), // cheeks
            Color.white,
            Hex(0xFF9F43), // butterfly wings
            Hex(0x6CAD52), // meadow, light (close to the bloomed garden ground)
            Hex(0x63A44B), // meadow
            Hex(0x5A9A46), // meadow, dark
            Hex(0x7AA97A), // far hills, toward the haze
            Hex(0x6BB24C), // leaves, light
            Hex(0x4E9640), // leaves
            Hex(0x3A7A36), // leaves, dark
            Hex(0x2F6B45), // pines
            Hex(0x7A5230), // trunks
            Hex(0xA9A79E), // rocks
            Hex(0x8C8A82), // rocks, shaded
            Hex(0xC2A273), // dirt
            Hex(0x4C8A3E), // shade baked under trees and rocks
            Hex(0xFFF0B8), // butterfly wing spots
            Hex(0x3CC8D8), // Dewdrop crystal
            Hex(0x86E3EE), // Dewdrop highlight
        };

        /// <summary>UV at the centre of swatch <paramref name="index"/>.</summary>
        public static Vector2 Uv(int index) => new Vector2(
            (index % Columns + 0.5f) / Columns,
            (index / Columns + 0.5f) / Rows);

        /// <summary>The palette material (Simple Lit on the palette texture), created on first use.</summary>
        public static Material Material()
        {
            Texture2D texture = LoadOrCreateTexture();
            Material material = EditorAssets.LoadOrCreateMaterial("Palette", Color.white);
            if (material.GetTexture("_BaseMap") != texture)
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        static Texture2D LoadOrCreateTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (existing != null)
            {
                if (Matches(existing))
                    return existing;
                // A colour changed: rewrite the file in place; its import settings stay.
                WritePng();
                AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Updated {TexturePath}");
                return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            }

            EditorAssets.EnsureFolder(Path.GetDirectoryName(TexturePath));
            WritePng();
            AssetDatabase.ImportAsset(TexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
            // 16x16 pixels: readable costs nothing and lets tests check a mesh's colours.
            importer.isReadable = true;
            importer.SaveAndReimport();
            Debug.Log($"Created {TexturePath}");
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        }

        static void WritePng()
        {
            var texture = new Texture2D(Columns * SwatchPixels, Rows * SwatchPixels, TextureFormat.RGBA32, false);
            for (int i = 0; i < Columns * Rows; i++)
            {
                int x0 = i % Columns * SwatchPixels;
                int y0 = i / Columns * SwatchPixels;
                for (int y = 0; y < SwatchPixels; y++)
                for (int x = 0; x < SwatchPixels; x++)
                    texture.SetPixel(x0 + x, y0 + y, Colors[i]);
            }
            File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        /// <summary>Every swatch of the (readable) texture still has its colour from the table.</summary>
        static bool Matches(Texture2D texture)
        {
            if (!texture.isReadable || texture.width != Columns * SwatchPixels || texture.height != Rows * SwatchPixels)
                return true;
            for (int i = 0; i < Columns * Rows; i++)
            {
                Color32 have = texture.GetPixel(i % Columns * SwatchPixels + 1, i / Columns * SwatchPixels + 1);
                Color32 want = Colors[i];
                if (Mathf.Abs(have.r - want.r) > 1 || Mathf.Abs(have.g - want.g) > 1 || Mathf.Abs(have.b - want.b) > 1)
                    return false;
            }
            return true;
        }

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
