﻿#if ValueTask
using ItemExecutionPromise = System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
#else
using ItemExecutionPromise = System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
#endif
using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Morestachio.Document.Contracts;
using Morestachio.Document.Items.Base;
using Morestachio.Document.Visitor;
using Morestachio.Framework.Context;
using Morestachio.Framework.Expression;
using Morestachio.Framework.IO;
using Morestachio.Helper;

namespace Morestachio.Document.Items.SwitchCase
{
	/// <summary>
	///		Defines an case to be used within a switch statement.
	///		If used outside a <see cref="SwitchDocumentItem"/>, it will unconditionally render its items
	/// </summary>
	[Serializable]
	public class SwitchCaseDocumentItem : ExpressionDocumentItemBase
	{
		/// <summary>
		///		Used for XML Serialization
		/// </summary>
		internal SwitchCaseDocumentItem()
		{

		}

		/// <inheritdoc />
		public SwitchCaseDocumentItem(IMorestachioExpression value)
		{
			MorestachioExpression = value;
		}
		
		/// <inheritdoc />
		[UsedImplicitly]
		protected SwitchCaseDocumentItem(SerializationInfo info, StreamingContext c) : base(info, c)
		{
		}

		/// <inheritdoc />
		public override ItemExecutionPromise Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			return Children.WithScope(context).ToPromise();
		}
		
		/// <inheritdoc />
		public override void Accept(IDocumentItemVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}