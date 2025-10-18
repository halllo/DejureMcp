using System;
using System.Collections.Generic;
using System.Text;

internal static class PercentEncoding
{
	public static string Encode(this string decoded)
	{
		if (string.IsNullOrEmpty(decoded)) return decoded;

		// Use ISO-8859-1 (Latin-1) encoding for German umlauts
		var latin1 = Encoding.GetEncoding("iso-8859-1");
		var bytes = latin1.GetBytes(decoded);

		var result = new StringBuilder();
		foreach (byte b in bytes)
		{
			// ASCII characters that don't need encoding
			if ((b >= 'A' && b <= 'Z') ||
				(b >= 'a' && b <= 'z') ||
				(b >= '0' && b <= '9') ||
				b == '-' || b == '_' || b == '.' || b == '~')
			{
				result.Append((char)b);
			}
			else
			{
				result.Append($"%{b:X2}");
			}
		}
		return result.ToString();
	}

	public static string Decode(this string encoded)
	{
		if (string.IsNullOrEmpty(encoded)) return encoded;

		// Try UTF-8 first
		try
		{
			var utf8 = Uri.UnescapeDataString(encoded);
			if (!utf8.Contains("%")) return utf8;
		}
		catch { }

		// Fall back to Latin-1 for German umlauts
		var bytes = new List<byte>();
		for (int i = 0; i < encoded.Length; i++)
		{
			if (encoded[i] == '%' && i + 2 < encoded.Length)
			{
				if (byte.TryParse(encoded.Substring(i + 1, 2), System.Globalization.NumberStyles.HexNumber, null, out byte b))
				{
					bytes.Add(b);
					i += 2;
				}
				else
				{
					bytes.Add((byte)encoded[i]);
				}
			}
			else
			{
				bytes.Add((byte)encoded[i]);
			}
		}
		return Encoding.GetEncoding("iso-8859-1").GetString(bytes.ToArray());
	}

	public static string NoSpans(this string input) => input.Replace("<span>", "").Replace("</span>", "");
}