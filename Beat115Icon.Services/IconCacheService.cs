using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IPA.Utilities;
using UnityEngine;

namespace Beat115Icon.Services;

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
		int num = _random.Next(0, _cachedSprites.Length);
		return _cachedSprites[num];
	}

	public static void LoadSprites(string customDir = null)
	{
		string text = ((string.IsNullOrEmpty(customDir) || !Directory.Exists(customDir)) ? Path.Combine(UnityGame.UserDataPath, "Beat115Icon") : customDir);
		if (!Directory.Exists(text))
		{
			try
			{
				Directory.CreateDirectory(text);
			}
			catch (Exception ex)
			{
				Plugin.Log?.Error("Failed to create folder " + text + ": " + ex.Message);
			}
		}
		List<Sprite> list = new List<Sprite>();
		if (Directory.Exists(text))
		{
			string[] files = Directory.GetFiles(text, "*.png", SearchOption.TopDirectoryOnly);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			string[] array = files;
			foreach (string text2 in array)
			{
				try
				{
					byte[] array2 = File.ReadAllBytes(text2);
					if (array2.Length != 0)
					{
						Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
						if (texture2D.LoadImage(array2))
						{
							texture2D.wrapMode = TextureWrapMode.Clamp;
							texture2D.filterMode = FilterMode.Trilinear;
							texture2D.name = Path.GetFileNameWithoutExtension(text2);
							UnityEngine.Object.DontDestroyOnLoad(texture2D);
							Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
							sprite.name = texture2D.name + "_Sprite";
							UnityEngine.Object.DontDestroyOnLoad(sprite);
							list.Add(sprite);
						}
					}
				}
				catch (Exception ex2)
				{
					Plugin.Log?.Warn("Failed to load image '" + text2 + "': " + ex2.Message);
				}
			}
		}
		if (list.Count == 0)
		{
			Plugin.Log?.Info("No PNG images found in UserData/Beat115Icon. Using embedded fallback icon.");
			Sprite sprite2 = CreateFallbackSprite();
			if (sprite2 != null)
			{
				list.Add(sprite2);
			}
		}
		_cachedSprites = list.ToArray();
		Plugin.Log?.Info($"Beat115Icon initialized with {_cachedSprites.Length} sprite(s).");
	}

	private static Sprite CreateFallbackSprite()
	{
		byte[] array = LoadEmbeddedPngBytes();
		if (array == null || array.Length == 0)
		{
			return null;
		}
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true);
		texture2D.LoadImage(array);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		texture2D.filterMode = FilterMode.Trilinear;
		texture2D.name = "Beat115IconFallbackTexture";
		UnityEngine.Object.DontDestroyOnLoad(texture2D);
		Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);
		sprite.name = "Beat115IconFallbackSprite";
		UnityEngine.Object.DontDestroyOnLoad(sprite);
		return sprite;
	}

	private static byte[] LoadEmbeddedPngBytes()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Beat115Icon.Resources.hibiscus.png");
		if (stream == null)
		{
			return null;
		}
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}
}
