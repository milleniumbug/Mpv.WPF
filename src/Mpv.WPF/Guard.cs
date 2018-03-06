using System;

namespace Mpv.WPF
{
	internal static class Guard
	{
		public static void AgainstNull(object value, string name)
		{
			if (value == null)
				throw new ArgumentNullException(name);
		}

		public static void AgainstNullOrEmptyOrWhiteSpaceString(string value, string name)
		{
			AgainstNull(value, name);

			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException(name);
		}
	}
}