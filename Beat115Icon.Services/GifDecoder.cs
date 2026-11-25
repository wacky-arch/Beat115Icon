using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Beat115Icon.Services;

public sealed class DecodedGifFrame
{
	public byte[] BgraPixels { get; }

	public float DelaySec { get; }

	public DecodedGifFrame(byte[] bgraPixels, float delaySec)
	{
		BgraPixels = bgraPixels;
		DelaySec = delaySec;
	}
}

public sealed class DecodedGif
{
	public int Width { get; }

	public int Height { get; }

	public DecodedGifFrame[] Frames { get; }

	public DecodedGif(int width, int height, DecodedGifFrame[] frames)
	{
		Width = width;
		Height = height;
		Frames = frames;
	}
}

public static class GifDecoder
{
	private const int PropertyTagFrameDelay = 20736;

	private const float DefaultDelaySec = 0.10f;

	private const float MinDelaySec = 0.02f;

	private const float MaxDelaySec = 10.0f;

	public static DecodedGif Decode(byte[] gifBytes, int maxFrameDimension = 512)
	{
		if (gifBytes == null || gifBytes.Length == 0)
		{
			return null;
		}

		using MemoryStream ms = new MemoryStream(gifBytes, writable: false);
		using Image image = Image.FromStream(ms);

		FrameDimension dimension = ResolveTimeDimension(image);
		int frameCount = Math.Max(1, image.GetFrameCount(dimension));
		byte[] delayBytes = TryGetDelayPropertyBytes(image);

		int srcWidth = Math.Max(1, image.Width);
		int srcHeight = Math.Max(1, image.Height);
		int outWidth = srcWidth;
		int outHeight = srcHeight;

		if (frameCount > 1 && maxFrameDimension > 0)
		{
			int maxSide = Math.Max(srcWidth, srcHeight);
			if (maxSide > maxFrameDimension)
			{
				float scale = (float)maxFrameDimension / maxSide;
				outWidth = Math.Max(1, (int)Math.Round(srcWidth * scale));
				outHeight = Math.Max(1, (int)Math.Round(srcHeight * scale));
			}
		}

		bool resized = outWidth != srcWidth || outHeight != srcHeight;
		DecodedGifFrame[] frames = new DecodedGifFrame[frameCount];
		Rectangle srcRect = new Rectangle(0, 0, srcWidth, srcHeight);
		Rectangle destRect = new Rectangle(0, 0, outWidth, outHeight);

		for (int i = 0; i < frameCount; i++)
		{
			image.SelectActiveFrame(dimension, i);
			float delaySec = ParseFrameDelaySec(delayBytes, i);
			byte[] pixels = ExtractFrameBgraPixels(image, srcRect, destRect, outWidth, outHeight, resized);
			frames[i] = new DecodedGifFrame(pixels, delaySec);
		}

		return new DecodedGif(outWidth, outHeight, frames);
	}

	private static FrameDimension ResolveTimeDimension(Image image)
	{
		Guid timeGuid = FrameDimension.Time.Guid;
		Guid[] dims = image.FrameDimensionsList;
		if (dims != null && dims.Length > 0)
		{
			for (int i = 0; i < dims.Length; i++)
			{
				if (dims[i] == timeGuid)
				{
					return FrameDimension.Time;
				}
			}
			return new FrameDimension(dims[0]);
		}
		return FrameDimension.Time;
	}

	private static byte[] TryGetDelayPropertyBytes(Image image)
	{
		try
		{
			int[] propIds = image.PropertyIdList;
			if (propIds == null || Array.IndexOf(propIds, PropertyTagFrameDelay) < 0)
			{
				return null;
			}
			return image.GetPropertyItem(PropertyTagFrameDelay)?.Value;
		}
		catch
		{
			return null;
		}
	}

	private static float ParseFrameDelaySec(byte[] delayBytes, int frameIndex)
	{
		if (delayBytes == null || (frameIndex * 4 + 4) > delayBytes.Length)
		{
			return DefaultDelaySec;
		}
		int centiseconds = BitConverter.ToInt32(delayBytes, frameIndex * 4);
		float sec = centiseconds * 0.01f;
		if (sec <= 0.015f)
		{
			return DefaultDelaySec;
		}
		if (sec < MinDelaySec)
		{
			return MinDelaySec;
		}
		if (sec > MaxDelaySec)
		{
			return MaxDelaySec;
		}
		return sec;
	}

	private static byte[] ExtractFrameBgraPixels(Image image, Rectangle srcRect, Rectangle destRect, int outWidth, int outHeight, bool resized)
	{
		using Bitmap bmp = new Bitmap(outWidth, outHeight, PixelFormat.Format32bppArgb);
		using (Graphics g = Graphics.FromImage(bmp))
		{
			g.Clear(Color.Transparent);
			g.CompositingMode = CompositingMode.SourceCopy;
			if (resized)
			{
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			}
			else
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.PixelOffsetMode = PixelOffsetMode.Half;
			}
			g.DrawImage(image, destRect, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel);
		}

		// Flip vertically so pixel row 0 corresponds to bottom-left in Unity Texture2D.
		bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

		int rowBytes = outWidth * 4;
		byte[] pixels = new byte[rowBytes * outHeight];
		BitmapData data = bmp.LockBits(destRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		try
		{
			if (data.Stride == rowBytes)
			{
				Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
			}
			else
			{
				for (int y = 0; y < outHeight; y++)
				{
					IntPtr rowPtr = IntPtr.Add(data.Scan0, y * data.Stride);
					Marshal.Copy(rowPtr, pixels, y * rowBytes, rowBytes);
				}
			}
		}
		finally
		{
			bmp.UnlockBits(data);
		}
		return pixels;
	}
}
