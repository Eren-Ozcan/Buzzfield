using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Art for the interface, made once and reused by the scene builder: TextMeshPro font assets
    /// (Fredoka for text, the Material Symbols subset for icons), generated 9-slice sprites
    /// (rounded panels, pressable buttons with a darker lip, pills, circles, soft shadows) and
    /// icons rendered from the game's own models. Existing files are kept; delete one to rebuild it.
    /// </summary>
    internal static class UiAssets
    {
        const string Root = EditorAssets.Root + "/UI";
        const string FontRoot = Root + "/Fonts";
        const string SpriteRoot = Root + "/Sprites";

        /// <summary>Fredoka SemiBold with every printable ASCII character (all UI text is ASCII).</summary>
        public static TMP_FontAsset TextFont() => LoadOrCreateFont($"{FontRoot}/Fredoka-SemiBold.ttf", $"{FontRoot}/Fredoka SDF.asset", Ascii(), 72, 8, 1024);

        /// <summary>The icon glyphs of <see cref="UiTheme"/> from the Material Symbols subset.</summary>
        public static TMP_FontAsset IconFont() => LoadOrCreateFont($"{FontRoot}/BuzzfieldIcons.ttf", $"{FontRoot}/Icons SDF.asset", UiTheme.AllIcons, 96, 10, 1024);

        /// <summary>Text material with a dark outline and a soft drop shadow, for numbers over the garden.</summary>
        public static Material OutlinedText()
        {
            string path = $"{FontRoot}/Fredoka SDF Outline.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(TextFont().material) { name = "Fredoka SDF Outline" };
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, UiTheme.TextDark);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.35f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static Sprite Round() => LoadOrCreateSprite("ui_round", 128, 128, new Vector4(52, 52, 52, 52), (x, y) => Shape(x, y, 128, 128, 48f, 0f));

        /// <summary>Pressable button: the face over a darker lip along the bottom, tinted by the Image colour.</summary>
        public static Sprite Button() => LoadOrCreateSprite("ui_button", 128, 144, new Vector4(52, 68, 52, 52), (x, y) =>
        {
            float alpha = Shape(x, y, 128, 144, 48f, 0f).a;
            // Lip: the bottom 16 px, a shade darker; a one-pixel soft line where the face meets it.
            float shade = y < 16 ? 0.72f : y < 17 ? 0.86f : 1f;
            return new Color(shade, shade, shade, alpha);
        });

        public static Sprite Pill() => LoadOrCreateSprite("ui_pill", 128, 128, new Vector4(63, 63, 63, 63), (x, y) => Shape(x, y, 128, 128, 64f, 0f));

        public static Sprite Circle() => LoadOrCreateSprite("ui_circle", 128, 128, Vector4.zero, (x, y) => Shape(x, y, 128, 128, 64f, 0f));

        /// <summary>Soft blurred rounded shape for drop shadows; tint it with a translucent black.</summary>
        public static Sprite Shadow() => LoadOrCreateSprite("ui_shadow", 160, 160, new Vector4(76, 76, 76, 76), (x, y) => Shape(x, y, 160, 160, 56f, 14f));

        public static Sprite BeeIcon() => LoadOrCreateIcon("icon_bee", "Bee_Worker", 150f, 0.82f);
        public static Sprite GoldenBeeIcon() => LoadOrCreateIcon("icon_golden_bee", "Bee_Golden", 210f, 0.82f);
        public static Sprite DewIcon() => LoadOrCreateIcon("icon_dewdrop", "Dewdrop", 20f, 1.05f);

        static string Ascii()
        {
            var chars = new char[95];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)(32 + i);
            return new string(chars);
        }

        static TMP_FontAsset LoadOrCreateFont(string fontPath, string assetPath, string characters, int samplingSize, int padding, int atlasSize)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, samplingSize, padding, GlyphRenderMode.SDFAA, atlasSize, atlasSize, AtlasPopulationMode.Dynamic, false);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            if (!asset.TryAddCharacters(characters, out string missing))
                Debug.LogWarning($"{asset.name}: no glyph for '{missing}'.");
            // Static: the atlas never changes at runtime, so play mode leaves the asset untouched.
            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTexture.name = $"{asset.name} Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            asset.material.name = $"{asset.name} Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {assetPath}");
            return asset;
        }

        /// <summary>
        /// White rounded rectangle of the given size with anti-aliased edges; <paramref name="blur"/>
        /// softens the edge over that many pixels (shadows).
        /// </summary>
        static Color Shape(int x, int y, int width, int height, float radius, float blur)
        {
            float inset = blur;
            float px = x + 0.5f;
            float py = y + 0.5f;
            float halfW = width * 0.5f - inset;
            float halfH = height * 0.5f - inset;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH));
            float dx = Mathf.Abs(px - width * 0.5f) - (halfW - r);
            float dy = Mathf.Abs(py - height * 0.5f) - (halfH - r);
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
            float alpha = blur > 0f ? Mathf.Clamp01(0.5f - outside / (2f * blur)) : Mathf.Clamp01(0.5f - outside);
            if (blur > 0f)
                alpha = alpha * alpha * (3f - 2f * alpha);
            return new Color(1f, 1f, 1f, alpha);
        }

        static Sprite LoadOrCreateSprite(string name, int width, int height, Vector4 border, System.Func<int, int, Color> pixel)
        {
            string path = $"{SpriteRoot}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, pixel(x, y));
            return SaveSprite(texture, path, border);
        }

        static Sprite SaveSprite(Texture2D texture, string path, Vector4 border)
        {
            EditorAssets.EnsureFolder(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Debug.Log($"Created {path}");
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// Renders a prefab three-quarters from above on a transparent background. The alpha comes
        /// from two renders, on black and on white: what the background shows through is the
        /// difference between them.
        /// </summary>
        static Sprite LoadOrCreateIcon(string name, string prefab, float yaw, float distance)
        {
            string path = $"{SpriteRoot}/{name}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{EditorAssets.PrefabRoot}/{prefab}.prefab");
            const int size = 256;
            var preview = new PreviewRenderUtility();
            try
            {
                GameObject instance = preview.InstantiatePrefabInScene(asset);
                instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                Bounds bounds = WorldBounds(instance);

                Camera camera = preview.camera;
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 50f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                Vector3 view = Quaternion.Euler(25f, 0f, 0f) * Vector3.forward;
                float fit = bounds.extents.magnitude / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                camera.transform.position = bounds.center - view * fit * distance * 0.62f;
                camera.transform.rotation = Quaternion.LookRotation(view);

                preview.lights[0].intensity = 1.15f;
                preview.lights[0].color = new Color(1f, 0.96f, 0.88f);
                preview.lights[0].transform.rotation = Quaternion.Euler(45f, -35f, 0f);
                preview.lights[1].intensity = 0.35f;
                preview.lights[1].transform.rotation = Quaternion.Euler(20f, 160f, 0f);
                preview.ambientColor = new Color(0.6f, 0.62f, 0.66f);

                Texture2D onBlack = Render(preview, Color.black, size);
                Texture2D onWhite = Render(preview, Color.white, size);
                var icon = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color b = onBlack.GetPixel(x, y);
                    Color w = onWhite.GetPixel(x, y);
                    float alpha = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
                    Color colour = alpha > 0.004f ? new Color(b.r / alpha, b.g / alpha, b.b / alpha) : Color.clear;
                    colour.a = alpha;
                    icon.SetPixel(x, y, colour);
                }
                Object.DestroyImmediate(onBlack);
                Object.DestroyImmediate(onWhite);
                return SaveSprite(icon, path, Vector4.zero);
            }
            finally
            {
                preview.Cleanup();
            }
        }

        static Texture2D Render(PreviewRenderUtility preview, Color background, int size)
        {
            preview.camera.backgroundColor = background;
            preview.BeginStaticPreview(new Rect(0, 0, size, size));
            preview.Render(true);
            return preview.EndStaticPreview();
        }

        static Bounds WorldBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
