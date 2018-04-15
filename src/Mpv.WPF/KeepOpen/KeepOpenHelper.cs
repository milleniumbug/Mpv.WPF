using System;

namespace Mpv.WPF
{
	internal static class KeepOpenHelper
	{
		public static string ToString(KeepOpen value)
		{
			switch (value)
			{
				case KeepOpen.Yes:
					return "yes";
				case KeepOpen.No:
					return "no";
				case KeepOpen.Always:
					return "always";
			}

			return null;
		}

		public static KeepOpen FromString(string stringValue)
		{
			switch (stringValue)
			{
				case "yes":
					return KeepOpen.Yes;
				case "no":
					return KeepOpen.No;
				case "always":
					return KeepOpen.Always;
			}

			throw new ArgumentException("Invalid value for \"keep-open\" property.");
		}
	}
}