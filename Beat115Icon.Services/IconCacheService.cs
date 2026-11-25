using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Beat115Icon.Configuration;
using IPA.Utilities;
using UnityEngine;

namespace Beat115Icon.Services;

public sealed class IconAsset
{
	public string Name { get; }

	public Sprite[] Frames { get; }

	public float[] FrameDelaysSec { get; }

	public Texture2D[] OwnedTextures { get; }

	public bool IsAnimated => Frames != null && Frames.Length > 1;

	public Sprite FirstSprite => (Frames != null && Frames.Length > 0) ? Frames[0] : null;

	public IconAsset(string name, Sprite[] frames, float[] frameDelaysSec, Texture2D[] ownedTextures)
	{
		Name = name;
		Frames = frames ?? Array.Empty<Sprite>();
		FrameDelaysSec = frameDelaysSec ?? Array.Empty<float>();
		OwnedTextures = ownedTextures ?? Array.Empty<Texture2D>();
	}
}

public static class IconCacheService
{
	private const string EmbeddedPngResource = "Beat115Icon.Resources.hibiscus.png";

	public const string DefaultFolderName = "Beat115Icon";

	private static IconAsset[] _cachedAssets = Array.Empty<IconAsset>();

	private static readonly System.Random _random = new System.Random();

	public static int CachedCount => _cachedAssets.Length;

	public static void Initialize(string customDir = null)
	{
		LoadSprites(customDir);
	}

	public static void Reload(string customDir = null)
	{
		string dir = customDir ?? PluginConfig.Instance?.CustomDirectoryPath;
		LoadSprites(dir);
	}

	public static IconAsset GetRandomAsset()
	{
		if (_cachedAssets == null || _cachedAssets.Length == 0)
		{
			return null;
		}
		if (_cachedAssets.Length == 1)
		{
			return _cachedAssets[0];
		}
		int num = _random.Next(0, _cachedAssets.Length);
		return _cachedAssets[num];
	}

	public static Sprite GetRandomSprite()
	{
		return GetRandomAsset()?.FirstSprite;
	}

	public static void LoadSprites(string customDir = null)
	{
		DisposeCachedAssets();

		string folderPath = ((string.IsNullOrEmpty(customDir) || !Directory.Exists(customDir)) ? Path.Combine(UnityGame.UserDataPath, DefaultFolderName) : customDir);
		if (!Directory.Exists(folderPath))
		{
			try
			{
				Directory.CreateDirectory(folderPath);
			}
			catch (Exception ex)
			{
				Plugin.Log?.Error("Failed to create folder " + folderPath + ": " + ex.Message);
			}
		}

		List<IconAsset> assets = new List<IconAsset>();
		if (Directory.Exists(folderPath))
		{
			List<string> imageFiles = new List<string>();
			foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
			{
				string ext = Path.GetExtension(filePath);
				if (string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase))
				{
					imageFiles.Add(filePath);
				}
			}
			imageFiles.Sort(StringComparer.OrdinalIgnoreCase);

			foreach (string filePath in imageFiles)
			{
				string ext = Path.GetExtension(filePath);
				IconAsset asset = string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase)
					? TryLoadGifAsset(filePath)
					: TryLoadPngAsset(filePath);
				if (asset != null)
				{
					assets.Add(asset);
				}
			}
		}

		if (assets.Count == 0)
		{
			Plugin.Log?.Info("No PNG/GIF images found in UserData/Beat115Icon. Using embedded fallback icon.");
			IconAsset fallback = CreateFallbackAsset();
			if (fallback != null)
			{
				assets.Add(fallback);
			}
		}

		_cachedAssets = assets.ToArray();
		int animatedCount = 0;
		for (int i = 0; i < _cachedAssets.Length; i++)
		{
			if (_cachedAssets[i].IsAnimated)
			{
				animatedCount++;
			}
		}
		Plugin.Log?.Info($"Beat115Icon loaded {_cachedAssets.Length} icon(s) ({animatedCount} animated GIF).");
	}

	private static IconAsset TryLoadPngAsset(string filePath)
	{
		try
		{
			byte[] bytes = File.ReadAllBytes(filePath);
			if (bytes.Length == 0)
			{
				return null;
			}
			Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
			if (!texture.LoadImage(bytes))
			{
				UnityEngine.Object.Destroy(texture);
				return null;
			}
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.filterMode = FilterMode.Trilinear;
			string baseName = Path.GetFileNameWithoutExtension(filePath);
			texture.name = baseName;
			UnityEngine.Object.DontDestroyOnLoad(texture);

			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
			sprite.name = baseName + "_Sprite";
			UnityEngine.Object.DontDestroyOnLoad(sprite);
			return new IconAsset(baseName, new Sprite[] { sprite }, new float[] { 0.1f }, new Texture2D[] { texture });
		}
		catch (Exception ex)
		{
			Plugin.Log?.Warn("Failed to load PNG image '" + filePath + "': " + ex.Message);
			return null;
		}
	}

	private static IconAsset TryLoadGifAsset(string filePath)
	{
		try
		{
			byte[] bytes = File.ReadAllBytes(filePath);
			if (bytes.Length == 0)
			{
				return null;
			}

			DecodedGif decoded = GifDecoder.Decode(bytes);
			if (decoded == null || decoded.Frames == null || decoded.Frames.Length == 0)
			{
				return null;
			}

			string baseName = Path.GetFileNameWithoutExtension(filePath);
			int frameCount = decoded.Frames.Length;

			if (frameCount == 1)
			{
				Texture2D tex = new Texture2D(decoded.Width, decoded.Height, TextureFormat.BGRA32, mipChain: false);
				tex.wrapMode = TextureWrapMode.Clamp;
				tex.filterMode = FilterMode.Bilinear;
				tex.name = baseName;
				tex.LoadRawTextureData(decoded.Frames[0].BgraPixels);
				tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
				UnityEngine.Object.DontDestroyOnLoad(tex);

				Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
				sprite.name = baseName + "_Sprite";
				UnityEngine.Object.DontDestroyOnLoad(sprite);
				return new IconAsset(baseName, new Sprite[] { sprite }, new float[] { decoded.Frames[0].DelaySec }, new Texture2D[] { tex });
			}

			Texture2D[] tempTextures = new Texture2D[frameCount];
			float[] delays = new float[frameCount];
			for (int i = 0; i < frameCount; i++)
			{
				DecodedGifFrame frame = decoded.Frames[i];
				delays[i] = frame.DelaySec;
				Texture2D frameTex = new Texture2D(decoded.Width, decoded.Height, TextureFormat.BGRA32, mipChain: false);
				frameTex.wrapMode = TextureWrapMode.Clamp;
				frameTex.filterMode = FilterMode.Bilinear;
				frameTex.LoadRawTextureData(frame.BgraPixels);
				frameTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
				tempTextures[i] = frameTex;
			}

			int maxAtlasSize = Mathf.Min(SystemInfo.maxTextureSize, 4096);
			Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
			atlas.wrapMode = TextureWrapMode.Clamp;
			atlas.filterMode = FilterMode.Bilinear;
			atlas.name = baseName + "_Atlas";

			Rect[] uvs = atlas.PackTextures(tempTextures, 2, maxAtlasSize, makeNoLongerReadable: true);
			if (uvs != null && uvs.Length == frameCount && atlas.width > 2 && atlas.height > 2)
			{
				for (int i = 0; i < frameCount; i++)
				{
					UnityEngine.Object.Destroy(tempTextures[i]);
				}
				UnityEngine.Object.DontDestroyOnLoad(atlas);

				Sprite[] sprites = new Sprite[frameCount];
				for (int i = 0; i < frameCount; i++)
				{
					Rect uv = uvs[i];
					float x = Mathf.Clamp(uv.x * atlas.width, 0f, Mathf.Max(0f, atlas.width - 1f));
					float y = Mathf.Clamp(uv.y * atlas.height, 0f, Mathf.Max(0f, atlas.height - 1f));
					float w = Mathf.Clamp(uv.width * atlas.width, 1f, atlas.width - x);
					float h = Mathf.Clamp(uv.height * atlas.height, 1f, atlas.height - y);
					Sprite sprite = Sprite.Create(atlas, new Rect(x, y, w, h), new Vector2(0.5f, 0.5f), 100f);
					sprite.name = $"{baseName}_Frame{i}";
					UnityEngine.Object.DontDestroyOnLoad(sprite);
					sprites[i] = sprite;
				}
				return new IconAsset(baseName, sprites, delays, new Texture2D[] { atlas });
			}

			// Fallback when PackTextures fails (e.g. many large frames exceeding atlas cap): keep individual frame textures.
			UnityEngine.Object.Destroy(atlas);
			Sprite[] fallbackSprites = new Sprite[frameCount];
			for (int i = 0; i < frameCount; i++)
			{
				Texture2D frameTex = tempTextures[i];
				frameTex.name = $"{baseName}_FrameTex{i}";
				frameTex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
				UnityEngine.Object.DontDestroyOnLoad(frameTex);
				Sprite sprite = Sprite.Create(frameTex, new Rect(0f, 0f, frameTex.width, frameTex.height), new Vector2(0.5f, 0.5f), 100f);
				sprite.name = $"{baseName}_Frame{i}";
				UnityEngine.Object.DontDestroyOnLoad(sprite);
				fallbackSprites[i] = sprite;
			}
			return new IconAsset(baseName, fallbackSprites, delays, tempTextures);
		}
		catch (Exception ex)
		{
			Plugin.Log?.Warn("Failed to load GIF image '" + filePath + "': " + ex.Message);
			return null;
		}
	}

	private static IconAsset CreateFallbackAsset()
	{
		byte[] bytes = LoadEmbeddedPngBytes();
		if (bytes == null || bytes.Length == 0)
		{
			return null;
		}
		Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
		texture.LoadImage(bytes);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Trilinear;
		texture.name = "Beat115IconFallbackTexture";
		UnityEngine.Object.DontDestroyOnLoad(texture);
		Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
		sprite.name = "Beat115IconFallbackSprite";
		UnityEngine.Object.DontDestroyOnLoad(sprite);
		return new IconAsset("hibiscus_fallback", new Sprite[] { sprite }, new float[] { 0.1f }, new Texture2D[] { texture });
	}

	private static void DisposeCachedAssets()
	{
		if (_cachedAssets == null || _cachedAssets.Length == 0)
		{
			return;
		}
		for (int i = 0; i < _cachedAssets.Length; i++)
		{
			IconAsset asset = _cachedAssets[i];
			if (asset == null)
			{
				continue;
			}
			if (asset.Frames != null)
			{
				for (int f = 0; f < asset.Frames.Length; f++)
				{
					if (asset.Frames[f] != null)
					{
						UnityEngine.Object.Destroy(asset.Frames[f]);
					}
				}
			}
			if (asset.OwnedTextures != null)
			{
				for (int t = 0; t < asset.OwnedTextures.Length; t++)
				{
					if (asset.OwnedTextures[t] != null)
					{
						UnityEngine.Object.Destroy(asset.OwnedTextures[t]);
					}
				}
			}
		}
		_cachedAssets = Array.Empty<IconAsset>();
	}

	private static byte[] LoadEmbeddedPngBytes()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedPngResource);
		if (stream == null)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
