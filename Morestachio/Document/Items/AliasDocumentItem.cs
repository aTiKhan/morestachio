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
#if ValueTask
using ItemExecutionPromise = System.Threading.Tasks.ValueTask<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
#else
using ItemExecutionPromise = System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Morestachio.Document.Contracts.DocumentItemExecution>>;
#endif
namespace Morestachio.Document.Items
{
	/// <summary>
	///		Creates an alias 
	/// </summary>
	[System.Serializable]
	public class AliasDocumentItem : ValueDocumentItemBase, ISupportCustomAsyncCompilation
	{
		/// <summary>
		///		Used for XML Serialization
		/// </summary>
		internal AliasDocumentItem()
		{

		}

		/// <summary>
		///		Creates a new Alias DocumentItem
		/// </summary>
		/// <param name="value">The name of the Alias</param>
		/// <param name="variableScope">The Scope id generated by the parser to determinate when to clean this variable</param>
		public AliasDocumentItem(CharacterLocation location,
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
		protected override void SerializeXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(IdVariableScope), IdVariableScope.ToString());
			base.SerializeXml(writer);
		}

		/// <inheritdoc />
		protected override void DeSerializeXml(XmlReader reader)
		{
			var varScope = reader.GetAttribute(nameof(IdVariableScope));
			if (!int.TryParse(varScope, out var intVarScope))
			{
				throw new XmlException($"Error while serializing '{nameof(AliasDocumentItem)}'. " +
									   $"The value for '{nameof(IdVariableScope)}' is expected to be an integer.");
			}
			IdVariableScope = intVarScope;
			base.DeSerializeXml(reader);
		}

		/// <param name="compiler"></param>
		/// <inheritdoc />
		public CompilationAsync Compile(IDocumentCompiler compiler)
		{
			var children = compiler.Compile(Children);
			return async (stream, context, scopeData) =>
			{
				CoreAction(context, scopeData);
				await children(stream, context, scopeData);
			};
		}

		/// <inheritdoc />
		public override ItemExecutionPromise Render(IByteCounterStream outputStream, ContextObject context, ScopeData scopeData)
		{
			CoreAction(context, scopeData);
			return Children.WithScope(context).ToPromise();
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
	}
}