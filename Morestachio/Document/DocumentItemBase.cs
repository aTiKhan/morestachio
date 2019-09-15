﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Morestachio.Framework;

namespace Morestachio.Document
{
	/// <summary>
	///		Base class for Document items
	/// </summary>
	[Serializable]
	public abstract class DocumentItemBase : IDocumentItem
	{
		/// <inheritdoc />
		protected DocumentItemBase()
		{
			Children = new List<IDocumentItem>();
		}

		protected DocumentItemBase(SerializationInfo info, StreamingContext c)
		{
			var documentItemBases = info.GetValue(nameof(Children), typeof(IDocumentItem[])) as IDocumentItem[];
			Children = new List<IDocumentItem>(documentItemBases);
			ExpressionStart =
				info.GetValue(nameof(ExpressionStart), typeof(Tokenizer.CharacterLocation)) as
					Tokenizer.CharacterLocation;
		}

		/// <inheritdoc />
		public abstract Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData);

		/// <inheritdoc />
		[IgnoreDataMember]
		[XmlIgnore]
		[SoapIgnore]
		public abstract string Kind { get; }

		/// <inheritdoc />
		public IList<IDocumentItem> Children { get; internal set; }

		/// <inheritdoc />
		public Tokenizer.CharacterLocation ExpressionStart { get; set; }

		/// <summary>
		///		Can be called to check if any stop is requested. If return true no stop is requested
		/// </summary>
		protected static bool ContinueBuilding(IByteCounterStream builder, ContextObject context)
		{
			return !context.AbortGeneration && !context.CancellationToken.IsCancellationRequested && !builder.ReachedLimit;
		}

		/// <inheritdoc />
		public void Add(params IDocumentItem[] documentChildren)
		{
			foreach (var documentItem in documentChildren)
			{
				//documentItem.Parent = this;
				Children.Add(documentItem);
			}
		}

		/// <inheritdoc />
		[SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(Kind), Kind);
			info.AddValue(nameof(ExpressionStart), ExpressionStart, typeof(Tokenizer.CharacterLocation));
			info.AddValue(nameof(Children), Children.ToArray(), typeof(IDocumentItem[]));
		}

		protected virtual void SerializeBinaryCore(SerializationInfo info, StreamingContext context)
		{

		}

		protected virtual void SerializeXml(XmlWriter writer)
		{
			
		}

		protected virtual void DeSerializeXml(XmlReader reader)
		{

		}

		void IDocumentItem.SerializeXmlCore(XmlWriter writer)
		{
			writer.WriteStartElement(GetType().Name);
			writer.WriteAttributeString(nameof(Kind), Kind);
			if (ExpressionStart != null)
			{
				writer.WriteAttributeString(nameof(ExpressionStart), ExpressionStart?.ToFormatString() ?? string.Empty);
			}
			SerializeXml(writer);
			if (Children.Any())
			{
				writer.WriteStartElement(nameof(Children));
				foreach (var documentItem in Children)
				{
					documentItem.SerializeXmlCore(writer);
				}

				writer.WriteEndElement(); //nameof(Children)	
			}
			writer.WriteEndElement(); //GetType().Name
		}

		void IDocumentItem.DeSerializeXmlCore(XmlReader reader)
		{
			AssertElement(reader, GetType().Name);

			var charLoc = reader.GetAttribute(nameof(ExpressionStart));
			if (charLoc != null)
			{
				ExpressionStart = Tokenizer.CharacterLocation.FromFormatString(charLoc);
			}

			if (!reader.IsEmptyElement)
			{
				DeSerializeXml(reader);
				if (reader.NodeType == XmlNodeType.EndElement && reader.Name.Equals(GetType().Name))
				{
					//there are no children and we have reached the end of the document
					reader.ReadEndElement();//GetType().Name
					return;
				}

				reader.ReadStartElement(); //nameof(Children)
				if (reader.Name.Equals(nameof(Children), StringComparison.InvariantCultureIgnoreCase)
				    && !reader.IsEmptyElement)
				{
					reader.ReadStartElement(); //AnyChild
					while (!reader.Name.Equals(nameof(Children)) && reader.NodeType != XmlNodeType.EndElement)
					{
						var type = Type.GetType(GetType().Namespace + "." + reader.Name)
						           ?? throw new InvalidOperationException($"The specified type '{reader.Name}' does not exist");

						if (!(type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
								null, Type.EmptyTypes, null)
							?.Invoke(null) is IDocumentItem child))
						{
							throw new InvalidOperationException($"The specified type '{reader.Name}' does not exist");
						}

						var childTree = reader.ReadSubtree();
						childTree.Read();
						child.DeSerializeXmlCore(childTree);
						reader.Skip();
						Children.Add(child);
					}
				}
				reader.ReadEndElement();//nameof(Children)
				reader.ReadEndElement();//GetType().Name
			}
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		protected internal void AssertElement(XmlReader reader, string elementName)
		{
			if (!reader.Name.Equals(elementName, StringComparison.InvariantCultureIgnoreCase))
			{
				throw new XmlSchemaException($"Unexpected Element '{reader.Name}' expected '{elementName}'");
			}
		}

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			((IDocumentItem) this).DeSerializeXmlCore(reader);
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Kind), Kind);
			SerializeXml(writer);
			if (Children.Any())
			{
				writer.WriteStartElement(nameof(Children));
				foreach (var documentItem in Children)
				{
					documentItem.SerializeXmlCore(writer);
				}

				writer.WriteEndElement(); //nameof(Children)	
			}
			//writer.WriteEndElement(); //GetType().Name
		}
	}
}