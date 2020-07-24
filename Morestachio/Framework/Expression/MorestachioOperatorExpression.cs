﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using Morestachio.Framework.Expression.Visitors;
#if ValueTask
using ContextObjectPromise = System.Threading.Tasks.ValueTask<Morestachio.Framework.ContextObject>;
#else
using ContextObjectPromise = System.Threading.Tasks.Task<Morestachio.Framework.ContextObject>;
#endif

namespace Morestachio.Framework.Expression
{
	/// <summary>
	///		An operator expression that calls an Operator function on two expressions
	/// </summary>
	[DebuggerTypeProxy(typeof(ExpressionDebuggerDisplay))]
	[Serializable]
	public class MorestachioOperatorExpression : IMorestachioExpression
	{
		/// <summary>
		///		The Operator that will be called
		/// </summary>
		public MorestachioOperator Operator { get; private set; }

		/// <summary>
		///		The left side of the operator
		/// </summary>
		public IMorestachioExpression LeftExpression { get; private set; }

		/// <summary>
		///		The right site of the operator
		/// </summary>
		public IMorestachioExpression RightExpression { get; set; }

		public MorestachioOperatorExpression()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected MorestachioOperatorExpression(SerializationInfo info, StreamingContext context)
		{
			Location = CharacterLocation.FromFormatString(info.GetString(nameof(Location)));
			var opText = info.GetString(nameof(Operator));
			Operator = MorestachioOperator.Yield().First(f => f.OperatorText.Equals(opText));
			LeftExpression =
				info.GetValue(nameof(LeftExpression), typeof(IMorestachioExpression)) as IMorestachioExpression;
			RightExpression =
				info.GetValue(nameof(RightExpression), typeof(IMorestachioExpression)) as IMorestachioExpression;
		}

		/// <summary>
		///		Creates a new Operator
		/// </summary>
		/// <param name="operator"></param>
		/// <param name="location"></param>
		public MorestachioOperatorExpression(MorestachioOperator @operator, IMorestachioExpression leftExpression, CharacterLocation location)
		{
			Operator = @operator;
			Location = location;
			LeftExpression = leftExpression;
		}

		/// <inheritdoc />
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(Operator), Operator.OperatorText);
			info.AddValue(nameof(Location), Location.ToFormatString());
			info.AddValue(nameof(LeftExpression), LeftExpression);
			info.AddValue(nameof(RightExpression), RightExpression);
		}

		/// <inheritdoc />
		public XmlSchema GetSchema()
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc />
		public void ReadXml(XmlReader reader)
		{
			var opText = reader.GetAttribute(nameof(Operator));
			Operator = MorestachioOperator.Yield().First(f => f.OperatorText.Equals(opText));
			Location = CharacterLocation.FromFormatString(reader.GetAttribute(nameof(Location)));
			reader.ReadStartElement();
			var leftSubTree = reader.ReadSubtree();
			LeftExpression = leftSubTree.ParseExpressionFromKind();
			if (reader.Name == nameof(RightExpression))
			{
				var rightSubtree = reader.ReadSubtree();
				RightExpression = rightSubtree.ParseExpressionFromKind();
			}
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Operator), Operator.OperatorText);
			writer.WriteAttributeString(nameof(Location), Location.ToFormatString());
			writer.WriteStartElement(nameof(LeftExpression));
			writer.WriteExpressionToXml(LeftExpression);
			writer.WriteEndElement();//LeftExpression
			if (RightExpression != null)
			{
				writer.WriteStartElement(nameof(RightExpression));
				writer.WriteExpressionToXml(RightExpression);
				writer.WriteEndElement();//RightExpression
			}
		}

		/// <inheritdoc />
		public bool Equals(object other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((MorestachioOperatorExpression)other);
		}

		/// <inheritdoc />
		public bool Equals(IMorestachioExpression other)
		{
			return Equals((object)other);
		}

		/// <inheritdoc />
		public bool Equals(MorestachioOperatorExpression other)
		{
			return Location.Equals(other.Location)
				   && Operator.OperatorText.Equals(other.Operator.OperatorText)
				   && LeftExpression.Equals(other.LeftExpression)
				   && (RightExpression == other.RightExpression || RightExpression.Equals(other.RightExpression));
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (Location != null ? Location.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Operator != null ? Operator.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (LeftExpression != null ? LeftExpression.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (RightExpression != null ? RightExpression.GetHashCode() : 0);
				return hashCode;
			}
		}

		/// <inheritdoc />
		public CharacterLocation Location { get; set; }

		/// <inheritdoc />
		public async ContextObjectPromise GetValue(ContextObject contextObject, ScopeData scopeData)
		{
			var val = await Operator.Execute(LeftExpression, RightExpression, contextObject, scopeData);
			return contextObject.Options.CreateContextObject(".", contextObject.CancellationToken, val,
				contextObject.Parent);
		}

		/// <inheritdoc />
		public void Accept(IMorestachioExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var visitor = new DebuggerViewExpressionVisitor();
			Accept(visitor);
			return visitor.StringBuilder.ToString();
		}	
		
		private class ExpressionDebuggerDisplay
		{
			private readonly MorestachioOperatorExpression _exp;

			public ExpressionDebuggerDisplay(MorestachioOperatorExpression exp)
			{
				_exp = exp;
			}

			public string Expression
			{
				get { return _exp.ToString(); }
			}

			public string Operator
			{
				get { return _exp.Operator.OperatorText; }
			}

			public IMorestachioExpression LeftExpression
			{
				get { return _exp.LeftExpression; }
			}

			public IMorestachioExpression RightExpression
			{
				get { return _exp.RightExpression; }
			}
		}
	}
}