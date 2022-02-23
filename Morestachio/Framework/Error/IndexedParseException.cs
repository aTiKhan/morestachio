﻿using System;
using Morestachio.Framework.Context.Resolver;
using Morestachio.Parsing.ParserErrors;

namespace Morestachio.Framework.Error;

/// <summary>
///     Indicates a parse error including line and character info.
/// </summary>
public class IndexedParseException : MorestachioException
{
	internal static string FormatMessage(string message, CharacterLocationExtended location)
	{
		return $"{location.Line}:{location.Character} {message}" +
			Environment.NewLine +
			location.Render();
	}

	internal IndexedParseException(CharacterLocationExtended location, string message)
		: base(FormatMessage(message, location))
	{
		Location = location;
	}

	/// <summary>
	///		The location of the error within the original template
	/// </summary>
	public CharacterLocationExtended Location { get; set; }
}

/// <summary>
///		Indicates a path lookup in a template that was build using the <see cref="ParserOptions.StrictExecution"/> flag.
/// </summary>
public class UnresolvedPathException : MorestachioException
{
	/// <inheritdoc />
	public UnresolvedPathException(InvalidPathEventArgs pathEventArgs) : base("Could not obtain path in a strict document.")
	{
		PathEventArgs = pathEventArgs;
	}

	/// <summary>
	///		Info of the invalid path.
	/// </summary>
	public InvalidPathEventArgs PathEventArgs { get; private set; }
}