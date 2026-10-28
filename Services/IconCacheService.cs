using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Beat115Icon.Services
{
    public static class IconCacheService
    {
        private const string EmbeddedPngResource = "Beat115Icon.Resources.hibiscus.png";
        public const string DefaultFolderName = "Beat115Icon";

        private static Sprite[] _cachedSprites = Array.Empty<Sprite>();
        private static readonly System.Random _random = new System.Random();

        public static int CachedCount => _cachedSprites.Length;

        public static void Initialize(string customDir = null)
        {
            LoadSprites(customDir);
        }

        public static Sprite GetRandomSprite()
        {
            if (_cachedSprites == null || _cachedSprites.Length == 0)
            {
                return null;
            }

            if (_cachedSprites.Length == 1)
            {
                return _cachedSprites[0];
            }

            int index = _random.Next(0, _cachedSprites.Length);
            return _cachedSprites[index];
        }

        public static void LoadSprites(string customDir = null)
        {
            string folderPath;
            if (!string.IsNullOrEmpty(customDir) && Directory.Exists(customDir))
            {
                folderPath = customDir;
            }
            else
            {
                folderPath = Path.Combine(IPA.Utilities.UnityGame.UserDataPath, DefaultFolderName);
            }

            if (!Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error($"Failed to create folder {folderPath}: {ex.Message}");
                }
            }

            List<Sprite> loadedSprites = new List<Sprite>();

            if (Directory.Exists(folderPath))
            {
                string[] pngFiles = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
                Array.Sort(pngFiles, StringComparer.OrdinalIgnoreCase);

                foreach (string filePath in pngFiles)
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        if (bytes.Length == 0) continue;

                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
                        if (ImageConversion.LoadImage(tex, bytes))
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Trilinear;
                            tex.name = Path.GetFileNameWithoutExtension(filePath);
                            UnityEngine.Object.DontDestroyOnLoad(tex);

                            Sprite sprite = Sprite.Create(
                                tex,
                                new Rect(0f, 0f, tex.width, tex.height),
                                new Vector2(0.5f, 0.5f),
                                100f);
                            sprite.name = tex.name + "_Sprite";
                            UnityEngine.Object.DontDestroyOnLoad(sprite);

                            loadedSprites.Add(sprite);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log?.Warn($"Failed to load image '{filePath}': {ex.Message}");
                    }
                }
            }

            if (loadedSprites.Count == 0)
            {
                Plugin.Log?.Info("No PNG images found in UserData/Beat115Icon. Using embedded fallback icon.");
                Sprite fallback = CreateFallbackSprite();
                if (fallback != null)
                {
                    loadedSprites.Add(fallback);
                }
            }

            _cachedSprites = loadedSprites.ToArray();
            Plugin.Log?.Info($"Beat115Icon initialized with {_cachedSprites.Length} sprite(s).");
        }

        private static Sprite CreateFallbackSprite()
        {
            byte[] pngBytes = LoadEmbeddedPngBytes();
            if (pngBytes == null || pngBytes.Length == 0)
            {
                return null;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
            ImageConversion.LoadImage(tex, pngBytes);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.name = "Beat115IconFallbackTexture";
            UnityEngine.Object.DontDestroyOnLoad(tex);

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = "Beat115IconFallbackSprite";
            UnityEngine.Object.DontDestroyOnLoad(sprite);

            return sprite;
        }

        private static byte[] LoadEmbeddedPngBytes()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedPngResource);
            if (stream == null)
            {
                return null;
            }

            using MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
