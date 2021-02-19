﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Morestachio.Formatter.Framework.Attributes;

namespace Morestachio.Formatter.Framework
{
	/// <summary>
	///		Add Extensions for easy runtime added Functions
	/// </summary>

	public static class MorestachioFormatterServiceExtensions
	{
		/// <summary>
		///     Adds all formatter that are decorated with the <see cref="MorestachioFormatterAttribute" />
		/// </summary>
		public static void AddFromType(this IMorestachioFormatterService service, Type type)
		{
			foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				var hasFormatterAttr = method.GetCustomAttributes<MorestachioFormatterAttribute>();
				foreach (var morestachioFormatterAttribute in hasFormatterAttr)
				{
					if (morestachioFormatterAttribute == null)
					{
						continue;
					}

					service.Add(method, morestachioFormatterAttribute);
				}
			}
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
			{
				var hasFormatterAttr = method.GetCustomAttributes<MorestachioFormatterAttribute>();
				foreach (var morestachioFormatterAttribute in hasFormatterAttr)
				{
					if (morestachioFormatterAttribute == null)
					{
						continue;
					}

					morestachioFormatterAttribute.LinkFunctionTarget = true;
					morestachioFormatterAttribute.IsSourceObjectAware = false;
					service.Add(method, morestachioFormatterAttribute);
				}
			}
		}
		/// <summary>
		///     Adds all formatter that are decorated with the <see cref="MorestachioFormatterAttribute" />
		/// </summary>
		public static void AddFromType<T>(this IMorestachioFormatterService service)
		{
			AddFromType(service, typeof(T));
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle(this IMorestachioFormatterService service, Delegate function, string name)
		{
			return service.AddSingle(function, new MorestachioFormatterAttribute(name, "Autogenerated description")
			{
				OutputType = function.Method.ReturnType,
				ReturnHint = "None",
			});
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal(this IMorestachioFormatterService service, Delegate function, string name)
		{
			return service.AddSingle(function, new MorestachioGlobalFormatterAttribute(name, "Autogenerated description")
			{
				OutputType = function.Method.ReturnType,
				ReturnHint = "None",
			});
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle(this IMorestachioFormatterService service, Delegate function, MorestachioFormatterAttribute attribute)
		{
			attribute.OutputType = function.Method.ReturnType;
			attribute.ReturnHint = "None";

			var morestachioFormatterModel = service.Add(function.Method, attribute);
			morestachioFormatterModel.FunctionTarget = function.Target;
			return morestachioFormatterModel.MetaData;
		}

		#region Action Overloads

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle(this IMorestachioFormatterService service, Action function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T>(this IMorestachioFormatterService service, Action<T> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1>(this IMorestachioFormatterService service, Action<T, T1> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2>(this IMorestachioFormatterService service, Action<T, T1, T2> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2, T3>(this IMorestachioFormatterService service, Action<T, T1, T2, T3> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2, T3, T4>(this IMorestachioFormatterService service, Action<T, T1, T2, T3, T4> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		#endregion

		#region Function Overloads

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T>(this IMorestachioFormatterService service, Func<T> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1>(this IMorestachioFormatterService service, Func<T, T1> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2>(this IMorestachioFormatterService service, Func<T, T1, T2> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2, T3>(this IMorestachioFormatterService service, Func<T, T1, T2, T3> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingle<T, T1, T2, T3, T4>(this IMorestachioFormatterService service, Func<T, T1, T2, T3, T4> function, string name)
		{
			return service.AddSingle((Delegate)function, name);
		}

		#endregion

		#region Action Overloads

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal(this IMorestachioFormatterService service, Action function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T>(this IMorestachioFormatterService service, Action<T> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1>(this IMorestachioFormatterService service, Action<T, T1> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2>(this IMorestachioFormatterService service, Action<T, T1, T2> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2, T3>(this IMorestachioFormatterService service, Action<T, T1, T2, T3> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2, T3, T4>(this IMorestachioFormatterService service, Action<T, T1, T2, T3, T4> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		#endregion

		#region Function Overloads

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T>(this IMorestachioFormatterService service, Func<T> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1>(this IMorestachioFormatterService service, Func<T, T1> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2>(this IMorestachioFormatterService service, Func<T, T1, T2> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2, T3>(this IMorestachioFormatterService service, Func<T, T1, T2, T3> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		/// <summary>
		///		Adds a new Function to the list of formatters
		/// </summary>
		public static MultiFormatterInfoCollection AddSingleGlobal<T, T1, T2, T3, T4>(this IMorestachioFormatterService service, Func<T, T1, T2, T3, T4> function, string name)
		{
			return service.AddSingleGlobal((Delegate)function, name);
		}

		#endregion
	}
}
