﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Document.Contracts;
using Morestachio.Document.Visitor;
using Morestachio.Framework;

#if ValueTask
using ItemExecutionPromise = System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
using Promise = System.Threading.Tasks.ValueTask;
#else
using ItemExecutionPromise = System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
using Promise = System.Threading.Tasks.Task;
#endif
namespace Morestachio.Document.Custom
{
	/// <summary>
	///		The Standard Block that is enclosed with an opening tag <code>{{#Anything}}</code> and closed with an closing tag <code>{{/Anything}}</code>
	/// </summary>
	public class BlockDocumentItemProvider : CustomDocumentItemProvider
	{
		private readonly string _tagOpen;
		private readonly string _tagClose;
		private readonly BlockDocumentProviderFunction _action;

		/// <summary>
		///		Creates a new Block
		/// </summary>
		/// <param name="tagOpen">Should contain full tag like <code>#Anything</code> excluding the brackets and any parameter</param>
		/// <param name="tagClose">Should contain full tag like <code>/Anything</code> excluding the brackets and any parameter</param>
		/// <param name="action"></param>
		public BlockDocumentItemProvider(string tagOpen, string tagClose, BlockDocumentProviderFunction action)
		{
			_tagOpen = tagOpen;
			_tagClose = tagClose;
			_action = action;
		}

		/// <summary>
		///		The General purpose block
		/// </summary>
		public class BlockDocumentItem : ValueDocumentItemBase
		{
			private readonly BlockDocumentProviderFunction _action;

			public BlockDocumentItem()
			{

			}

			public BlockDocumentItem(string kind, BlockDocumentProviderFunction action, string value)
			{
				_action = action;
				Kind = kind;
				Value = value;
			}
			public override async ItemExecutionPromise Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
			{
				return await _action(outputStream, context, scopeData, Value, Children);
				//return Array.Empty<DocumentItemExecution>();
			}

			public override string Kind { get; }
			public override void Accept(IDocumentItemVisitor visitor)
			{
				visitor.Visit(this);
			}
		}

		public override IEnumerable<TokenPair> Tokenize(TokenInfo token, ParserOptions options)
		{
			var trim = token.Token.Trim('{', '}');
			if (trim == _tagOpen)
			{
				yield return new TokenPair(_tagOpen, trim, token.TokenizerContext.CurrentLocation);
			}
			if (trim == _tagClose)
			{
				yield return new TokenPair(_tagClose, trim, token.TokenizerContext.CurrentLocation);
			}
		}

		public override bool ShouldParse(TokenPair token, ParserOptions options)
		{
			return token.Type.Equals(_tagOpen) || token.Type.Equals(_tagClose);
		}

		public override IDocumentItem Parse(TokenPair token, ParserOptions options, Stack<DocumentScope> buildStack,
			Func<int> getScope)
		{
			if (token.Value == _tagOpen)
			{
				var tagDocumentItem = new BlockDocumentItem(_tagOpen, _action, token.Value?.Remove(0, _tagOpen.Length).Trim());
				buildStack.Push(new DocumentScope(tagDocumentItem, getScope));
				return tagDocumentItem;
			}
			else if (token.Value == _tagClose)
			{
				buildStack.Pop();
			}
			return null;
		}

		public override bool ShouldTokenize(string token)
		{
			return token.StartsWith("{{" + _tagOpen, StringComparison.InvariantCultureIgnoreCase)
			       || token.StartsWith("{{" + _tagClose, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}