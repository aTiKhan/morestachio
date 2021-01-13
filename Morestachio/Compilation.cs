﻿using System.Threading.Tasks;
using Morestachio.Document;
using Morestachio.Document.Contracts;
using Morestachio.Framework.Context;
using Morestachio.Framework.IO;
#if ValueTask
using Promise = System.Threading.Tasks.ValueTask;
#else
using Promise = System.Threading.Tasks.Task;
#endif
namespace Morestachio
{
	/// <summary>
	///		Delegate for the result of an Compile() call of an <see cref="IDocumentItem"/> that implements <see cref="ISupportCustomCompilation"/>
	/// </summary>
	/// <param name="outputStream"></param>
	/// <param name="context"></param>
	/// <param name="scopeData"></param>
	/// <returns></returns>
	public delegate Promise Compilation(IByteCounterStream outputStream,
		ContextObject context,
		ScopeData scopeData);
}