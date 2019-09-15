﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Morestachio.Framework;

namespace Morestachio.Document
{
	/// <summary>
	///		The end of a Partial declaration
	/// </summary>
	public class RenderPartialDoneDocumentItem : ValueDocumentItemBase
	{
		/// <summary>
		///		Used for XML Serialization
		/// </summary>
		internal RenderPartialDoneDocumentItem()
		{

		}

		/// <inheritdoc />
		public RenderPartialDoneDocumentItem(string partialName)
		{
			Value = partialName;
		}

		/// <inheritdoc />
		public override string Kind { get; } = "EndPartial";
		
		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			await Task.CompletedTask;
			scopeData.PartialDepth.Pop();
			return new DocumentItemExecution[0];
		}
	}
}