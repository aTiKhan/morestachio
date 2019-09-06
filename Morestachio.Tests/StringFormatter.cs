﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Morestachio.Formatter.Framework.Tests
{
	public static class StringFormatter
	{
		[MorestachioFormatter("Plus", "XXX")]
		public static int Reverse(object any, int right, int additional)
		{
			return right + additional;
		}

		[MorestachioFormatter("reverse", "XXX")]
		public static string Reverse(string originalObject)
		{
			return originalObject.Reverse().Select(e => e.ToString()).Aggregate((e, f) => e + f);
		}

		[MorestachioFormatter("reverseAsync", "XXX")]
		public static async Task<string> ReverseAsync(string originalObject)
		{
			await Task.Delay(500);
			return originalObject.Reverse().Select(e => e.ToString()).Aggregate((e, f) => e + f);
		}

		[MorestachioFormatter("optional", "XXX")]
		public static string Optional(string originalObject, [Optional]int optional)
		{
			return "OPTIONAL " + originalObject;
		}

		[MorestachioFormatter("defaultValue", "XXX")]
		public static string DefaultValue(string originalObject, int optional = 12)
		{
			return "DEFAULT " + originalObject;
		}

		[MorestachioFormatter("reverse-arg", "XXX")]
		public static string ReverseWithArgSuccess(string originalObject, string argument)
		{
			return argument;
		}

		[MorestachioFormatter("fod", "XXX")]
		public static T GenericTest<T>(IEnumerable<T> originalObject)
		{
			return originalObject.FirstOrDefault();
		}
	}
}