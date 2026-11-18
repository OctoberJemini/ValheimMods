using System;

namespace OctoberJemini.WisplightControl
{
	public static class WispRules
	{
		public const int MaxQuality = 4;

		public const int CraftAmount = 1;

		public const int WispsPerUpgrade = 20;

		public const int SilverPerLevel = 4;

        public static int MaterialCost(string prefab, int targetQuality)
        {
            int quality = Quality(targetQuality);
            if (prefab == "Wisp") return quality == 1 ? 1 : WispsPerUpgrade;
            if (prefab == "Silver") return quality == 1 ? 1 : UpgradeCost(quality, SilverPerLevel);
            if (prefab == "Eitr") return quality == 2 ? 10 : quality == 3 ? 25 : 0;
            if (prefab == "TrophyGjall") return quality == 3 ? 1 : 0;
            if (prefab == "TrophySeekerBrute") return quality == 4 ? 1 : 0;
            return 0;
        }

		public static bool CharacterGlow(bool wispHidden, float brightness)
		{
			if (wispHidden)
			{
				return brightness > 0f;
			}
			return false;
		}

		public static int Quality(int quality)
		{
			return Math.Max(1, Math.Min(4, quality));
		}

		public static float Radius(int quality, float firstRadius, float step)
		{
			return Math.Max(1f, firstRadius) + (float)(Quality(quality) - 1) * Math.Max(0f, step);
		}

		public static int UpgradeCost(int targetQuality, int perLevel)
		{
			int num = Quality(targetQuality);
			if (num > 1)
			{
				return ((num == 4) ? 4 : (num - 1)) * Math.Max(0, perLevel);
			}
			return 0;
		}

		public static bool IsWispHidden(string saved)
		{
			return saved != "visible";
		}

		public static string Toggle(string saved)
		{
			if (!IsWispHidden(saved))
			{
				return "hidden";
			}
			return "visible";
		}
	}
}
