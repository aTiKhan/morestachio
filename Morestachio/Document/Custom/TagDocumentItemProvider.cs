﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Document.Contracts;
using Morestachio.Document.Visitor;
using Morestachio.Framework;

namespace Morestachio.Document.Custom
{
	/// <summary>
	///		Can be used to create a single statement Tag
	/// </summary>
	public class TagDocumentItemProvider : CustomDocumentItemProvider
	{
		private readonly string _tag;
		private readonly TagDocumentProviderFunction _action;

		/// <summary>
		///		
		/// </summary>
		/// <param name="tag">Should contain full tag like <code>#Anything</code> excluding the brackets and any parameter</param>
		/// <param name="action"></param>
		public TagDocumentItemProvider(string tag, TagDocumentProviderFunction action)
		{
			_tag = tag;
			_action = action;
		}

		private class TagDocumentItem : ValueDocumentItemBase
		{
			private readonly TagDocumentProviderFunction _action;

			public TagDocumentItem()
			{

			}

			public TagDocumentItem(string kind, TagDocumentProviderFunction action, string value)
			{
				_action = action;
				Kind = kind;
				Value = value;
			}
			public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
			{
				await _action(outputStream, context, scopeData, Value);
				return Array.Empty<DocumentItemExecution>();
			}

			public override string Kind { get; }
			public override void Accept(IDocumentItemVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public override IEnumerable<TokenPair> Tokenize(TokenInfo token, ParserOptions options)
		{
			yield return new TokenPair(_tag, token.Token, token.TokenizerContext.CurrentLocation);
		}

		public override bool ShouldParse(TokenPair token, ParserOptions options)
		{
			return token.Type.Equals(_tag);
		}

		public override IDocumentItem Parse(TokenPair token, ParserOptions options, Stack<DocumentScope> buildStack,
			Func<int> getScope)
		{
			return new TagDocumentItem(_tag, _action, token.Value?.Trim('{', '}').Remove(0, _tag.Length).Trim());
		}

		public override bool ShouldTokenize(string token)
		{
			return token.StartsWith("{{" + _tag, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}