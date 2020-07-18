﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using JetBrains.Annotations;
using Morestachio.Document.Contracts;
using Morestachio.Document.Visitor;
using Morestachio.Framework;
using Morestachio.Framework.Expression;

namespace Morestachio.Document
{
	/// <summary>
	///		Creates an alias 
	/// </summary>
	[System.Serializable]
	public class AliasDocumentItem : ValueDocumentItemBase
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="variableScope"></param>
		public AliasDocumentItem(string value, int variableScope)
		{
			Value = value;
			IdVariableScope = variableScope;
		}

		/// <summary>
		///		Used for XML Serialization
		/// </summary>
		internal AliasDocumentItem()
		{
			
		}

		/// <inheritdoc />
		[UsedImplicitly]
		protected AliasDocumentItem(SerializationInfo info, StreamingContext c) : base(info, c)
		{
			IdVariableScope = info.GetInt32(nameof(IdVariableScope));
		}

		/// <inheritdoc />
		protected override void SerializeBinaryCore(SerializationInfo info, StreamingContext context)
		{
			base.SerializeBinaryCore(info, context);
			info.AddValue(nameof(IdVariableScope), IdVariableScope);
		}
		
		/// <inheritdoc />
		protected override void SerializeXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(IdVariableScope), IdVariableScope.ToString());
			base.SerializeXml(writer);
		}
		
		/// <inheritdoc />
		protected override void DeSerializeXml(XmlReader reader)
		{
			IdVariableScope = int.Parse(reader.GetAttribute(nameof(IdVariableScope)));
			base.DeSerializeXml(reader);
		}

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			scopeData.AddVariable(Value, context.CloneForEdit(), IdVariableScope);

			await Task.CompletedTask;
			return Children.WithScope(context);
		}

		/// <inheritdoc />
		public override string Kind { get; } = "Alias";
		
		/// <summary>
		///		Gets or Sets the Scope of the variable
		/// </summary>
		public int IdVariableScope { get; set; }

		/// <inheritdoc />
		public override void Accept(IDocumentItemVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}