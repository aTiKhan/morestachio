﻿using System.Globalization;
using System.IO;
using System.Text;
using Morestachio.Document.Custom;
using Morestachio.Formatter.Framework;
using Morestachio.Framework;
using Morestachio.Framework.Context.FallbackResolver;
using Morestachio.Framework.Context.Options;
using Morestachio.Framework.Context.Resolver;
using Morestachio.Framework.IO;
using Morestachio.Helper.Logging;
using Morestachio.TemplateContainers;

namespace Morestachio;

/// <summary>
///		Allows the fluid creation of a <see cref="ParserOptions"/> object
/// </summary>
public class ParserOptionsBuilder : IParserOptionsBuilder
{
	private ParserOptionsBuilder()
	{
		_builders = new Dictionary<string, Func<ParserOptions, ParserOptions>>();
	}

	private readonly IDictionary<string, Func<ParserOptions, ParserOptions>> _builders;

	/// <summary>
	///		Creates a new ParserOptions Builder
	/// </summary>
	/// <returns></returns>
	public static IParserOptionsBuilder New()
	{
		return new ParserOptionsBuilder();
	}

	/// <inheritdoc />
	public ParserOptions Build()
	{
		return Apply(ParserOptionsDefaultBuilder.GetDefaults().Apply(new ParserOptions()));
	}

	/// <inheritdoc />
	public ParserOptions Apply(ParserOptions options)
	{
		return _builders.Aggregate(options, (current, builder) => builder.Value(current));
	}

	private IParserOptionsBuilder WithValue(string name, Action<ParserOptions> operation)
	{
		_builders[name] = options =>
		{
			operation(options);
			return options;
		};
		return this;
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTemplate(string value)
	{
		return WithValue(nameof(ParserOptions.Template), options => options.Template = new StringTemplateContainer(value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTemplate(ITemplateContainer value)
	{
		return WithValue(nameof(ParserOptions.Template), options => options.Template = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStrictExecution(bool value)
	{
		return WithValue(nameof(ParserOptions.StrictExecution), options => options.StrictExecution = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTokenizeComments(bool value)
	{
		return WithValue(nameof(ParserOptions.TokenizeComments), options => options.TokenizeComments = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithPartialsStore(IPartialsStore value)
	{
		return WithValue(nameof(ParserOptions.PartialsStore), options => options.PartialsStore = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder AddCustomDocument(CustomDocumentItemProvider value)
	{
		return WithValue(nameof(ParserOptions.CustomDocumentItemProviders), options => options.CustomDocumentItemProviders.Add(value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithValueResolver(IValueResolver value)
	{
		return WithValue(nameof(ParserOptions.ValueResolver), options => options.ValueResolver = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithFallbackValueResolver(IFallbackValueResolver value)
	{
		return WithValue(nameof(ParserOptions.FallbackValueResolver), options => options.FallbackValueResolver = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithCultureInfo(CultureInfo value)
	{
		return WithValue(nameof(ParserOptions.CultureInfo), options => options.CultureInfo = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithFormatterService(IMorestachioFormatterService value)
	{
		return WithValue(nameof(ParserOptions.Formatters), options => options.Formatters = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithPartialStackSize(uint value)
	{
		return WithValue(nameof(ParserOptions.PartialStackSize), options => options.PartialStackSize = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithScopingBehavior(ScopingBehavior value)
	{
		return WithValue(nameof(ParserOptions.ScopingBehavior), options => options.ScopingBehavior = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStackOverflowBehavior(PartialStackOverflowBehavior value)
	{
		return WithValue(nameof(ParserOptions.StackOverflowBehavior), options => options.StackOverflowBehavior = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithUnmatchedFormatterBehavior(UnmatchedFormatterBehavior value)
	{
		return WithValue(nameof(ParserOptions.UnmatchedFormatterBehavior), options => options.UnmatchedFormatterBehavior = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithUnmatchedTagBehavior(UnmatchedTagBehavior value)
	{
		return WithValue(nameof(ParserOptions.UnmatchedTagBehavior), options => options.UnmatchedTagBehavior = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTimeout(TimeSpan value)
	{
		return WithValue(nameof(ParserOptions.Timeout), options => options.Timeout = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithDisableContentEscaping(bool value)
	{
		return WithValue(nameof(ParserOptions.DisableContentEscaping), options => options.DisableContentEscaping = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithMaxSize(long value)
	{
		return WithValue(nameof(ParserOptions.MaxSize), options => options.MaxSize = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStreamFactory(ByteCounterFactory value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTargetStream(Func<IByteCounterStream> value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = new ByteCounterFactory((opt) => value()));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTargetStream(Func<ParserOptions, IByteCounterStream> value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = new ByteCounterFactory(value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTargetStream(Func<Stream> value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = new ByteCounterFactory(value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithEncoding(Encoding value)
	{
		return WithValue(nameof(ParserOptions.Encoding), options => options.Encoding = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithLogger(ILogger value)
	{
		return WithValue(nameof(ParserOptions.Logger), options => options.Logger = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithNull(string value)
	{
		return WithValue(nameof(ParserOptions.Null), options => options.Null = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithHandleDictionaryAsObject(bool value)
	{
		return WithValue(nameof(ParserOptions.HandleDictionaryAsObject), options => options.HandleDictionaryAsObject = value);
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTemplate(Func<string> value)
	{
		return WithValue(nameof(ParserOptions.Template), options => options.Template = new StringTemplateContainer(value()));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTemplate(Func<ITemplateContainer> value)
	{
		return WithValue(nameof(ParserOptions.Template), options => options.Template = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStrictExecution(Func<bool> value)
	{
		return WithValue(nameof(ParserOptions.StrictExecution), options => options.StrictExecution = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTokenizeComments(Func<bool> value)
	{
		return WithValue(nameof(ParserOptions.TokenizeComments), options => options.TokenizeComments = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithPartialsStore(Func<IPartialsStore> value)
	{
		return WithValue(nameof(ParserOptions.PartialsStore), options => options.PartialsStore = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder AddCustomDocument(Func<CustomDocumentItemProvider> value)
	{
		return WithValue(nameof(ParserOptions.CustomDocumentItemProviders), options => options.CustomDocumentItemProviders.Add(value()));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithValueResolver(Func<IValueResolver> value)
	{
		return WithValue(nameof(ParserOptions.ValueResolver), options => options.ValueResolver = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithFallbackValueResolver(Func<IFallbackValueResolver> value)
	{
		return WithValue(nameof(ParserOptions.FallbackValueResolver), options => options.FallbackValueResolver = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithCultureInfo(Func<CultureInfo> value)
	{
		return WithValue(nameof(ParserOptions.CultureInfo), options => options.CultureInfo = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithFormatterService(Func<IMorestachioFormatterService> value)
	{
		return WithValue(nameof(ParserOptions.Formatters), options => options.Formatters = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithPartialStackSize(Func<uint> value)
	{
		return WithValue(nameof(ParserOptions.PartialStackSize), options => options.PartialStackSize = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithScopingBehavior(Func<ScopingBehavior> value)
	{
		return WithValue(nameof(ParserOptions.ScopingBehavior), options => options.ScopingBehavior = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStackOverflowBehavior(Func<PartialStackOverflowBehavior> value)
	{
		return WithValue(nameof(ParserOptions.StackOverflowBehavior), options => options.StackOverflowBehavior = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithUnmatchedFormatterBehavior(Func<UnmatchedFormatterBehavior> value)
	{
		return WithValue(nameof(ParserOptions.UnmatchedFormatterBehavior), options => options.UnmatchedFormatterBehavior = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithUnmatchedTagBehavior(Func<UnmatchedTagBehavior> value)
	{
		return WithValue(nameof(ParserOptions.UnmatchedTagBehavior), options => options.UnmatchedTagBehavior = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTimeout(Func<TimeSpan> value)
	{
		return WithValue(nameof(ParserOptions.Timeout), options => options.Timeout = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithDisableContentEscaping(Func<bool> value)
	{
		return WithValue(nameof(ParserOptions.DisableContentEscaping), options => options.DisableContentEscaping = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithMaxSize(Func<long> value)
	{
		return WithValue(nameof(ParserOptions.MaxSize), options => options.MaxSize = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithStreamFactory(Func<ByteCounterFactory> value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTargetStream(IByteCounterStream value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = new ByteCounterFactory((opt) => value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithTargetStream(Stream value)
	{
		return WithValue(nameof(ParserOptions.StreamFactory), options => options.StreamFactory = new ByteCounterFactory(() => value));
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithEncoding(Func<Encoding> value)
	{
		return WithValue(nameof(ParserOptions.Encoding), options => options.Encoding = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithLogger(Func<ILogger> value)
	{
		return WithValue(nameof(ParserOptions.Logger), options => options.Logger = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithNull(Func<string> value)
	{
		return WithValue(nameof(ParserOptions.Null), options => options.Null = value());
	}

	/// <inheritdoc />
	public IParserOptionsBuilder WithHandleDictionaryAsObject(Func<bool> value)
	{
		return WithValue(nameof(ParserOptions.HandleDictionaryAsObject), options => options.HandleDictionaryAsObject = value());
	}
}