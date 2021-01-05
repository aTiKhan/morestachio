﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Morestachio.Framework.Context.Options;
using Morestachio.Framework.Expression;

namespace Morestachio.Framework.Tokenizing
{
	/// <summary>
	///     The token that has been lexed out of template content.
	/// </summary>
	[DebuggerTypeProxy(typeof(TokenPairDebuggerProxy))]
	public readonly struct TokenPair
	{
		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			CharacterLocation tokenLocation,
			IEnumerable<ITokenOption> tokenOptions,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
			: this(type, value, tokenLocation, null, tokenOptions, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			CharacterLocation tokenLocation,
			IMorestachioExpression expression,
			IEnumerable<ITokenOption> tokenOptions,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
			: this(type, null, tokenLocation, expression, tokenOptions, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			CharacterLocation tokenLocation,
			IMorestachioExpression expression,
			IEnumerable<ITokenOption> tokenOptions,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
		{
			Type = type;
			MorestachioExpression = expression;
			var tokenOps = tokenOptions?.ToArray();
			if (tokenOps?.Length > 0)
			{
				TokenOptions = tokenOps;
			}
			else
			{
				TokenOptions = null;
			}
			
			IsEmbeddedToken = isEmbeddedToken;
			TokenLocation = tokenLocation;
			Value = value;
		}

		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			CharacterLocation tokenLocation,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
			: this(type, value, tokenLocation, null, null, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			CharacterLocation tokenLocation,
			IMorestachioExpression expression,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
			: this(type, null, tokenLocation, expression, null, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			CharacterLocation tokenLocation,
			IMorestachioExpression expression,
			EmbeddedInstructionOrigin isEmbeddedToken = EmbeddedInstructionOrigin.Self)
			: this(type, value, tokenLocation, expression, null, isEmbeddedToken)
		{
		}

		/// <summary>
		///		The type of this Token
		/// </summary>
		public EmbeddedInstructionOrigin IsEmbeddedToken { get; }

		/// <summary>
		///		The type of this Token
		/// </summary>
		public IComparable Type { get; }

		/// <summary>
		///		With what format should this token be evaluated
		/// </summary>
		internal IMorestachioExpression MorestachioExpression { get; }

		/// <summary>
		///		Gets the options set with the Token
		/// </summary>
		public IEnumerable<ITokenOption> TokenOptions { get; }

		/// <summary>
		///		What is the Value of this token
		/// </summary>
		
		public string Value { get; }

		/// <summary>
		///		Where does this token occure in the Template
		/// </summary>
		public CharacterLocation TokenLocation { get; }

		/// <summary>
		///		Searches in the TokenOptions and returns the value if found
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T FindOption<T>(string name)
		{
			return FindOption<T>(name, () => default);
		}

		/// <summary>
		///		Searches in the TokenOptions and returns the value if found
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T FindOption<T>(string name, Func<T> getDefault)
		{
			if (TokenOptions?.FirstOrDefault(e => string.Equals(name, e.Name))?.Value is T val)
			{
				return val;
			}
			return getDefault();
		}

		/// <summary>
		///		Searches in the TokenOptions and returns the value if found
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public T[] FindOptions<T>(string name)
		{
			return (TokenOptions.FirstOrDefault(e => e.Name.Equals(name))?.Value as IEnumerable<T>)?.ToArray();
		}

		
		private class TokenPairDebuggerProxy
		{
			private readonly TokenPair _pair;

			public TokenPairDebuggerProxy(TokenPair pair)
			{
				_pair = pair;
			}

			public string Type
			{
				get { return _pair.Type.ToString(); }
			}

			public string Value
			{
				get { return _pair.Value; }
			}

			public IMorestachioExpression Expression
			{
				get { return _pair.MorestachioExpression; }
			}

			public override string ToString()
			{
				if (_pair.MorestachioExpression != null)
				{
					return $"{Type} {_pair.MorestachioExpression}";
				}
				return $"{Type} {Value}";
			}
		}

	}
}