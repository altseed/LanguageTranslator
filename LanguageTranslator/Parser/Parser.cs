﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using LanguageTranslator.Definition;

namespace LanguageTranslator.Parser
{
    class Parser
    {
		readonly string swig_namespace_keyword = "asd.swig";

        Definition.Definitions definitions = null;
        public List<string> TypesNotParsed = new List<string>();
		public List<string> TypesWhoseMemberNotParsed = new List<string>();
		public List<string> TypesWhosePrivateNotParsed = new List<string>();
		public List<string> TypesNotExported = new List<string>();

        public Definition.Definitions Parse(string[] pathes, string[] dlls)
        {
            definitions = new Definition.Definitions();

            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var path in pathes)
            {
                var tree = CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(path), null, path);
                syntaxTrees.Add(tree);
            }

            var assemblyPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location);

            var mscorelib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "mscorlib.dll"));
			var systemlib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.dll"));
            var systemCorelib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "System.Core.dll"));

            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

			List<MetadataReference> references = new List<MetadataReference>();
			references.Add(mscorelib);
			references.Add(systemlib);
            references.Add(systemCorelib);

            // DLL読み込み
            foreach (var dll in dlls)
			{
				if (System.IO.File.Exists(dll))
				{
					var lib = MetadataReference.CreateFromFile(dll);
					references.Add(lib);
				}
			}
			
			
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                        "Compilation",
                        syntaxTrees: syntaxTrees.ToArray(),
                        references: references.ToArray(),
                        options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                                                  Microsoft.CodeAnalysis.OutputKind.ConsoleApplication));

			// デフォルト追加
			definitions.AddDefault();

            // 定義のみ取得
            foreach (var tree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(tree);

                var decl = semanticModel.GetDeclarationDiagnostics();
                var methodBodies = semanticModel.GetMethodBodyDiagnostics();
                var root = semanticModel.SyntaxTree.GetRoot();

                ParseRoot(root, semanticModel);
            }

            var blockParser = new BlockParser();
            blockParser.Parse(definitions, syntaxTrees, compilation);

            return definitions;
        }

		private void ParseTypeParameters(ITypeParameters def, TypeParameterListSyntax typeParameterListSyntax, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SemanticModel semanticModel)
		{
			#region Generics
			if (typeParameterListSyntax != null)
			{
				foreach (var item in typeParameterListSyntax.Parameters)
				{
					def.TypeParameters.Add(new TypeParameterDef
					{
						Name = item.Identifier.ValueText,
					});
				}
			}

			if (constraintClauses != null)
			{
				foreach (var item in constraintClauses)
				{
					var def_ = def.TypeParameters.First(x => x.Name == item.Name.Identifier.ValueText);
					foreach (var constraint in item.Constraints)
					{
						var classOrStruct = constraint as ClassOrStructConstraintSyntax;
						var type = constraint as TypeConstraintSyntax;

						if (classOrStruct != null)
						{
							def_.IsConstraintedAsValueType = classOrStruct.ClassOrStructKeyword.ValueText == "struct";
							def_.IsConstraintedAsReferenceType = classOrStruct.ClassOrStructKeyword.ValueText == "class";
						}
						if (type != null)
						{
							def_.BaseTypeConstraints.Add(ParseTypeSpecifier(type.Type, semanticModel));
						}
					}
				}
			}
			#endregion
		}

        private void ParseRoot(SyntaxNode root, SemanticModel semanticModel)
        {
            var compilationUnitSyntax = root as CompilationUnitSyntax;

            var usings = compilationUnitSyntax.Usings;
            var members = compilationUnitSyntax.Members;

			ParseMembers("", members, semanticModel);
        }

		private void ParseMembers(string namespace_, SyntaxList<MemberDeclarationSyntax> members, SemanticModel semanticModel)
		{
			foreach (var member in members)
			{
				var namespaceSyntax = member as NamespaceDeclarationSyntax;
				var classSyntax = member as ClassDeclarationSyntax;
				var enumSyntax = member as EnumDeclarationSyntax;
				var structSyntax = member as StructDeclarationSyntax;
				var interfaceSyntax = member as InterfaceDeclarationSyntax;

				if (namespaceSyntax != null)
				{
					ParseNamespace(namespaceSyntax, semanticModel);
				}
				if (enumSyntax != null)
				{
					ParseEnum(namespace_, enumSyntax, semanticModel);
				}
				if (classSyntax != null)
				{
					ParseClass(namespace_, classSyntax, semanticModel);
				}
				if (structSyntax != null)
				{
					ParseStrcut(namespace_, structSyntax, semanticModel);
				}
				if (interfaceSyntax != null)
				{
					ParseInterface(namespace_, interfaceSyntax, semanticModel);
				}
			}
		}

        private void ParseNamespace(NamespaceDeclarationSyntax namespaceSyntax, SemanticModel semanticModel)
        {
            var members = namespaceSyntax.Members;

            // TODO 正しいnamespaceの処理
            var nameSyntax_I = namespaceSyntax.Name as IdentifierNameSyntax;
            var nameSyntax_Q = namespaceSyntax.Name as QualifiedNameSyntax;

            string namespace_ = string.Empty;
            if (nameSyntax_I != null) namespace_ = nameSyntax_I.Identifier.ValueText;
            if (nameSyntax_Q != null)
            {
                namespace_ = nameSyntax_Q.ToFullString().Trim();
            }

			ParseMembers(namespace_, members, semanticModel);
        }

        private void ParseEnum(string namespace_, EnumDeclarationSyntax enumSyntax, SemanticModel semanticModel)
        {
            var enumDef = new EnumDef();

            // 名称
            enumDef.Name = enumSyntax.Identifier.ValueText;

            // ネームスペース
            enumDef.Namespace = namespace_;

            // swig
			enumDef.IsDefinedBySWIG = namespace_.Contains(swig_namespace_keyword);

            foreach (var member in enumSyntax.Members)
            {
                var def = ParseEnumMember(member, semanticModel);
                enumDef.Members.Add(def);
            }

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(enumSyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			enumDef.Summary = SummaryComment.Parse(xml);

			definitions.Enums.Add(enumDef);
        }

        private EnumMemberDef ParseEnumMember(EnumMemberDeclarationSyntax syntax, SemanticModel semanticModel)
        {
            EnumMemberDef dst = new EnumMemberDef();

            // 名称
            dst.Name = syntax.Identifier.ValueText;
            dst.Internal = syntax;

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(syntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			dst.Summary = SummaryComment.Parse(xml);

			return dst;
        }


        private void ParseTypeDeclaration(TypeDef typeDef, TypeDeclarationSyntax typeSyntax, SemanticModel semanticModel)
        {
            typeDef.AccessLevel = ParseAccessLevel(typeSyntax.Modifiers) ?? AccessLevel.Internal;

			#region Generics
			ParseTypeParameters(typeDef, typeSyntax.TypeParameterList, typeSyntax.ConstraintClauses, semanticModel);
			#endregion

			var isMemberNotParsed = TypesWhoseMemberNotParsed.Contains(typeDef.Namespace + "." + typeDef.Name);
            var isPrivateNotParsed = TypesWhosePrivateNotParsed.Contains(typeDef.Namespace + "." + typeDef.Name);

			// 構造体は現状、継承なしとする
			if (typeSyntax.BaseList != null && typeDef.BaseTypes != null)
            {
                foreach (var item in typeSyntax.BaseList.Types)
                {
                    typeDef.BaseTypes.Add(ParseTypeSpecifier(item.Type, semanticModel));
                }
            }

            #region Members
			Func<AccessLevel, bool> isSkipped = level => (isPrivateNotParsed && level == AccessLevel.Private) || isMemberNotParsed;
            foreach (var member in typeSyntax.Members)
            {
                var methodSyntax = member as MethodDeclarationSyntax;
                if (methodSyntax != null)
                {
                    var method = ParseMethod(methodSyntax, semanticModel);
                    if (!isSkipped(method.AccessLevel))
                    {
                        typeDef.Methods.Add(method);
                    }
                }

                var propertySyntax = member as PropertyDeclarationSyntax;
                if (propertySyntax != null)
                {
                    var property = ParseProperty(propertySyntax, semanticModel);
                    if (!isSkipped(property.AccessLevel))
                    {
                        typeDef.Properties.Add(property);
                    }
                }

                var fieldSyntax = member as FieldDeclarationSyntax;
                if (fieldSyntax != null)
                {
                    var field = ParseField(fieldSyntax, semanticModel);
                    if (!isSkipped(field.AccessLevel))
                    {
                        typeDef.Fields.Add(field);
                    }
                }

                var operatorSyntax = member as OperatorDeclarationSyntax;
                if (operatorSyntax != null)
                {
                    var @operator = ParseOperator(operatorSyntax, semanticModel);
                    if (!isSkipped(@operator.AccessLevel))
                    {
                        typeDef.Operators.Add(@operator);
                    }
                }

                var constructorSyntax = member as ConstructorDeclarationSyntax;
                if (constructorSyntax != null)
                {
                    var constructor = ParseConstructor(constructorSyntax, semanticModel);
                    if (!isSkipped(constructor.AccessLevel))
                    {
                        typeDef.Constructors.Add(constructor);
                    }
                }

                var destructorSyntax = member as DestructorDeclarationSyntax;
                if (destructorSyntax != null)
                {
                    typeDef.Destructors.Add(ParseDestructor(destructorSyntax, semanticModel));
                }
            } 
            #endregion
        }

        private static AccessLevel? ParseAccessLevel(SyntaxTokenList modifiers)
        {
            var modifiersText = modifiers.Select(x => x.ValueText);
            if (modifiersText.Contains("protected"))
            {
                if (modifiersText.Contains("internal"))
                {
                    return AccessLevel.ProtectedInternal;
                }
                else
                {
                    return AccessLevel.Protected;
                }
            }
            else if (modifiersText.Contains("public"))
            {
                return AccessLevel.Public;
            }
            else if (modifiersText.Contains("private"))
            {
                return AccessLevel.Private;
            }
            else if (modifiersText.Contains("internal"))
            {
                return AccessLevel.Internal;
            }
            else
            {
                return null;
            }
        }

        private void ParseClass(string namespace_, ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
        {
            var classDef = new ClassDef();

            // swig
			classDef.IsDefinedBySWIG = namespace_.Contains(swig_namespace_keyword);

            classDef.Namespace = namespace_;
            classDef.Name = classSyntax.Identifier.ValueText;

            var fullName = namespace_ + "." + classDef.Name;

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

			if (TypesNotExported.Contains(fullName))
			{
				classDef.IsExported = false;
			}

            var partial = definitions.Classes.FirstOrDefault(x => x.Namespace + "." + x.Name == fullName);
            if (partial != null)
            {
                classDef = partial;
            }

            if (classSyntax.Modifiers.Any(x => x.ValueText == "abstract"))
            {
                classDef.IsAbstract = true;
            }

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(classSyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			classDef.Summary = SummaryComment.Parse(xml);

			ParseTypeDeclaration(classDef, classSyntax, semanticModel);

            definitions.Classes.Add(classDef);
        }

        private void ParseStrcut(string namespace_, StructDeclarationSyntax structSyntax, SemanticModel semanticModel)
        {
            var structDef = new StructDef();
            structDef.Internal = structSyntax;

            structDef.Namespace = namespace_;
            structDef.Name = structSyntax.Identifier.ValueText;

            var fullName = namespace_ + "." + structDef.Name;

            {
                var partial = definitions.Structs.FirstOrDefault(x => x.Namespace + "." + x.Name == fullName);
                if (partial != null)
                {
                    structDef = partial;
                }
            }

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(structSyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			structDef.Summary = SummaryComment.Parse(xml);

			ParseTypeDeclaration(structDef, structSyntax, semanticModel);

            definitions.Structs.Add(structDef);
        }

        private void ParseInterface(string namespace_, InterfaceDeclarationSyntax interfaceSyntax, SemanticModel semanticModel)
        {
            var interfaceDef = new InterfaceDef();
            interfaceDef.Namespace = namespace_;
            interfaceDef.Name = interfaceSyntax.Identifier.ValueText;

            var fullName = interfaceDef.Namespace + "." + interfaceDef.Name;

            if (TypesNotParsed.Contains(fullName))
            {
                return;
            }

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(interfaceSyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			interfaceDef.Summary = SummaryComment.Parse(xml);

			ParseTypeDeclaration(interfaceDef, interfaceSyntax, semanticModel);

            definitions.Interfaces.Add(interfaceDef);
        }


        private FieldDef ParseField(FieldDeclarationSyntax fieldSyntax, SemanticModel semanticModel)
        {
            var fieldDef = new FieldDef();

            if (fieldSyntax.Declaration.Variables.Count != 1)
            {
                var span = fieldSyntax.SyntaxTree.GetLineSpan(fieldSyntax.Declaration.Variables.Span);
                throw new ParseException(string.Format("{0} : 変数の複数同時宣言は禁止です。", span));
            }

			var declaration = fieldSyntax.Declaration;
			var variable = fieldSyntax.Declaration.Variables[0];

			// 主にfixed配列対象
			ArgumentSyntax arguments = null;
			if (variable.ArgumentList != null)
			{
				arguments = variable.ArgumentList.Arguments.FirstOrDefault();
			}	

			var type = ParseTypeSpecifier(declaration.Type, semanticModel);

			// 無理やり書き換える
			if(arguments != null)
			{
				if(type is SimpleType)
				{
					var at = new ArrayType();
					at.BaseType = (SimpleType)type;
					type = at;
				}

				fieldDef.Argument = arguments.ToString();
			}

            fieldDef.Internal = fieldSyntax;
			fieldDef.Name = variable.Identifier.ValueText;
            fieldDef.Type = type;
            fieldDef.AccessLevel = ParseAccessLevel(fieldSyntax.Modifiers) ?? AccessLevel.Private;
            fieldDef.IsStatic = fieldSyntax.Modifiers.Any(x => x.ValueText == "static");

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(fieldSyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			fieldDef.Summary = SummaryComment.Parse(xml);

			return fieldDef;
        }

        private PropertyDef ParseProperty(PropertyDeclarationSyntax propertySyntax, SemanticModel semanticModel)
        {
            var propertyDef = new PropertyDef();
            propertyDef.Internal = propertySyntax;

            propertyDef.Name = propertySyntax.Identifier.ValueText;
            propertyDef.Type = ParseTypeSpecifier(propertySyntax.Type, semanticModel);
            propertyDef.AccessLevel = ParseAccessLevel(propertySyntax.Modifiers) ?? AccessLevel.Private;
            propertyDef.IsStatic = propertySyntax.Modifiers.Any(x => x.ValueText == "static");

            foreach (var accessor in propertySyntax.AccessorList.Accessors)
            {
                var acc = new AccessorDef();
                acc.Internal = accessor;
                acc.AccessLevel = ParseAccessLevel(accessor.Modifiers) ?? propertyDef.AccessLevel;

                if (accessor.Keyword.Text == "get")
                {
                    propertyDef.Getter = acc;
                }
                else if (accessor.Keyword.Text == "set")
                {
                    propertyDef.Setter = acc;
                }
            }

			// Summary
			var declaredSymbol = semanticModel.GetDeclaredSymbol(propertySyntax);
			var xml = declaredSymbol?.GetDocumentationCommentXml();
			propertyDef.Summary = SummaryComment.Parse(xml);

			return propertyDef;
        }

        private MethodDef ParseMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel)
        {
            var methodDef = new MethodDef();
            methodDef.Internal = methodSyntax;

            methodDef.Name = methodSyntax.Identifier.ValueText;
            methodDef.ReturnType = ParseTypeSpecifier(methodSyntax.ReturnType, semanticModel);
            methodDef.AccessLevel = ParseAccessLevel(methodSyntax.Modifiers) ?? AccessLevel.Private;
            methodDef.IsStatic = methodSyntax.Modifiers.Any(x => x.ValueText == "static");
			methodDef.IsAbstract = methodSyntax.Modifiers.Any(x => x.ValueText == "abstract");

            foreach (var parameter in methodSyntax.ParameterList.Parameters)
            {
                methodDef.Parameters.Add(ParseParameter(parameter, semanticModel));
            }


			#region Generics
			ParseTypeParameters(methodDef, methodSyntax.TypeParameterList, methodSyntax.ConstraintClauses, semanticModel);
			#endregion

            return methodDef;
        }

        private OperatorDef ParseOperator(OperatorDeclarationSyntax operatorSyntax, SemanticModel semanticModel)
        {
            var operatorDef = new OperatorDef();
            operatorDef.Operator = operatorSyntax.OperatorToken.ValueText;
            operatorDef.ReturnType = ParseTypeSpecifier(operatorSyntax.ReturnType, semanticModel);
            operatorDef.AccessLevel = ParseAccessLevel(operatorSyntax.Modifiers) ?? AccessLevel.Private;

            foreach (var item in operatorSyntax.ParameterList.Parameters)
            {
                operatorDef.Parameters.Add(ParseParameter(item, semanticModel));
            }

            return operatorDef;
        }

        private ParameterDef ParseParameter(ParameterSyntax parameter, SemanticModel semanticModel)
        {
            var parameterDef = new ParameterDef();
            parameterDef.Name = parameter.Identifier.ValueText;
            parameterDef.Type = ParseTypeSpecifier(parameter.Type, semanticModel);

            return parameterDef;
        }

        private ConstructorDef ParseConstructor(ConstructorDeclarationSyntax constructorSyntax, SemanticModel semanticModel)
        {
            var constructorDef = new ConstructorDef();

			constructorDef.Internal = constructorSyntax;

			constructorDef.AccessLevel = ParseAccessLevel(constructorSyntax.Modifiers) ?? AccessLevel.Private;
            constructorDef.IsStatic = constructorSyntax.Modifiers.Any(x => x.ValueText == "static");

            if (constructorSyntax.Initializer != null)
            {
                constructorDef.Initializer = new ConstructorInitializer
                {
                    ThisOrBase = constructorSyntax.Initializer.ThisOrBaseKeyword.ValueText,
                };

				constructorDef.Initializer.Internal = constructorSyntax.Initializer;
            }

            foreach (var parameter in constructorSyntax.ParameterList.Parameters)
            {
                constructorDef.Parameters.Add(ParseParameter(parameter, semanticModel));
            }

            return constructorDef;
        }

        private DestructorDef ParseDestructor(DestructorDeclarationSyntax destructorSyntax, SemanticModel semanticModel)
        {
			var def = new DestructorDef();
			def.Internal = destructorSyntax;
			return def;
        }


        private TypeSpecifier ParseTypeSpecifier(TypeSyntax typeSyntax, SemanticModel semanticModel)
        {
            if (typeSyntax is ArrayTypeSyntax)
            {
                try
                {
                    return new ArrayType
                    {
                        BaseType = (SimpleType)ParseTypeSpecifier(((ArrayTypeSyntax)typeSyntax).ElementType, semanticModel),
                    };
                }
                catch (InvalidCastException)
                {
                    throw new ParseException("SimpleType以外の配列は使用禁止です。");
                }
            }
            else if (typeSyntax is NullableTypeSyntax)
            {
                try
                {
                    return new NullableType
                    {
                        BaseType = (SimpleType)ParseTypeSpecifier(((NullableTypeSyntax)typeSyntax).ElementType, semanticModel),
                    };
                }
                catch (InvalidCastException)
                {
                    throw new ParseException("SimpleType以外のnull可能型は使用禁止です。");
                }
            }
            else if (typeSyntax is GenericNameSyntax)
            {
                var g = (GenericNameSyntax)typeSyntax;
                
                {
					var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
					var symbolInfo = semanticModel.GetSymbolInfo(typeSyntax);
					
                    return new GenericType
                    {
                        OuterType = new SimpleType
                        {
							Namespace = Utils.ToStr(typeInfo.Type.ContainingNamespace),
                            TypeName = g.Identifier.ValueText,
                        },
                        InnerType = g.TypeArgumentList.Arguments.Select(x => ParseTypeSpecifier(x, semanticModel)).ToList(),
                    };
                }
            }
            else
            {
                var type = semanticModel.GetTypeInfo(typeSyntax);

                var specifier = new SimpleType
                {
                    Namespace = Utils.ToStr(type.Type.ContainingNamespace),
                    TypeName = type.Type.Name,
                };

                switch (type.Type.TypeKind)
                {
                case TypeKind.Class:
                    specifier.TypeKind = SimpleTypeKind.Class;
                    break;
                case TypeKind.Enum:
                    specifier.TypeKind = SimpleTypeKind.Enum;
                    break;
                case TypeKind.Error:
                    specifier.TypeKind = SimpleTypeKind.Error;
                    break;
                case TypeKind.Interface:
                    specifier.TypeKind = SimpleTypeKind.Interface;
                    break;
                case TypeKind.Struct:
                    specifier.TypeKind = SimpleTypeKind.Struct;
                    break;
                case TypeKind.TypeParameter:
                    specifier.TypeKind = SimpleTypeKind.TypeParameter;
					// 基本的にGenericsの型なのでNamespaceは必要ない
					specifier.Namespace = string.Empty;
                    break;
                default:
                    specifier.TypeKind = SimpleTypeKind.Other;
                    break;
                }

                return specifier;
            }
        }
    }
}
