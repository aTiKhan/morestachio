﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Morestachio.Document.Contracts;
using Morestachio.Document.Custom;
using Morestachio.Framework.Expression;
using Morestachio.Framework.Expression.Parser;
using Morestachio.Framework.Tokenizing;
using Morestachio.Helper.Localization.Documents.LocDocument;

namespace Morestachio.Helper.Localization.Documents.LocPDocument
{
	/// <summary>
	///		Provides access to <see cref="MorestachioLocalizationDocumentItem"/>
	/// </summary>
	public class MorestachioLocalizationBlockProvider : BlockDocumentItemProviderBase
	{
		/// <inheritdoc />
		public MorestachioLocalizationBlockProvider() : base(OpenTag, CloseTag)
		{
		}

		/// <summary>
		///		Defines the opening tag for the Localization provider
		/// </summary>
		public const string OpenTag = "#LOCP ";

		/// <summary>
		///		Defines the closing tag for the Localization provider
		/// </summary>
		public const string CloseTag = "/LOCP";

		/// <inheritdoc />
		public override IEnumerable<TokenPair> Tokenize(TokenInfo token, ParserOptions options)
		{
			var trim = token.Token;
			if (trim.StartsWith(TagOpen, StringComparison.OrdinalIgnoreCase))
			{
				yield return new TokenPair(TagOpen.Trim(), token.TokenizerContext.CurrentLocation, ExpressionParser.ParseExpression(trim.Remove(0, OpenTag.Length).Trim(), token.TokenizerContext));
			}
			if (string.Equals(trim, TagClose, StringComparison.OrdinalIgnoreCase))
			{
				yield return new TokenPair(TagClose, trim, token.TokenizerContext.CurrentLocation);
			}
		}

		/// <inheritdoc />
		public override IDocumentItem CreateDocumentItem(string tag, string value, TokenPair token,
			ParserOptions options, IEnumerable<ITokenOption> tagCreationOptions)
		{
			return new MorestachioLocalizationDocumentItem(token.TokenLocation, token.MorestachioExpression, null, tagCreationOptions);
		}
	}
}
