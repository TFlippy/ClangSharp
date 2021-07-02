// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ClangSharp
{
	public partial class PInvokeGenerator
	{
		private void VisitClassTemplateDecl(ClassTemplateDecl classTemplateDecl)
		{
			this.AddDiagnostic(DiagnosticLevel.Warning, $"Class templates are not supported: '{this.GetCursorQualifiedName(classTemplateDecl)}'. Generated bindings may be incomplete.", classTemplateDecl);
		}

		private void VisitClassTemplateSpecializationDecl(ClassTemplateSpecializationDecl classTemplateSpecializationDecl)
		{
			this.AddDiagnostic(DiagnosticLevel.Warning, $"Class template specializations are not supported: '{this.GetCursorQualifiedName(classTemplateSpecializationDecl)}'. Generated bindings may be incomplete.", classTemplateSpecializationDecl);
		}

		private void VisitDecl(Decl decl)
		{
			//Console.WriteLine(decl.Kind);

			if (this.IsExcluded(decl))
			{
				return;
			}

			switch (decl.Kind)
			{
				case CX_DeclKind.CX_DeclKind_AccessSpec:
				{
					// Access specifications are also exposed as a queryable property
					// on the declarations they impact, so we don't need to do anything
					break;
				}

				// case CX_DeclKind.CX_DeclKind_Block:
				// case CX_DeclKind.CX_DeclKind_Captured:
				// case CX_DeclKind.CX_DeclKind_ClassScopeFunctionSpecialization:

				case CX_DeclKind.CX_DeclKind_Empty:
				{
					// Nothing to generate for empty declarations
					break;
				}

				// case CX_DeclKind.CX_DeclKind_Export:
				// case CX_DeclKind.CX_DeclKind_ExternCContext:
				// case CX_DeclKind.CX_DeclKind_FileScopeAsm:
				// case CX_DeclKind.CX_DeclKind_Friend:
				// case CX_DeclKind.CX_DeclKind_FriendTemplate:
				// case CX_DeclKind.CX_DeclKind_Import:

				case CX_DeclKind.CX_DeclKind_LinkageSpec:
				{
					this.VisitLinkageSpecDecl((LinkageSpecDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_Label:

				case CX_DeclKind.CX_DeclKind_Namespace:
				{
					this.VisitNamespaceDecl((NamespaceDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_NamespaceAlias:
				// case CX_DeclKind.CX_DeclKind_ObjCCompatibleAlias:
				// case CX_DeclKind.CX_DeclKind_ObjCCategory:
				// case CX_DeclKind.CX_DeclKind_ObjCCategoryImpl:
				// case CX_DeclKind.CX_DeclKind_ObjCImplementation:
				// case CX_DeclKind.CX_DeclKind_ObjCInterface:
				// case CX_DeclKind.CX_DeclKind_ObjCProtocol:
				// case CX_DeclKind.CX_DeclKind_ObjCMethod:
				// case CX_DeclKind.CX_DeclKind_ObjCProperty:
				// case CX_DeclKind.CX_DeclKind_BuiltinTemplate:
				// case CX_DeclKind.CX_DeclKind_Concept:

				case CX_DeclKind.CX_DeclKind_ClassTemplate:
				{
					this.VisitClassTemplateDecl((ClassTemplateDecl)decl);
					break;
				}

				case CX_DeclKind.CX_DeclKind_FunctionTemplate:
				{
					this.VisitFunctionTemplateDecl((FunctionTemplateDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_TypeAliasTemplate:
				// case CX_DeclKind.CX_DeclKind_VarTemplate:
				// case CX_DeclKind.CX_DeclKind_TemplateTemplateParm:

				case CX_DeclKind.CX_DeclKind_Enum:
				{
					this.VisitEnumDecl((EnumDecl)decl);
					break;
				}

				case CX_DeclKind.CX_DeclKind_Record:
				case CX_DeclKind.CX_DeclKind_CXXRecord:
				{
					this.VisitRecordDecl((RecordDecl)decl);
					break;
				}

				case CX_DeclKind.CX_DeclKind_ClassTemplateSpecialization:
				{
					this.VisitClassTemplateSpecializationDecl((ClassTemplateSpecializationDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_ClassTemplatePartialSpecialization:
				// case CX_DeclKind.CX_DeclKind_TemplateTypeParm:
				// case CX_DeclKind.CX_DeclKind_ObjCTypeParam:
				// case CX_DeclKind.CX_DeclKind_TypeAlias:

				case CX_DeclKind.CX_DeclKind_Typedef:
				{
					this.VisitTypedefDecl((TypedefDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_UnresolvedUsingTypename:

				case CX_DeclKind.CX_DeclKind_Using:
				{
					// Using declarations only introduce existing members into
					// the current scope. There isn't an easy way to translate
					// this to C#, so we will ignore them for now.
					break;
				}

				// case CX_DeclKind.CX_DeclKind_UsingDirective:
				// case CX_DeclKind.CX_DeclKind_UsingPack:
				// case CX_DeclKind.CX_DeclKind_UsingShadow:
				// case CX_DeclKind.CX_DeclKind_ConstructorUsingShadow:
				// case CX_DeclKind.CX_DeclKind_Binding:

				case CX_DeclKind.CX_DeclKind_Field:
				{
					this.VisitFieldDecl((FieldDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_ObjCAtDefsField:
				// case CX_DeclKind.CX_DeclKind_ObjCIvar:

				case CX_DeclKind.CX_DeclKind_Function:
				case CX_DeclKind.CX_DeclKind_CXXMethod:
				case CX_DeclKind.CX_DeclKind_CXXConstructor:
				case CX_DeclKind.CX_DeclKind_CXXDestructor:
				case CX_DeclKind.CX_DeclKind_CXXConversion:
				{
					this.VisitFunctionDecl((FunctionDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_CXXDeductionGuide:
				// case CX_DeclKind.CX_DeclKind_MSProperty:
				// case CX_DeclKind.CX_DeclKind_NonTypeTemplateParm:

				case CX_DeclKind.CX_DeclKind_Var:
				{
					this.VisitVarDecl((VarDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_Decomposition:
				// case CX_DeclKind.CX_DeclKind_ImplicitParam:
				// case CX_DeclKind.CX_DeclKind_OMPCapturedExpr:

				case CX_DeclKind.CX_DeclKind_ParmVar:
				{
					this.VisitParmVarDecl((ParmVarDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_VarTemplateSpecialization:
				// case CX_DeclKind.CX_DeclKind_VarTemplatePartialSpecialization:

				case CX_DeclKind.CX_DeclKind_EnumConstant:
				{
					this.VisitEnumConstantDecl((EnumConstantDecl)decl);
					break;
				}

				// case CX_DeclKind.CX_DeclKind_IndirectField:
				// case CX_DeclKind.CX_DeclKind_OMPDeclareMapper:
				// case CX_DeclKind.CX_DeclKind_OMPDeclareReduction:
				// case CX_DeclKind.CX_DeclKind_UnresolvedUsingValue:
				// case CX_DeclKind.CX_DeclKind_OMPAllocate:
				// case CX_DeclKind.CX_DeclKind_OMPRequires:
				// case CX_DeclKind.CX_DeclKind_OMPThreadPrivate:
				// case CX_DeclKind.CX_DeclKind_ObjCPropertyImpl:

				case CX_DeclKind.CX_DeclKind_PragmaComment:
				{
					// Pragma comments can't be easily modeled in C#
					// We'll ignore them for now.
					break;
				}

				// case CX_DeclKind.CX_DeclKind_PragmaDetectMismatch:

				case CX_DeclKind.CX_DeclKind_StaticAssert:
				{
					// Static asserts can't be easily modeled in C#
					// We'll ignore them for now.
					break;
				}

				case CX_DeclKind.CX_DeclKind_TranslationUnit:
				{
					this.VisitTranslationUnitDecl((TranslationUnitDecl)decl);
					break;
				}

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported declaration: '{decl.Kind}'. Generated bindings may be incomplete.", decl);
					break;
				}
			}
		}

		private void VisitEnumConstantDecl(EnumConstantDecl enumConstantDecl)
		{
			var accessSpecifier = string.Empty;
			var name = this.GetRemappedCursorName(enumConstantDecl);
			var escapedName = this.EscapeName(name);
			var isAnonymousEnum = false;

			var typeName = string.Empty;

			if (enumConstantDecl.DeclContext is EnumDecl enumDecl)
			{
				if (this.GetRemappedCursorName(enumDecl).StartsWith("__AnonymousEnum_"))
				{
					isAnonymousEnum = true;
					accessSpecifier = this.GetAccessSpecifierName(enumDecl);
				}
				typeName = this.GetRemappedTypeName(enumDecl, context: null, enumDecl.IntegerType, out var nativeTypeName);
			}
			else
			{
				typeName = this.GetRemappedTypeName(enumConstantDecl, context: null, enumConstantDecl.Type, out var nativeTypeName);
			}

			this._outputBuilder.WriteIndentation();

			if (isAnonymousEnum)
			{
				this._outputBuilder.Write(accessSpecifier);
				this._outputBuilder.Write(" const ");
				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');
			}

			this._outputBuilder.Write(escapedName);

			if (enumConstantDecl.InitExpr != null)
			{
				this._outputBuilder.Write(" = ");

				//if (isAnonymousEnum)
				//{
				//    _outputBuilder.Write(_config.MethodClassName);
				//    _outputBuilder.Write('.');
				//}

				this.UncheckStmt(typeName, enumConstantDecl.InitExpr);
			}
			else if (isAnonymousEnum)
			{
				this._outputBuilder.Write(" = ");

				if (IsUnsigned(typeName))
				{
					this._outputBuilder.Write(enumConstantDecl.UnsignedInitVal);
				}
				else
				{
					this._outputBuilder.Write(enumConstantDecl.InitVal);
				}
			}

			this._outputBuilder.WriteLine(isAnonymousEnum ? ';' : ',');
		}

		private void VisitEnumDecl(EnumDecl enumDecl)
		{
			var accessSpecifier = this.GetAccessSpecifierName(enumDecl);
			var name = this.GetRemappedCursorName(enumDecl);
			var escapedName = this.EscapeName(name);
			var isAnonymousEnum = false;

			if (name.StartsWith("__AnonymousEnum_"))
			{
				isAnonymousEnum = true;
				name = this._config.MethodClassName;
			}

			this.StartUsingOutputBuilder(name);
			{
				if (!isAnonymousEnum)
				{
					var typeName = this.GetRemappedTypeName(enumDecl, context: null, enumDecl.IntegerType, out var nativeTypeName);
					this.AddNativeTypeNameAttribute(nativeTypeName);

					this._outputBuilder.WriteIndented(accessSpecifier);
					this._outputBuilder.Write(" enum ");
					this._outputBuilder.Write(escapedName);

					if (!typeName.Equals("int"))
					{
						this._outputBuilder.Write(" : ");
						this._outputBuilder.Write(typeName);
					}

					this._outputBuilder.NeedsNewline = true;
					this._outputBuilder.WriteBlockStart();
				}

				this.Visit(enumDecl.Enumerators);
				this.Visit(enumDecl.Decls, excludedCursors: enumDecl.Enumerators);

				if (!isAnonymousEnum)
				{
					this._outputBuilder.WriteBlockEnd();
				}
			}
			this.StopUsingOutputBuilder();
		}

		private void VisitFieldDecl(FieldDecl fieldDecl)
		{
			if (fieldDecl.IsBitField)
			{
				return;
			}

			var accessSpecifier = this.GetAccessSpecifierName(fieldDecl);
			var name = this.GetRemappedCursorName(fieldDecl);
			var escapedName = this.EscapeName(name);

			var type = fieldDecl.Type;
			var typeName = this.GetRemappedTypeName(fieldDecl, context: null, type, out var nativeTypeName);

			if (fieldDecl.Parent.IsUnion)
			{
				this._outputBuilder.WriteIndentedLine("[FieldOffset(0)]");
			}
			else
			{
				this._outputBuilder.WriteIndentedLine($"[FieldOffset({fieldDecl.Handle.OffsetOfField / 8})]");
			}

			this.AddNativeTypeNameAttribute(nativeTypeName);

			this._outputBuilder.WriteIndented(accessSpecifier);
			this._outputBuilder.Write(' ');

			if (this.NeedsNewKeyword(name))
			{
				this._outputBuilder.Write("new ");
			}

			if (type.CanonicalType is ConstantArrayType constantArrayType)
			{
				if (this.IsSupportedFixedSizedBufferType(typeName))
				{
					this._outputBuilder.Write("fixed ");
					this._outputBuilder.Write(typeName);
					this._outputBuilder.Write(' ');
					this._outputBuilder.Write(escapedName);
					this._outputBuilder.Write('[');
					this._outputBuilder.Write(Math.Max(constantArrayType.Size, 1));

					var elementType = constantArrayType.ElementType;

					while (elementType.CanonicalType is ConstantArrayType subConstantArrayType)
					{
						this._outputBuilder.Write(" * ");
						this._outputBuilder.Write(Math.Max(subConstantArrayType.Size, 1));

						elementType = subConstantArrayType.ElementType;
					}

					this._outputBuilder.Write(']');
				}
				else
				{
					this._outputBuilder.Write(this.GetArtificialFixedSizedBufferName(fieldDecl));
					this._outputBuilder.Write(' ');
					this._outputBuilder.Write(escapedName);
				}
			}
			else
			{
				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');
				this._outputBuilder.Write(escapedName);
			}

			this._outputBuilder.WriteSemicolon();
			this._outputBuilder.WriteNewline();
		}

		private void VisitFunctionDecl(FunctionDecl functionDecl)
		{
			if (this.IsExcluded(functionDecl)) return;
			if (functionDecl.HasBody) return;
			if (functionDecl.Access == CX_CXXAccessSpecifier.CX_CXXProtected || functionDecl.Access == CX_CXXAccessSpecifier.CX_CXXPrivate) return;
			if (functionDecl is CXXConstructorDecl) return;
			if (functionDecl is CXXConversionDecl) return;
			if (functionDecl is CXXDestructorDecl) return;

			var accessSppecifier = this.GetAccessSpecifierName(functionDecl);
			var name = this.GetRemappedCursorName(functionDecl);
			var escapedName = this.EscapeName(name);

			if (!(functionDecl.DeclContext is CXXRecordDecl cxxRecordDecl))
			{
				cxxRecordDecl = null;
				this.StartUsingOutputBuilder(this._config.MethodClassName);
			}

			this.WithAttributes("*");
			this.WithAttributes(name);

			this.WithUsings("*");
			this.WithUsings(name);

			var type = functionDecl.Type;
			var callConv = CXCallingConv.CXCallingConv_Invalid;

			if (type is AttributedType attributedType)
			{
				type = attributedType.ModifiedType;
				callConv = attributedType.Handle.FunctionTypeCallingConv;
			}
			var functionType = (FunctionType)type;

			if (callConv == CXCallingConv.CXCallingConv_Invalid)
			{
				callConv = functionType.CallConv;
			}

			var cxxMethodDecl = functionDecl as CXXMethodDecl;
			var body = functionDecl.Body;
			var isVirtual = (cxxMethodDecl != null) && cxxMethodDecl.IsVirtual;

			var suppress_gc = false;
			foreach (var regex in this.Config.SuppressGCMethods)
			{
				var match = Regex.Match(name, regex);
				if (match.Success && match.Value.Length == name.Length)
				{
					Console.WriteLine(name);
					suppress_gc = true;
					break;
				}
			}

			this.AddComments(functionDecl.Handle.ParsedComment, notes: new KeyValuePair<string, object>[]
			{
				new("SuppressGCTransition", suppress_gc),
			}, location: functionDecl.Location);

			if (suppress_gc)
			{
				this._outputBuilder.WriteLine("[SuppressGCTransition]");
			}

			if (isVirtual)
			{
				Debug.Assert(!this._config.GeneratePreviewCodeFnptr);

				this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");

				var callingConventionName = this.GetCallingConventionName(functionDecl, callConv, name, isForFnptr: false);

				this._outputBuilder.WriteIndented("[UnmanagedFunctionPointer");

				if (callingConventionName != "Winapi")
				{
					this._outputBuilder.Write("(CallingConvention.");
					this._outputBuilder.Write(callingConventionName);
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.WriteLine(']');
			}
			else if (body is null)
			{
				this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");

				this._outputBuilder.WriteIndented("[DllImport(");

				this.WithLibraryPath(name);

				this._outputBuilder.Write(", ");

				var callingConventionName = this.GetCallingConventionName(functionDecl, callConv, name, isForFnptr: false);

				if (callingConventionName != "Winapi")
				{
					this._outputBuilder.Write("CallingConvention = CallingConvention.");
					this._outputBuilder.Write(callingConventionName);
					this._outputBuilder.Write(", ");
				}

				var entryPoint = (cxxMethodDecl is null) ? this.GetCursorName(functionDecl) : cxxMethodDecl.Handle.Mangling.CString;

				if (entryPoint != name)
				{
					this._outputBuilder.Write("EntryPoint = \"");
					this._outputBuilder.Write(entryPoint);
					this._outputBuilder.Write("\", ");
				}

				this._outputBuilder.Write("ExactSpelling = true");
				this.WithSetLastError(name);
				this._outputBuilder.WriteLine(")]");
			}

			//if (this.Config.SuppressGCMethods.Contains(name))
			//{
			//    Console.WriteLine(name);
			//    _outputBuilder.WriteLine("[SuppressGCTransition]");
			//}

			var returnType = functionDecl.ReturnType;
			var returnTypeName = this.GetRemappedTypeName(functionDecl, cxxRecordDecl, returnType, out var nativeTypeName);

			if ((isVirtual || (body is null)) && (returnTypeName == "bool"))
			{
				// bool is not blittable, so we shouldn't use it for P/Invoke signatures
				returnTypeName = "byte";
				nativeTypeName = string.IsNullOrWhiteSpace(nativeTypeName) ? "bool" : nativeTypeName;
			}

			this.AddNativeTypeNameAttribute(nativeTypeName, attributePrefix: "return: ");

			this._outputBuilder.WriteIndented(accessSppecifier);

			if (isVirtual)
			{
				this._outputBuilder.Write(" delegate");
			}
			else if ((body is null) || (cxxMethodDecl is null) || cxxMethodDecl.IsStatic)
			{
				this._outputBuilder.Write(" static");

				if (body is null)
				{
					this._outputBuilder.Write(" extern");
				}
			}

			this._outputBuilder.Write(' ');

			if (!isVirtual)
			{
				if (this.NeedsNewKeyword(name, functionDecl.Parameters))
				{
					this._outputBuilder.Write("new ");
				}

				if (this.IsUnsafe(functionDecl))
				{
					if (cxxRecordDecl is null)
					{
						this._isMethodClassUnsafe = true;
					}
					else if (!this.IsUnsafe(cxxRecordDecl))
					{
						this._outputBuilder.Write("unsafe ");
					}
				}
			}

			var needsReturnFixup = isVirtual && this.NeedsReturnFixup(cxxMethodDecl);

			if (!(functionDecl is CXXConstructorDecl))
			{
				this._outputBuilder.Write(returnTypeName);

				if (needsReturnFixup)
				{
					this._outputBuilder.Write('*');
				}

				this._outputBuilder.Write(' ');
			}

			if (isVirtual)
			{
				this._outputBuilder.Write(this.PrefixAndStripName(name));
			}
			else
			{
				this._outputBuilder.Write(this.EscapeAndStripName(name));
			}

			this._outputBuilder.Write('(');

			if (isVirtual)
			{
				Debug.Assert(cxxRecordDecl != null);

				if (!this.IsPrevContextDecl<CXXRecordDecl>(out var thisCursor))
				{
					thisCursor = cxxRecordDecl;
				}

				var cxxRecordDeclName = this.GetRemappedCursorName(thisCursor);
				this._outputBuilder.Write(this.EscapeName(cxxRecordDeclName));
				this._outputBuilder.Write("* pThis");

				if (needsReturnFixup)
				{
					this._outputBuilder.Write(", ");
					this._outputBuilder.Write(returnTypeName);
					this._outputBuilder.Write("* _result");
				}

				if (functionDecl.Parameters.Any())
				{
					this._outputBuilder.Write(", ");
				}
			}

			this.Visit(functionDecl.Parameters);

			this._outputBuilder.Write(')');

			if ((body is null) || isVirtual)
			{
				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();
			}
			else
			{
				this._outputBuilder.NeedsNewline = true;

				var firstCtorInitializer = functionDecl.Parameters.Any() ? (functionDecl.CursorChildren.IndexOf(functionDecl.Parameters.Last()) + 1) : 0;
				var lastCtorInitializer = (functionDecl.Body != null) ? functionDecl.CursorChildren.IndexOf(functionDecl.Body) : functionDecl.CursorChildren.Count;

				this._outputBuilder.WriteBlockStart();

				if (functionDecl is CXXConstructorDecl cxxConstructorDecl)
				{
					VisitCtorInitializers(cxxConstructorDecl, firstCtorInitializer, lastCtorInitializer);
				}

				if (body is CompoundStmt compoundStmt)
				{
					this.VisitStmts(compoundStmt.Body);
				}
				else
				{
					this._outputBuilder.WriteIndentation();
					this.Visit(body);
				}

				this._outputBuilder.WriteSemicolonIfNeeded();
				this._outputBuilder.WriteNewlineIfNeeded();
				this._outputBuilder.WriteBlockEnd();
			}
			this._outputBuilder.NeedsNewline = true;

			this.Visit(functionDecl.Decls, excludedCursors: functionDecl.Parameters);

			if (cxxRecordDecl is null)
			{
				this.StopUsingOutputBuilder();
			}

			void VisitCtorInitializers(CXXConstructorDecl cxxConstructorDecl, int firstCtorInitializer, int lastCtorInitializer)
			{
				for (var i = firstCtorInitializer; i < lastCtorInitializer; i++)
				{
					if (cxxConstructorDecl.CursorChildren[i] is Attr)
					{
						continue;
					}

					var memberRef = (Ref)cxxConstructorDecl.CursorChildren[i];
					var memberInit = (Stmt)cxxConstructorDecl.CursorChildren[++i];

					if (memberInit is ImplicitValueInitExpr)
					{
						continue;
					}

					var memberRefName = this.GetRemappedCursorName(memberRef.Referenced);
					var memberInitName = memberInit.Spelling;

					if ((memberInit is CastExpr castExpr) && (castExpr.SubExprAsWritten is DeclRefExpr declRefExpr))
					{
						memberInitName = this.GetRemappedCursorName(declRefExpr.Decl);
					}
					this._outputBuilder.WriteIndentation();

					if (memberRefName.Equals(memberInitName))
					{
						this._outputBuilder.Write("this");
						this._outputBuilder.Write('.');
					}

					this.Visit(memberRef);
					this._outputBuilder.Write(' ');
					this._outputBuilder.Write('=');
					this._outputBuilder.Write(' ');

					var memberRefTypeName = this.GetRemappedTypeName(memberRef, context: null, memberRef.Type, out var memberRefNativeTypeName);

					this.UncheckStmt(memberRefTypeName, memberInit);

					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
				}
			}
		}

		private void AddComments(CXComment parent, IEnumerable<KeyValuePair<string, object>> notes = null, CXSourceLocation? location = null)
		{
			var count = parent.NumChildren;

			var paragraphs = new List<CXComment>(8);
			var misc = new List<CXComment>(8);

			if (count > 0)
			{
				for (var i = 0u; i < count; i++)
				{
					var child = parent.GetChild(i);

					if (child.Kind == CXCommentKind.CXComment_Paragraph)
					{
						paragraphs.Add(child);
					}
					else if (child.Kind != CXCommentKind.CXComment_Null)
					{
						misc.Add(child);
					}

					//AddComment(child);
				}
			}

			if (paragraphs.Count > 0)
			{
				this._outputBuilder.WriteIndentedLine("/// <summary>");
				for (var i = 0; i < paragraphs.Count; i++)
				{
					var comment = paragraphs[i];
					this.WriteCommentRecursive(comment);
				}
				this._outputBuilder.WriteIndentedLine("/// </summary>");
			}

			if (misc.Count > 0)
			{
				foreach (var comment in misc)
				{
					this.WriteCommentRecursive(comment);
				}
			}

			this._outputBuilder.WriteIndentedLine("/// <remarks>");
			if (notes != null)
			{
				foreach (var note in notes)
				{
					this._outputBuilder.WriteIndentedLine($"/// <br><b>{note.Key}</b>: {note.Value}</br>");
				}

				if (location != null)
				{
					location.Value.GetFileLocation(out var file, out var line, out var col, out var line_offset);
					this._outputBuilder.WriteIndentedLine($"/// <br><b>Source</b>: <c>{file.ToString().Replace('\\', '/')} ({line}, {col})</c></br>");
				}
			}
			this._outputBuilder.WriteIndentedLine("/// </remarks>");
		}

		private void WriteCommentRecursive(CXComment parent)
		{
			switch (parent.Kind)
			{
				case CXCommentKind.CXComment_Paragraph:
				{
					var count = parent.NumChildren;
					if (count > 0)
					{
						this._outputBuilder.WriteIndentedLine("/// <para>");
						for (var i = 0u; i < count; i++)
						{
							this.WriteCommentRecursive(parent.GetChild(i));
						}
						this._outputBuilder.WriteIndentedLine("/// </para>");
					}
				}
				break;

				case CXCommentKind.CXComment_ParamCommand:
				{
					//_outputBuilder.WriteIndentedLine($"/// <param name=\"{parent.ParamCommandComment_ParamName}\">{(count == 1 ? parent.GetChild(0).Kind : default)}</param>");
					this._outputBuilder.WriteIndentedLine($"/// <param name=\"{parent.ParamCommandComment_ParamName}\">");
					var count = parent.NumChildren;
					if (count > 0)
					{
						for (var i = 0u; i < count; i++)
						{
							var child = parent.GetChild(i);
							this.WriteCommentRecursive(child);
						}
					}
					this._outputBuilder.WriteIndentedLine("/// </param>");
				}
				break;

				case CXCommentKind.CXComment_BlockCommand:
				{
					//_outputBuilder.WriteIndentedLine($"/// <param name=\"{parent.ParamCommandComment_ParamName}\">{(count == 1 ? parent.GetChild(0).Kind : default)}</param>");
					this._outputBuilder.WriteIndentedLine($"/// <returns>");
					var count = parent.NumChildren;
					if (count > 0)
					{
						for (var i = 0u; i < count; i++)
						{
							var child = parent.GetChild(i);
							this.WriteCommentRecursive(child);
						}
					}
					this._outputBuilder.WriteIndentedLine("/// </returns>");
				}
				break;

				case CXCommentKind.CXComment_Text:
				{
					if (!parent.IsWhitespace)
					{
						this._outputBuilder.WriteIndentedLine($"/// <br>{parent.TextComment_Text}</br>");
					}
				}
				break;

				case CXCommentKind.CXComment_Null:
				{

				}
				break;

				default:
				{
					Console.WriteLine(parent.Kind);
				}
				break;
			}
		}

		private void VisitFunctionTemplateDecl(FunctionTemplateDecl functionTemplateDecl)
		{
			this.AddDiagnostic(DiagnosticLevel.Warning, $"Function templates are not supported: '{this.GetCursorQualifiedName(functionTemplateDecl)}'. Generated bindings may be incomplete.", functionTemplateDecl);
		}

		private void VisitLinkageSpecDecl(LinkageSpecDecl linkageSpecDecl)
		{
			foreach (var cursor in linkageSpecDecl.CursorChildren)
			{
				this.Visit(cursor);
			}
		}

		private void VisitNamespaceDecl(NamespaceDecl namespaceDecl)
		{
			// We don't currently include the namespace name anywhere in the
			// generated bindings. We might want to in the future...

			foreach (var cursor in namespaceDecl.CursorChildren)
			{
				this.Visit(cursor);
			}
		}

		private void VisitParmVarDecl(ParmVarDecl parmVarDecl)
		{
			if (this.IsExcluded(parmVarDecl))
			{
				return;
			}

			if (this.IsPrevContextDecl<FunctionDecl>(out var functionDecl))
			{
				ForFunctionDecl(parmVarDecl, functionDecl);
			}
			else if (this.IsPrevContextDecl<TypedefDecl>(out var typedefDecl))
			{
				ForTypedefDecl(parmVarDecl, typedefDecl);
			}
			else
			{
				this.IsPrevContextDecl<Decl>(out var previousContext);
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported parameter variable declaration parent: '{previousContext.CursorKindSpelling}'. Generated bindings may be incomplete.", previousContext);
			}

			void ForFunctionDecl(ParmVarDecl parmVarDecl, FunctionDecl functionDecl)
			{
				var type = parmVarDecl.Type;
				var typeName = this.GetRemappedTypeName(parmVarDecl, context: null, type, out var nativeTypeName);

				if ((((functionDecl is CXXMethodDecl cxxMethodDecl) && cxxMethodDecl.IsVirtual) || (functionDecl.Body is null)) && (typeName == "bool"))
				{
					// bool is not blittable, so we shouldn't use it for P/Invoke signatures
					typeName = "byte";
					nativeTypeName = string.IsNullOrWhiteSpace(nativeTypeName) ? "bool" : nativeTypeName;
				}

				this.AddNativeTypeNameAttribute(nativeTypeName, prefix: "", postfix: " ");

				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');

				var name = this.GetRemappedCursorName(parmVarDecl);
				var escapedName = this.EscapeName(name);

				this._outputBuilder.Write(escapedName);

				var parameters = functionDecl.Parameters;
				var index = parameters.IndexOf(parmVarDecl);
				var lastIndex = parameters.Count - 1;

				if (name.Equals("param"))
				{
					this._outputBuilder.Write(index);
				}

				if (parmVarDecl.HasDefaultArg)
				{
					this._outputBuilder.Write(" = ");
					this.UncheckStmt(typeName, parmVarDecl.DefaultArg);
				}

				if (index != lastIndex)
				{
					this._outputBuilder.Write(", ");
				}
			}

			void ForTypedefDecl(ParmVarDecl parmVarDecl, TypedefDecl typedefDecl)
			{
				var type = parmVarDecl.Type;
				var typeName = this.GetRemappedTypeName(parmVarDecl, context: null, type, out var nativeTypeName);
				this.AddNativeTypeNameAttribute(nativeTypeName, prefix: "", postfix: " ");

				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');

				var name = this.GetRemappedCursorName(parmVarDecl);
				this._outputBuilder.Write(this.EscapeName(name));

				var parameters = typedefDecl.CursorChildren.OfType<ParmVarDecl>().ToList();
				var index = parameters.IndexOf(parmVarDecl);
				var lastIndex = parameters.Count - 1;

				if (name.Equals("param"))
				{
					this._outputBuilder.Write(index);
				}

				if (parmVarDecl.HasDefaultArg)
				{
					this._outputBuilder.Write(" = ");
					this.UncheckStmt(typeName, parmVarDecl.DefaultArg);
				}

				if (index != lastIndex)
				{
					this._outputBuilder.Write(", ");
				}
			}
		}

		private void VisitRecordDecl(RecordDecl recordDecl)
		{
			var nativeName = this.GetCursorName(recordDecl);
			var name = this.GetRemappedCursorName(recordDecl);
			var escapedName = this.EscapeName(name);

			this.StartUsingOutputBuilder(name, includeTestOutput: true);
			{
				var cxxRecordDecl = recordDecl as CXXRecordDecl;
				var hasVtbl = false;

				if (cxxRecordDecl != null)
				{
					hasVtbl = this.HasVtbl(cxxRecordDecl);
				}

				var size = recordDecl.TypeForDecl.Handle.SizeOf;
				var alignment = recordDecl.TypeForDecl.Handle.AlignOf;

				if (alignment <= 0) alignment = 1;
				if (size <= 0) size = 1;

				var maxAlignm = recordDecl.Fields.Any() ? recordDecl.Fields.Max((fieldDecl) => fieldDecl.Type.Handle.AlignOf) : alignment;

				if ((this._testOutputBuilder != null) && !recordDecl.IsAnonymousStructOrUnion && !(recordDecl.DeclContext is RecordDecl))
				{
					this._testOutputBuilder.WriteIndented("/// <summary>Provides validation of the <see cref=\"");
					this._testOutputBuilder.Write(escapedName);
					this._testOutputBuilder.WriteLine("\" /> struct.</summary>");
					this._testOutputBuilder.WriteIndented("public static unsafe class ");
					this._testOutputBuilder.Write(escapedName);
					this._testOutputBuilder.WriteLine("Tests");
					this._testOutputBuilder.WriteBlockStart();
				}

				this.AddComments(recordDecl.Handle.ParsedComment, notes: new KeyValuePair<string, object>[]
				{
					new("Size", size),
					new("Alignment", alignment),
				}, location: recordDecl.Location);

				if (recordDecl.IsUnion)
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
					this._outputBuilder.WriteIndented($"[StructLayout(LayoutKind.Explicit, Size = {size}, Pack = {alignment})]");
					this._outputBuilder.WriteNewline();
				}
				else
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
					this._outputBuilder.WriteIndented($"[StructLayout(LayoutKind.Explicit, Size = {size}, Pack = {alignment})]");
					this._outputBuilder.WriteNewline();
				}

				if (this.TryGetUuid(recordDecl, out var uuid))
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");

					this._outputBuilder.WriteIndented("[Guid(\"");
					this._outputBuilder.Write(uuid.ToString("D", CultureInfo.InvariantCulture).ToUpperInvariant());
					this._outputBuilder.WriteLine("\")]");

					var iidName = this.GetRemappedName($"IID_{nativeName}", recordDecl, tryRemapOperatorName: false);

					this._uuidsToGenerate.Add(iidName, uuid);

					if (this._testOutputBuilder != null)
					{
						this._testOutputBuilder.AddUsingDirective("System");
						this._testOutputBuilder.AddUsingDirective($"static {this._config.Namespace}.{this._config.MethodClassName}");

						this._testOutputBuilder.WriteIndented("/// <summary>Validates that the <see cref=\"Guid\" /> of the <see cref=\"");
						this._testOutputBuilder.Write(escapedName);
						this._testOutputBuilder.WriteLine("\" /> struct is correct.</summary>");

						this.WithTestAttribute();

						this._testOutputBuilder.WriteIndentedLine("public static void GuidOfTest()");
						this._testOutputBuilder.WriteBlockStart();

						if (this._config.GenerateTestsNUnit)
						{
							this._testOutputBuilder.WriteIndented("Assert.That");
						}
						else if (this._config.GenerateTestsXUnit)
						{
							this._testOutputBuilder.WriteIndented("Assert.Equal");
						}

						this._testOutputBuilder.Write("(typeof(");
						this._testOutputBuilder.Write(escapedName);
						this._testOutputBuilder.Write(").GUID, ");

						if (this._config.GenerateTestsNUnit)
						{
							this._testOutputBuilder.Write("Is.EqualTo(");
						}

						this._testOutputBuilder.Write(iidName);

						if (this._config.GenerateTestsNUnit)
						{
							this._testOutputBuilder.Write(')');
						}

						this._testOutputBuilder.Write(')');
						this._testOutputBuilder.WriteSemicolon();
						this._testOutputBuilder.WriteNewline();
						this._testOutputBuilder.WriteBlockEnd();
						this._testOutputBuilder.NeedsNewline = true;
					}
				}

				if ((cxxRecordDecl != null) && cxxRecordDecl.Bases.Any())
				{
					var nativeTypeNameBuilder = new StringBuilder();

					nativeTypeNameBuilder.Append(recordDecl.IsUnion ? "union " : "struct ");
					nativeTypeNameBuilder.Append(nativeName);
					nativeTypeNameBuilder.Append(" : ");

					var baseName = this.GetCursorName(cxxRecordDecl.Bases[0].Referenced);
					nativeTypeNameBuilder.Append(baseName);

					for (var i = 1; i < cxxRecordDecl.Bases.Count; i++)
					{
						nativeTypeNameBuilder.Append(", ");
						baseName = this.GetCursorName(cxxRecordDecl.Bases[i].Referenced);
						nativeTypeNameBuilder.Append(baseName);
					}

					this.AddNativeTypeNameAttribute(nativeTypeNameBuilder.ToString());
				}

				this._outputBuilder.WriteIndented(this.GetAccessSpecifierName(recordDecl));
				this._outputBuilder.Write(' ');

				if (this.IsUnsafe(recordDecl))
				{
					this._outputBuilder.Write("unsafe ");
				}

				this._outputBuilder.Write("partial struct ");
				this._outputBuilder.Write(escapedName);
				this._outputBuilder.WriteNewline();
				this._outputBuilder.WriteBlockStart();

				if (hasVtbl)
				{
					if (this._config.GenerateExplicitVtbls)
					{
						this._outputBuilder.WriteIndented("public Vtbl* lpVtbl");
					}
					else
					{
						this._outputBuilder.WriteIndented("public void** lpVtbl");
					}

					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
					this._outputBuilder.NeedsNewline = true;
				}

				if (cxxRecordDecl != null)
				{
					foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
					{
						var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);

						if (HasFields(baseCxxRecordDecl))
						{
							this._outputBuilder.WriteIndented(this.GetAccessSpecifierName(baseCxxRecordDecl));
							this._outputBuilder.Write(' ');
							this._outputBuilder.Write(this.GetRemappedCursorName(baseCxxRecordDecl));
							this._outputBuilder.Write(' ');

							var baseFieldName = this.GetAnonymousName(cxxBaseSpecifier, "Base");
							baseFieldName = this.GetRemappedName(baseFieldName, cxxBaseSpecifier, tryRemapOperatorName: true);

							this._outputBuilder.Write(baseFieldName);
							this._outputBuilder.WriteSemicolon();
							this._outputBuilder.WriteNewline();

							this._outputBuilder.NeedsNewline = true;
						}
					}
				}

				if ((this._testOutputBuilder != null) && !recordDecl.IsAnonymousStructOrUnion && !(recordDecl.DeclContext is RecordDecl))
				{
					this._testOutputBuilder.WriteIndented("/// <summary>Validates that the <see cref=\"");
					this._testOutputBuilder.Write(escapedName);
					this._testOutputBuilder.WriteLine("\" /> struct is blittable.</summary>");

					this.WithTestAttribute();

					this._testOutputBuilder.WriteIndentedLine("public static void IsBlittableTest()");
					this._testOutputBuilder.WriteBlockStart();

					this.WithTestAssertEqual($"sizeof({escapedName})", $"Marshal.SizeOf<{escapedName}>()");

					this._testOutputBuilder.WriteBlockEnd();
					this._testOutputBuilder.NeedsNewline = true;

					this._testOutputBuilder.WriteIndented("/// <summary>Validates that the <see cref=\"");
					this._testOutputBuilder.Write(escapedName);
					this._testOutputBuilder.WriteLine("\" /> struct has the right <see cref=\"LayoutKind\" />.</summary>");

					this.WithTestAttribute();

					this._testOutputBuilder.WriteIndented("public static void IsLayout");

					if (recordDecl.IsUnion)
					{
						this._testOutputBuilder.Write("Explicit");
					}
					else
					{
						this._testOutputBuilder.Write("Sequential");
					}

					this._testOutputBuilder.WriteLine("Test()");
					this._testOutputBuilder.WriteBlockStart();

					this.WithTestAssertTrue($"typeof({escapedName}).Is{(recordDecl.IsUnion ? "ExplicitLayout" : "LayoutSequential")}");

					this._testOutputBuilder.WriteBlockEnd();
					this._testOutputBuilder.NeedsNewline = true;

					long alignment32 = -1;
					long alignment64 = -1;

					this.GetTypeSize(recordDecl, recordDecl.TypeForDecl, ref alignment32, ref alignment64, out var size32, out var size64);

					if (((size32 == 0) || (size64 == 0)) && !this.TryGetUuid(recordDecl, out _))
					{
						this.AddDiagnostic(DiagnosticLevel.Info, $"{escapedName} has a size of 0");
					}

					this._testOutputBuilder.WriteIndented("/// <summary>Validates that the <see cref=\"");
					this._testOutputBuilder.Write(escapedName);
					this._testOutputBuilder.WriteLine("\" /> struct has the correct size.</summary>");

					this.WithTestAttribute();

					this._testOutputBuilder.WriteIndentedLine("public static void SizeOfTest()");
					this._testOutputBuilder.WriteBlockStart();

					if (size32 != size64)
					{
						this._testOutputBuilder.AddUsingDirective("System");

						this._testOutputBuilder.WriteIndentedLine("if (Environment.Is64BitProcess)");
						this._testOutputBuilder.WriteBlockStart();

						this.WithTestAssertEqual($"{Math.Max(size64, 1)}", $"sizeof({escapedName})");

						this._testOutputBuilder.WriteBlockEnd();
						this._testOutputBuilder.WriteIndentedLine("else");
						this._testOutputBuilder.WriteBlockStart();
					}

					this.WithTestAssertEqual($"{Math.Max(size32, 1)}", $"sizeof({escapedName})");

					if (size32 != size64)
					{
						this._testOutputBuilder.WriteBlockEnd();
					}

					this._testOutputBuilder.WriteBlockEnd();
				}

				var bitfieldTypes = this.GetBitfieldCount(recordDecl);
				var bitfieldIndex = (bitfieldTypes.Length == 1) ? -1 : 0;

				var bitfieldPreviousSize = 0L;
				var bitfieldRemainingBits = 0L;

				foreach (var declaration in recordDecl.Decls)
				{
					if (declaration is FieldDecl fieldDecl)
					{
						if (fieldDecl.IsBitField)
						{
							VisitBitfieldDecl(fieldDecl, bitfieldTypes, recordDecl, contextName: "", ref bitfieldIndex, ref bitfieldPreviousSize, ref bitfieldRemainingBits);
						}
						else
						{
							bitfieldPreviousSize = 0;
							bitfieldRemainingBits = 0;
						}
						this.Visit(fieldDecl);

						this._outputBuilder.NeedsNewline = true;
					}
					else if ((declaration is RecordDecl nestedRecordDecl) && nestedRecordDecl.IsAnonymousStructOrUnion)
					{
						VisitAnonymousRecordDecl(recordDecl, nestedRecordDecl);
					}
				}

				if (cxxRecordDecl != null)
				{
					foreach (var cxxConstructorDecl in cxxRecordDecl.Ctors)
					{
						this.Visit(cxxConstructorDecl);
						this._outputBuilder.NeedsNewline = true;
					}

					if (cxxRecordDecl.HasUserDeclaredDestructor)
					{
						this.Visit(cxxRecordDecl.Destructor);
						this._outputBuilder.NeedsNewline = true;
					}

					if (hasVtbl)
					{
						OutputDelegateSignatures(cxxRecordDecl, cxxRecordDecl, hitsPerName: new Dictionary<string, int>());
					}
				}

				var excludedCursors = recordDecl.Fields.AsEnumerable<Cursor>();

				if (cxxRecordDecl != null)
				{
					OutputMethods(cxxRecordDecl, cxxRecordDecl);
					excludedCursors = excludedCursors.Concat(cxxRecordDecl.Methods);
				}

				this.Visit(recordDecl.Decls, excludedCursors);

				foreach (var constantArray in recordDecl.Fields.Where((field) => field.Type.CanonicalType is ConstantArrayType))
				{
					VisitConstantArrayFieldDecl(recordDecl, constantArray);
				}

				if (hasVtbl)
				{
					if (!this._config.GenerateCompatibleCode)
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
					}

					if (!this._config.GeneratePreviewCodeFnptr)
					{
						this._outputBuilder.AddUsingDirective("System");
						this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
					}

					var index = 0;
					OutputVtblHelperMethods(cxxRecordDecl, cxxRecordDecl, ref index, hitsPerName: new Dictionary<string, int>());

					if (this._config.GenerateExplicitVtbls)
					{
						this._outputBuilder.NeedsNewline = true;
						this._outputBuilder.WriteIndentedLine("public partial struct Vtbl");
						this._outputBuilder.WriteBlockStart();
						OutputVtblEntries(cxxRecordDecl, cxxRecordDecl, hitsPerName: new Dictionary<string, int>());
						this._outputBuilder.WriteBlockEnd();
					}
				}

				this._outputBuilder.WriteBlockEnd();

				if ((this._testOutputBuilder != null) && !recordDecl.IsAnonymousStructOrUnion && !(recordDecl.DeclContext is RecordDecl))
				{
					this._testOutputBuilder.WriteBlockEnd();
				}
			}
			this.StopUsingOutputBuilder();

			string FixupNameForMultipleHits(CXXMethodDecl cxxMethodDecl, Dictionary<string, int> hitsPerName)
			{
				var remappedName = this.GetRemappedCursorName(cxxMethodDecl);

				if (hitsPerName.TryGetValue(remappedName, out var hits))
				{
					hitsPerName[remappedName] = (hits + 1);

					var name = this.GetCursorName(cxxMethodDecl);
					var remappedNames = (Dictionary<string, string>)this._config.RemappedNames;

					remappedNames[name] = $"{remappedName}{hits}";
				}
				else
				{
					hitsPerName.Add(remappedName, 1);
				}

				return remappedName;
			}

			bool HasFields(RecordDecl recordDecl)
			{
				if (recordDecl.Fields.Count != 0)
				{
					return true;
				}

				foreach (var decl in recordDecl.Decls)
				{
					if ((decl is RecordDecl nestedRecordDecl) && nestedRecordDecl.IsAnonymousStructOrUnion && HasFields(nestedRecordDecl))
					{
						return true;
					}
				}

				if (recordDecl is CXXRecordDecl cxxRecordDecl)
				{
					foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
					{
						var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);

						if (HasFields(baseCxxRecordDecl))
						{
							return true;
						}
					}
				}

				return false;
			}

			void OutputDelegateSignatures(CXXRecordDecl rootCxxRecordDecl, CXXRecordDecl cxxRecordDecl, Dictionary<string, int> hitsPerName)
			{
				foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
				{
					var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);
					OutputDelegateSignatures(rootCxxRecordDecl, baseCxxRecordDecl, hitsPerName);
				}

				foreach (var cxxMethodDecl in cxxRecordDecl.Methods)
				{
					if (!cxxMethodDecl.IsVirtual || this.IsExcluded(cxxMethodDecl))
					{
						continue;
					}

					if (!this._config.GeneratePreviewCodeFnptr)
					{
						this._outputBuilder.NeedsNewline = true;

						var remappedName = FixupNameForMultipleHits(cxxMethodDecl, hitsPerName);
						Debug.Assert(this.CurrentContext == rootCxxRecordDecl);
						this.Visit(cxxMethodDecl);
						RestoreNameForMultipleHits(cxxMethodDecl, hitsPerName, remappedName);
					}
				}
			}

			void OutputMethods(CXXRecordDecl rootCxxRecordDecl, CXXRecordDecl cxxRecordDecl)
			{
				foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
				{
					var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);
					OutputMethods(rootCxxRecordDecl, baseCxxRecordDecl);
				}

				foreach (var cxxMethodDecl in cxxRecordDecl.Methods)
				{
					if (cxxMethodDecl.IsVirtual || (cxxMethodDecl is CXXConstructorDecl) || (cxxMethodDecl is CXXDestructorDecl))
					{
						continue;
					}

					Debug.Assert(this.CurrentContext == rootCxxRecordDecl);
					this.Visit(cxxMethodDecl);
					this._outputBuilder.NeedsNewline = true;
				}
			}

			void OutputVtblEntries(CXXRecordDecl rootCxxRecordDecl, CXXRecordDecl cxxRecordDecl, Dictionary<string, int> hitsPerName)
			{
				foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
				{
					var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);
					OutputVtblEntries(rootCxxRecordDecl, baseCxxRecordDecl, hitsPerName);
				}

				var cxxMethodDecls = cxxRecordDecl.Methods;

				if (cxxMethodDecls.Count != 0)
				{
					foreach (var cxxMethodDecl in cxxMethodDecls)
					{
						OutputVtblEntry(rootCxxRecordDecl, cxxMethodDecl, hitsPerName);
						this._outputBuilder.NeedsNewline = true;
					}
				}
			}

			void OutputVtblEntry(CXXRecordDecl cxxRecordDecl, CXXMethodDecl cxxMethodDecl, Dictionary<string, int> hitsPerName)
			{
				if (!cxxMethodDecl.IsVirtual || this.IsExcluded(cxxMethodDecl))
				{
					return;
				}

				var cxxMethodDeclTypeName = this.GetRemappedTypeName(cxxMethodDecl, cxxRecordDecl, cxxMethodDecl.Type, out var nativeTypeName);
				this.AddNativeTypeNameAttribute(nativeTypeName);

				var accessSpecifier = this.GetAccessSpecifierName(cxxMethodDecl);
				var remappedName = FixupNameForMultipleHits(cxxMethodDecl, hitsPerName);
				var name = this.GetRemappedCursorName(cxxMethodDecl);
				RestoreNameForMultipleHits(cxxMethodDecl, hitsPerName, remappedName);

				this._outputBuilder.WriteIndented(accessSpecifier);
				this._outputBuilder.Write(' ');

				if (this.NeedsNewKeyword(remappedName))
				{
					this._outputBuilder.Write("new ");
				}

				this._outputBuilder.Write(cxxMethodDeclTypeName);
				this._outputBuilder.Write(' ');

				this._outputBuilder.Write(this.EscapeAndStripName(name));

				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();
			}

			void OutputVtblHelperMethod(CXXRecordDecl cxxRecordDecl, CXXMethodDecl cxxMethodDecl, ref int vtblIndex, Dictionary<string, int> hitsPerName)
			{
				if (!cxxMethodDecl.IsVirtual)
				{
					return;
				}

				if (this.IsExcluded(cxxMethodDecl, out var isExcludedByConflictingDefinition))
				{
					if (!isExcludedByConflictingDefinition)
					{
						vtblIndex += 1;
					}
					return;
				}

				var currentContext = this._context.AddLast(cxxMethodDecl);

				if (this._config.GenerateAggressiveInlining)
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
					this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
				}

				var accessSpecifier = this.GetAccessSpecifierName(cxxMethodDecl);
				var returnType = cxxMethodDecl.ReturnType;
				var returnTypeName = this.GetRemappedTypeName(cxxMethodDecl, cxxRecordDecl, returnType, out var nativeTypeName);
				this.AddNativeTypeNameAttribute(nativeTypeName, attributePrefix: "return: ");

				this._outputBuilder.WriteIndented(accessSpecifier);
				this._outputBuilder.Write(' ');

				var remappedName = FixupNameForMultipleHits(cxxMethodDecl, hitsPerName);
				var name = this.GetRemappedCursorName(cxxMethodDecl);
				RestoreNameForMultipleHits(cxxMethodDecl, hitsPerName, remappedName);

				if (this.NeedsNewKeyword(remappedName, cxxMethodDecl.Parameters))
				{
					this._outputBuilder.Write("new");
					this._outputBuilder.Write(' ');
				}

				this._outputBuilder.Write(returnTypeName);
				this._outputBuilder.Write(' ');
				this._outputBuilder.Write(this.EscapeAndStripName(remappedName));
				this._outputBuilder.Write('(');

				this.Visit(cxxMethodDecl.Parameters);

				this._outputBuilder.WriteLine(')');
				this._outputBuilder.WriteBlockStart();

				var needsReturnFixup = false;
				var cxxRecordDeclName = this.GetRemappedCursorName(cxxRecordDecl);
				var escapedCXXRecordDeclName = this.EscapeName(cxxRecordDeclName);

				this._outputBuilder.WriteIndentation();

				if (this._config.GenerateCompatibleCode)
				{
					this._outputBuilder.Write("fixed (");
					this._outputBuilder.Write(escapedCXXRecordDeclName);
					this._outputBuilder.WriteLine("* pThis = &this)");
					this._outputBuilder.WriteBlockStart();
					this._outputBuilder.WriteIndentation();
				}

				if (returnType.Kind != CXTypeKind.CXType_Void)
				{
					needsReturnFixup = this.NeedsReturnFixup(cxxMethodDecl);

					if (needsReturnFixup)
					{
						this._outputBuilder.Write(returnTypeName);
						this._outputBuilder.Write(" result");
						this._outputBuilder.WriteSemicolon();
						this._outputBuilder.WriteNewline();
						this._outputBuilder.WriteIndentation();
					}

					this._outputBuilder.Write("return ");
				}

				if (needsReturnFixup)
				{
					this._outputBuilder.Write('*');
				}

				if (!this._config.GeneratePreviewCodeFnptr)
				{
					this._outputBuilder.Write("Marshal.GetDelegateForFunctionPointer<");
					this._outputBuilder.Write(this.PrefixAndStripName(name));
					this._outputBuilder.Write(">(");
				}

				if (this._config.GenerateExplicitVtbls)
				{
					this._outputBuilder.Write("lpVtbl->");
					this._outputBuilder.Write(this.EscapeAndStripName(name));
				}
				else
				{
					var cxxMethodDeclTypeName = this.GetRemappedTypeName(cxxMethodDecl, cxxRecordDecl, cxxMethodDecl.Type, out var _);

					if (this._config.GeneratePreviewCodeFnptr)
					{
						this._outputBuilder.Write('(');
					}

					this._outputBuilder.Write('(');
					this._outputBuilder.Write(cxxMethodDeclTypeName);
					this._outputBuilder.Write(")(lpVtbl[");
					this._outputBuilder.Write(vtblIndex);
					this._outputBuilder.Write("])");

					if (this._config.GeneratePreviewCodeFnptr)
					{
						this._outputBuilder.Write(')');
					}
				}

				if (!this._config.GeneratePreviewCodeFnptr)
				{
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.Write('(');

				if (this._config.GenerateCompatibleCode)
				{
					this._outputBuilder.Write("pThis");
				}
				else
				{
					this._outputBuilder.Write('(');
					this._outputBuilder.Write(escapedCXXRecordDeclName);
					this._outputBuilder.Write("*)Unsafe.AsPointer(ref this)");
				}

				if (needsReturnFixup)
				{
					this._outputBuilder.Write(", &result");
				}

				var parmVarDecls = cxxMethodDecl.Parameters;

				for (var index = 0; index < parmVarDecls.Count; index++)
				{
					this._outputBuilder.Write(", ");

					var parmVarDeclName = this.GetRemappedCursorName(parmVarDecls[index]);
					var escapedParmVarDeclName = this.EscapeName(parmVarDeclName);
					this._outputBuilder.Write(escapedParmVarDeclName);

					if (parmVarDeclName.Equals("param"))
					{
						this._outputBuilder.Write(index);
					}
				}

				this._outputBuilder.Write(')');

				if (returnTypeName == "bool")
				{
					this._outputBuilder.Write(" != 0");
				}

				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();

				if (this._config.GenerateCompatibleCode)
				{
					this._outputBuilder.WriteBlockEnd();
				}

				this._outputBuilder.WriteBlockEnd();
				vtblIndex += 1;

				Debug.Assert(this._context.Last == currentContext);
				this._context.RemoveLast();
			}

			void OutputVtblHelperMethods(CXXRecordDecl rootCxxRecordDecl, CXXRecordDecl cxxRecordDecl, ref int index, Dictionary<string, int> hitsPerName)
			{
				foreach (var cxxBaseSpecifier in cxxRecordDecl.Bases)
				{
					var baseCxxRecordDecl = GetRecordDeclForBaseSpecifier(cxxBaseSpecifier);
					OutputVtblHelperMethods(rootCxxRecordDecl, baseCxxRecordDecl, ref index, hitsPerName);
				}

				var cxxMethodDecls = cxxRecordDecl.Methods;
				var outputBuilder = this._outputBuilder;

				foreach (var cxxMethodDecl in cxxMethodDecls)
				{
					this._outputBuilder.NeedsNewline = true;
					OutputVtblHelperMethod(rootCxxRecordDecl, cxxMethodDecl, ref index, hitsPerName);
				}
			}

			void RestoreNameForMultipleHits(CXXMethodDecl cxxMethodDecl, Dictionary<string, int> hitsPerName, string remappedName)
			{
				if (hitsPerName[remappedName] != 1)
				{
					var name = this.GetCursorName(cxxMethodDecl);
					var remappedNames = (Dictionary<string, string>)this._config.RemappedNames;

					if (name.Equals(remappedName))
					{
						remappedNames.Remove(name);
					}
					else
					{
						remappedNames[name] = remappedName;
					}
				}
			}

			void VisitAnonymousRecordDecl(RecordDecl recordDecl, RecordDecl nestedRecordDecl)
			{
				var nestedRecordDeclFieldName = this.GetRemappedCursorName(nestedRecordDecl);

				if (nestedRecordDeclFieldName.StartsWith("_"))
				{
					var suffixLength = 0;

					if (nestedRecordDeclFieldName.EndsWith("_e__Union"))
					{
						suffixLength = 10;
					}
					else if (nestedRecordDeclFieldName.EndsWith("_e__Struct"))
					{
						suffixLength = 11;
					}

					if (suffixLength != 0)
					{
						nestedRecordDeclFieldName = nestedRecordDeclFieldName.Substring(1, nestedRecordDeclFieldName.Length - suffixLength);
					}
				}

				var nestedRecordDeclName = this.GetRemappedTypeName(nestedRecordDecl, context: null, nestedRecordDecl.TypeForDecl, out var nativeTypeName);

				if (recordDecl.IsUnion)
				{
					this._outputBuilder.WriteIndentedLine("[FieldOffset(0)]");
				}
				this.AddNativeTypeNameAttribute(nativeTypeName);

				this._outputBuilder.WriteIndented("public ");
				this._outputBuilder.Write(nestedRecordDeclName);
				this._outputBuilder.Write(' ');
				this._outputBuilder.Write(nestedRecordDeclFieldName);
				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();
				this._outputBuilder.NeedsNewline = true;

				if (!recordDecl.IsAnonymousStructOrUnion)
				{
					VisitAnonymousRecordDeclFields(recordDecl, nestedRecordDecl, nestedRecordDeclName, nestedRecordDeclFieldName);
				}
			}

			void VisitAnonymousRecordDeclFields(RecordDecl rootRecordDecl, RecordDecl anonymousRecordDecl, string contextType, string contextName)
			{
				foreach (var declaration in anonymousRecordDecl.Decls)
				{
					if (declaration is FieldDecl fieldDecl)
					{
						var type = fieldDecl.Type;

						var accessSpecifier = this.GetAccessSpecifierName(anonymousRecordDecl);
						var typeName = this.GetRemappedTypeName(fieldDecl, context: null, type, out var fieldNativeTypeName);
						var name = this.GetRemappedCursorName(fieldDecl);
						var escapedName = this.EscapeName(name);

						this._outputBuilder.WriteIndented(accessSpecifier);
						this._outputBuilder.Write(' ');

						var isFixedSizedBuffer = (type.CanonicalType is ConstantArrayType);
						var generateCompatibleCode = this._config.GenerateCompatibleCode;

						if (!fieldDecl.IsBitField && (!isFixedSizedBuffer || generateCompatibleCode))
						{
							this._outputBuilder.Write("ref ");
						}

						if (type.CanonicalType is RecordType recordType)
						{
							var recordDecl = recordType.Decl;

							while ((recordDecl.DeclContext is RecordDecl parentRecordDecl) && (parentRecordDecl != rootRecordDecl))
							{
								var parentRecordDeclName = this.GetRemappedCursorName(parentRecordDecl);
								var escapedParentRecordDeclName = this.EscapeName(parentRecordDeclName);

								this._outputBuilder.Write(escapedParentRecordDeclName);
								this._outputBuilder.Write('.');

								recordDecl = parentRecordDecl;
							}
						}

						var isSupportedFixedSizedBufferType = isFixedSizedBuffer && this.IsSupportedFixedSizedBufferType(typeName);

						if (isFixedSizedBuffer)
						{
							if (!generateCompatibleCode)
							{
								this._outputBuilder.AddUsingDirective("System");
								this._outputBuilder.Write("Span<");
							}
							else if (!isSupportedFixedSizedBufferType)
							{
								this._outputBuilder.Write(contextType);
								this._outputBuilder.Write('.');
								typeName = this.GetArtificialFixedSizedBufferName(fieldDecl);
							}
						}

						this._outputBuilder.Write(typeName);

						if (isFixedSizedBuffer && !generateCompatibleCode)
						{
							this._outputBuilder.Write('>');
						}

						this._outputBuilder.Write(' ');
						this._outputBuilder.Write(escapedName);

						generateCompatibleCode |= ((type.CanonicalType is PointerType) || (type.CanonicalType is ReferenceType)) && ((typeName != "IntPtr") && (typeName != "UIntPtr"));

						this._outputBuilder.WriteNewline();
						this._outputBuilder.WriteBlockStart();

						if (this._config.GenerateAggressiveInlining)
						{
							this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
							this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
						}

						this._outputBuilder.WriteIndentedLine("get");
						this._outputBuilder.WriteBlockStart();

						if (fieldDecl.IsBitField)
						{
							this._outputBuilder.WriteIndented("return ");
							this._outputBuilder.Write(contextName);
							this._outputBuilder.Write('.');
							this._outputBuilder.Write(escapedName);
							this._outputBuilder.WriteSemicolon();
							this._outputBuilder.WriteNewline();
							this._outputBuilder.WriteBlockEnd();

							this._outputBuilder.WriteNewline();

							if (this._config.GenerateAggressiveInlining)
							{
								this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
								this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
							}

							this._outputBuilder.WriteIndentedLine("set");
							this._outputBuilder.WriteBlockStart();
							this._outputBuilder.WriteIndented(contextName);
							this._outputBuilder.Write('.');
							this._outputBuilder.Write(escapedName);
							this._outputBuilder.Write(" = value");
							this._outputBuilder.WriteSemicolon();
							this._outputBuilder.WriteNewline();
						}
						else if (generateCompatibleCode)
						{
							this._outputBuilder.WriteIndented("fixed (");
							this._outputBuilder.Write(contextType);
							this._outputBuilder.Write("* pField = &");
							this._outputBuilder.Write(contextName);
							this._outputBuilder.WriteLine(')');
							this._outputBuilder.WriteBlockStart();
							this._outputBuilder.WriteIndented("return ref pField->");
							this._outputBuilder.Write(escapedName);

							if (isSupportedFixedSizedBufferType)
							{
								this._outputBuilder.Write("[0]");
							}

							this._outputBuilder.WriteSemicolon();
							this._outputBuilder.WriteNewline();
							this._outputBuilder.WriteBlockEnd();
						}
						else
						{
							this._outputBuilder.WriteIndented("return ");

							if (!isFixedSizedBuffer)
							{
								this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
								this._outputBuilder.Write("ref MemoryMarshal.GetReference(");
							}

							if (!isFixedSizedBuffer || isSupportedFixedSizedBufferType)
							{
								this._outputBuilder.Write("MemoryMarshal.CreateSpan(ref ");
							}

							this._outputBuilder.Write(contextName);
							this._outputBuilder.Write('.');
							this._outputBuilder.Write(escapedName);

							if (isFixedSizedBuffer)
							{
								if (isSupportedFixedSizedBufferType)
								{
									this._outputBuilder.Write("[0], ");
									this._outputBuilder.Write(((ConstantArrayType)type.CanonicalType).Size);
								}
								else
								{
									this._outputBuilder.Write(".AsSpan(");
								}
							}
							else
							{
								this._outputBuilder.Write(", 1)");
							}

							this._outputBuilder.Write(')');
							this._outputBuilder.WriteSemicolon();
							this._outputBuilder.WriteNewline();
						}

						this._outputBuilder.WriteBlockEnd();
						this._outputBuilder.WriteBlockEnd();

						this._outputBuilder.NeedsNewline = true;
					}
					else if ((declaration is RecordDecl nestedRecordDecl) && nestedRecordDecl.IsAnonymousStructOrUnion)
					{
						var nestedRecordDeclName = this.GetRemappedTypeName(nestedRecordDecl, context: null, nestedRecordDecl.TypeForDecl, out var nativeTypeName);
						var name = this.GetRemappedCursorName(nestedRecordDecl);

						if (name.StartsWith("_"))
						{
							var suffixLength = 0;

							if (name.EndsWith("_e__Union"))
							{
								suffixLength = 10;
							}
							else if (name.EndsWith("_e__Struct"))
							{
								suffixLength = 11;
							}

							if (suffixLength != 0)
							{
								name = name.Substring(1, name.Length - suffixLength);
							}
						}
						var escapedName = this.EscapeName(name);

						VisitAnonymousRecordDeclFields(rootRecordDecl, nestedRecordDecl, $"{contextType}.{nestedRecordDeclName}", $"{contextName}.{escapedName}");
					}
				}
			}

			void VisitBitfieldDecl(FieldDecl fieldDecl, Type[] types, RecordDecl recordDecl, string contextName, ref int index, ref long previousSize, ref long remainingBits)
			{
				Debug.Assert(fieldDecl.IsBitField);

				var outputBuilder = this._outputBuilder;

				var type = fieldDecl.Type;
				var typeName = this.GetRemappedTypeName(fieldDecl, context: null, type, out var nativeTypeName);

				if (string.IsNullOrWhiteSpace(nativeTypeName))
				{
					nativeTypeName = typeName;
				}
				nativeTypeName += $" : {fieldDecl.BitWidthValue}";

				if (fieldDecl.Parent.IsUnion)
				{
					this._outputBuilder.WriteIndentedLine("[FieldOffset(0)]");
				}
				var currentSize = fieldDecl.Type.Handle.SizeOf;

				var bitfieldName = "_bitfield";

				Type typeBacking;
				string typeNameBacking;

				if ((!this._config.GenerateUnixTypes && (currentSize != previousSize)) || (fieldDecl.BitWidthValue > remainingBits))
				{
					if (index >= 0)
					{
						index++;
						bitfieldName += index.ToString();
					}

					remainingBits = currentSize * 8;
					previousSize = 0;

					typeBacking = (index > 0) ? types[index - 1] : types[0];
					typeNameBacking = this.GetRemappedTypeName(fieldDecl, context: null, typeBacking, out _);

					if (fieldDecl.Parent == recordDecl)
					{
						this._outputBuilder.WriteIndented("public ");
						this._outputBuilder.Write(typeNameBacking);
						this._outputBuilder.Write(' ');
						this._outputBuilder.Write(bitfieldName);
						this._outputBuilder.WriteSemicolon();
						this._outputBuilder.WriteNewline();
						this._outputBuilder.NeedsNewline = true;
					}
				}
				else
				{
					currentSize = Math.Max(previousSize, currentSize);

					if (this._config.GenerateUnixTypes && (currentSize > previousSize))
					{
						remainingBits += (currentSize - previousSize) * 8;
					}

					if (index >= 0)
					{
						bitfieldName += index.ToString();
					}

					typeBacking = (index > 0) ? types[index - 1] : types[0];
					typeNameBacking = this.GetRemappedTypeName(fieldDecl, context: null, typeBacking, out _);
				}

				this.AddNativeTypeNameAttribute(nativeTypeName);

				var bitfieldOffset = (currentSize * 8) - remainingBits;

				var bitwidthHexStringBacking = ((1 << fieldDecl.BitWidthValue) - 1).ToString("X");
				var canonicalTypeBacking = typeBacking.CanonicalType;

				switch (canonicalTypeBacking.Kind)
				{
					case CXTypeKind.CXType_Char_U:
					case CXTypeKind.CXType_UChar:
					case CXTypeKind.CXType_UShort:
					case CXTypeKind.CXType_UInt:
					{
						bitwidthHexStringBacking += "u";
						break;
					}

					case CXTypeKind.CXType_ULong:
					{
						if (this._config.GenerateUnixTypes)
						{
							goto default;
						}
						goto case CXTypeKind.CXType_UInt;
					}

					case CXTypeKind.CXType_ULongLong:
					{
						bitwidthHexStringBacking += "UL";
						break;
					}

					case CXTypeKind.CXType_Char_S:
					case CXTypeKind.CXType_SChar:
					case CXTypeKind.CXType_Short:
					case CXTypeKind.CXType_Int:
					{
						break;
					}

					case CXTypeKind.CXType_Long:
					{
						if (this._config.GenerateUnixTypes)
						{
							goto default;
						}
						goto case CXTypeKind.CXType_Int;
					}

					case CXTypeKind.CXType_LongLong:
					{
						bitwidthHexStringBacking += "L";
						break;
					}

					default:
					{
						this.AddDiagnostic(DiagnosticLevel.Warning, $"Unsupported bitfield type: '{canonicalTypeBacking.TypeClass}'. Generated bindings may be incomplete.", fieldDecl);
						break;
					}
				}

				var bitwidthHexString = ((1 << fieldDecl.BitWidthValue) - 1).ToString("X");

				var canonicalType = type.CanonicalType;

				if (canonicalType is EnumType enumType)
				{
					canonicalType = enumType.Decl.IntegerType.CanonicalType;
				}

				switch (canonicalType.Kind)
				{
					case CXTypeKind.CXType_Char_U:
					case CXTypeKind.CXType_UChar:
					case CXTypeKind.CXType_UShort:
					case CXTypeKind.CXType_UInt:
					{
						bitwidthHexString += "u";
						break;
					}

					case CXTypeKind.CXType_ULong:
					{
						if (this._config.GenerateUnixTypes)
						{
							goto default;
						}
						goto case CXTypeKind.CXType_UInt;
					}

					case CXTypeKind.CXType_ULongLong:
					{
						bitwidthHexString += "UL";
						break;
					}

					case CXTypeKind.CXType_Char_S:
					case CXTypeKind.CXType_SChar:
					case CXTypeKind.CXType_Short:
					case CXTypeKind.CXType_Int:
					{
						break;
					}

					case CXTypeKind.CXType_Long:
					{
						if (this._config.GenerateUnixTypes)
						{
							goto default;
						}
						goto case CXTypeKind.CXType_Int;
					}

					case CXTypeKind.CXType_LongLong:
					{
						bitwidthHexString += "L";
						break;
					}

					default:
					{
						this.AddDiagnostic(DiagnosticLevel.Warning, $"Unsupported bitfield type: '{canonicalType.TypeClass}'. Generated bindings may be incomplete.", fieldDecl);
						break;
					}
				}

				canonicalType = type.CanonicalType;

				var accessSpecifier = this.GetAccessSpecifierName(fieldDecl);
				var name = this.GetRemappedCursorName(fieldDecl);
				var escapedName = this.EscapeName(name);

				this._outputBuilder.WriteIndented(accessSpecifier);
				this._outputBuilder.Write(' ');
				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');
				this._outputBuilder.WriteLine(escapedName);
				this._outputBuilder.WriteBlockStart();

				if (this._config.GenerateAggressiveInlining)
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
					this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
				}

				this._outputBuilder.WriteIndentedLine("get");
				this._outputBuilder.WriteBlockStart();
				this._outputBuilder.WriteIndented("return ");

				if ((currentSize < 4) || (canonicalTypeBacking != canonicalType))
				{
					this._outputBuilder.Write('(');
					this._outputBuilder.Write(typeName);
					this._outputBuilder.Write(")(");
				}

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write('(');
				}

				if (!string.IsNullOrWhiteSpace(contextName))
				{
					this._outputBuilder.Write(contextName);
					this._outputBuilder.Write('.');
				}
				this._outputBuilder.Write(bitfieldName);

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write(" >> ");
					this._outputBuilder.Write(bitfieldOffset);
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.Write(" & 0x");
				this._outputBuilder.Write(bitwidthHexStringBacking);

				if ((currentSize < 4) || (canonicalTypeBacking != canonicalType))
				{
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();
				this._outputBuilder.WriteBlockEnd();

				this._outputBuilder.NeedsNewline = true;

				if (this._config.GenerateAggressiveInlining)
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
					this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
				}

				this._outputBuilder.WriteIndentedLine("set");
				this._outputBuilder.WriteBlockStart();
				this._outputBuilder.WriteIndentation();

				if (!string.IsNullOrWhiteSpace(contextName))
				{
					this._outputBuilder.Write(contextName);
					this._outputBuilder.Write('.');
				}
				this._outputBuilder.Write(bitfieldName);

				this._outputBuilder.Write(" = ");

				if (currentSize < 4)
				{
					this._outputBuilder.Write('(');
					this._outputBuilder.Write(typeNameBacking);
					this._outputBuilder.Write(")(");
				}

				this._outputBuilder.Write('(');

				if (!string.IsNullOrWhiteSpace(contextName))
				{
					this._outputBuilder.Write(contextName);
					this._outputBuilder.Write('.');
				}
				this._outputBuilder.Write(bitfieldName);

				this._outputBuilder.Write(" & ~");

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write('(');
				}

				this._outputBuilder.Write("0x");
				this._outputBuilder.Write(bitwidthHexStringBacking);

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write(" << ");
					this._outputBuilder.Write(bitfieldOffset);
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.Write(") | ");

				if ((canonicalTypeBacking != canonicalType) && !(canonicalType is EnumType))
				{
					this._outputBuilder.Write('(');
					this._outputBuilder.Write(typeNameBacking);
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.Write('(');

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write('(');
				}

				if (canonicalType is EnumType)
				{
					this._outputBuilder.Write('(');
					this._outputBuilder.Write(typeNameBacking);
					this._outputBuilder.Write(")(value)");
				}
				else
				{
					this._outputBuilder.Write("value");
				}

				this._outputBuilder.Write(" & 0x");
				this._outputBuilder.Write(bitwidthHexString);

				if (bitfieldOffset != 0)
				{
					this._outputBuilder.Write(") << ");
					this._outputBuilder.Write(bitfieldOffset);
				}

				this._outputBuilder.Write(')');

				if (currentSize < 4)
				{
					this._outputBuilder.Write(')');
				}

				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();
				this._outputBuilder.WriteBlockEnd();
				this._outputBuilder.WriteBlockEnd();

				remainingBits -= fieldDecl.BitWidthValue;
				previousSize = Math.Max(previousSize, currentSize);
			}

			void VisitConstantArrayFieldDecl(RecordDecl recordDecl, FieldDecl constantArray)
			{
				Debug.Assert(constantArray.Type.CanonicalType is ConstantArrayType);

				var outputBuilder = this._outputBuilder;
				var type = (ConstantArrayType)constantArray.Type.CanonicalType;
				var typeName = this.GetRemappedTypeName(constantArray, context: null, constantArray.Type, out _);

				if (this.IsSupportedFixedSizedBufferType(typeName))
				{
					return;
				}

				this._outputBuilder.NeedsNewline = true;

				var alignment = recordDecl.TypeForDecl.Handle.AlignOf;
				var maxAlignm = recordDecl.Fields.Max((fieldDecl) => fieldDecl.Type.Handle.AlignOf);

				this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
				this._outputBuilder.WriteIndented("[StructLayout(LayoutKind.Sequential");
				this._outputBuilder.Write(", Pack = ");
				this._outputBuilder.Write(alignment);
				this._outputBuilder.WriteLine(")]");

				var accessSpecifier = this.GetAccessSpecifierName(constantArray);
				var canonicalElementType = type.ElementType.CanonicalType;
				var isUnsafeElementType = ((canonicalElementType is PointerType) || (canonicalElementType is ReferenceType)) && ((typeName != "IntPtr") && (typeName != "UIntPtr"));

				this._outputBuilder.WriteIndented(accessSpecifier);
				this._outputBuilder.Write(' ');

				if (isUnsafeElementType)
				{
					this._outputBuilder.Write("unsafe ");
				}

				var name = this.GetArtificialFixedSizedBufferName(constantArray);
				var escapedName = this.EscapeName(name);

				this._outputBuilder.Write("partial struct ");
				this._outputBuilder.WriteLine(escapedName);
				this._outputBuilder.WriteBlockStart();

				var totalSize = Math.Max(type.Size, 1);
				var sizePerDimension = new List<(long index, long size)>() {
					(0, type.Size)
				};

				var elementType = type.ElementType;

				while (elementType.CanonicalType is ConstantArrayType subConstantArrayType)
				{
					totalSize *= Math.Max(subConstantArrayType.Size, 1);
					sizePerDimension.Add((0, Math.Max(subConstantArrayType.Size, 1)));
					elementType = subConstantArrayType.ElementType;
				}

				for (long i = 0; i < totalSize; i++)
				{
					this._outputBuilder.WriteIndented("public ");
					this._outputBuilder.Write(typeName);
					this._outputBuilder.Write(" e");

					var dimension = sizePerDimension[0];
					this._outputBuilder.Write(dimension.index++);
					sizePerDimension[0] = dimension;

					for (var d = 1; d < sizePerDimension.Count; d++)
					{
						dimension = sizePerDimension[d];
						this._outputBuilder.Write('_');
						this._outputBuilder.Write(dimension.index);
						sizePerDimension[d] = dimension;

						var previousDimension = sizePerDimension[d - 1];

						if (previousDimension.index == previousDimension.size)
						{
							previousDimension.index = 0;
							dimension.index++;
							sizePerDimension[d - 1] = previousDimension;
							this._outputBuilder.NeedsNewline = true;
						}

						sizePerDimension[d] = dimension;
					}

					if (this._outputBuilder.NeedsNewline)
					{
						this._outputBuilder.WriteSemicolon();
						this._outputBuilder.WriteNewline();
						this._outputBuilder.NeedsNewline = true;
					}
					else
					{
						this._outputBuilder.WriteSemicolon();
						this._outputBuilder.WriteNewline();
					}
				}

				this._outputBuilder.NeedsNewline = true;
				this._outputBuilder.WriteIndented("public ");

				var generateCompatibleCode = this._config.GenerateCompatibleCode;

				if (generateCompatibleCode && !isUnsafeElementType)
				{
					this._outputBuilder.Write("unsafe ");
				}
				else if (!isUnsafeElementType)
				{
					this._outputBuilder.AddUsingDirective("System");
					this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");
				}

				this._outputBuilder.Write("ref ");
				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write(' ');

				if (generateCompatibleCode || isUnsafeElementType)
				{
					this._outputBuilder.WriteLine("this[int index]");
					this._outputBuilder.WriteBlockStart();

					if (this._config.GenerateAggressiveInlining)
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
						this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
					}

					this._outputBuilder.WriteIndentedLine("get");
					this._outputBuilder.WriteBlockStart();
					this._outputBuilder.WriteIndented("fixed (");
					this._outputBuilder.Write(typeName);
					this._outputBuilder.WriteLine("* pThis = &e0)");
					this._outputBuilder.WriteBlockStart();
					this._outputBuilder.WriteIndented("return ref pThis[index]");
					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
					this._outputBuilder.WriteBlockEnd();
					this._outputBuilder.WriteBlockEnd();
					this._outputBuilder.WriteBlockEnd();
				}
				else
				{
					this._outputBuilder.WriteLine("this[int index]");
					this._outputBuilder.WriteBlockStart();

					if (this._config.GenerateAggressiveInlining)
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
						this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
					}

					this._outputBuilder.WriteIndentedLine("get");
					this._outputBuilder.WriteBlockStart();
					//_outputBuilder.WriteIndented("return ref AsSpan(");
					this._outputBuilder.WriteIndented($"return ref (({typeName}*)Unsafe.AsPointer(ref this))[index]");

					//if (type.Size == 1)
					//{
					//    _outputBuilder.Write("int.MaxValue");
					//}

					//_outputBuilder.Write("[index]");
					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
					this._outputBuilder.WriteBlockEnd();
					this._outputBuilder.WriteBlockEnd();

					this._outputBuilder.NeedsNewline = true;

					if (this._config.GenerateAggressiveInlining)
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
						this._outputBuilder.WriteIndentedLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
					}

					this._outputBuilder.WriteIndented("public Span<");
					this._outputBuilder.Write(typeName);
					this._outputBuilder.Write("> AsSpan(");

					if (type.Size == 1)
					{
						this._outputBuilder.Write("int length");
					}

					this._outputBuilder.Write(") => MemoryMarshal.CreateSpan(ref e0, ");

					if (type.Size == 1)
					{
						this._outputBuilder.Write("length");
					}
					else
					{
						this._outputBuilder.Write(totalSize);
					}

					this._outputBuilder.Write(')');
					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
				}

				this._outputBuilder.WriteBlockEnd();
			}
		}

		private void VisitTranslationUnitDecl(TranslationUnitDecl translationUnitDecl)
		{
			foreach (var cursor in translationUnitDecl.CursorChildren)
			{
				this.Visit(cursor);
			}
		}

		private void VisitTypedefDecl(TypedefDecl typedefDecl)
		{
			ForUnderlyingType(typedefDecl, typedefDecl.UnderlyingType);

			void ForFunctionProtoType(TypedefDecl typedefDecl, FunctionProtoType functionProtoType, Type parentType)
			{
				if (this._config.GeneratePreviewCodeFnptr)
				{
					return;
				}

				var name = this.GetRemappedCursorName(typedefDecl);
				var escapedName = this.EscapeName(name);

				this.StartUsingOutputBuilder(name);
				{
					this._outputBuilder.AddUsingDirective("System.Runtime.InteropServices");

					var callingConventionName = this.GetCallingConventionName(typedefDecl, (parentType is AttributedType) ? parentType.Handle.FunctionTypeCallingConv : functionProtoType.CallConv, name, isForFnptr: false);

					this._outputBuilder.WriteIndented("[UnmanagedFunctionPointer");

					if (callingConventionName != "Winapi")
					{
						this._outputBuilder.Write("(CallingConvention.");
						this._outputBuilder.Write(callingConventionName);
						this._outputBuilder.Write(')');
					}

					this._outputBuilder.WriteLine(']');

					var returnType = functionProtoType.ReturnType;
					var returnTypeName = this.GetRemappedTypeName(typedefDecl, context: null, returnType, out var nativeTypeName);
					this.AddNativeTypeNameAttribute(nativeTypeName, attributePrefix: "return: ");

					this._outputBuilder.WriteIndented(this.GetAccessSpecifierName(typedefDecl));
					this._outputBuilder.Write(' ');

					if (this.IsUnsafe(typedefDecl, functionProtoType))
					{
						this._outputBuilder.Write("unsafe ");
					}

					this._outputBuilder.Write("delegate ");
					this._outputBuilder.Write(returnTypeName);
					this._outputBuilder.Write(' ');
					this._outputBuilder.Write(escapedName);
					this._outputBuilder.Write('(');

					this.Visit(typedefDecl.CursorChildren.OfType<ParmVarDecl>());

					this._outputBuilder.Write(')');
					this._outputBuilder.WriteSemicolon();
					this._outputBuilder.WriteNewline();
				}
				this.StopUsingOutputBuilder();
			}

			void ForPointeeType(TypedefDecl typedefDecl, Type parentType, Type pointeeType)
			{
				if (pointeeType is AttributedType attributedType)
				{
					ForPointeeType(typedefDecl, attributedType, attributedType.ModifiedType);
				}
				else if (pointeeType is ElaboratedType elaboratedType)
				{
					ForPointeeType(typedefDecl, elaboratedType, elaboratedType.NamedType);
				}
				else if (pointeeType is FunctionProtoType functionProtoType)
				{
					ForFunctionProtoType(typedefDecl, functionProtoType, parentType);
				}
				else if (pointeeType is PointerType pointerType)
				{
					ForPointeeType(typedefDecl, pointerType, pointerType.PointeeType);
				}
				else if (pointeeType is TypedefType typedefType)
				{
					ForPointeeType(typedefDecl, typedefType, typedefType.Decl.UnderlyingType);
				}
				else if (!(pointeeType is ConstantArrayType) && !(pointeeType is BuiltinType) && !(pointeeType is TagType))
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported pointee type: '{pointeeType.TypeClass}'. Generating bindings may be incomplete.", typedefDecl);
				}
			}

			void ForUnderlyingType(TypedefDecl typedefDecl, Type underlyingType)
			{
				if (underlyingType is ArrayType arrayType)
				{
					// Nothing to do for array types
				}
				else if (underlyingType is AttributedType attributedType)
				{
					ForUnderlyingType(typedefDecl, attributedType.ModifiedType);
				}
				else if (underlyingType is BuiltinType builtinType)
				{
					// Nothing to do for builtin types
				}
				else if (underlyingType is ElaboratedType elaboratedType)
				{
					ForUnderlyingType(typedefDecl, elaboratedType.NamedType);
				}
				else if (underlyingType is FunctionProtoType functionProtoType)
				{
					ForFunctionProtoType(typedefDecl, functionProtoType, parentType: null);
				}
				else if (underlyingType is PointerType pointerType)
				{
					ForPointeeType(typedefDecl, parentType: null, pointerType.PointeeType);
				}
				else if (underlyingType is ReferenceType referenceType)
				{
					ForPointeeType(typedefDecl, parentType: null, referenceType.PointeeType);
				}
				else if (underlyingType is TagType)
				{
					// Nothing to do for tag types
				}
				else if (underlyingType is TypedefType typedefType)
				{
					ForUnderlyingType(typedefDecl, typedefType.Decl.UnderlyingType);
				}
				else
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported underlying type: '{underlyingType.TypeClass}'. Generating bindings may be incomplete.", typedefDecl);
				}
				return;
			}

			string GetUndecoratedName(Type type)
			{
				if (type is AttributedType attributedType)
				{
					return GetUndecoratedName(attributedType.ModifiedType);
				}
				else if (type is ElaboratedType elaboratedType)
				{
					return GetUndecoratedName(elaboratedType.NamedType);
				}
				else
				{
					return type.AsString;
				}
			}
		}

		private void VisitVarDecl(VarDecl varDecl)
		{
			if (this.IsPrevContextStmt<DeclStmt>(out var declStmt))
			{
				ForDeclStmt(varDecl, declStmt);
			}
			else if (this.IsPrevContextDecl<TranslationUnitDecl>(out _) || this.IsPrevContextDecl<LinkageSpecDecl>(out _) || this.IsPrevContextDecl<RecordDecl>(out _))
			{
				if (!varDecl.HasInit)
				{
					// Nothing to do if a top level const declaration doesn't have an initializer
					return;
				}

				var type = varDecl.Type;
				var isMacroDefinitionRecord = false;

				var nativeName = this.GetCursorName(varDecl);
				if (nativeName.StartsWith("ClangSharpMacro_"))
				{
					type = varDecl.Init.Type;
					nativeName = nativeName.Substring("ClangSharpMacro_".Length);
					isMacroDefinitionRecord = true;
				}

				var accessSpecifier = this.GetAccessSpecifierName(varDecl);
				var name = this.GetRemappedName(nativeName, varDecl, tryRemapOperatorName: false);
				var escapedName = this.EscapeName(name);

				if (isMacroDefinitionRecord)
				{
					if (this.IsStmtAsWritten<DeclRefExpr>(varDecl.Init, out var declRefExpr, removeParens: true))
					{
						if ((declRefExpr.Decl is NamedDecl namedDecl) && (name == this.GetCursorName(namedDecl)))
						{
							return;
						}
					}
				}

				var openedOutputBuilder = false;

				if (this._outputBuilder is null)
				{
					this.StartUsingOutputBuilder(this._config.MethodClassName);
					openedOutputBuilder = true;

					if (this.IsUnsafe(varDecl, type) && (!varDecl.HasInit || !this.IsStmtAsWritten<StringLiteral>(varDecl.Init, out _, removeParens: true)))
					{
						this._isMethodClassUnsafe = true;
					}
				}

				this.WithAttributes("*");
				this.WithAttributes(name);

				this.WithUsings("*");
				this.WithUsings(name);

				var typeName = this.GetRemappedTypeName(varDecl, context: null, type, out var nativeTypeName);

				if (typeName == "Guid")
				{
					this._generatedUuids.Add(name);
				}

				if (isMacroDefinitionRecord)
				{
					var nativeTypeNameBuilder = new StringBuilder("#define");

					nativeTypeNameBuilder.Append(' ');
					nativeTypeNameBuilder.Append(nativeName);
					nativeTypeNameBuilder.Append(' ');

					var macroValue = this.GetSourceRangeContents(varDecl.TranslationUnit.Handle, varDecl.Init.Extent);
					nativeTypeNameBuilder.Append(macroValue);

					nativeTypeName = nativeTypeNameBuilder.ToString();
				}

				this.AddNativeTypeNameAttribute(nativeTypeName);

				this._outputBuilder.WriteIndented(accessSpecifier);
				this._outputBuilder.Write(' ');

				var isProperty = false;

				if (this.IsStmtAsWritten<StringLiteral>(varDecl.Init, out var stringLiteral, removeParens: true))
				{
					switch (stringLiteral.Kind)
					{
						case CX_CharacterKind.CX_CLK_Ascii:
						case CX_CharacterKind.CX_CLK_UTF8:
						{
							this._outputBuilder.AddUsingDirective("System");
							this._outputBuilder.Write("static ");

							typeName = "ReadOnlySpan<byte>";
							isProperty = true;
							break;
						}

						case CX_CharacterKind.CX_CLK_Wide:
						{
							if (this._config.GenerateUnixTypes)
							{
								goto default;
							}

							goto case CX_CharacterKind.CX_CLK_UTF16;
						}

						case CX_CharacterKind.CX_CLK_UTF16:
						{
							this._outputBuilder.Write("const ");

							typeName = "string";
							break;
						}

						default:
						{
							this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported string literal kind: '{stringLiteral.Kind}'. Generated bindings may be incomplete.", stringLiteral);
							break;
						}
					}
				}
				else if ((type.IsLocalConstQualified || isMacroDefinitionRecord) && CanBeConstant(type, varDecl.Init))
				{
					this._outputBuilder.Write("const ");
				}
				else if ((varDecl.StorageClass == CX_StorageClass.CX_SC_Static) || openedOutputBuilder)
				{
					this._outputBuilder.Write("static ");

					if (type.IsLocalConstQualified || isMacroDefinitionRecord)
					{
						this._outputBuilder.Write("readonly ");
					}
				}

				this._outputBuilder.Write(typeName);

				if (type is ArrayType)
				{
					this._outputBuilder.Write("[]");
				}

				this._outputBuilder.Write(' ');

				this._outputBuilder.Write(escapedName);

				if (varDecl.HasInit)
				{
					this._outputBuilder.Write(" =");

					if (isProperty)
					{
						this._outputBuilder.Write('>');
					}

					this._outputBuilder.Write(' ');

					if ((type.CanonicalType is PointerType pointerType) && (pointerType.PointeeType.CanonicalType is FunctionType) && isMacroDefinitionRecord)
					{
						this._outputBuilder.Write('&');
					}
					this.UncheckStmt(typeName, varDecl.Init);
				}

				this._outputBuilder.WriteSemicolon();
				this._outputBuilder.WriteNewline();

				if (openedOutputBuilder)
				{
					this.StopUsingOutputBuilder();
				}
				else
				{
					this._outputBuilder.NeedsNewline = true;
				}
			}
			else
			{
				this.IsPrevContextDecl<Decl>(out var previousContext);
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported variable declaration parent: '{previousContext.CursorKindSpelling}'. Generated bindings may be incomplete.", previousContext);
			}

			void ForDeclStmt(VarDecl varDecl, DeclStmt declStmt)
			{
				var name = this.GetRemappedCursorName(varDecl);
				var escapedName = this.EscapeName(name);

				if (varDecl == declStmt.Decls.First())
				{
					var type = varDecl.Type;
					var typeName = this.GetRemappedTypeName(varDecl, context: null, type, out var nativeTypeName);

					this._outputBuilder.Write(typeName);

					if (type is ArrayType)
					{
						this._outputBuilder.Write("[]");
					}

					this._outputBuilder.Write(' ');
				}

				this._outputBuilder.Write(escapedName);

				if (varDecl.HasInit)
				{
					this._outputBuilder.Write(' ');
					this._outputBuilder.Write('=');
					this._outputBuilder.Write(' ');

					var varDeclTypeName = this.GetRemappedTypeName(varDecl, context: null, varDecl.Type, out var varDeclNativeTypeName);
					this.UncheckStmt(varDeclTypeName, varDecl.Init);
				}
			}

			bool CanBeConstant(Type type, Expr initExpr)
			{
				if (type is AttributedType attributedType)
				{
					return CanBeConstant(attributedType.ModifiedType, initExpr);
				}
				else if (type is AutoType autoType)
				{
					return CanBeConstant(autoType.CanonicalType, initExpr);
				}
				else if (type is BuiltinType builtinType)
				{
					switch (type.Kind)
					{
						case CXTypeKind.CXType_Bool:
						case CXTypeKind.CXType_Char_U:
						case CXTypeKind.CXType_UChar:
						case CXTypeKind.CXType_Char16:
						case CXTypeKind.CXType_UShort:
						case CXTypeKind.CXType_UInt:
						case CXTypeKind.CXType_ULong:
						case CXTypeKind.CXType_ULongLong:
						case CXTypeKind.CXType_Char_S:
						case CXTypeKind.CXType_SChar:
						case CXTypeKind.CXType_WChar:
						case CXTypeKind.CXType_Short:
						case CXTypeKind.CXType_Int:
						case CXTypeKind.CXType_Long:
						case CXTypeKind.CXType_LongLong:
						case CXTypeKind.CXType_Float:
						case CXTypeKind.CXType_Double:
						{
							return this.IsConstant(initExpr);
						}
					}
				}
				else if (type is ElaboratedType elaboratedType)
				{
					return CanBeConstant(elaboratedType.NamedType, initExpr);
				}
				else if (type is EnumType enumType)
				{
					return CanBeConstant(enumType.Decl.IntegerType, initExpr);
				}
				else if (type is TypedefType typedefType)
				{
					return CanBeConstant(typedefType.Decl.UnderlyingType, initExpr);
				}

				return false;
			}
		}

		private bool IsConstant(Expr initExpr)
		{
			switch (initExpr.StmtClass)
			{
				// case CX_StmtClass.CX_StmtClass_BinaryConditionalOperator:

				case CX_StmtClass.CX_StmtClass_ConditionalOperator:
				{
					return false;
				}

				// case CX_StmtClass.CX_StmtClass_AddrLabelExpr:
				// case CX_StmtClass.CX_StmtClass_ArrayInitIndexExpr:
				// case CX_StmtClass.CX_StmtClass_ArrayInitLoopExpr:
				// case CX_StmtClass.CX_StmtClass_ArraySubscriptExpr:
				// case CX_StmtClass.CX_StmtClass_ArrayTypeTraitExpr:
				// case CX_StmtClass.CX_StmtClass_AsTypeExpr:
				// case CX_StmtClass.CX_StmtClass_AtomicExpr:

				case CX_StmtClass.CX_StmtClass_BinaryOperator:
				{
					var binaryOperator = (BinaryOperator)initExpr;
					return this.IsConstant(binaryOperator.LHS) && this.IsConstant(binaryOperator.RHS);
				}

				// case CX_StmtClass.CX_StmtClass_CompoundAssignOperator:
				// case CX_StmtClass.CX_StmtClass_BlockExpr:
				// case CX_StmtClass.CX_StmtClass_CXXBindTemporaryExpr:

				case CX_StmtClass.CX_StmtClass_CXXBoolLiteralExpr:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_CXXConstructExpr:
				// case CX_StmtClass.CX_StmtClass_CXXTemporaryObjectExpr:
				// case CX_StmtClass.CX_StmtClass_CXXDefaultArgExpr:
				// case CX_StmtClass.CX_StmtClass_CXXDefaultInitExpr:
				// case CX_StmtClass.CX_StmtClass_CXXDeleteExpr:
				// case CX_StmtClass.CX_StmtClass_CXXDependentScopeMemberExpr:
				// case CX_StmtClass.CX_StmtClass_CXXFoldExpr:
				// case CX_StmtClass.CX_StmtClass_CXXInheritedCtorInitExpr:
				// case CX_StmtClass.CX_StmtClass_CXXNewExpr:
				// case CX_StmtClass.CX_StmtClass_CXXNoexceptExpr:

				case CX_StmtClass.CX_StmtClass_CXXNullPtrLiteralExpr:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_CXXPseudoDestructorExpr:
				// case CX_StmtClass.CX_StmtClass_CXXRewrittenBinaryOperator:
				// case CX_StmtClass.CX_StmtClass_CXXScalarValueInitExpr:
				// case CX_StmtClass.CX_StmtClass_CXXStdInitializerListExpr:
				// case CX_StmtClass.CX_StmtClass_CXXThisExpr:
				// case CX_StmtClass.CX_StmtClass_CXXThrowExpr:
				// case CX_StmtClass.CX_StmtClass_CXXTypeidExpr:
				// case CX_StmtClass.CX_StmtClass_CXXUnresolvedConstructExpr:
				// case CX_StmtClass.CX_StmtClass_CXXUuidofExpr:

				case CX_StmtClass.CX_StmtClass_CallExpr:
				{
					return false;
				}

				// case CX_StmtClass.CX_StmtClass_CUDAKernelCallExpr:
				// case CX_StmtClass.CX_StmtClass_CXXMemberCallExpr:

				case CX_StmtClass.CX_StmtClass_CXXOperatorCallExpr:
				{
					var cxxOperatorCall = (CXXOperatorCallExpr)initExpr;

					if (cxxOperatorCall.CalleeDecl is FunctionDecl functionDecl)
					{
						var functionDeclName = this.GetCursorName(functionDecl);

						if (this.IsEnumOperator(functionDecl, functionDeclName))
						{
							return true;
						}
					}
					return false;
				}

				// case CX_StmtClass.CX_StmtClass_UserDefinedLiteral:
				// case CX_StmtClass.CX_StmtClass_BuiltinBitCastExpr:

				case CX_StmtClass.CX_StmtClass_CStyleCastExpr:
				{
					var cStyleCastExpr = (CStyleCastExpr)initExpr;
					return this.IsConstant(cStyleCastExpr.SubExprAsWritten);
				}

				// case CX_StmtClass.CX_StmtClass_CXXFunctionalCastExpr:
				// case CX_StmtClass.CX_StmtClass_CXXConstCastExpr:
				// case CX_StmtClass.CX_StmtClass_CXXDynamicCastExpr:
				// case CX_StmtClass.CX_StmtClass_CXXReinterpretCastExpr:

				case CX_StmtClass.CX_StmtClass_CXXStaticCastExpr:
				{
					var cxxStaticCastExpr = (CXXStaticCastExpr)initExpr;
					return this.IsConstant(cxxStaticCastExpr.SubExprAsWritten);
				}

				// case CX_StmtClass.CX_StmtClass_ObjCBridgedCastExpr:

				case CX_StmtClass.CX_StmtClass_ImplicitCastExpr:
				{
					var implicitCastExpr = (ImplicitCastExpr)initExpr;
					return this.IsConstant(implicitCastExpr.SubExprAsWritten);
				}

				case CX_StmtClass.CX_StmtClass_CharacterLiteral:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_ChooseExpr:
				// case CX_StmtClass.CX_StmtClass_CompoundLiteralExpr:
				// case CX_StmtClass.CX_StmtClass_ConceptSpecializationExpr:
				// case CX_StmtClass.CX_StmtClass_ConvertVectorExpr:
				// case CX_StmtClass.CX_StmtClass_CoawaitExpr:
				// case CX_StmtClass.CX_StmtClass_CoyieldExpr:

				case CX_StmtClass.CX_StmtClass_DeclRefExpr:
				{
					var declRefExpr = (DeclRefExpr)initExpr;
					return (declRefExpr.Decl is EnumConstantDecl) ||
						   ((declRefExpr.Decl is VarDecl varDecl) && varDecl.HasInit && this.IsConstant(varDecl.Init));
				}

				// case CX_StmtClass.CX_StmtClass_DependentCoawaitExpr:
				// case CX_StmtClass.CX_StmtClass_DependentScopeDeclRefExpr:
				// case CX_StmtClass.CX_StmtClass_DesignatedInitExpr:
				// case CX_StmtClass.CX_StmtClass_DesignatedInitUpdateExpr:
				// case CX_StmtClass.CX_StmtClass_ExpressionTraitExpr:
				// case CX_StmtClass.CX_StmtClass_ExtVectorElementExpr:
				// case CX_StmtClass.CX_StmtClass_FixedPointLiteral:

				case CX_StmtClass.CX_StmtClass_FloatingLiteral:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_ConstantExpr:
				// case CX_StmtClass.CX_StmtClass_ExprWithCleanups:
				// case CX_StmtClass.CX_StmtClass_FunctionParmPackExpr:
				// case CX_StmtClass.CX_StmtClass_GNUNullExpr:
				// case CX_StmtClass.CX_StmtClass_GenericSelectionExpr:
				// case CX_StmtClass.CX_StmtClass_ImaginaryLiteral:
				// case CX_StmtClass.CX_StmtClass_ImplicitValueInitExpr:
				// case CX_StmtClass.CX_StmtClass_InitListExpr:

				case CX_StmtClass.CX_StmtClass_IntegerLiteral:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_LambdaExpr:
				// case CX_StmtClass.CX_StmtClass_MSPropertyRefExpr:
				// case CX_StmtClass.CX_StmtClass_MSPropertySubscriptExpr:
				// case CX_StmtClass.CX_StmtClass_MaterializeTemporaryExpr:

				case CX_StmtClass.CX_StmtClass_MemberExpr:
				{
					return false;
				}

				// case CX_StmtClass.CX_StmtClass_NoInitExpr:
				// case CX_StmtClass.CX_StmtClass_OMPArraySectionExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCArrayLiteral:
				// case CX_StmtClass.CX_StmtClass_ObjCAvailabilityCheckExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCBoolLiteralExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCBoxedExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCDictionaryLiteral:
				// case CX_StmtClass.CX_StmtClass_ObjCEncodeExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCIndirectCopyRestoreExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCIsaExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCIvarRefExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCMessageExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCPropertyRefExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCProtocolExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCSelectorExpr:
				// case CX_StmtClass.CX_StmtClass_ObjCStringLiteral:
				// case CX_StmtClass.CX_StmtClass_ObjCSubscriptRefExpr:
				// case CX_StmtClass.CX_StmtClass_OffsetOfExpr:
				// case CX_StmtClass.CX_StmtClass_OpaqueValueExpr:
				// case CX_StmtClass.CX_StmtClass_UnresolvedLookupExpr:
				// case CX_StmtClass.CX_StmtClass_UnresolvedMemberExpr:
				// case CX_StmtClass.CX_StmtClass_PackExpansionExpr:

				case CX_StmtClass.CX_StmtClass_ParenExpr:
				{
					var parenExpr = (ParenExpr)initExpr;
					return this.IsConstant(parenExpr.SubExpr);
				}

				// case CX_StmtClass.CX_StmtClass_ParenListExpr:
				// case CX_StmtClass.CX_StmtClass_PredefinedExpr:
				// case CX_StmtClass.CX_StmtClass_PseudoObjectExpr:
				// case CX_StmtClass.CX_StmtClass_RequiresExpr:
				// case CX_StmtClass.CX_StmtClass_ShuffleVectorExpr:
				// case CX_StmtClass.CX_StmtClass_SizeOfPackExpr:
				// case CX_StmtClass.CX_StmtClass_SourceLocExpr:
				// case CX_StmtClass.CX_StmtClass_StmtExpr:

				case CX_StmtClass.CX_StmtClass_StringLiteral:
				{
					return true;
				}

				// case CX_StmtClass.CX_StmtClass_SubstNonTypeTemplateParmExpr:
				// case CX_StmtClass.CX_StmtClass_SubstNonTypeTemplateParmPackExpr:
				// case CX_StmtClass.CX_StmtClass_TypeTraitExpr:
				// case CX_StmtClass.CX_StmtClass_TypoExpr:

				case CX_StmtClass.CX_StmtClass_UnaryExprOrTypeTraitExpr:
				{
					var unaryExprOrTypeTraitExpr = (UnaryExprOrTypeTraitExpr)initExpr;
					var argumentType = unaryExprOrTypeTraitExpr.TypeOfArgument;

					long size32;
					long size64;

					long alignment32 = -1;
					long alignment64 = -1;

					this.GetTypeSize(unaryExprOrTypeTraitExpr, argumentType, ref alignment32, ref alignment64, out size32, out size64);

					switch (unaryExprOrTypeTraitExpr.Kind)
					{
						case CX_UnaryExprOrTypeTrait.CX_UETT_SizeOf:
						{
							return (size32 == size64);
						}

						case CX_UnaryExprOrTypeTrait.CX_UETT_AlignOf:
						case CX_UnaryExprOrTypeTrait.CX_UETT_PreferredAlignOf:
						{
							return (alignment32 == alignment64);
						}

						default:
						{
							return false;
						}
					}
				}

				case CX_StmtClass.CX_StmtClass_UnaryOperator:
				{
					var unaryOperator = (UnaryOperator)initExpr;
					return this.IsConstant(unaryOperator.SubExpr);
				}

				// case CX_StmtClass.CX_StmtClass_VAArgExpr:

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Warning, $"Unsupported statement class: '{initExpr.StmtClassName}'. Generated bindings may not be constant.", initExpr);
					return false;
				}
			}
		}
	}
}
