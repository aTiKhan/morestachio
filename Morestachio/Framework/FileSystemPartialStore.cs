﻿using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Morestachio.Helper;
using Morestachio.TemplateContainers;

namespace Morestachio.Framework
{
	/// <summary>
	///		Provides partials backed by the file system
	/// </summary>
	public class FileSystemPartialStore : IAsyncPartialsStore
	{
		/// <summary>
		///		The Directory path
		/// </summary>
		public string DirectoryPath { get; }

		/// <summary>
		///		The pattern to search in the <see cref="DirectoryPath"/>
		/// </summary>
		public string SearchPattern { get; }

		/// <summary>
		///		If set to true the files extension must not be included by <code>{{#IMPORT 'Filename'}}</code>
		/// </summary>
		public bool IgnoreExtension { get; }

		/// <summary>
		///		If set to true all files can be accessed without setting the whole path
		/// </summary>
		public bool IgnoreFileStructure { get; }

		/// <summary>
		///		If set prefixes all partials generated by this store with the prefix
		/// </summary>
		public string NamePrefix { get; }
		
		/// <summary>
		///		Creates a new FileSystemPartialStore
		/// </summary>
		/// <param name="directoryPath">Where on the file system should the Partials be enumerated</param>
		/// <param name="ignoreExtension">If set to true the files extension must not be included by <code>{{#IMPORT 'Filename'}}</code></param>
		/// <param name="ignoreFileStructure">If set to true all files can be accessed without setting the whole path</param>
		/// <param name="namePrefix">If set prefixes all partials generated by this store with the prefix</param>
		public FileSystemPartialStore(string directoryPath,
			string searchPattern,
			bool ignoreExtension,
			bool ignoreFileStructure,
			string namePrefix = null)
		{
			DirectoryPath = directoryPath;
			SearchPattern = searchPattern;
			IgnoreExtension = ignoreExtension;
			IgnoreFileStructure = ignoreFileStructure;
			NamePrefix = namePrefix;
		}

		/// <inheritdoc />
		public bool IsSealed { get; private set; }
		
		/// <inheritdoc />
		public void Seal()
		{
			IsSealed = true;

			if (IsSealed)
			{
				return;
			}
		}

		private string GetPartial(string partialName)
		{
			return Directory.EnumerateFiles(DirectoryPath, SearchPattern, SearchOption.AllDirectories)
				.FirstOrDefault(e => GetPartialName(e) == partialName);
		}

		private string GetPartialName(string partialFile)
		{
			var partialName = partialFile.Remove(0, DirectoryPath.Length);
			if (IgnoreExtension)
			{
				partialName = Path.ChangeExtension(partialName, null);
			}

			if (IgnoreFileStructure)
			{
				partialName = Path.GetFileNameWithoutExtension(partialName);
			}

			return NamePrefix + partialName;
		}

		/// <inheritdoc />
		public MorestachioDocumentInfo GetPartial(string name, ParserOptions parserOptions)
		{
			throw new System.NotImplementedException();
		}
		
		/// <inheritdoc />
		public string[] GetNames(ParserOptions parserOptions)
		{
			throw new System.NotImplementedException();
		}
		
		/// <inheritdoc />
		public async Task<MorestachioDocumentInfo> GetPartialAsync(string name, ParserOptions parserOptions)
		{
			var fileName = GetPartial(name);
			if (fileName == null)
			{
				return null;
			}

			var partialContent = File.ReadAllText(fileName, parserOptions.Encoding);
			var option = parserOptions.Copy();
			option.Template = new StringTemplateContainer(partialContent);
			return await Parser.ParseWithOptionsAsync(option);
		}
		
		/// <inheritdoc />
		public async Task<string[]> GetNamesAsync(ParserOptions parserOptions)
		{
			await AsyncHelper.FakePromise();
			return Directory.EnumerateFiles(DirectoryPath, SearchPattern, SearchOption.AllDirectories)
				.Select(GetPartialName)
				.ToArray();
		}
	}
}