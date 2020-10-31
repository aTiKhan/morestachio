﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
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
			IEnumerable<TokenOption> tokenOptions,
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
			: this(type, value, null, tokenLocation, tokenOptions, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			IMorestachioExpression expression,
			CharacterLocation tokenLocation,
			IEnumerable<TokenOption> tokenOptions,
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
			: this(type, null, expression, tokenLocation, tokenOptions, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			IMorestachioExpression expression,
			CharacterLocation tokenLocation,
			IEnumerable<TokenOption> tokenOptions,
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
		{
			Type = type;
			MorestachioExpression = expression;
			TokenOptions = tokenOptions;
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
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
			: this(type, value, null, tokenLocation, null, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			IMorestachioExpression expression,
			CharacterLocation tokenLocation,
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
			: this(type, null, expression, tokenLocation, null, isEmbeddedToken)
		{
		}


		/// <summary>
		///		Creates a new Token Pair
		/// </summary>
		public TokenPair(IComparable type,
			string value,
			IMorestachioExpression expression,
			CharacterLocation tokenLocation,
			EmbeddedState isEmbeddedToken = EmbeddedState.None)
			: this(type, value, expression, tokenLocation, null, isEmbeddedToken)
		{
		}

		/// <summary>
		///		The type of this Token
		/// </summary>
		public EmbeddedState IsEmbeddedToken { get; }

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
		public IEnumerable<TokenOption> TokenOptions { get; }

		/// <summary>
		///		What is the Value of this token
		/// </summary>
		[CanBeNull]
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
			if (TokenOptions.FirstOrDefault(e => e.Name.Equals(name)).Value is T val)
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
			return (TokenOptions.FirstOrDefault(e => e.Name.Equals(name)).Value as IEnumerable<T>)?.ToArray();
		}

		[PublicAPI]
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

	/// <summary>
	///		Defines an option declared inline with the keyword
	/// </summary>
	public readonly struct TokenOption
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public TokenOption(string name, object value)
		{
			Name = name;
			Value = value;
		}

		/// <summary>
		///		The name of the Option
		/// </summary>
		public string Name { get; }

		/// <summary>
		///		The value of the Option
		/// </summary>
		public object Value { get; }
	}
}