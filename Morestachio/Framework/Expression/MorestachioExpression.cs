﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using Morestachio.Document;
using Morestachio.Formatter.Framework;
using Morestachio.Framework.Context;
using Morestachio.Framework.Expression.Framework;
using Morestachio.Framework.Expression.Visitors;
using Morestachio.Framework.Tokenizing;
using Morestachio.Helper;
using Morestachio.Parsing.ParserErrors;
#if ValueTask
using ContextObjectPromise = System.Threading.Tasks.ValueTask<Morestachio.Framework.Context.ContextObject>;
#else
using ContextObjectPromise = System.Threading.Tasks.Task<Morestachio.Framework.Context.ContextObject>;
#endif

namespace Morestachio.Framework.Expression
{
	/// <summary>
	///		Defines a path with an optional formatting expression including sub expressions
	/// </summary>
	[DebuggerTypeProxy(typeof(ExpressionDebuggerDisplay))]
	[Serializable]
	public class MorestachioExpression : IMorestachioExpression
	{
		internal MorestachioExpression()
		{
			PathParts = Traversable.Empty;
			Formats = new List<ExpressionArgument>();
		}

		internal MorestachioExpression(CharacterLocation location) : this()
		{
			Location = location;
			_pathTokenizer = new PathTokenizer();
		}

		/// <summary>
		///		Serialization constructor 
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected MorestachioExpression(SerializationInfo info, StreamingContext context)
		{
			PathParts = new Traversable(info.GetValue(nameof(PathParts), typeof(KeyValuePair<string, PathType>[])) as KeyValuePair<string, PathType>[]);
			Formats = info.GetValue(nameof(Formats), typeof(IList<ExpressionArgument>))
				as IList<ExpressionArgument>;
			FormatterName = info.GetString(nameof(FormatterName));
			Location = CharacterLocation.FromFormatString(info.GetString(nameof(Location)));
			EndsWithDelimiter = info.GetBoolean(nameof(EndsWithDelimiter));
		}

		/// <inheritdoc />
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(PathParts), PathParts.ToArray());
			info.AddValue(nameof(Formats), Formats);
			info.AddValue(nameof(FormatterName), FormatterName);
			info.AddValue(nameof(Location), Location.ToFormatString());
			info.AddValue(nameof(EndsWithDelimiter), EndsWithDelimiter);
		}

		/// <inheritdoc />
		public XmlSchema GetSchema()
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void ReadXml(XmlReader reader)
		{
			Location = CharacterLocation.FromFormatString(reader.GetAttribute(nameof(Location)));
			EndsWithDelimiter = reader.GetAttribute(nameof(EndsWithDelimiter)) == bool.TrueString;
			var pathParts = new List<KeyValuePair<string, PathType>>();
			reader.ReadStartElement();//Path

			if (reader.Name == "Path")
			{
				reader.ReadStartElement();//Any SubPath

				while (reader.Name != "Path" && reader.NodeType != XmlNodeType.EndElement)
				{
					var partName = reader.Name;
					string partValue = null;
					if (reader.IsEmptyElement)
					{
						reader.ReadStartElement();
					}
					else
					{
						partValue = reader.ReadElementContentAsString();
					}
					pathParts.Add(new KeyValuePair<string, PathType>(partValue, (PathType)Enum.Parse(typeof(PathType), partName)));
				}
				reader.ReadEndElement();//</Path>
			}
			PathParts = new Traversable(pathParts);
			if (reader.Name == "Format" && reader.NodeType == XmlNodeType.Element)
			{
				FormatterName = reader.GetAttribute(nameof(FormatterName));
				if (reader.IsEmptyElement)
				{
					reader.ReadStartElement();
				}
				else
				{
					reader.ReadStartElement(); //<Argument>
					while (reader.Name == "Argument" && reader.NodeType != XmlNodeType.EndElement)
					{
						var formatSubTree = reader.ReadSubtree();
						formatSubTree.Read();

						var expressionArgument = new ExpressionArgument();
						Formats.Add(expressionArgument);

						expressionArgument.ReadXml(formatSubTree);

						reader.Skip();
						reader.ReadEndElement();
					}
					reader.ReadEndElement();//</Format>
				}
			}
			reader.ReadEndElement();
		}

		/// <inheritdoc />
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString(nameof(Location), Location.ToFormatString());
			if (EndsWithDelimiter)
			{
				writer.WriteAttributeString(nameof(EndsWithDelimiter), bool.TrueString);
			}

			if (PathParts.Any())
			{
				writer.WriteStartElement("Path");
				foreach (var pathPart in PathParts.ToArray())
				{
					writer.WriteElementString(pathPart.Value.ToString(), pathPart.Key);
				}
				writer.WriteEndElement();//</Path>
			}
			if (FormatterName != null)
			{
				writer.WriteStartElement("Format");
				writer.WriteAttributeString(nameof(FormatterName), FormatterName);
				foreach (var expressionArgument in Formats)
				{
					writer.WriteStartElement("Argument");
					expressionArgument.WriteXml(writer);
					writer.WriteEndElement();//</Argument>
				}
				writer.WriteEndElement();//</Format>
			}
		}

		/// <summary>
		///		Contains all parts of the path
		/// </summary>
		public Traversable PathParts { get; internal set; }

		/// <summary>
		///		If filled contains the arguments to be used to format the value located at PathParts
		/// </summary>
		public IList<ExpressionArgument> Formats { get; internal set; }

		/// <summary>
		///		If set the formatter name to be used to format the value located at PathParts
		/// </summary>
		public string FormatterName { get; internal set; }

		/// <inheritdoc />
		public CharacterLocation Location { get; private set; }

		/// <summary>
		///		The prepared call for an formatter
		/// </summary>
		public FormatterCache? Cache { get; private set; }

		/// <summary>
		///		Gets whenever this expression was explicitly closed
		/// </summary>
		public bool EndsWithDelimiter { get; private set; }

		/// <inheritdoc />
		public CompiledExpression Compile()
		{
			if (!PathParts.HasValue && Formats.Count == 0 && FormatterName == null)
			{
				return (contextObject, data) => contextObject.ToPromise();
			}

			if (PathParts.Count == 1 && PathParts.Current.Value == PathType.Null)
			{
				return (contextObject, data) => contextObject.Options
					.CreateContextObject("x:null", contextObject.CancellationToken, null).ToPromise();
			}

			var pathQueue = new List<Func<ContextObject, ScopeData, IMorestachioExpression, ContextObjectPromise>>();
			var pathParts = PathParts.ToArray();

			if (pathParts.Length > 0 && pathParts.First().Value == PathType.DataPath)
			{
				var firstItem = pathParts.First();

				pathQueue.Add((context, scopeData, expression) =>
				{
					var variable = scopeData.GetVariable(context, firstItem.Key);
					if (variable != null)
					{
						return variable.ToPromise();
					}

					return context.ExecuteDataPath(firstItem.Key, expression, context.Value?.GetType()).ToPromise();
				});
				pathParts = pathParts.Skip(1).ToArray();
			}

			foreach (var pathPart in pathParts)
			{
				switch (pathPart.Value)
				{
					case PathType.DataPath:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							return contextObject.ExecuteDataPath(pathPart.Key, expression, contextObject.Value?.GetType()).ToPromise();
						});
						break;
					case PathType.RootSelector:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							return contextObject.ExecuteRootSelector().ToPromise();
						});
						break;
					case PathType.ParentSelector:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							var natContext = contextObject.FindNextNaturalContextObject();
							return (natContext?.Parent ?? contextObject).ToPromise();
						});
						break;
					case PathType.ObjectSelector:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							return contextObject.ExecuteObjectSelector(pathPart.Key, contextObject.Value?.GetType())
								.ToPromise();
						});
						break;
					case PathType.Null:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							return contextObject.Options.CreateContextObject("x:null", contextObject.CancellationToken, null)
								.ToPromise();
						});
						break;
					case PathType.Boolean:
						pathQueue.Add((contextObject, scopeData, expression) =>
						{
							var booleanContext =
								contextObject.Options.CreateContextObject(".", contextObject.CancellationToken,
									pathPart.Key == "true", contextObject);
							booleanContext.IsNaturalContext = contextObject.IsNaturalContext;
							return booleanContext.ToPromise();
						});
						break;
					case PathType.SelfAssignment:
						pathQueue.Add((contextObject, scopeDate, expression) => contextObject.ToPromise());
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			Func<ContextObject, ScopeData, IMorestachioExpression, ContextObjectPromise> getContext;

			if (pathQueue.Count != 0)
			{
				getContext =
					async (contextObject, data, expression) =>
					{
						foreach (var func in pathQueue)
						{
							contextObject = await func(contextObject, data, expression);
						}

						return contextObject;
					};
			}
			else
			{
				getContext = (context, scopeData, expression) => context.ToPromise();
			}


			if (!Formats.Any() && FormatterName == null)
			{
				return (contextObject, data) => getContext(contextObject, data, this);
			}

			var formatsCompiled = Formats.ToDictionary(f => f, f => f.Compile()).ToArray();

			FormatterCache? cache = null;
			return async (contextObject, scopeData) =>
			{
				var ctx = await getContext(contextObject, scopeData, this);

				if (ctx == contextObject)
				{
					ctx = contextObject.CloneForEdit();
				}

				var arguments = new FormatterArgumentType[formatsCompiled.Length];
				var naturalValue = contextObject.FindNextNaturalContextObject();
				for (var index = 0; index < formatsCompiled.Length; index++)
				{
					var formatterArgument = formatsCompiled[index];
					var value = await formatterArgument.Value(naturalValue, scopeData);
					arguments[index] = new FormatterArgumentType(index, formatterArgument.Key.Name, value?.Value);
				}

				if (cache == null)
				{
					cache = ctx.PrepareFormatterCall(
						ctx.Value?.GetType() ?? typeof(object),
						FormatterName,
						arguments,
						scopeData);
				}

				if (cache != null && !Equals(cache.Value, default(FormatterCache)))
				{
					ctx.Value = await contextObject.Options.Formatters.Execute(cache.Value, ctx.Value, arguments);
					ctx.MakeSyntetic();
				}

				return ctx;
			};
		}

		/// <inheritdoc />
		public async ContextObjectPromise GetValue(ContextObject contextObject, ScopeData scopeData)
		{
			var contextForPath = await contextObject.GetContextForPath(PathParts, scopeData, this);
			if (!Formats.Any() && FormatterName == null)
			{
				return contextForPath;
			}

			if (contextForPath == contextObject)
			{
				contextForPath = contextObject.CloneForEdit();
			}

			var arguments = new FormatterArgumentType[Formats.Count];
			var naturalValue = contextObject.FindNextNaturalContextObject();
			for (var index = 0; index < Formats.Count; index++)
			{
				var formatterArgument = Formats[index];
				var value = await formatterArgument.MorestachioExpression.GetValue(naturalValue, scopeData);
				arguments[index] = new FormatterArgumentType(index, formatterArgument.Name, value?.Value);
			}
			//contextForPath.Value = await contextForPath.Format(FormatterName, argList, scopeData);

			if (Cache == null)
			{
				Cache = contextForPath.PrepareFormatterCall(
					contextForPath.Value?.GetType() ?? typeof(object),
					FormatterName,
					arguments,
					scopeData);
			}

			if (Cache != null && !Equals(Cache.Value, default(FormatterCache)))
			{
				contextForPath.Value = await contextObject.Options.Formatters.Execute(Cache.Value, contextForPath.Value, arguments);
				contextForPath.MakeSyntetic();
			}
			return contextForPath;
		}

		/// <inheritdoc />
		public void Accept(IMorestachioExpressionVisitor visitor)
		{
			visitor.Visit(this);
		}

		private readonly PathTokenizer _pathTokenizer;

		/// <summary>
		///		Parses the text into one or more expressions
		/// </summary>
		/// <param name="text">the path to parse excluding {{ and }}</param>
		/// <param name="context">The context used to tokenize the text</param>
		/// <param name="indexedUntil">the index of where the parsing stoped</param>
		/// <returns></returns>
		public static IMorestachioExpression ParseFrom(string text,
			TokenzierContext context,
			out int indexedUntil,
			int index = 0)
		{
			text = text.TrimEnd(Tokenizer.GetWhitespaceDelimiters());
			var orIndx = index;

			if (!text.Contains("(") && !text.Any(Tokenizer.IsOperationChar))
			//this is the fast parsing branch. In case there are no operators or formatters used we can take the whole text as an single expression
			{
				if (text.Length > 0 && char.IsDigit(text[0]))
				{
					var expression = MorestachioExpressionNumber.ParseFrom(text, 0, context, out index);
					context.SetLocation(index);
					indexedUntil = index;
					return expression;
				}
				else
				{
					var expression = new MorestachioExpression(context.CurrentLocation);
					Func<IMorestachioError> errFunc;
					for (; index < text.Length; index++)
					{
						var c = text[index];
						if (c == ';')
						{
							expression.EndsWithDelimiter = true;
							index++;
							break;
						}
						if (c == '#')
						{
							index--;
							break;
						}
						if (!expression._pathTokenizer.Add(c, context, index, out errFunc))
						{
							context.Errors.Add(errFunc());
							indexedUntil = 0;
							return null;
						}
					}
					expression.PathParts = new Traversable(expression._pathTokenizer.CompileListWithCurrent(context, 0, out errFunc));
					if (errFunc != null)
					{
						context.Errors.Add(errFunc());
					}
					indexedUntil = index;
					context.AdvanceLocation(index - orIndx);
					return expression;
				}
			}

			var morestachioExpressions = new MorestachioMultiPartExpressionList(context.CurrentLocation);
			HeaderTokenMatch currentScope;
			//this COULD be made with regexes, i have made it and rejected it as it was no longer readable in any way.
			var tokenScopes = new Stack<HeaderTokenMatch>();
			tokenScopes.Push(new HeaderTokenMatch
			{
				State = TokenState.DecideArgumentType,
				TokenLocation = context.CurrentLocation
			});
			bool reprocess;

			//var currentPathPart = new StringBuilder();
			while (index < text.Length)
			//for (; index < text.Length; index++)
			{
				reprocess = false;
				currentScope = tokenScopes.Peek();
				switch (currentScope.State)
				{
					case TokenState.ArgumentStart:
						{
							//we are at the start of an argument
							index = SkipWhitespaces(text, index);

							if (text[index] == '[')
							{
								index++;
								currentScope.ArgumentName = new string(Take(text, f => f != ']', ref index));
								index += 1;
							}
							reprocess = true;
							//index--; //reprocess the char
							currentScope.State = TokenState.DecideArgumentType;
						}
						break;
					case TokenState.DecideArgumentType:
						{
							//we are at the start of an argument
							index = SkipWhitespaces(text, index);

							var idx = index;
							if (Tokenizer.IsStringDelimiter(text[index]))
							{
								//this is an string
								var cidx = context.Character;
								currentScope.Value = MorestachioExpressionString.ParseFrom(text, context, out index, index);
								currentScope.State = TokenState.Expression;
								context.SetLocation(cidx);
								if (SeekNext(text, index, out _) == '.')
								{
									var morestachioExpressionList = new MorestachioMultiPartExpressionList(new List<IMorestachioExpression>()
									{
										currentScope.Value
									}, context.CurrentLocation.Offset(index));
									currentScope.Value = morestachioExpressionList;

									TerminateCurrentScope(tokenScopes);
									var morestachioExpression = new MorestachioExpression(context.CurrentLocation.Offset(index));
									morestachioExpressionList.Add(morestachioExpression);
									tokenScopes.Push(new HeaderTokenMatch
									{
										State = TokenState.Expression,
										Parent = currentScope.Parent,
										TokenLocation = context.CurrentLocation.Offset(index + 1),
										Value = morestachioExpression
									});
								}

								var nIndexEoex = index;
								if (Eoex(text, ref nIndexEoex))
								{
									TerminateCurrentScope(tokenScopes);
								}
							}
							else if (Tokenizer.IsNumberExpressionChar(text[index]))
							{
								//this is an string
								var cidx = context.Character;
								currentScope.Value = MorestachioExpressionNumber.ParseFrom(text, index, context, out index);
								currentScope.State = TokenState.Expression;
								context.SetLocation(cidx);

								if (SeekNext(text, index, out _) == '.')
								{
									var morestachioExpressionList = new MorestachioMultiPartExpressionList(new List<IMorestachioExpression>()
									{
										currentScope.Value
									}, context.CurrentLocation.Offset(index));
									currentScope.Value = morestachioExpressionList;

									TerminateCurrentScope(tokenScopes);
									var morestachioExpression = new MorestachioExpression(context.CurrentLocation.Offset(index));
									morestachioExpressionList.Add(morestachioExpression);
									tokenScopes.Push(new HeaderTokenMatch
									{
										State = TokenState.Expression,
										Parent = currentScope.Parent,
										TokenLocation = context.CurrentLocation.Offset(index + 1),
										Value = morestachioExpression
									});
								}
							}
							else if (Tokenizer.IsExpressionChar(text[index]))
							{
								currentScope.State = TokenState.Expression;
								//this is the first char of an expression.
								reprocess = true;
								//index--;
								currentScope.Value = new MorestachioExpression(context.CurrentLocation.Offset(index));
							}
							else
							{
								//this is not the start of an expression and not a string
								context.Errors.Add(new InvalidPathSyntaxError(
									context.CurrentLocation.Offset(index)
										.AddWindow(new CharacterSnippedLocation(1, index, text)),
									currentScope.Value?.ToString()));
								indexedUntil = 0;
								return null;
							}

							if (currentScope.Parent == null)
							{
								currentScope.Parent = new HeaderTokenMatch()
								{
									Value = morestachioExpressions
								};
							}
							AddCurrentScopeToParent(idx, context, currentScope, currentScope.Value, currentScope.Parent.Value);
							if (!reprocess && IsNextCharOperator(text, index, out var nIndex))
							{
								index = nIndex;
								ParseOperationCall(text, index, out index, context, tokenScopes, currentScope);
							}
						}
						break;
					case TokenState.Expression:
						{
							index = SkipWhitespaces(text, index);
							if (text[index] == '(')
							{
								//in this case the current path has ended and we must prepare for arguments

								//if this scope was opened multible times, set an error
								if (currentScope.BracketsCounter > 1)
								{
									context.Errors.Add(new MorestachioSyntaxError(context.CurrentLocation.Offset(index)
											.AddWindow(new CharacterSnippedLocation(1, index, text)),
										"Format", "(", "Name of Formatter",
										"Did expect to find the name of a formatter but found single path. Did you forgot to put an . before the 2nd formatter?"));
									indexedUntil = 0;
									return null;
								}

								var currentExpression = currentScope.Value as MorestachioExpression;
								//get the last part of the path as the name of the formatter
								currentExpression.FormatterName = currentExpression._pathTokenizer.GetFormatterName(context, index, out var found, out var errProducer);
								if (errProducer != null)
								{
									context.Errors.Add(errProducer());

									indexedUntil = 0;
									return null;
								}
								currentExpression.PathParts = new Traversable(currentExpression._pathTokenizer.Compile(context, index));
								currentScope.Evaluated = true;
								if (currentExpression.PathParts.Count == 0 && !found)
								{
									//in this case there are no parts in the path that indicates ether {{(}} or {{data.(())}}
									context.Errors.Add(new MorestachioSyntaxError(context.CurrentLocation.Offset(index)
											.AddWindow(new CharacterSnippedLocation(1, index, text)),
										"Format", "(", "Name of Formatter",
										"Did expect to find the name of a formatter but found single path. Did you forgot to put an . before the 2nd formatter?"));
									indexedUntil = 0;
									return null;
								}

								currentScope.BracketsCounter++;
								//seek the next non whitespace char. That should be ether " or an expression char
								var temIndex = Seek(text, index, f => !Tokenizer.IsWhiteSpaceDelimiter(f), false);
								if (temIndex != -1)
								{
									index = temIndex;
								}
								if (text[index] == ')')
								{
									//the only next char is the closing bracket so no arguments
									//currentScope.BracketsCounter--;
									index = EndParameterBracket(text, index, tokenScopes, currentScope, context, morestachioExpressions);
								}
								else if (text[index] == '(')
								{
									//in this case there are no parts in the path that indicates ether {{(}} or {{data.(())}}
									context.Errors.Add(new MorestachioSyntaxError(context.CurrentLocation.Offset(index)
											.AddWindow(new CharacterSnippedLocation(1, index, text)),
										"Format", "(", "Formatter arguments",
										"Did not expect to find the start of another formatter here. Use .( if you want to call a formatter on yourself."));
									indexedUntil = 0;
									return null;
								}
								else
								{
									if (Eoex(text, ref index))
									{
										//in this case there are no parts in the path that indicates ether {{(}} or {{data.(())}}
										context.Errors.Add(new MorestachioSyntaxError(context.CurrentLocation.Offset(index)
												.AddWindow(new CharacterSnippedLocation(1, index, text)),
											"Format", "(", "Formatter arguments",
											"Did not expect to find the end of the expression here"));
										indexedUntil = 0;
										return null;
									}
									//indicates the start of an argument
									reprocess = true;
									//index--;
									tokenScopes.Push(new HeaderTokenMatch
									{
										State = TokenState.ArgumentStart,
										Parent = currentScope
									});
								}
							}
							else if (text[index] == ')')
							{
								////close the current scope. This scope is an parameter expression
								//TerminateCurrentScope();

								var parentExpression = currentScope.Parent?.Value as MorestachioExpression;
								//currentScope.Parent.BracketsCounter--;
								if (currentScope.Value is MorestachioExpression currentScopeValue)
								{
									if (!EvaluateScope(context, index, currentScope, currentScopeValue))
									{
										indexedUntil = 0;
										return null;
									}
									//currentScope.Evaluated = true;
									//currentScopeValue.PathParts = new Traversable(currentScopeValue._pathTokenizer.CompileListWithCurrent(context, index, out var errProducer));
									//if (errProducer != null)
									//{
									//	context.Errors.Add(errProducer());

									//	indexedUntil = 0;
									//	return null;
									//}
									if (currentScopeValue != null &&
										!currentScopeValue.PathParts.Any() && parentExpression?.Formats.Any() == true)
									{
										context.Errors.Add(new InvalidPathSyntaxError(
											context.CurrentLocation.Offset(index)
												.AddWindow(new CharacterSnippedLocation(1, index, text)),
											currentScope.Value.ToString()));

										indexedUntil = 0;
										return null;
									}
								}

								TerminateCurrentScope(tokenScopes);
								index = EndParameterBracket(text, index, tokenScopes, tokenScopes.Peek(), context, morestachioExpressions);
							}
							else if (text[index] == ',')
							{
								if (currentScope.Value is MorestachioExpression currentScopeValue)
								{
									if (!EvaluateScope(context, index, currentScope, currentScopeValue))
									{
										indexedUntil = 0;
										return null;
									}
									//currentScope.Evaluated = true;
									//currentScopeValue.PathParts = new Traversable(currentScopeValue._pathTokenizer.CompileListWithCurrent(context, index, out var errProducer));
									//if (errProducer != null)
									//{
									//	context.Errors.Add(errProducer());

									//	indexedUntil = 0;
									//	return null;
									//}
									if (currentScopeValue != null &&
										!currentScopeValue.PathParts.Any())
									{
										context.Errors.Add(
											new InvalidPathSyntaxError(currentScopeValue.Location
													.AddWindow(new CharacterSnippedLocation(1, index, text)),
												","));

										indexedUntil = 0;
										return null;
									}
								}

								TerminateCurrentScope(tokenScopes);
								//add a new one into the stack as , indicates a new argument
								tokenScopes.Push(new HeaderTokenMatch
								{
									State = TokenState.ArgumentStart,
									Parent = currentScope.Parent
								});
							}
							else
							{
								Func<IMorestachioError> errFunc;
								if ((currentScope.Value is MorestachioExpression exp)
									&& text[index] != ';'
									&& text[index] != '#'
									&& exp._pathTokenizer.Add(text[index], context, index, out errFunc) == false)
								{
									var parseOp = ParseOperationCall(text, index, out index, context, tokenScopes, currentScope);
									if (parseOp == true)
									{
										if (!EvaluateScope(context, index, currentScope, exp))
										{
											indexedUntil = 0;
											return null;
										}

										//TerminateCurrentScope();
										break;
									}
									else if (parseOp == false)
									{
										context.Errors.Add(errFunc());
										indexedUntil = 0;
										return null;
									}
									context.Errors.Add(errFunc());
									indexedUntil = 0;
									return null;
								}

								if (Eoex(text, ref index))
								{
									//an expression can be ended just at any time
									//it just should not end with an .

									if (text[index] == '.')
									{
										context.Errors.Add(new MorestachioSyntaxError(context.CurrentLocation.Offset(index)
												.AddWindow(new CharacterSnippedLocation(1, index, text)),
											"Format", "(", "Name of Formatter",
											"Did not expect a . at the end of an expression without an formatter"));
									}

									if (!EvaluateScope(context, index, currentScope,
										currentScope.Value as MorestachioExpression))
									{
										indexedUntil = 0;
										return null;
									}
									//currentScope.Evaluated = true;
									//var expr = (currentScope.Value as MorestachioExpression);
									//expr.PathParts =
									//	new Traversable(expr._pathTokenizer.CompileListWithCurrent(context, index, out var errProducer));
									//if (errProducer != null)
									//{
									//	context.Errors.Add(errProducer());
									//}
									TerminateCurrentScope(tokenScopes);
								}
							}
						}
						break;
				}

				if (index > -1)
				{
					if (text[index] == ';') //expression delimiter char, advance the index and "process" it that way and stop this expression
					{
						morestachioExpressions.EndsWithDelimiter = true;
						index++;
						break;
					}
					if (text[index] == '#') //expression delimiter char, as the # indicates the start of another inline expression do not process it and reset the index
					{
						index--;
						break;
					}

				}

				if (!reprocess)
				{
					index++;
				}
			}

			if (tokenScopes.Any())
			{
				context.Errors.Add(new InvalidPathSyntaxError(context.CurrentLocation.Offset(index)
						.AddWindow(new CharacterSnippedLocation(1, index, text)),
					text));
			}

			context.AdvanceLocation(index - orIndx);
			indexedUntil = index;
			return morestachioExpressions;
		}

		private static bool EvaluateScope(TokenzierContext context, int index,
			HeaderTokenMatch currentScope, MorestachioExpression exp)
		{
			if (!currentScope.Evaluated)
			{
				currentScope.Evaluated = true;
				exp.PathParts =
					new Traversable(exp._pathTokenizer.CompileListWithCurrent(context, index, out var errProducer));
				if (errProducer != null)
				{
					context.Errors.Add(errProducer());
					return false;
				}

				return true;
			}

			return true;
		}

		#region TextEditHelper

		private static bool Eoex(string text, ref int index)
		{
			index = SkipWhitespaces(text, index);
			return index + 1 == text.Length || text[index] == ';' || text[index] == '#' || index == -1;
		}

		internal static int SkipWhitespaces(string text, int index)
		{
			if (Tokenizer.IsWhiteSpaceDelimiter(text[index]))
			{
				var skipWhitespaces = Seek(text, index, f => !Tokenizer.IsWhiteSpaceDelimiter(f), true);
				return skipWhitespaces == -1 ? index : skipWhitespaces;
			}

			return index;
		}

		private static int Seek(string text,
			int index,
			Func<char, bool> condition,
			bool includeCurrent)
		{
			var idx = index;
			if (!includeCurrent)
			{
				if (idx + 1 >= text.Length)
				{
					return idx;
				}

				idx++;
			}

			for (; idx < text.Length; idx++)
			{
				if (condition(text[idx]))
				{
					return idx;
				}
			}
			return -1;
		}

		private static char? SeekNext(string text, int index, out int nIndex)
		{
			index = Seek(text, index, f => Tokenizer.IsExpressionChar(f) ||
										   Tokenizer.IsPathDelimiterChar(f) ||
										   Tokenizer.IsOperationChar(f), false);
			if (index != -1 && index < text.Length)
			{
				nIndex = index;
				return text[index];
			}

			nIndex = index;
			return null;
		}

		private static bool IsNextCharOperator(string text, int index, out int nIndex)
		{
			index = Seek(text, index, f => !Tokenizer.IsWhiteSpaceDelimiter(f), false);
			if (index != -1)
			{
				nIndex = index;
				if (index > text.Length - 1)
				{
					return false;
				}

				return Tokenizer.IsOperationChar(text[index]);
			}

			nIndex = index;
			return index != -1;
		}

		private static char[] Take(string text, Func<char, bool> condition, ref int index)
		{
			var chrs = new List<char>();
			for (int i = index; i < text.Length; i++)
			{
				var c = text[i];
				index = i;
				if (!condition(c))
				{
					break;
				}
				chrs.Add(c);
			}

			return chrs.ToArray();
		}

		private static void TerminateCurrentScope(Stack<HeaderTokenMatch> tokenScopes, bool tryTerminate = false)
		{
			if ((tryTerminate && tokenScopes.Any()) || !tryTerminate)
			{
				//var headerTokenMatch = tokenScopes.Peek();
				//if (headerTokenMatch.BracketsCounter != 0)
				//{
				//	context.Errors.Add(new InvalidPathSyntaxError(
				//		headerTokenMatch.TokenLocation
				//			.AddWindow(new CharacterSnippedLocation(1, headerTokenMatch.TokenLocation.Character, text)),
				//		headerTokenMatch.Value.ToString()));
				//}

				tokenScopes.Pop();
			}
		}

		private static int EndParameterBracket(string text,
			int index,
			Stack<HeaderTokenMatch> tokenScopes,
			HeaderTokenMatch currentScope,
			TokenzierContext context,
			MorestachioMultiPartExpressionList morestachioExpressions)
		{
			var parent = currentScope.Parent?.Parent;
			char? seekNext;
			var currentChar = text[index];
			var nIndex = index;
			while (Tokenizer.IsEndOfFormatterArgument(seekNext = SeekNext(text, index, out nIndex)))
			{
				index = nIndex;
				if (seekNext == ')') //the next char is also an closing bracket so there is no next parameter nor an followup expression
				{
					tokenScopes.Peek().BracketsCounter--;
					//there is nothing after this expression so close it
					TerminateCurrentScope(tokenScopes);
					HeaderTokenMatch scope = null;
					if (tokenScopes.Any())
					{
						scope = tokenScopes.Peek();
					}

					if (scope?.Value is MorestachioExpressionListBase)//if the parent expression is a list close that list too
					{
						tokenScopes.Peek().BracketsCounter--;
						TerminateCurrentScope(tokenScopes);
						parent = parent?.Parent;
					}
					parent = parent?.Parent;//set the new parent for followup expression
				}
				else
				{
					//there is something after this expression
					if (seekNext == '.')//the next char indicates a followup expression
					{
						HeaderTokenMatch scope = null;
						if (tokenScopes.Any())
						{
							scope = tokenScopes.Peek();
						}
						if (scope != null && scope.Parent != null) //this is a nested expression
						{
							if ((scope.Parent?.Value is MorestachioExpressionListBase))//the parents parent expression is already a list so close the parent
							{
								scope = scope.Parent;
								TerminateCurrentScope(tokenScopes);
							}
							else if (!(scope.Value is MorestachioExpressionListBase))//the parent is not an list expression so replace the parent with a list expression
							{
								var oldValue = scope.Value as MorestachioExpression;
								scope.Value = new MorestachioMultiPartExpressionList(new List<IMorestachioExpression>
									{
										oldValue
									}, oldValue.Location);
								var parValue = (scope.Parent.Value as MorestachioExpression);
								var hasFormat = parValue.Formats.FirstOrDefault(f => f.MorestachioExpression == oldValue);
								if (hasFormat != null)
								{
									hasFormat.MorestachioExpression = scope.Value;
								}
							}
							else if (currentChar == ')' && scope.Value is MorestachioExpressionListBase)
							{
								scope = scope.Parent.Parent;
								TerminateCurrentScope(tokenScopes);
							}
							parent = scope;
						}
						else
						{
							//we are at root level no need to do anything as the root is already a list expression
							TerminateCurrentScope(tokenScopes, true);
						}
					}
					else
					{
						if (tokenScopes.Any())
						{
							parent = tokenScopes.Peek().Parent;
						}
						//the next char indicates a new parameter so close this expression and allow next
						TerminateCurrentScope(tokenScopes, true);
					}

					if (!Eoex(text, ref index))
					{
						HeaderTokenMatch item;
						if (parent != null)
						{
							//if there is a parent set then this indicates a new argument
							item = new HeaderTokenMatch
							{
								State = TokenState.ArgumentStart,
								Parent = parent,
								TokenLocation = context.CurrentLocation.Offset(index + 1)
							};
						}
						else
						{
							index++;
							index = SkipWhitespaces(text, index);
							if (!Tokenizer.IsStartOfExpressionPathChar(text[index]) && text[index] != ')')
							{
								context.Errors.Add(new InvalidPathSyntaxError(
									context.CurrentLocation.Offset(index)
										.AddWindow(new CharacterSnippedLocation(1, index, text)),
									currentScope.Value.ToString()));
								return text.Length;
							}

							if (morestachioExpressions != null)
							{
								if (text[index] != '.')
								{
									context.Errors.Add(new InvalidPathSyntaxError(
										context.CurrentLocation.Offset(index)
											.AddWindow(new CharacterSnippedLocation(1, index, text)),
										currentScope.Value.ToString()));
									return text.Length;
								}
							}

							index--;
							//currentScope.State = TokenState.DecideArgumentType;

							//if there is no parent set this indicates a followup expression
							item = new HeaderTokenMatch
							{
								State = TokenState.DecideArgumentType,
								Parent = parent,
								Value = new MorestachioExpression(context.CurrentLocation.Offset(index)),
								TokenLocation = context.CurrentLocation.Offset(index + 1)
							};
						}

						tokenScopes.Push(item);
					}

					if (seekNext == '.')
					{
						index--;
					}

					break;
				}

				if (tokenScopes.Count > 0)
				{
					tokenScopes.Peek().BracketsCounter = 0;
				}
				if (Eoex(text, ref index))
				{
					TerminateCurrentScope(tokenScopes, true);
					break;
				}

				currentChar = seekNext.Value;
			}

			return index;
		}

		private static bool GetMatchingOperator(string text,
			ref int index,
			TokenzierContext context,
			HeaderTokenMatch currentScope,
			MorestachioOperator[] morestachioOperators,
			out MorestachioOperator morestachioOperator)
		{
			var crrChar = text[index].ToString();
			if (Eoex(text, ref index) && morestachioOperators.SingleOrDefault()?.AcceptsTwoExpressions == true)
			{
				context.Errors.Add(new InvalidPathSyntaxError(
					context.CurrentLocation.Offset(index)
						.AddWindow(new CharacterSnippedLocation(1, index, text)),
					currentScope.Value?.ToString()));
				morestachioOperator = null;
				return false;
			}

			var nxtChar = text[index + 1];
			var mCopList = morestachioOperators.SingleOrDefault(e => e.OperatorText.Equals(crrChar + nxtChar));
			if (mCopList != null)
			{
				morestachioOperator = mCopList;
				index = index + 1;
			}
			else
			{
				morestachioOperator = morestachioOperators.SingleOrDefault(f => f.OperatorText.Equals(crrChar));
			}

			return true;
		}

		private static bool? ParseOperationCall(string text,
			int index,
			out int nIndex,
			TokenzierContext context,
			Stack<HeaderTokenMatch> tokenScopes,
			HeaderTokenMatch currentScope)
		{
			MorestachioOperatorExpression AddToParent(MorestachioOperator op, IMorestachioExpression opExpression)
			{
				MorestachioOperatorExpression morestachioOperatorExpression;
				//in this case we have a carry over
				morestachioOperatorExpression =
					new MorestachioOperatorExpression(op, opExpression, context.CurrentLocation.Offset(index));

				//AddCurrentScopeToParent(index, morestachioOperatorExpression, currentScope.Parent.Value);

				if (currentScope.Parent.Value is MorestachioExpressionListBase morestachioExpressionList)
				{
					morestachioExpressionList.Expressions.Remove(opExpression);
					morestachioExpressionList.Expressions.Add(morestachioOperatorExpression);
				}
				else if (currentScope.Parent.Value is MorestachioExpression morestachioExpression)
				{
					var format = morestachioExpression.Formats.Single(e =>
						e.MorestachioExpression == opExpression);
					format.MorestachioExpression = morestachioOperatorExpression;
				}

				return morestachioOperatorExpression;
			}

			var opList = MorestachioOperator.Yield().Where(e => e.OperatorText.StartsWith(text[index].ToString())).ToArray();

			nIndex = index;
			if (opList.Length > 0)
			{
				//index = index + 1;
				if (!GetMatchingOperator(text, ref index, context, currentScope, opList, out var op))
				{
					return false;
				}
				nIndex = index;

				TerminateCurrentScope(tokenScopes);

				MorestachioOperatorExpression morestachioOperatorExpression;
				if ((currentScope.Parent.Value is MorestachioExpression parentExpression &&
					 parentExpression.Formats.LastOrDefault()?.MorestachioExpression is MorestachioOperatorExpression opExpressionA)
					)
				{
					morestachioOperatorExpression = AddToParent(op, opExpressionA);
				}
				else if (currentScope.Parent.Value is MorestachioMultiPartExpressionList mpExp &&
						 mpExp.Expressions.LastOrDefault() is MorestachioOperatorExpression opExpressionB)
				{
					morestachioOperatorExpression = AddToParent(op, opExpressionB);
				}
				else
				{
					//remove the current scope and the operator will replace it
					morestachioOperatorExpression = new MorestachioOperatorExpression(op, currentScope.Value, context.CurrentLocation.Offset(index));
					//AddCurrentScopeToParent(index, morestachioOperatorExpression, currentScope.Parent.Value);

					if (currentScope.Parent.Value is MorestachioExpressionListBase morestachioExpressionList)
					{
						morestachioExpressionList.Expressions.Remove(currentScope.Value);
						morestachioExpressionList.Expressions.Add(morestachioOperatorExpression);
					}
					else if (currentScope.Parent.Value is MorestachioExpression morestachioExpression)
					{
						var format = morestachioExpression.Formats.Single(e =>
							e.MorestachioExpression == currentScope.Value);
						format.MorestachioExpression = morestachioOperatorExpression;
					}
				}

				if (op.AcceptsTwoExpressions)
				{
					//tokenScopes.Push(headerTokenMatch);
					tokenScopes.Push(new HeaderTokenMatch
					{
						State = TokenState.DecideArgumentType,
						Parent = new HeaderTokenMatch()
						{
							State = TokenState.Expression,
							Value = morestachioOperatorExpression,
							Parent = currentScope.Parent,
						},
					});
				}

				return true; // break;
			}

			return null;
		}

		private static void AddCurrentScopeToParent(int idx1, TokenzierContext context, HeaderTokenMatch currentScope, IMorestachioExpression value, IMorestachioExpression parent)
		{
			if (parent is MorestachioExpression exp)
			{
				exp.Formats.Add(
					new ExpressionArgument(context.CurrentLocation.Offset(idx1), value, currentScope.ArgumentName));
			}
			else if (parent is MorestachioExpressionListBase expList)
			{
				expList.Add(value);
			}
			else if (parent is MorestachioOperatorExpression mOperator)
			{
				mOperator.RightExpression = value;
				currentScope.Parent = currentScope.Parent.Parent;
			}
		}

		#endregion

		/// <inheritdoc />
		protected bool Equals(MorestachioExpression other)
		{
			if (other.PathParts.Count != PathParts.Count)
			{
				return false;
			}
			if (other.Formats.Count != Formats.Count)
			{
				return false;
			}

			if (other.FormatterName != FormatterName)
			{
				return false;
			}

			if (!other.Location.Equals(Location))
			{
				return false;
			}

			var parts = PathParts.ToArray();
			var otherParts = other.PathParts.ToArray();
			if (parts.Length != otherParts.Length || Formats.Count != other.Formats.Count)
			{
				return false;
			}

			for (var index = 0; index < parts.Length; index++)
			{
				var thisPart = parts[index];
				var thatPart = otherParts[index];
				if (thatPart.Value != thisPart.Value || thatPart.Key != thisPart.Key)
				{
					return false;
				}
			}

			for (var index = 0; index < Formats.Count; index++)
			{
				var thisArgument = Formats[index];
				var thatArgument = other.Formats[index];
				if (!thisArgument.Equals(thatArgument))
				{
					return false;
				}
			}

			return true;
		}

		/// <inheritdoc />
		public bool Equals(IMorestachioExpression other)
		{
			return Equals((object)other);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((MorestachioExpression)obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (PathParts != null ? PathParts.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Formats != null ? Formats.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (FormatterName != null ? FormatterName.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Location.GetHashCode());
				return hashCode;
			}
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
			private readonly MorestachioExpression _exp;

			public ExpressionDebuggerDisplay(MorestachioExpression exp)
			{
				_exp = exp;
			}

			public string Path
			{
				get { return string.Join(".", _exp.PathParts); }
			}

			public string FormatterName
			{
				get { return _exp.FormatterName; }
			}

			public string Expression
			{
				get { return _exp.ToString(); }
			}
		}
	}
}