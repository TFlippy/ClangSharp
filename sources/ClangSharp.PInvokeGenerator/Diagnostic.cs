// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using ClangSharp.Interop;
using System;

namespace ClangSharp
{
	public sealed class Diagnostic: IEquatable<Diagnostic>
	{
		private readonly DiagnosticLevel _level;
		private readonly string _message;
		private readonly string _location;

		public Diagnostic(DiagnosticLevel level, string message) : this(level, message, "")
		{
		}

		public Diagnostic(DiagnosticLevel level, string message, CXSourceLocation location) : this(level, message, location.ToString().Replace('\\', '/'))
		{
		}

		public Diagnostic(DiagnosticLevel level, string message, string location)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(nameof(message));
			}

			this._level = level;
			this._message = message;
			this._location = location;
		}

		public DiagnosticLevel Level => this._level;

		public string Location => this._location;

		public string Message => this._message;

		public override bool Equals(object obj)
		{
			return (obj is Diagnostic other) && this.Equals(other);
		}

		public bool Equals(Diagnostic other)
		{
			return (this._level == other.Level)
				&& (this._location == other.Location)
				&& (this._message == other.Message);
		}

		public override string ToString()
		{
			if (string.IsNullOrWhiteSpace(this._location))
			{
				return $"{this._level}: {this._message}";
			}
			return $"{this._level} ({this._location}): {this._message}";
		}
	}
}
