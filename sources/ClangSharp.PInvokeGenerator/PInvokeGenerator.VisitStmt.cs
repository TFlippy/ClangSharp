// Copyright (c) Microsoft and Contributors. All rights reserved. Licensed under the University of Illinois/NCSA Open Source License. See LICENSE.txt in the project root for license information.

using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClangSharp
{
	public partial class PInvokeGenerator
	{
		private void VisitArraySubscriptExpr(ArraySubscriptExpr arraySubscriptExpr)
		{
			this.Visit(arraySubscriptExpr.Base);
			this._outputBuilder.Write('[');
			this.Visit(arraySubscriptExpr.Idx);
			this._outputBuilder.Write(']');
		}

		private void VisitBinaryOperator(BinaryOperator binaryOperator)
		{
			this.Visit(binaryOperator.LHS);
			this._outputBuilder.Write(' ');
			this._outputBuilder.Write(binaryOperator.OpcodeStr);
			this._outputBuilder.Write(' ');
			this.Visit(binaryOperator.RHS);
		}

		private void VisitBreakStmt(BreakStmt breakStmt)
		{
			this._outputBuilder.Write("break");
		}

		private void VisitBody(Stmt stmt)
		{
			if (stmt is CompoundStmt)
			{
				this.Visit(stmt);
			}
			else
			{
				this._outputBuilder.WriteBlockStart();
				this._outputBuilder.WriteIndentation();
				this._outputBuilder.NeedsSemicolon = true;
				this._outputBuilder.NeedsNewline = true;

				this.Visit(stmt);

				this._outputBuilder.WriteSemicolonIfNeeded();
				this._outputBuilder.WriteNewlineIfNeeded();
				this._outputBuilder.WriteBlockEnd();
			}
		}

		private void VisitCallExpr(CallExpr callExpr)
		{
			var calleeDecl = callExpr.CalleeDecl;

			if (calleeDecl is FunctionDecl functionDecl)
			{
				switch (functionDecl.Name)
				{
					case "memcpy":
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
						this._outputBuilder.Write("Unsafe.CopyBlockUnaligned");
						VisitArgs(callExpr);
						break;
					}

					case "memset":
					{
						this._outputBuilder.AddUsingDirective("System.Runtime.CompilerServices");
						this._outputBuilder.Write("Unsafe.InitBlockUnaligned");
						VisitArgs(callExpr);
						break;
					}

					default:
					{
						this.Visit(callExpr.Callee);
						VisitArgs(callExpr);
						break;
					}
				}
			}
			else if (calleeDecl is FieldDecl)
			{
				this.Visit(callExpr.Callee);
				VisitArgs(callExpr);
			}
			else
			{
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported callee declaration: '{calleeDecl?.Kind}'. Generated bindings may be incomplete.", calleeDecl);
			}

			void VisitArgs(CallExpr callExpr)
			{
				this._outputBuilder.Write('(');

				var args = callExpr.Args;

				if (args.Count != 0)
				{
					this.Visit(args[0]);

					for (var i = 1; i < args.Count; i++)
					{
						this._outputBuilder.Write(", ");
						this.Visit(args[i]);
					}
				}

				this._outputBuilder.Write(')');
			}
		}

		private void VisitCaseStmt(CaseStmt caseStmt)
		{
			this._outputBuilder.Write("case ");
			this.Visit(caseStmt.LHS);
			this._outputBuilder.WriteLine(':');

			if (caseStmt.SubStmt is SwitchCase)
			{
				this._outputBuilder.WriteIndentation();
				this.Visit(caseStmt.SubStmt);
			}
			else
			{
				this.VisitBody(caseStmt.SubStmt);
			}
		}

		private void VisitCharacterLiteral(CharacterLiteral characterLiteral)
		{
			switch (characterLiteral.Kind)
			{
				case CX_CharacterKind.CX_CLK_Ascii:
				case CX_CharacterKind.CX_CLK_UTF8:
				{
					if (characterLiteral.Value > ushort.MaxValue)
					{
						this._outputBuilder.Write("0x");
						this._outputBuilder.Write(characterLiteral.Value.ToString("X8"));
					}
					else if (characterLiteral.Value > byte.MaxValue)
					{
						this._outputBuilder.Write("0x");
						this._outputBuilder.Write(characterLiteral.Value.ToString("X4"));
					}
					else
					{
						var isPreviousExplicitCast = this.IsPrevContextStmt<ExplicitCastExpr>(out _);

						if (!isPreviousExplicitCast)
						{
							this._outputBuilder.Write("(byte)(");
						}

						this._outputBuilder.Write('\'');
						this._outputBuilder.Write(this.EscapeCharacter((char)characterLiteral.Value));
						this._outputBuilder.Write('\'');

						if (!isPreviousExplicitCast)
						{
							this._outputBuilder.Write(')');
						}
					}
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
					if (characterLiteral.Value > ushort.MaxValue)
					{
						this._outputBuilder.Write("0x");
						this._outputBuilder.Write(characterLiteral.Value.ToString("X8"));
					}
					else
					{
						this._outputBuilder.Write('\'');
						this._outputBuilder.Write(this.EscapeCharacter((char)characterLiteral.Value));
						this._outputBuilder.Write('\'');
					}
					break;
				}

				case CX_CharacterKind.CX_CLK_UTF32:
				{
					this._outputBuilder.Write("0x");
					this._outputBuilder.Write(characterLiteral.Value.ToString("X8"));
					break;
				}

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported character literal kind: '{characterLiteral.Kind}'. Generated bindings may be incomplete.", characterLiteral);
					break;
				}
			}
		}

		private void VisitCompoundStmt(CompoundStmt compoundStmt)
		{
			this._outputBuilder.WriteBlockStart();

			this.VisitStmts(compoundStmt.Body);

			this._outputBuilder.WriteSemicolonIfNeeded();
			this._outputBuilder.WriteNewlineIfNeeded();
			this._outputBuilder.WriteBlockEnd();
		}

		private void VisitConditionalOperator(ConditionalOperator conditionalOperator)
		{
			this.Visit(conditionalOperator.Cond);
			this._outputBuilder.Write(" ? ");
			this.Visit(conditionalOperator.TrueExpr);
			this._outputBuilder.Write(" : ");
			this.Visit(conditionalOperator.FalseExpr);
		}

		private void VisitContinueStmt(ContinueStmt continueStmt)
		{
			this._outputBuilder.Write("continue");
		}

		private void VisitCXXBoolLiteralExpr(CXXBoolLiteralExpr cxxBoolLiteralExpr)
		{
			this._outputBuilder.Write(cxxBoolLiteralExpr.ValueString);
		}

		private void VisitCXXConstCastExpr(CXXConstCastExpr cxxConstCastExpr)
		{
			// C# doesn't have a concept of const pointers so
			// ignore rather than adding a cast from T* to T*

			this.Visit(cxxConstCastExpr.SubExprAsWritten);
		}

		private void VisitCXXConstructExpr(CXXConstructExpr cxxConstructExpr)
		{
			var isCopyConstructor = cxxConstructExpr.Constructor.IsCopyConstructor;

			if (!isCopyConstructor)
			{
				this._outputBuilder.Write("new ");

				var constructorName = this.GetRemappedCursorName(cxxConstructExpr.Constructor);

				this._outputBuilder.Write(constructorName);
				this._outputBuilder.Write('(');
			}

			var args = cxxConstructExpr.Args;

			if (args.Count != 0)
			{
				this.Visit(args[0]);

				for (var i = 1; i < args.Count; i++)
				{
					this._outputBuilder.Write(", ");
					this.Visit(args[i]);
				}
			}

			if (!isCopyConstructor)
			{
				this._outputBuilder.Write(')');
			}
		}

		private void VisitCXXFunctionalCastExpr(CXXFunctionalCastExpr cxxFunctionalCastExpr)
		{
			if (cxxFunctionalCastExpr.SubExpr is CXXConstructExpr cxxConstructExpr)
			{
				this.Visit(cxxConstructExpr);
			}
			else
			{
				this.VisitExplicitCastExpr(cxxFunctionalCastExpr);
			}
		}

		private void VisitCXXNullPtrLiteralExpr(CXXNullPtrLiteralExpr cxxNullPtrLiteralExpr)
		{
			this._outputBuilder.Write("null");
		}

		private void VisitCXXOperatorCallExpr(CXXOperatorCallExpr cxxOperatorCallExpr)
		{
			var calleeDecl = cxxOperatorCallExpr.CalleeDecl;

			if (calleeDecl is FunctionDecl functionDecl)
			{
				if (functionDecl.DeclContext is CXXRecordDecl)
				{
					this.Visit(cxxOperatorCallExpr.Args[0]);
					this._outputBuilder.Write('.');
				}

				var functionDeclName = this.GetCursorName(functionDecl);
				var args = cxxOperatorCallExpr.Args;

				if (this.IsEnumOperator(functionDecl, functionDeclName))
				{
					switch (functionDeclName)
					{
						case "operator|":
						case "operator|=":
						case "operator&":
						case "operator&=":
						case "operator^":
						case "operator^=":
						{
							this.Visit(args[0]);
							this._outputBuilder.Write(' ');
							this._outputBuilder.Write(functionDeclName.Substring(8));
							this._outputBuilder.Write(' ');
							this.Visit(args[1]);
							return;
						}

						case "operator~":
						{
							this._outputBuilder.Write(functionDeclName.Substring(8));
							this.Visit(args[0]);
							return;
						}

						default:
						{
							break;
						}
					}
				}

				var name = this.GetRemappedCursorName(functionDecl);
				this._outputBuilder.Write(name);

				this._outputBuilder.Write('(');

				if (args.Count != 0)
				{
					var firstIndex = (functionDecl.DeclContext is CXXRecordDecl) ? 1 : 0;
					this.Visit(args[firstIndex]);

					for (var i = firstIndex + 1; i < args.Count; i++)
					{
						this._outputBuilder.Write(", ");
						this.Visit(args[i]);
					}
				}

				this._outputBuilder.Write(')');
			}
			else
			{
				this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported callee declaration: '{calleeDecl.Kind}'. Generated bindings may be incomplete.", calleeDecl);
			}
		}

		private void VisitCXXThisExpr(CXXThisExpr cxxThisExpr)
		{
			this._outputBuilder.Write("this");
		}

		private void VisitCXXUuidofExpr(CXXUuidofExpr cxxUuidofExpr)
		{
			this._outputBuilder.Write("typeof(");

			var type = cxxUuidofExpr.IsTypeOperand ? cxxUuidofExpr.TypeOperand : cxxUuidofExpr.ExprOperand.Type;
			var typeName = this.GetRemappedTypeName(cxxUuidofExpr, context: null, type, out _);
			this._outputBuilder.Write(typeName);

			this._outputBuilder.Write(").GUID");
		}

		private void VisitDeclRefExpr(DeclRefExpr declRefExpr)
		{
			var name = this.GetRemappedCursorName(declRefExpr.Decl);

			if ((declRefExpr.Decl is EnumConstantDecl enumConstantDecl) && (declRefExpr.DeclContext != enumConstantDecl.DeclContext) && (enumConstantDecl.DeclContext is NamedDecl namedDecl))
			{
				name = $"{this._config.MethodClassName}.{name}";

				//var enumName = GetRemappedCursorName(namedDecl);
				//_outputBuilder.AddUsingDirective($"static {_config.Namespace}.{enumName}");
			}

			this._outputBuilder.Write(this.EscapeAndStripName(name));
		}

		private void VisitDeclStmt(DeclStmt declStmt)
		{
			if (declStmt.IsSingleDecl)
			{
				this.Visit(declStmt.SingleDecl);
			}
			else
			{
				this.Visit(declStmt.Decls.First());

				foreach (var decl in declStmt.Decls.Skip(1))
				{
					this._outputBuilder.Write(", ");
					this.Visit(decl);
				}
			}
		}

		private void VisitDefaultStmt(DefaultStmt defaultStmt)
		{
			this._outputBuilder.WriteLine("default:");

			if (defaultStmt.SubStmt is SwitchCase)
			{
				this._outputBuilder.WriteIndentation();
				this.Visit(defaultStmt.SubStmt);
			}
			else
			{
				this.VisitBody(defaultStmt.SubStmt);
			}
		}

		private void VisitDoStmt(DoStmt doStmt)
		{
			this._outputBuilder.WriteLine("do");

			this.VisitBody(doStmt.Body);

			this._outputBuilder.WriteIndented("while (");

			this.Visit(doStmt.Cond);

			this._outputBuilder.Write(')');
			this._outputBuilder.WriteSemicolon();
			this._outputBuilder.WriteNewline();

			this._outputBuilder.NeedsNewline = true;
		}

		private void VisitExplicitCastExpr(ExplicitCastExpr explicitCastExpr)
		{
			var type = explicitCastExpr.Type;
			var typeName = this.GetRemappedTypeName(explicitCastExpr, context: null, type, out var nativeTypeName);

			this._outputBuilder.Write('(');
			this._outputBuilder.Write(typeName);
			this._outputBuilder.Write(')');

			this.ParenthesizeStmt(explicitCastExpr.SubExprAsWritten);
		}

		private void VisitFloatingLiteral(FloatingLiteral floatingLiteral)
		{
			if (floatingLiteral.ValueString.EndsWith(".f"))
			{
				this._outputBuilder.Write(floatingLiteral.ValueString.Substring(0, floatingLiteral.ValueString.Length - 1));
				this._outputBuilder.Write("0f");
			}
			else
			{
				this._outputBuilder.Write(floatingLiteral.ValueString);

				if (floatingLiteral.ValueString.EndsWith("."))
				{
					this._outputBuilder.Write('0');
				}
			}
		}

		private void VisitForStmt(ForStmt forStmt)
		{
			this._outputBuilder.Write("for (");

			if (forStmt.ConditionVariableDeclStmt != null)
			{
				this.Visit(forStmt.ConditionVariableDeclStmt);
			}
			else if (forStmt.Init != null)
			{
				this.Visit(forStmt.Init);
			}
			this._outputBuilder.WriteSemicolon();

			if (forStmt.Cond != null)
			{
				this._outputBuilder.Write(' ');
				this.Visit(forStmt.Cond);
			}
			this._outputBuilder.WriteSemicolon();

			if (forStmt.Inc != null)
			{
				this._outputBuilder.Write(' ');
				this.Visit(forStmt.Inc);
			}
			this._outputBuilder.WriteLine(')');

			this.VisitBody(forStmt.Body);
		}

		private void VisitGotoStmt(GotoStmt gotoStmt)
		{
			this._outputBuilder.Write("goto ");
			this._outputBuilder.Write(gotoStmt.Label.Name);
		}

		private void VisitIfStmt(IfStmt ifStmt)
		{
			this._outputBuilder.Write("if (");

			this.Visit(ifStmt.Cond);

			this._outputBuilder.WriteLine(')');

			this.VisitBody(ifStmt.Then);

			if (ifStmt.Else != null)
			{
				this._outputBuilder.WriteIndented("else");

				if (ifStmt.Else is IfStmt)
				{
					this._outputBuilder.Write(' ');
					this.Visit(ifStmt.Else);
				}
				else
				{
					this._outputBuilder.WriteNewline();
					this.VisitBody(ifStmt.Else);
				}
			}
		}

		private void VisitImplicitCastExpr(ImplicitCastExpr implicitCastExpr)
		{
			var subExpr = implicitCastExpr.SubExprAsWritten;

			switch (implicitCastExpr.CastKind)
			{
				case CX_CastKind.CX_CK_NullToPointer:
				{
					this._outputBuilder.Write("null");
					break;
				}

				case CX_CastKind.CX_CK_PointerToBoolean:
				{
					if ((subExpr is UnaryOperator unaryOperator) && (unaryOperator.Opcode == CX_UnaryOperatorKind.CX_UO_LNot))
					{
						this.Visit(subExpr);
					}
					else
					{
						this.ParenthesizeStmt(subExpr);
						this._outputBuilder.Write(" != null");
					}
					break;
				}

				case CX_CastKind.CX_CK_IntegralCast:
				{
					if (subExpr.Type.CanonicalType.Kind == CXTypeKind.CXType_Bool)
					{
						goto case CX_CastKind.CX_CK_BooleanToSignedIntegral;
					}
					else
					{
						goto default;
					}
				}

				case CX_CastKind.CX_CK_IntegralToBoolean:
				{
					if ((subExpr is UnaryOperator unaryOperator) && (unaryOperator.Opcode == CX_UnaryOperatorKind.CX_UO_LNot))
					{
						this.Visit(subExpr);
					}
					else
					{
						this.ParenthesizeStmt(subExpr);
						this._outputBuilder.Write(" != 0");
					}
					break;
				}

				case CX_CastKind.CX_CK_BooleanToSignedIntegral:
				{
					var needsCast = implicitCastExpr.Type.Handle.SizeOf < 4;

					if (needsCast)
					{
						this._outputBuilder.Write("(byte)(");
					}

					this.ParenthesizeStmt(subExpr);
					this._outputBuilder.Write(" ? 1 : 0");

					if (needsCast)
					{
						this._outputBuilder.Write(')');
					}

					break;
				}

				default:
				{
					if ((subExpr is DeclRefExpr declRefExpr) && (declRefExpr.Decl is EnumConstantDecl enumConstantDecl))
					{
						ForEnumConstantDecl(implicitCastExpr, enumConstantDecl);
					}
					else
					{
						this.Visit(subExpr);
					}
					break;
				}
			}

			void ForEnumConstantDecl(ImplicitCastExpr implicitCastExpr, EnumConstantDecl enumConstantDecl)
			{
				var subExpr = implicitCastExpr.SubExprAsWritten;

				if (this.IsPrevContextStmt<BinaryOperator>(out var binaryOperator) && ((binaryOperator.Opcode == CX_BinaryOperatorKind.CX_BO_EQ) || (binaryOperator.Opcode == CX_BinaryOperatorKind.CX_BO_NE)))
				{
					this.Visit(subExpr);
				}
				else if (this.IsPrevContextDecl<EnumConstantDecl>(out _))
				{
					this.Visit(subExpr);
				}
				else
				{
					var type = implicitCastExpr.Type;
					var typeName = this.GetRemappedTypeName(implicitCastExpr, context: null, type, out var nativeTypeName);

					this._outputBuilder.Write('(');
					this._outputBuilder.Write(typeName);
					this._outputBuilder.Write(')');

					this.ParenthesizeStmt(subExpr);
				}
			}
		}

		private void VisitImplicitValueInitExpr(ImplicitValueInitExpr implicitValueInitExpr)
		{
			this._outputBuilder.Write("default");
		}

		private void VisitInitListExpr(InitListExpr initListExpr)
		{
			ForType(initListExpr, initListExpr.Type);

			void ForArrayType(InitListExpr initListExpr, ArrayType arrayType)
			{
				this._outputBuilder.Write("new ");

				var type = initListExpr.Type;
				var typeName = this.GetRemappedTypeName(initListExpr, context: null, type, out var nativeTypeName);

				this._outputBuilder.Write(typeName);
				this._outputBuilder.Write('[');

				long size = -1;

				if (arrayType is ConstantArrayType constantArrayType)
				{
					size = constantArrayType.Size;
				}
				else
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported array type kind: '{type.KindSpelling}'. Generated bindings may be incomplete.", initListExpr);
				}

				if (size != -1)
				{
					this._outputBuilder.Write(size);
				}

				this._outputBuilder.WriteLine(']');
				this._outputBuilder.WriteBlockStart();

				for (var i = 0; i < initListExpr.Inits.Count; i++)
				{
					this._outputBuilder.WriteIndentation();
					this.Visit(initListExpr.Inits[i]);
					this._outputBuilder.WriteLine(',');
				}

				for (var i = initListExpr.Inits.Count; i < size; i++)
				{
					this._outputBuilder.WriteIndentedLine("default,");
				}

				this._outputBuilder.DecreaseIndentation();
				this._outputBuilder.WriteIndented('}');
				this._outputBuilder.NeedsSemicolon = true;
			}

			void ForRecordType(InitListExpr initListExpr, RecordType recordType)
			{
				this._outputBuilder.Write("new ");

				var type = initListExpr.Type;
				var typeName = this.GetRemappedTypeName(initListExpr, context: null, type, out var nativeTypeName);

				this._outputBuilder.Write(typeName);

				if (typeName == "Guid")
				{
					this._outputBuilder.Write('(');

					this.Visit(initListExpr.Inits[0]);

					this._outputBuilder.Write(", ");

					this.Visit(initListExpr.Inits[1]);

					this._outputBuilder.Write(", ");

					this.Visit(initListExpr.Inits[2]);
					initListExpr = (InitListExpr)initListExpr.Inits[3];

					for (var i = 0; i < initListExpr.Inits.Count; i++)
					{
						this._outputBuilder.Write(", ");

						this.Visit(initListExpr.Inits[i]);
					}

					this._outputBuilder.Write(')');
				}
				else
				{
					this._outputBuilder.WriteNewline();
					this._outputBuilder.WriteBlockStart();

					var decl = recordType.Decl;

					for (var i = 0; i < initListExpr.Inits.Count; i++)
					{
						var init = initListExpr.Inits[i];

						if (init is ImplicitValueInitExpr)
						{
							continue;
						}

						var fieldName = this.GetRemappedCursorName(decl.Fields[i]);

						this._outputBuilder.WriteIndented(fieldName);
						this._outputBuilder.Write(" = ");
						this.Visit(init);
						this._outputBuilder.WriteLine(',');
					}

					this._outputBuilder.DecreaseIndentation();
					this._outputBuilder.WriteIndented('}');
					this._outputBuilder.NeedsSemicolon = true;
				}
			}

			void ForType(InitListExpr initListExpr, Type type)
			{
				if (type is ArrayType arrayType)
				{
					ForArrayType(initListExpr, arrayType);
				}
				else if (type is ElaboratedType elaboratedType)
				{
					ForType(initListExpr, elaboratedType.NamedType);
				}
				else if (type is RecordType recordType)
				{
					ForRecordType(initListExpr, recordType);
				}
				else if (type is TypedefType typedefType)
				{
					ForType(initListExpr, typedefType.Decl.UnderlyingType);
				}
				else
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported init list expression type: '{type.KindSpelling}'. Generated bindings may be incomplete.", initListExpr);
				}
			}
		}

		private void VisitIntegerLiteral(IntegerLiteral integerLiteral)
		{
			var valueString = integerLiteral.ValueString;

			if (valueString.EndsWith("l", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 1);
			}
			else if (valueString.EndsWith("ui8", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 3);
			}
			else if (valueString.EndsWith("i8", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 2);
			}
			else if (valueString.EndsWith("ui16", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 4);
			}
			else if (valueString.EndsWith("i16", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 3);
			}
			else if (valueString.EndsWith("i32", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 3);
			}
			else if (valueString.EndsWith("i64", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 3) + "L";
			}

			if (valueString.EndsWith("ul", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 2) + "UL";
			}
			else if (valueString.EndsWith("l", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 1) + "L";
			}
			else if (valueString.EndsWith("u", StringComparison.OrdinalIgnoreCase))
			{
				valueString = valueString.Substring(0, valueString.Length - 1) + "U";
			}

			this._outputBuilder.Write(valueString);
		}

		private void VisitLabelStmt(LabelStmt labelStmt)
		{
			this._outputBuilder.Write(labelStmt.Decl.Name);
			this._outputBuilder.WriteLine(':');

			this._outputBuilder.WriteIndentation();
			this.Visit(labelStmt.SubStmt);
		}

		private void VisitMemberExpr(MemberExpr memberExpr)
		{
			if (!memberExpr.IsImplicitAccess)
			{
				this.Visit(memberExpr.Base);

				Type type;

				if (memberExpr.Base is CXXThisExpr)
				{
					type = null;
				}
				else if (memberExpr.Base is DeclRefExpr declRefExpr)
				{
					type = declRefExpr.Decl.Type.CanonicalType;
				}
				else
				{
					type = memberExpr.Base.Type.CanonicalType;
				}

				if ((type != null) && ((type is PointerType) || (type is ReferenceType)))
				{
					this._outputBuilder.Write("->");
				}
				else
				{
					this._outputBuilder.Write('.');
				}
			}
			this._outputBuilder.Write(this.GetRemappedCursorName(memberExpr.MemberDecl));
		}

		private void VisitParenExpr(ParenExpr parenExpr)
		{
			this._outputBuilder.Write('(');
			this.Visit(parenExpr.SubExpr);
			this._outputBuilder.Write(')');
		}

		private void VisitReturnStmt(ReturnStmt returnStmt)
		{
			if (this.IsPrevContextDecl<FunctionDecl>(out var functionDecl) && (functionDecl.ReturnType.CanonicalType.Kind != CXTypeKind.CXType_Void))
			{
				this._outputBuilder.Write("return");

				if (returnStmt.RetValue != null)
				{
					this._outputBuilder.Write(' ');
					this.Visit(returnStmt.RetValue);
				}
			}
			else if (returnStmt.RetValue != null)
			{
				this.Visit(returnStmt.RetValue);
			}
			else
			{
				this._outputBuilder.Write("return");
			}
		}

		private void VisitStmt(Stmt stmt)
		{
			switch (stmt.StmtClass)
			{
				// case CX_StmtClass.CX_StmtClass_GCCAsmStmt:
				// case CX_StmtClass.CX_StmtClass_MSAsmStmt:

				case CX_StmtClass.CX_StmtClass_BreakStmt:
				{
					this.VisitBreakStmt((BreakStmt)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_CXXCatchStmt:
				// case CX_StmtClass.CX_StmtClass_CXXForRangeStmt:
				// case CX_StmtClass.CX_StmtClass_CXXTryStmt:
				// case CX_StmtClass.CX_StmtClass_CapturedStmt:

				case CX_StmtClass.CX_StmtClass_CompoundStmt:
				{
					this.VisitCompoundStmt((CompoundStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_ContinueStmt:
				{
					this.VisitContinueStmt((ContinueStmt)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_CoreturnStmt:
				// case CX_StmtClass.CX_StmtClass_CoroutineBodyStmt:

				case CX_StmtClass.CX_StmtClass_DeclStmt:
				{
					this.VisitDeclStmt((DeclStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_DoStmt:
				{
					this.VisitDoStmt((DoStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_ForStmt:
				{
					this.VisitForStmt((ForStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_GotoStmt:
				{
					this.VisitGotoStmt((GotoStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_IfStmt:
				{
					this.VisitIfStmt((IfStmt)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_IndirectGotoStmt:
				// case CX_StmtClass.CX_StmtClass_MSDependentExistsStmt:
				// case CX_StmtClass.CX_StmtClass_NullStmt:
				// case CX_StmtClass.CX_StmtClass_OMPAtomicDirective:
				// case CX_StmtClass.CX_StmtClass_OMPBarrierDirective:
				// case CX_StmtClass.CX_StmtClass_OMPCancelDirective:
				// case CX_StmtClass.CX_StmtClass_OMPCancellationPointDirective:
				// case CX_StmtClass.CX_StmtClass_OMPCriticalDirective:
				// case CX_StmtClass.CX_StmtClass_OMPFlushDirective:
				// case CX_StmtClass.CX_StmtClass_OMPDistributeDirective:
				// case CX_StmtClass.CX_StmtClass_OMPDistributeParallelForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPDistributeParallelForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPDistributeSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPParallelForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPParallelForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetParallelForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetTeamsDistributeDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetTeamsDistributeParallelForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetTeamsDistributeParallelForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetTeamsDistributeSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskLoopDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskLoopSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTeamsDistributeDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTeamsDistributeParallelForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTeamsDistributeParallelForSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTeamsDistributeSimdDirective:
				// case CX_StmtClass.CX_StmtClass_OMPMasterDirective:
				// case CX_StmtClass.CX_StmtClass_OMPOrderedDirective:
				// case CX_StmtClass.CX_StmtClass_OMPParallelDirective:
				// case CX_StmtClass.CX_StmtClass_OMPParallelSectionsDirective:
				// case CX_StmtClass.CX_StmtClass_OMPSectionDirective:
				// case CX_StmtClass.CX_StmtClass_OMPSectionsDirective:
				// case CX_StmtClass.CX_StmtClass_OMPSingleDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetDataDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetEnterDataDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetExitDataDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetParallelDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetParallelForDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetTeamsDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTargetUpdateDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskgroupDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskwaitDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTaskyieldDirective:
				// case CX_StmtClass.CX_StmtClass_OMPTeamsDirective:
				// case CX_StmtClass.CX_StmtClass_ObjCAtCatchStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCAtFinallyStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCAtSynchronizedStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCAtThrowStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCAtTryStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCAutoreleasePoolStmt:
				// case CX_StmtClass.CX_StmtClass_ObjCForCollectionStmt:

				case CX_StmtClass.CX_StmtClass_ReturnStmt:
				{
					this.VisitReturnStmt((ReturnStmt)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_SEHExceptStmt:
				// case CX_StmtClass.CX_StmtClass_SEHFinallyStmt:
				// case CX_StmtClass.CX_StmtClass_SEHLeaveStmt:
				// case CX_StmtClass.CX_StmtClass_SEHTryStmt:

				case CX_StmtClass.CX_StmtClass_CaseStmt:
				{
					this.VisitCaseStmt((CaseStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_DefaultStmt:
				{
					this.VisitDefaultStmt((DefaultStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_SwitchStmt:
				{
					this.VisitSwitchStmt((SwitchStmt)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_AttributedStmt:
				// case CX_StmtClass.CX_StmtClass_BinaryConditionalOperator:

				case CX_StmtClass.CX_StmtClass_ConditionalOperator:
				{
					this.VisitConditionalOperator((ConditionalOperator)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_AddrLabelExpr:
				// case CX_StmtClass.CX_StmtClass_ArrayInitIndexExpr:
				// case CX_StmtClass.CX_StmtClass_ArrayInitLoopExpr:

				case CX_StmtClass.CX_StmtClass_ArraySubscriptExpr:
				{
					this.VisitArraySubscriptExpr((ArraySubscriptExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_ArrayTypeTraitExpr:
				// case CX_StmtClass.CX_StmtClass_AsTypeExpr:
				// case CX_StmtClass.CX_StmtClass_AtomicExpr:

				case CX_StmtClass.CX_StmtClass_BinaryOperator:
				case CX_StmtClass.CX_StmtClass_CompoundAssignOperator:
				{
					this.VisitBinaryOperator((BinaryOperator)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_BlockExpr:
				// case CX_StmtClass.CX_StmtClass_CXXBindTemporaryExpr:

				case CX_StmtClass.CX_StmtClass_CXXBoolLiteralExpr:
				{
					this.VisitCXXBoolLiteralExpr((CXXBoolLiteralExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_CXXConstructExpr:
				{
					this.VisitCXXConstructExpr((CXXConstructExpr)stmt);
					break;
				}

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
					this.VisitCXXNullPtrLiteralExpr((CXXNullPtrLiteralExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_CXXPseudoDestructorExpr:
				// case CX_StmtClass.CX_StmtClass_CXXScalarValueInitExpr:
				// case CX_StmtClass.CX_StmtClass_CXXStdInitializerListExpr:

				case CX_StmtClass.CX_StmtClass_CXXThisExpr:
				{
					this.VisitCXXThisExpr((CXXThisExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_CXXThrowExpr:
				// case CX_StmtClass.CX_StmtClass_CXXTypeidExpr:
				// case CX_StmtClass.CX_StmtClass_CXXUnresolvedConstructExpr:

				case CX_StmtClass.CX_StmtClass_CXXUuidofExpr:
				{
					this.VisitCXXUuidofExpr((CXXUuidofExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_CallExpr:
				case CX_StmtClass.CX_StmtClass_CXXMemberCallExpr:
				{
					this.VisitCallExpr((CallExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_CUDAKernelCallExpr:

				case CX_StmtClass.CX_StmtClass_CXXOperatorCallExpr:
				{
					this.VisitCXXOperatorCallExpr((CXXOperatorCallExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_UserDefinedLiteral:
				// case CX_StmtClass.CX_StmtClass_BuiltinBitCastExpr:

				case CX_StmtClass.CX_StmtClass_CStyleCastExpr:
				case CX_StmtClass.CX_StmtClass_CXXDynamicCastExpr:
				case CX_StmtClass.CX_StmtClass_CXXReinterpretCastExpr:
				case CX_StmtClass.CX_StmtClass_CXXStaticCastExpr:
				{
					this.VisitExplicitCastExpr((ExplicitCastExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_CXXFunctionalCastExpr:
				{
					this.VisitCXXFunctionalCastExpr((CXXFunctionalCastExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_CXXConstCastExpr:
				{
					this.VisitCXXConstCastExpr((CXXConstCastExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_ObjCBridgedCastExpr:

				case CX_StmtClass.CX_StmtClass_ImplicitCastExpr:
				{
					this.VisitImplicitCastExpr((ImplicitCastExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_CharacterLiteral:
				{
					this.VisitCharacterLiteral((CharacterLiteral)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_ChooseExpr:
				// case CX_StmtClass.CX_StmtClass_CompoundLiteralExpr:
				// case CX_StmtClass.CX_StmtClass_ConvertVectorExpr:
				// case CX_StmtClass.CX_StmtClass_CoawaitExpr:
				// case CX_StmtClass.CX_StmtClass_CoyieldExpr:

				case CX_StmtClass.CX_StmtClass_DeclRefExpr:
				{
					this.VisitDeclRefExpr((DeclRefExpr)stmt);
					break;
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
					this.VisitFloatingLiteral((FloatingLiteral)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_ConstantExpr:
				// case CX_StmtClass.CX_StmtClass_ExprWithCleanups:
				// case CX_StmtClass.CX_StmtClass_FunctionParmPackExpr:
				// case CX_StmtClass.CX_StmtClass_GNUNullExpr:
				// case CX_StmtClass.CX_StmtClass_GenericSelectionExpr:
				// case CX_StmtClass.CX_StmtClass_ImaginaryLiteral:

				case CX_StmtClass.CX_StmtClass_ImplicitValueInitExpr:
				{
					this.VisitImplicitValueInitExpr((ImplicitValueInitExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_InitListExpr:
				{
					this.VisitInitListExpr((InitListExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_IntegerLiteral:
				{
					this.VisitIntegerLiteral((IntegerLiteral)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_LambdaExpr:
				// case CX_StmtClass.CX_StmtClass_MSPropertyRefExpr:
				// case CX_StmtClass.CX_StmtClass_MSPropertySubscriptExpr:
				// case CX_StmtClass.CX_StmtClass_MaterializeTemporaryExpr:

				case CX_StmtClass.CX_StmtClass_MemberExpr:
				{
					this.VisitMemberExpr((MemberExpr)stmt);
					break;
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
					this.VisitParenExpr((ParenExpr)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_ParenListExpr:
				// case CX_StmtClass.CX_StmtClass_PredefinedExpr:
				// case CX_StmtClass.CX_StmtClass_PseudoObjectExpr:
				// case CX_StmtClass.CX_StmtClass_ShuffleVectorExpr:
				// case CX_StmtClass.CX_StmtClass_SizeOfPackExpr:
				// case CX_StmtClass.CX_StmtClass_SourceLocExpr:
				// case CX_StmtClass.CX_StmtClass_StmtExpr:

				case CX_StmtClass.CX_StmtClass_StringLiteral:
				{
					this.VisitStringLiteral((StringLiteral)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_SubstNonTypeTemplateParmExpr:
				// case CX_StmtClass.CX_StmtClass_SubstNonTypeTemplateParmPackExpr:
				// case CX_StmtClass.CX_StmtClass_TypeTraitExpr:
				// case CX_StmtClass.CX_StmtClass_TypoExpr:

				case CX_StmtClass.CX_StmtClass_UnaryExprOrTypeTraitExpr:
				{
					this.VisitUnaryExprOrTypeTraitExpr((UnaryExprOrTypeTraitExpr)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_UnaryOperator:
				{
					this.VisitUnaryOperator((UnaryOperator)stmt);
					break;
				}

				// case CX_StmtClass.CX_StmtClass_VAArgExpr:

				case CX_StmtClass.CX_StmtClass_LabelStmt:
				{
					this.VisitLabelStmt((LabelStmt)stmt);
					break;
				}

				case CX_StmtClass.CX_StmtClass_WhileStmt:
				{
					this.VisitWhileStmt((WhileStmt)stmt);
					break;
				}

				default:
				{
					var context = string.Empty;

					if (this.IsPrevContextDecl<NamedDecl>(out var namedDecl))
					{
						context = $" in {this.GetCursorQualifiedName(namedDecl)}";
					}

					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported statement: '{stmt.StmtClass}'{context}. Generated bindings may be incomplete.", stmt);
					break;
				}
			}
		}

		private void VisitStmts(IReadOnlyList<Stmt> stmts)
		{
			var lastIndex = stmts.Count - 1;
			var previousStmt = null as Stmt;

			for (var i = 0; i < lastIndex; i++)
			{
				var stmt = stmts[i];

				if ((previousStmt is DeclStmt) && !(stmt is DeclStmt))
				{
					this._outputBuilder.NeedsNewline = true;
				}

				this._outputBuilder.WriteIndentation();
				this._outputBuilder.NeedsSemicolon = true;
				this._outputBuilder.NeedsNewline = true;

				this.Visit(stmts[i]);

				this._outputBuilder.WriteSemicolonIfNeeded();
				this._outputBuilder.WriteNewline();

				previousStmt = stmt;
			}

			if (lastIndex != -1)
			{
				var stmt = stmts[lastIndex];

				if ((previousStmt is DeclStmt) && !(stmt is DeclStmt))
				{
					this._outputBuilder.NeedsNewline = true;
				}

				this._outputBuilder.WriteIndentation();
				this._outputBuilder.NeedsSemicolon = true;
				this._outputBuilder.NeedsNewline = true;

				this.Visit(stmt);

				this._outputBuilder.WriteSemicolonIfNeeded();
				this._outputBuilder.WriteNewlineIfNeeded();
			}
		}

		private void VisitStringLiteral(StringLiteral stringLiteral)
		{
			switch (stringLiteral.Kind)
			{
				case CX_CharacterKind.CX_CLK_Ascii:
				case CX_CharacterKind.CX_CLK_UTF8:
				{
					this._outputBuilder.Write("new byte[] { ");

					var bytes = Encoding.UTF8.GetBytes(stringLiteral.String);

					foreach (var b in bytes)
					{
						this._outputBuilder.Write("0x");
						this._outputBuilder.Write(b.ToString("X2"));
						this._outputBuilder.Write(", ");
					}

					this._outputBuilder.Write("0x00 }");
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
					this._outputBuilder.Write('"');
					this._outputBuilder.Write(this.EscapeString(stringLiteral.String));
					this._outputBuilder.Write('"');
					break;
				}

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported string literal kind: '{stringLiteral.Kind}'. Generated bindings may be incomplete.", stringLiteral);
					break;
				}
			}
		}

		private void VisitSwitchStmt(SwitchStmt switchStmt)
		{
			this._outputBuilder.Write("switch (");

			this.Visit(switchStmt.Cond);

			this._outputBuilder.WriteLine(')');

			this.VisitBody(switchStmt.Body);
		}

		private void VisitUnaryExprOrTypeTraitExpr(UnaryExprOrTypeTraitExpr unaryExprOrTypeTraitExpr)
		{
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
					if ((size32 == size64) && this.IsPrevContextDecl<VarDecl>(out _))
					{
						this._outputBuilder.Write(size32);
					}
					else
					{
						if (this._outputBuilder.Name == this._config.MethodClassName)
						{
							this._isMethodClassUnsafe = true;
						}

						var parentType = null as Type;

						if (this.IsPrevContextStmt<CallExpr>(out var callExpr))
						{
							var index = callExpr.Args.IndexOf(unaryExprOrTypeTraitExpr);
							var calleeDecl = callExpr.CalleeDecl;

							if (calleeDecl is FunctionDecl functionDecl)
							{
								parentType = functionDecl.Parameters[index].Type.CanonicalType;
							}
							else
							{
								this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported callee declaration: '{calleeDecl?.Kind}'. Generated bindings may be incomplete.", calleeDecl);
							}

						}
						else if (this.IsPrevContextStmt<Expr>(out var expr))
						{
							parentType = expr.Type.CanonicalType;
						}
						else if (this.IsPrevContextDecl<TypeDecl>(out var typeDecl))
						{
							parentType = typeDecl.TypeForDecl.CanonicalType;
						}

						var needsCast = false;
						var typeName = this.GetRemappedTypeName(unaryExprOrTypeTraitExpr, context: null, argumentType, out _);

						if (parentType != null)
						{
							needsCast = (parentType.Kind == CXTypeKind.CXType_UInt);
							needsCast |= (parentType.Kind == CXTypeKind.CXType_ULong);
							needsCast &= !this.IsSupportedFixedSizedBufferType(typeName);
							needsCast &= (argumentType.CanonicalType.Kind != CXTypeKind.CXType_Enum);
						}

						if (needsCast)
						{
							this._outputBuilder.Write("(uint)(");
						}

						this._outputBuilder.Write("sizeof(");
						this._outputBuilder.Write(typeName);
						this._outputBuilder.Write(')');

						if (needsCast)
						{
							this._outputBuilder.Write(')');
						}
					}
					break;
				}

				case CX_UnaryExprOrTypeTrait.CX_UETT_AlignOf:
				case CX_UnaryExprOrTypeTrait.CX_UETT_PreferredAlignOf:
				{
					if (alignment32 == alignment64)
					{
						this._outputBuilder.Write(alignment32);
					}
					else
					{
						this._outputBuilder.Write("Environment.Is64BitProcess ? ");
						this._outputBuilder.Write(alignment64);
						this._outputBuilder.Write(" : ");
						this._outputBuilder.Write(alignment32);
					}

					break;
				}

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported unary or type trait expression: '{unaryExprOrTypeTraitExpr.Kind}'. Generated bindings may be incomplete.", unaryExprOrTypeTraitExpr);
					break;
				}
			}
		}

		private void VisitUnaryOperator(UnaryOperator unaryOperator)
		{
			switch (unaryOperator.Opcode)
			{
				case CX_UnaryOperatorKind.CX_UO_PostInc:
				case CX_UnaryOperatorKind.CX_UO_PostDec:
				{
					this.Visit(unaryOperator.SubExpr);
					this._outputBuilder.Write(unaryOperator.OpcodeStr);
					break;
				}

				case CX_UnaryOperatorKind.CX_UO_PreInc:
				case CX_UnaryOperatorKind.CX_UO_PreDec:
				case CX_UnaryOperatorKind.CX_UO_Deref:
				case CX_UnaryOperatorKind.CX_UO_Plus:
				case CX_UnaryOperatorKind.CX_UO_Minus:
				case CX_UnaryOperatorKind.CX_UO_Not:
				{
					this._outputBuilder.Write(unaryOperator.OpcodeStr);
					this.Visit(unaryOperator.SubExpr);
					break;
				}

				case CX_UnaryOperatorKind.CX_UO_LNot:
				{
					var subExpr = this.GetExprAsWritten(unaryOperator.SubExpr, removeParens: true);
					var canonicalType = subExpr.Type.CanonicalType;

					if (canonicalType.IsIntegerType && (canonicalType.Kind != CXTypeKind.CXType_Bool))
					{
						this.Visit(subExpr);
						this._outputBuilder.Write(" == 0");
					}
					else if ((canonicalType is PointerType) || (canonicalType is ReferenceType))
					{
						this.Visit(subExpr);
						this._outputBuilder.Write(" == null");
					}
					else
					{
						this._outputBuilder.Write(unaryOperator.OpcodeStr);
						this.Visit(unaryOperator.SubExpr);
					}
					break;
				}

				case CX_UnaryOperatorKind.CX_UO_AddrOf:
				{
					if ((unaryOperator.SubExpr is DeclRefExpr declRefExpr) && (declRefExpr.Decl.Type is LValueReferenceType))
					{
						this.Visit(unaryOperator.SubExpr);
					}
					else
					{
						this._outputBuilder.Write(unaryOperator.OpcodeStr);
						this.Visit(unaryOperator.SubExpr);
					}
					break;
				}

				default:
				{
					this.AddDiagnostic(DiagnosticLevel.Error, $"Unsupported unary operator opcode: '{unaryOperator.OpcodeStr}'. Generated bindings may be incomplete.", unaryOperator);
					break;
				}
			}
		}

		private void VisitWhileStmt(WhileStmt whileStmt)
		{
			this._outputBuilder.Write("while (");

			this.Visit(whileStmt.Cond);

			this._outputBuilder.WriteLine(')');

			this.VisitBody(whileStmt.Body);
		}
	}
}
