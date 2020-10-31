﻿#if ValueTask
using ItemExecutionPromise = System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
using Promise = System.Threading.Tasks.ValueTask;
#else
using ItemExecutionPromise = System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
using Promise = System.Threading.Tasks.Task;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Morestachio.Document.Contracts;
using Morestachio.Document.Items.Base;
using Morestachio.Document.Visitor;
using Morestachio.Framework;
using Morestachio.Framework.Context;
using Morestachio.Framework.Error;
using Morestachio.Framework.Expression;
using Morestachio.Framework.IO;
using Morestachio.Parsing.ParserErrors;

namespace Morestachio.Document.Items
{
	/// <summary>
	///		Emits N items that are in the collection
	/// </summary>
	[Serializable]
	public class EachDocumentItem : ExpressionDocumentItemBase, ISupportCustomCompilation
	{
		/// <summary>
		///		Used for XML Serialization
		/// </summary>
		internal EachDocumentItem() : base(CharacterLocation.Unknown, null)
		{

		}

		/// <inheritdoc />
		public EachDocumentItem(CharacterLocation location, IMorestachioExpression value) : base(location, value)
		{
		}
		
		/// <inheritdoc />
		[UsedImplicitly]
		protected EachDocumentItem(SerializationInfo info, StreamingContext c) : base(info, c)
		{
		}
		
		/// <inheritdoc />
		public Compilation Compile()
		{
			var children = MorestachioDocument.CompileItemsAndChildren(Children);

			return async (outputStream, context, scopeData) =>
			{
				await CoreAction(outputStream, context, scopeData,
					async o => { await children(outputStream, o, scopeData); });
			};
		}
		
		/// <exception cref="IndexedParseException"></exception>
		/// <inheritdoc />
		public override async ItemExecutionPromise Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			var contexts = new List<DocumentItemExecution>();
			await CoreAction(outputStream, context, scopeData, async itemContext =>
			{
				contexts.AddRange(Children.WithScope(itemContext));
			});
			return contexts;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private async Promise CoreAction(IByteCounterStream outputStream,
			ContextObject context,
			ScopeData scopeData,
			Func<ContextObject, Promise> onItem)
		{
			var c = await MorestachioExpression.GetValue(context, scopeData);

			if (!c.Exists())
			{
				return;
			}

			if (!(c.Value is IEnumerable value) || value is string || value is IDictionary<string, object>)
			{
				var path = new Stack<string>();
				var parent = context.Parent;
				while (parent != null)
				{
					path.Push(parent.Key);
					parent = parent.Parent;
				}

				throw new IndexedParseException(CharacterLocationExtended.Empty,
					string.Format(
						"{1}'{0}' is used like an array by the template, but is a scalar value or object in your model." +
						" Complete Expression until Error:{2}",
						MorestachioExpression, ExpressionStart,
						(path.Count == 0 ? "Empty" : path.Aggregate((e, f) => e + "\r\n" + f))));
			}
			
			//Use this "lookahead" enumeration to allow the $last keyword
			var index = 0;
			var enumerator = value.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				return;
			}

			var current = enumerator.Current;
			do
			{
				var next = enumerator.MoveNext() ? enumerator.Current : null;

				var innerContext =
					new ContextCollection(index, next == null, context.Options, $"[{index}]", c, current)
						.MakeNatural();
				await onItem(innerContext);
				index++;
				current = next;
			} while (current != null && ContinueBuilding(outputStream, context));
		}

		/// <inheritdoc />
		public override void Accept(IDocumentItemVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}