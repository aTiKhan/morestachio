﻿
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using Morestachio.Document.Contracts;
using Morestachio.Document.Items.Base;
using Morestachio.Document.Visitor;
using Morestachio.Framework;
using Morestachio.Framework.Context;
using Morestachio.Framework.IO;
using Morestachio.Framework.Tokenizing;
using Morestachio.Helper;
using Morestachio.Helper.Serialization;
using Morestachio.Parsing.ParserErrors;

namespace Morestachio.Document.Items;

/// <summary>
///		Creates an alias 
/// </summary>
[Serializable]
public class AliasDocumentItem : ValueDocumentItemBase, 
								ISupportCustomAsyncCompilation,
								IReportUsage
{
	/// <summary>
	///		Used for Serialization
	/// </summary>
	internal AliasDocumentItem()
	{

	}

	/// <summary>
	///		Creates a new Alias DocumentItem
	/// </summary>
	/// <param name="value">The name of the Alias</param>
	/// <param name="variableScope">The Scope id generated by the parser to determinate when to clean this variable</param>
	public AliasDocumentItem(TextRange location,
							string value,
							int variableScope,
							IEnumerable<ITokenOption> tagCreationOptions) : base(location, value, tagCreationOptions)
	{
		IdVariableScope = variableScope;
	}

	/// <inheritdoc />
		
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
	protected override void SerializeXmlHeaderCore(XmlWriter writer)
	{
		base.SerializeXmlHeaderCore(writer);
		writer.WriteAttributeString(nameof(IdVariableScope), IdVariableScope.ToString());
	}

	/// <inheritdoc />
	protected override void DeSerializeXmlHeaderCore(XmlReader reader)
	{
		base.DeSerializeXmlHeaderCore(reader);
		
		var varScope = reader.GetAttribute(nameof(IdVariableScope));
		if (!int.TryParse(varScope, out var intVarScope))
		{
			throw new XmlException($"Error while serializing '{nameof(AliasDocumentItem)}'. " +
				$"The value for '{nameof(IdVariableScope)}' is expected to be an integer.");
		}
		IdVariableScope = intVarScope;
	}
	
	/// <param name="compiler"></param>
	/// <param name="parserOptions"></param>
	/// <inheritdoc />
	public CompilationAsync Compile(IDocumentCompiler compiler, ParserOptions parserOptions)
	{			
		return (stream, context, scopeData) =>
		{
			CoreAction(context, scopeData);
			return AsyncHelper.FakePromise();
		};
	}

	/// <inheritdoc />
	public override ItemExecutionPromise Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
	{
		CoreAction(context, scopeData);
		return Enumerable.Empty<DocumentItemExecution>().ToPromise();
	}
		
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CoreAction(ContextObject context, ScopeData scopeData)
	{
		scopeData.AddVariable(Value, context, IdVariableScope);
	}

	/// <summary>
	///		Gets or Sets the Scope of the variable
	/// </summary>
	public int IdVariableScope { get; private set; }

	/// <inheritdoc />
	public override void Accept(IDocumentItemVisitor visitor)
	{
		visitor.Visit(this);
	}

	/// <inheritdoc />
	public void ReportUsage(UsageData data)
	{
		data.PushVariable(Value, data.CurrentPath);
	}
}