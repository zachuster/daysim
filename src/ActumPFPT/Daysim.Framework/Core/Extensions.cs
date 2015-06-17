﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Daysim.Framework.Core {
	public static class Extensions {
		private const long ONE_KILOBYTE = 1024L;
		private const long ONE_MEGABYTE = ONE_KILOBYTE * 1024L;
		private const long ONE_GIGABYTE = ONE_MEGABYTE * 1024L;

		public static bool IsBetween(this double value, double start, double end) {
			return value >= start && value <= end;
		}

		public static bool IsBetween(this int value, int start, int end) {
			return value >= start && value <= end;
		}

		public static bool IsLeftExclusiveBetween(this double value, double start, double end) {
			return value > start && value <= end;
		}

		public static bool IsLeftExclusiveBetween(this int value, int start, int end) {
			return value > start && value <= end;
		}

		public static bool IsRightExclusiveBetween(this double value, double start, double end) {
			return value >= start && value < end;
		}

		public static bool IsRightExclusiveBetween(this int value, int start, int end) {
			return value >= start && value < end;
		}

		public static bool AlmostEquals(this double x, double y) {
			//var epsilon = Math.Pow(.1, precision); // 14

			return (Math.Abs(x - y) < Constants.EPSILON);
		}

		public static string Truncate(this string s, int length) {
			return s.Substring(0, Math.Min(length, s.Length));
		}

		public static string Clean(this string s) {
			if (s == null || s == "null") {
				return null;
			}

			return s.ToLower();
		}

		public static int ToMode(this string s) {
			switch (s) {
				case "walk":
					return Constants.Mode.WALK;

				case "bike":
					return Constants.Mode.BIKE;

				case "sov":
					return Constants.Mode.SOV;

				case "hov2":
					return Constants.Mode.HOV2;

				case "hov3":
					return Constants.Mode.HOV3;

				case "transit":
					return Constants.Mode.TRANSIT;

				case "park-and-ride":
					return Constants.Mode.PARK_AND_RIDE;

				case "school-bus":
					return Constants.Mode.SCHOOL_BUS;
			}

			return Constants.Mode.NONE;
		}

		public static int ToPathType(this string s) {
			switch (s) {
				case "full-network":
					return Constants.PathType.FULL_NETWORK;
				case "no-tolls":
					return Constants.PathType.NO_TOLLS;
				case "local-bus":
					return Constants.PathType.LOCAL_BUS;
				case "light-rail":
					return Constants.PathType.LIGHT_RAIL;
				case "premium-bus":
					return Constants.PathType.PREMIUM_BUS;
				case "commuter-rail":
					return Constants.PathType.COMMUTER_RAIL;
				case "ferry":
					return Constants.PathType.FERRY;
				case "no-toll-network":
					return Constants.PathType.NO_TOLLS;
			}

			return Constants.PathType.NONE;
		}

		public static int ToVotGroup(this string s) {
			switch (s) {
				case "very-low":
					return Constants.VotGroup.VERY_LOW;
				case "low":
					return Constants.VotGroup.LOW;
				case "medium":
					return Constants.VotGroup.MEDIUM;
				case "high":
					return Constants.VotGroup.HIGH;
				case "very-high":
					return Constants.VotGroup.VERY_HIGH;
				case "any":
				case "all":
				case "default":
					return Constants.VotGroup.DEFAULT;
			}

			return Constants.VotGroup.NONE;
		}

		public static double ToFactor(this string s) {
			if (s == null || s == "null") {
				return 1;
			}

			return double.Parse(s.Trim());
		}

		public static int GetIndex(this int[] a, string s) {
			var hashCode = s.GetHashCode();

			for (var i = 0; i < a.Length; i++) {
				if (a[i] == hashCode) {
					return i;
				}
			}

			return -1;
		}

		public static int GetIndex(this int[] a, int hashCode) {
			for (var i = 0; i < a.Length; i++) {
				if (a[i] == hashCode) {
					return i;
				}
			}

			return -1;
		}

		public static int ToMinutesAfter3AM(this int minute) {
			var offset = minute - 179;

			if (offset <= 0) {
				return 1440 + offset;
			}

			return offset;

//			int minutes;
//			var hours = Math.DivRem(clockTime24Hour, 100, out minutes);
//			
//			if (hours >= 3) {
//				return 60 * (hours - 3) + minutes; // subtract 3 hours
//			}
//
//			return 60 * (hours - 3 + 24) + minutes; // subtract 3 hours and add 24 hours to make it greater than 0
		}

		public static int ToMinutesAfterMidnight(this int minute) {
			var offset = 179 + minute;

			if (offset >= 1440) {
				return offset - 1440;
			}

			return offset;

//			int minutes;
//			var hours = Math.DivRem(minutesAfter3AM, 60, out minutes);
//			
//			if (hours < 21) {
//				return 100 * (hours + 3) + minutes; // add 3 hours
//			}
//
//			return 100 * (hours + 3 - 24) + minutes; // add 3 hours and subtract 24 hours to make it less than 2400
		}

		public static int ToFlag(this bool expression) {
			return expression ? 1 : 0;
		}

		public static string ToSentenceCase(this string s) {
			return Regex.Replace(s, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
		}

		public static string ToFileSize(this long l) {
			return String.Format(new FileSizeFormatProvider(), "{0:fs}", l);
		}

		public static FileInfo ToFile(this string s) {
			return string.IsNullOrEmpty(s) ? null : new FileInfo(s);
		}

		public static string ToMD5Checksum(this FileInfo file) {
			using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var md5 = new MD5CryptoServiceProvider();
				var hashCode = md5.ComputeHash(stream);
				var checksum = BitConverter.ToString(hashCode);

				return checksum;
			}
		}

		public static string ToUncPath(this string path, string machineName) {
			return 
				Environment.MachineName.Equals(machineName, StringComparison.OrdinalIgnoreCase) 
				? path 
				: Regex.Replace(path, "^[A-Z]:", string.Format(@"\\{0}", machineName));
		}
	}
}