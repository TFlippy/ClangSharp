// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClangSharp
{
	public sealed class PInvokeGeneratorConfiguration
	{
		private const string DefaultMethodClassName = "Methods";

		private readonly Dictionary<string, string> _remappedNames;
		private readonly Dictionary<string, IReadOnlyList<string>> _withAttributes;
		private readonly Dictionary<string, string> _withCallConvs;
		private readonly Dictionary<string, string> _withLibraryPaths;
		private readonly Dictionary<string, string> _withTypes;
		private readonly Dictionary<string, IReadOnlyList<string>> _withUsings;
		private readonly PInvokeGeneratorConfigurationOptions _options;

		public PInvokeGeneratorConfiguration(string libraryPath, string namespaceName, string outputLocation, string testOutputLocation, PInvokeGeneratorConfigurationOptions options = PInvokeGeneratorConfigurationOptions.None, string[] excludedNames = null, string headerFile = null, string methodClassName = null, string methodPrefixToStrip = null, IReadOnlyDictionary<string, string> remappedNames = null, string[] traversalNames = null, IReadOnlyDictionary<string, IReadOnlyList<string>> withAttributes = null, IReadOnlyDictionary<string, string> withCallConvs = null, IReadOnlyDictionary<string, string> withLibraryPaths = null, string[] withSetLastErrors = null, IReadOnlyDictionary<string, string> withTypes = null, IReadOnlyDictionary<string, IReadOnlyList<string>> withUsings = null, string[] suppressGcMethods = null)
		{
			if (excludedNames is null)
			{
				excludedNames = Array.Empty<string>();
			}

			if (suppressGcMethods is null)
			{
				suppressGcMethods = Array.Empty<string>();
			}

			if (string.IsNullOrWhiteSpace(libraryPath))
			{
				libraryPath = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(methodClassName))
			{
				methodClassName = DefaultMethodClassName;
			}

			if (string.IsNullOrWhiteSpace(methodPrefixToStrip))
			{
				methodPrefixToStrip = string.Empty;
			}

			if (string.IsNullOrWhiteSpace(namespaceName))
			{
				throw new ArgumentNullException(nameof(namespaceName));
			}

			if (options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateCompatibleCode) && options.HasFlag(PInvokeGeneratorConfigurationOptions.GeneratePreviewCode))
			{
				throw new ArgumentOutOfRangeException(nameof(options));
			}

			if (string.IsNullOrWhiteSpace(outputLocation))
			{
				throw new ArgumentNullException(nameof(outputLocation));
			}

			if (traversalNames is null)
			{
				traversalNames = Array.Empty<string>();
			}

			if (withSetLastErrors is null)
			{
				withSetLastErrors = Array.Empty<string>();
			}

			this._options = options;
			this._remappedNames = new Dictionary<string, string>();
			this._withAttributes = new Dictionary<string, IReadOnlyList<string>>();
			this._withCallConvs = new Dictionary<string, string>();
			this._withLibraryPaths = new Dictionary<string, string>();
			this._withTypes = new Dictionary<string, string>();
			this._withUsings = new Dictionary<string, IReadOnlyList<string>>();

			this.ExcludedNames = excludedNames.ToHashSet();
			this.SuppressGCMethods = suppressGcMethods.ToList();
			this.HeaderText = string.IsNullOrWhiteSpace(headerFile) ? string.Empty : File.ReadAllText(headerFile);
			this.LibraryPath = $@"""{libraryPath}""";
			this.MethodClassName = methodClassName;
			this.MethodPrefixToStrip = methodPrefixToStrip;
			this.Namespace = namespaceName;
			this.OutputLocation = Path.GetFullPath(outputLocation);
			this.TestOutputLocation = !string.IsNullOrWhiteSpace(testOutputLocation) ? Path.GetFullPath(testOutputLocation) : string.Empty;

			// Normalize the traversal names to use \ rather than / so path comparisons are simpler
			this.TraversalNames = traversalNames.Select(traversalName => traversalName.Replace('\\', '/')).ToArray();
			this.WithSetLastErrors = withSetLastErrors;

			if (!this._options.HasFlag(PInvokeGeneratorConfigurationOptions.NoDefaultRemappings))
			{
				if (this.GeneratePreviewCodeNint)
				{
					this._remappedNames.Add("intptr_t", "nint");
					this._remappedNames.Add("ptrdiff_t", "nint");
					this._remappedNames.Add("size_t", "nuint");
					this._remappedNames.Add("uintptr_t", "nuint");
				}
				else
				{
					this._remappedNames.Add("intptr_t", "IntPtr");
					this._remappedNames.Add("ptrdiff_t", "IntPtr");
					this._remappedNames.Add("size_t", "UIntPtr");
					this._remappedNames.Add("uintptr_t", "UIntPtr");
				}
			}

			AddRange(this._remappedNames, remappedNames);
			AddRange(this._withAttributes, withAttributes);
			AddRange(this._withCallConvs, withCallConvs);
			AddRange(this._withLibraryPaths, withLibraryPaths);
			AddRange(this._withTypes, withTypes);
			AddRange(this._withUsings, withUsings);
		}

		public bool ExcludeComProxies => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.ExcludeComProxies);

		public bool ExcludeEmptyRecords => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.ExcludeEmptyRecords);

		public bool ExcludeEnumOperators => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.ExcludeEnumOperators);

		public HashSet<string> ExcludedNames { get; }
		public List<string> SuppressGCMethods { get; }

		public bool GenerateAggressiveInlining => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateAggressiveInlining);

		public bool GenerateCompatibleCode => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateCompatibleCode);

		public bool GenerateExplicitVtbls => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateExplicitVtbls);

		public bool GenerateMacroBindings => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateMacroBindings);

		public bool GeneratePreviewCodeFnptr => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GeneratePreviewCodeFnptr);

		public bool GeneratePreviewCodeNint => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GeneratePreviewCodeNint);

		public bool GenerateMultipleFiles => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateMultipleFiles);

		public bool GenerateTestsNUnit => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateTestsNUnit);

		public bool GenerateTestsXUnit => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateTestsXUnit);

		public bool GenerateUnixTypes => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.GenerateUnixTypes);

		public string HeaderText { get; }

		public string LibraryPath { get; }

		public bool LogExclusions => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.LogExclusions);

		public bool LogVisitedFiles => this._options.HasFlag(PInvokeGeneratorConfigurationOptions.LogVisitedFiles);

		public string MethodClassName { get; }

		public string MethodPrefixToStrip { get; }

		public string Namespace { get; }

		public string OutputLocation { get; }

		public IReadOnlyDictionary<string, string> RemappedNames => this._remappedNames;

		public string TestOutputLocation { get; }

		public string[] TraversalNames { get; }

		public IReadOnlyDictionary<string, IReadOnlyList<string>> WithAttributes => this._withAttributes;

		public IReadOnlyDictionary<string, string> WithCallConvs => this._withCallConvs;

		public IReadOnlyDictionary<string, string> WithLibraryPaths => this._withLibraryPaths;

		public string[] WithSetLastErrors { get; }

		public IReadOnlyDictionary<string, string> WithTypes => this._withTypes;

		public IReadOnlyDictionary<string, IReadOnlyList<string>> WithUsings => this._withUsings;

		private static void AddRange<TValue>(Dictionary<string, TValue> dictionary, IEnumerable<KeyValuePair<string, TValue>> keyValuePairs)
		{
			if (keyValuePairs != null)
			{
				foreach (var keyValuePair in keyValuePairs)
				{
					// Use the indexer, rather than Add, so that any
					// default mappings can be overwritten if desired.
					dictionary[keyValuePair.Key] = keyValuePair.Value;
				}
			}
		}
	}
}
