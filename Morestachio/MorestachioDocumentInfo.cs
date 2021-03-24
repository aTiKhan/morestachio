﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Morestachio.Document;
using Morestachio.Document.Contracts;
using Morestachio.Document.Items;
using Morestachio.Fluent;
using Morestachio.Helper;
using Morestachio.Parsing.ParserErrors;
using Morestachio.Profiler;
using Morestachio.Rendering;
#if ValueTask
using MorestachioDocumentResultPromise = System.Threading.Tasks.ValueTask<Morestachio.MorestachioDocumentResult>;
using StringPromise = System.Threading.Tasks.ValueTask<string>;
using Promise = System.Threading.Tasks.ValueTask;
#else
using MorestachioDocumentResultPromise = System.Threading.Tasks.Task<Morestachio.MorestachioDocumentResult>;
using StringPromise = System.Threading.Tasks.Task<string>;

#endif

namespace Morestachio
{
	/// <summary>
	///     Provided when parsing a template and getting information about the embedded variables.
	/// </summary>
	public class MorestachioDocumentInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorestachioDocumentInfo"/> class.
		/// </summary>
		/// <param name="options">The options.</param>
		/// <param name="document">The document.</param>
		public MorestachioDocumentInfo(ParserOptions options, IDocumentItem document)
			: this(options, document ?? throw new ArgumentNullException(nameof(document)), null)
		{

		}

		internal MorestachioDocumentInfo(ParserOptions options, IDocumentItem document, IEnumerable<IMorestachioError> errors)
		{
			ParserOptions = options ?? throw new ArgumentNullException(nameof(options));
			Document = document;
			Errors = errors ?? Enumerable.Empty<IMorestachioError>();
		}

		/// <summary>
		///		If enabled will output all variables set in the template in the <see cref="MorestachioDocumentResult.CapturedVariables"/>
		/// </summary>
		public bool CaptureVariables { get; set; }

		/// <summary>
		///		The Morestachio Document generated by the <see cref="Parser"/>
		/// </summary>
		public IDocumentItem Document { get; }

		/// <summary>
		///     The parser Options object that was used to create the Template Delegate
		/// </summary>
		public ParserOptions ParserOptions { get; }

		/// <summary>
		///		Gets a list of errors occured while parsing the Template
		/// </summary>
		public IEnumerable<IMorestachioError> Errors { get; }

		internal const int BufferSize = 2024;

		/// <summary>
		///		Creates an Fluent api wrapper for the current Document
		/// </summary>
		/// <returns></returns>
		public MorestachioDocumentFluentApi Fluent()
		{
			if (Errors.Any())
			{
				throw new AggregateException("You cannot access this Template as there are one or more Errors. See Inner Exception for more infos.", Errors.Select(e => e.GetException())).Flatten();
			}
			return new MorestachioDocumentFluentApi(this);
		}

		/// <summary>
		///		Creates a new Build-In renderer
		/// </summary>
		/// <returns></returns>
		public virtual IRenderer CreateRenderer()
		{
			return new Renderer(Document, ParserOptions, CaptureVariables);
		}

		/// <summary>
		///		Creates a new Build-In renderer
		/// </summary>
		/// <returns></returns>
		public virtual IRenderer CreateCompiledRenderer(IDocumentCompiler compiler = null)
		{
			return new CompiledRenderer(Document, ParserOptions, CaptureVariables, compiler ?? new DocumentCompiler());
		}

		/// <summary>
		///		Returns an delegate that can be executed to perform the rendering tasks
		/// </summary>
		/// <returns></returns>
		public CompilationResult Compile()
		{
			if (Errors.Any())
			{
				throw new AggregateException("You cannot Create this Template as there are one or more Errors. See Inner Exception for more infos.", Errors.Select(e => e.GetException())).Flatten();
			}

			if (Document is MorestachioDocument morestachioDocument && morestachioDocument.MorestachioVersion !=
				MorestachioDocument.GetMorestachioVersion())
			{
				throw new InvalidOperationException($"The supplied version in the Morestachio document " +
													$"'{morestachioDocument.MorestachioVersion}'" +
													$" is not compatible with the current morestachio version of " +
													$"'{MorestachioDocument.GetMorestachioVersion()}'");
			}
			
			var compiledRenderer = new CompiledRenderer(Document, ParserOptions, CaptureVariables, new DocumentCompiler());
			compiledRenderer.PreCompile();
			return async (data, token) => await compiledRenderer.Render(data, token);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="data"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Will be thrown when the Version of the document mismatches</exception>
		/// <exception cref="AggregateException">Will be thrown when there where any errors</exception>
		/// <exception cref="TimeoutException">Will be thrown when the given timeout expires</exception>
		public async MorestachioDocumentResultPromise CreateAsync(object data, CancellationToken token)
		{
			if (Errors.Any())
			{
				throw new AggregateException("You cannot Create this Template as there are one or more Errors. See Inner Exception for more infos.", Errors.Select(e => e.GetException())).Flatten();
			}

			if (Document is MorestachioDocument morestachioDocument && morestachioDocument.MorestachioVersion !=
				MorestachioDocument.GetMorestachioVersion())
			{
				throw new InvalidOperationException($"The supplied version in the Morestachio document " +
													$"'{morestachioDocument.MorestachioVersion}'" +
													$" is not compatible with the current morestachio version of " +
													$"'{MorestachioDocument.GetMorestachioVersion()}'");
			}

			return await CreateRenderer().Render(data, token);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public async MorestachioDocumentResultPromise CreateAsync(object data)
		{
			return await CreateAsync(data, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public async StringPromise CreateAndStringifyAsync(object source)
		{
			return await CreateAndStringifyAsync(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public async StringPromise CreateAndStringifyAsync(object source, CancellationToken token)
		{
			using (var stream = (await CreateAsync(source, token)).Stream)
			{
				return stream.Stringify(true, ParserOptions.Encoding);
			}
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public MorestachioDocumentResult Create(object source, CancellationToken token)
		{
			return CreateAsync(source, token).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public MorestachioDocumentResult Create(object source)
		{
			return Create(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public string CreateAndStringify(object source)
		{
			return CreateAndStringify(source, CancellationToken.None);
		}

		/// <summary>
		///     Calls the Underlying Template Delegate and Produces a Stream
		/// </summary>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public string CreateAndStringify(object source, CancellationToken token)
		{
			using (var stream = Create(source, token).Stream)
			{
				return stream.Stringify(true, ParserOptions.Encoding);
			}
		}
	}
}