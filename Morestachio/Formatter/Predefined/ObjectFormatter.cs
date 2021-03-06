using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Morestachio.Document;
using Morestachio.Formatter.Framework;
using Morestachio.Formatter.Framework.Attributes;
using Morestachio.Framework.Context.Resolver;
using Morestachio.Helper.Logging;

namespace Morestachio.Formatter.Predefined
{
#pragma warning disable CS1591
	/// <summary>
	///		Contains the basic Formatting operations
	/// </summary>
	public static class ObjectFormatter
	{
		[MorestachioFormatter("ToString", "Formats a value according to the structure set by the argument")]
		public static string Formattable(IFormattable source, string argument, [ExternalData]ParserOptions options)
		{
			return source.ToString(argument, options.CultureInfo);
		}
		
		[MorestachioFormatter("ToString", "Formats the value according to the build in rules for that object")]
		public static string Formattable(object source)
		{
			return source.ToString();
		}
		
		[MorestachioFormatter("ToXml", null)]
		public static string ToXml(object source, [ExternalData]ParserOptions options)
		{
			var xmlSerializer = new XmlSerializer(source.GetType());
			using (var xmlStream = new MemoryStream())
			{
				xmlSerializer.Serialize(xmlStream, source);
				return options.Encoding.GetString(xmlStream.ToArray());
			}
		}

		[MorestachioFormatter("AsObject", "Wraps an IDictionary as an object")]
		[MorestachioGlobalFormatter("AsObject", "Wraps an IDictionary as an object")]
		public static object AsObject(IDictionary<string, object> value)
		{
			return new DicFassade(value);
		}

		private class DicFassade : DynamicObject
		{
			private readonly IDictionary<string, object> _values;

			public DicFassade(IDictionary<string, object> values)
			{
				_values = values;
			}

			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				_values[binder.Name] = value;
				return true;
			}

			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				return _values.TryGetValue(binder.Name, out result);
			}
		}
		
		[MorestachioFormatter("Call", "Calls a formatter by dynamically providing name and arguments")]
		public static async Task<object> Call(object source, string formatterName, 
			[ExternalData] ParserOptions options,
			[ExternalData] ScopeData scopeData,
			[RestParameter] params object[] arguments)
		{
			var argumentTypes = arguments.Select((item, index) => new FormatterArgumentType(index, null, item)).ToArray();
			var formatterMatch = options.Formatters.PrepareCallMostMatchingFormatter(source.GetType(),
				argumentTypes,
				formatterName, options, scopeData);
			if (formatterMatch == null)
			{
				return null;
			}

			return await options.Formatters.Execute(formatterMatch, source, options,
				argumentTypes);
		}
		
		[MorestachioFormatter("Get", "Gets a specific property from an object or IDictionary")]
		public static object Get(object source, string propertyName, [ExternalData]ParserOptions options)
		{
			if (options.ValueResolver?.CanResolve(source.GetType(), source, propertyName, null) == true)
			{
				return options.ValueResolver.Resolve(source.GetType(), source, propertyName, null);
			}

			if (!options.HandleDictionaryAsObject && source is IDictionary<string, object> dict)
			{
				if (dict.TryGetValue(propertyName, out var val))
				{
					return val;
				}

				return null;
			}

			if (source is IMorestachioPropertyResolver cResolver)
			{
				if (cResolver.TryGetValue(propertyName, out var value))
				{
					return value;
				}

				return null;
			}

			if (source is ICustomTypeDescriptor ctype)
			{
				return ctype.GetProperties().Find(propertyName, false)?.GetValue(source);
			}

			return source.GetType().GetProperty(propertyName)?.GetValue(source);
		}
	}
}