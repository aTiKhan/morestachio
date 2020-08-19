﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Morestachio.Formatter.Framework.Attributes;
using Morestachio.Formatter.Framework.Converter;

namespace Morestachio.Tests
{
	public static class CustomConverterFormatter
	{
		public class TestObject
		{
			public int No { get; set; }
		}

		public class ExpectedObject
		{
			public int No { get; set; }
		}

		public class TestToExpectedObjectConverter : IFormatterValueConverter
		{
			public bool CanConvert(Type sourceType, Type requestedType)
			{
				return requestedType == typeof(ExpectedObject) && sourceType == typeof(TestObject);
			}

			public object Convert(object value, Type requestedType)
			{
				return new ExpectedObject()
				{
					No = (value as TestObject).No
				};
			}
		}

		[MorestachioFormatter("ReturnValue", "XXX")]
		public static int ReturnValue(ExpectedObject value)
		{
			return value.No;
		}

		[MorestachioFormatter("ReturnValueExplicitConverter", "XXX")]
		public static int ReturnValueA([FormatterValueConverter(typeof(TestToExpectedObjectConverter))]ExpectedObject value)
		{
			return value.No;
		}
	}

	public static class StringFormatter
	{
		[MorestachioFormatter("ExpectInt", "XXX")]
		public static string ExpectInt(int expected)
		{
			return expected.ToString("X2");
		}

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

		[MorestachioFormatter("rest", "XXX")]
		public static string Rest(string originalObject, [RestParameter]object[] rest)
		{
			return "ORIGINAL: " + originalObject + " REST:" + rest.Select(f => f.ToString()).Aggregate((e, f) => e + f);
		}

		[MorestachioFormatter("defaultValue", "XXX")]
		public static string DefaultValue(string originalObject, int optional = 12)
		{
			return "DEFAULT " + originalObject;
		}

		[MorestachioFormatter("ReverseArg", "XXX")]
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