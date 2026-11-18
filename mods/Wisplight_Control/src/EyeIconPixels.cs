using System;

namespace OctoberJemini.WisplightControl
{
	public static class EyeIconPixels
	{
		public const int Size = 64;

		public static byte[] Render(bool hidden)
		{
			byte[] array = new byte[16384];
			for (int i = 0; i < 64; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					double num = ((double)j + 0.5 - 32.0) / 32.0;
					double num2 = ((double)i + 0.5 - 32.0) / 32.0;
					int num3 = (i * 64 + j) * 4;
					if (!(num * num + num2 * num2 > 0.8835999999999999))
					{
						array[num3] = 18;
						array[num3 + 1] = 20;
						array[num3 + 2] = 23;
						array[num3 + 3] = 235;
						bool flag = num * num + num2 * num2 > 0.7569;
						double num4 = Math.Abs(num);
						double num5 = 0.38 * Math.Sin(Math.PI * (num + 0.72) / 1.44);
						bool num6 = num4 < 0.72 && Math.Abs(Math.Abs(num2) - num5) < 0.055;
						bool flag2 = num * num + num2 * num2 < 0.0256;
						if (flag)
						{
							array[num3] = 82;
							array[num3 + 1] = 74;
							array[num3 + 2] = 53;
						}
						if (num6 || flag2)
						{
							array[num3] = (byte)(hidden ? 166 : byte.MaxValue);
							array[num3 + 1] = (byte)(hidden ? 170 : 222);
							array[num3 + 2] = (byte)(hidden ? 177 : 138);
						}
						if (hidden && num4 < 0.67 && Math.Abs(num2 + num * 0.85) < 0.085)
						{
							array[num3] = byte.MaxValue;
							array[num3 + 1] = 179;
							array[num3 + 2] = 68;
						}
					}
				}
			}
			return array;
		}
	}
}
