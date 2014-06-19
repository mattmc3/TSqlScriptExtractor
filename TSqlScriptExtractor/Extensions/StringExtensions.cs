using System;

namespace Common.Extensions {
	public static class StringExtensions {
		
		public static string FormatWith(this string format, params object[] args) {
			return String.Format(format, args);
		}
		
		public static string FormatWith(this string format, object arg0) {
			return String.Format(format, arg0);
		}
		
		public static string FormatWith(this string format, object arg0, object arg1) {
			return String.Format(format, arg0, arg1);
		}
		
		public static string FormatWith(this string format, object arg0, object arg1, object arg2) {
			return String.Format(format, arg0, arg1, arg2);
		}
		
		public static string[] SplitLines(this string that) {
			return that.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		}
	}
}
