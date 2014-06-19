using System;
using System.Text.RegularExpressions;

namespace Common.Extensions {
	public static class RegexOptionsHelper {
		/// <summary>
		/// Converts a string to regex options.  The string matches Perl's mappings.
		/// x = RegexOptions.IgnorePatternWhitespace,
		/// c = RegexOptions.Compiled,
		/// m = RegexOptions.Multiline,
		/// s = RegexOptions.Singleline,
		/// i = RegexOptions.IgnoreCase
		/// </summary>
		public static RegexOptions GetRegexOptions(string opts) {
			var resultOpts = RegexOptions.None;
			foreach (char c in opts.ToLower().ToCharArray()) {
				switch (c) {
					case 'x': resultOpts |= RegexOptions.IgnorePatternWhitespace; break;
					case 'm': resultOpts |= RegexOptions.Multiline; break;
					case 's': resultOpts |= RegexOptions.Singleline; break;
					case 'i': resultOpts |= RegexOptions.IgnoreCase; break;
					case 'c': resultOpts |= RegexOptions.Compiled; break;
					default: break;
				}
			}
			return resultOpts;
		}
	}
}
