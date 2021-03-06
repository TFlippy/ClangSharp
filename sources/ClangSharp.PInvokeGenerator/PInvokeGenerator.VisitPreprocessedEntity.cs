// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using ClangSharp.Interop;

namespace ClangSharp
{
	public partial class PInvokeGenerator
	{
		private unsafe void VisitMacroDefinitionRecord(MacroDefinitionRecord macroDefinitionRecord)
		{
			if (this.IsExcluded(macroDefinitionRecord))
			{
				return;
			}

			if (macroDefinitionRecord.IsFunctionLike)
			{
				this.AddDiagnostic(DiagnosticLevel.Warning, $"Function like macro definition records are not supported: '{macroDefinitionRecord.Name}'. Generated bindings may be incomplete.", macroDefinitionRecord);
				return;
			}

			var translationUnitHandle = macroDefinitionRecord.TranslationUnit.Handle;
			var tokens = translationUnitHandle.Tokenize(macroDefinitionRecord.Extent).ToArray();

			if ((tokens[0].Kind == CXTokenKind.CXToken_Identifier) && (tokens[0].GetSpelling(translationUnitHandle).CString == macroDefinitionRecord.Spelling))
			{
				if (tokens.Length == 1)
				{
					// Nothing to do for simple macro definitions with no value
					return;
				}

				var macroName = $"ClangSharpMacro_{macroDefinitionRecord.Spelling}";

				this._fileContentsBuilder.Append('\n');
				this._fileContentsBuilder.Append($"const auto {macroName} = ");

				var sourceRangeEnd = tokens[tokens.Length - 1].GetExtent(translationUnitHandle).End;
				var sourceRangeStart = tokens[1].GetLocation(translationUnitHandle);

				var sourceRange = CXSourceRange.Create(sourceRangeStart, sourceRangeEnd);

				var macroValue = this.GetSourceRangeContents(translationUnitHandle, sourceRange);
				this._fileContentsBuilder.Append(macroValue);

				this._fileContentsBuilder.Append(';');
				this._fileContentsBuilder.Append('\n');
			}
			else
			{
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported macro definition record: {macroDefinitionRecord.Name}. Generated bindings may be incomplete.", macroDefinitionRecord);
			}
		}

		private void VisitPreprocessingDirective(PreprocessingDirective preprocessingDirective)
		{
			if (preprocessingDirective is InclusionDirective)
			{
				// Not currently handling inclusion directives
			}
			else if (preprocessingDirective is MacroDefinitionRecord macroDefinitionRecord)
			{
				this.VisitMacroDefinitionRecord(macroDefinitionRecord);
			}
			else
			{
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported preprocessing directive: '{preprocessingDirective.CursorKind}'. Generated bindings may be incomplete.", preprocessingDirective);
			}
		}

		private void VisitPreprocessedEntity(PreprocessedEntity preprocessedEntity)
		{
			if (!this._config.GenerateMacroBindings)
			{
				return;
			}

			if (preprocessedEntity is MacroExpansion)
			{
				// Not currently handling macro expansions
			}
			else if (preprocessedEntity is PreprocessingDirective preprocessingDirective)
			{
				this.VisitPreprocessingDirective(preprocessingDirective);
			}
			else
			{
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported preprocessed entity: '{preprocessedEntity.CursorKind}'. Generated bindings may be incomplete.", preprocessedEntity);
			}
		}
	}
}
