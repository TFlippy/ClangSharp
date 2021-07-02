// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ClangSharp
{
	public sealed class OutputBuilder
	{
		public const string DefaultIndentationString = "    ";

		private readonly string _name;
		private readonly List<string> _contents;
		private readonly StringBuilder _currentLine;
		private readonly SortedSet<string> _usingDirectives;
		private readonly SortedSet<string> _staticUsingDirectives;
		private readonly string _indentationString;
		private readonly bool _isTestOutput;

		private int _indentationLevel;

		public OutputBuilder(string name, string indentationString = DefaultIndentationString, bool isTestOutput = false)
		{
			this._name = name;
			this._contents = new List<string>();
			this._currentLine = new StringBuilder();
			this._usingDirectives = new SortedSet<string>();
			this._staticUsingDirectives = new SortedSet<string>();
			this._indentationString = indentationString;
			this._isTestOutput = isTestOutput;
		}

		public IEnumerable<string> Contents => this._contents;

		public string IndentationString => this._indentationString;

		public bool IsTestOutput => this._isTestOutput;

		public string Name => this._name;

		public bool NeedsNewline { get; set; }

		public bool NeedsSemicolon { get; set; }

		public IEnumerable<string> StaticUsingDirectives => this._staticUsingDirectives;

		public IEnumerable<string> UsingDirectives => this._usingDirectives;

		public void AddUsingDirective(string namespaceName)
		{
			if (namespaceName.StartsWith("static "))
			{
				this._staticUsingDirectives.Add(namespaceName);
			}
			else
			{
				this._usingDirectives.Add(namespaceName);
			}
		}

		public void DecreaseIndentation()
		{
			if (this._indentationLevel == 0)
			{
				throw new InvalidOperationException();
			}

			this._indentationLevel--;
		}

		public void IncreaseIndentation()
		{
			this._indentationLevel++;
		}

		public void WriteBlockStart()
		{
			this.WriteIndentedLine('{');
			this.IncreaseIndentation();
		}

		public void WriteBlockEnd()
		{
			// We don't need a newline if immediately closing the scope
			this.NeedsNewline = false;

			// We don't need a semicolon if immediately closing the scope
			this.NeedsSemicolon = false;

			this.DecreaseIndentation();
			this.WriteIndentedLine('}');
		}

		public void Write<T>(T value)
		{
			this._currentLine.Append(value);
		}

		public void WriteIndentation()
		{
			this.WriteNewlineIfNeeded();

			for (var i = 0; i < this._indentationLevel; i++)
			{
				this._currentLine.Append(this._indentationString);
			}
		}

		public void WriteIndented<T>(T value)
		{
			this.WriteIndentation();
			this.Write(value);
		}

		public void WriteIndentedLine<T>(T value)
		{
			this.WriteIndentation();
			this.WriteLine(value);
		}

		public void WriteLine<T>(T value)
		{
			this.Write(value);
			this.WriteNewline();
		}

		public void WriteNewline()
		{
			this._contents.Add(this._currentLine.ToString());
			this._currentLine.Clear();
			this.NeedsNewline = false;
		}

		public void WriteNewlineIfNeeded()
		{
			if (this.NeedsNewline)
			{
				this.WriteNewline();
			}
		}

		public void WriteSemicolon()
		{
			this.Write(';');
			this.NeedsSemicolon = false;
			this.NeedsNewline = true;
		}

		public void WriteSemicolonIfNeeded()
		{
			if (this.NeedsSemicolon)
			{
				this.WriteSemicolon();
			}
		}
	}
}
