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
		Definition.Definitions definitions = null;

		public Definition.Definitions Parse(string[] pathes)
		{
			definitions = new Definition.Definitions();

			List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
			foreach (var path in pathes)
			{
				var tree = CSharpSyntaxTree.ParseText(System.IO.File.ReadAllText(path));
				syntaxTrees.Add(tree);
			}

			var assemblyPath = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location);

			var mscorelib = MetadataReference.CreateFromFile(System.IO.Path.Combine(assemblyPath, "mscorlib.dll"));

			var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
						"Compilation",
						syntaxTrees: syntaxTrees.ToArray(),
						references: new[] { mscorelib },
						options: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
												  Microsoft.CodeAnalysis.OutputKind.ConsoleApplication));

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

		void ParseRoot(SyntaxNode root, SemanticModel semanticModel)
		{
			var compilationUnitSyntax = root as CompilationUnitSyntax;

			var usings = compilationUnitSyntax.Usings;
			var members = compilationUnitSyntax.Members;

			foreach (var member in members)
			{
				var namespaceSyntax = member as NamespaceDeclarationSyntax;
				ParseNamespace(namespaceSyntax, semanticModel);
			}
		}

		void ParseNamespace(NamespaceDeclarationSyntax namespaceSyntax, SemanticModel semanticModel)
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

			foreach (var member in members)
			{
				var classSyntax = member as ClassDeclarationSyntax;
				var enumSyntax = member as EnumDeclarationSyntax;
				var structSyntax = member as StructDeclarationSyntax;

				if (enumSyntax != null)
				{
					ParseEnum(namespace_, enumSyntax, semanticModel);
				}
				if (classSyntax != null)
				{
					ParseClass(namespace_, classSyntax);
				}
			}
		}

		void ParseClass(string namespace_, ClassDeclarationSyntax classSyntax)
		{
			var classDef = new ClassDef();

			// swig
			classDef.IsDefinedBySWIG = namespace_.Contains("ace.swig");

			classDef.Name = classSyntax.Identifier.ValueText;

			foreach (var member in classSyntax.Members)
			{
				var methodSyntax = member as MethodDeclarationSyntax;
			}

			definitions.Classes.Add(classDef);
		}

		void ParseEnum(string namespace_, EnumDeclarationSyntax enumSyntax, SemanticModel semanticModel)
		{
			var enumDef = new EnumDef();

			// 名称
			enumDef.Name = enumSyntax.Identifier.ValueText;

			// ネームスペース
			enumDef.Namespace = namespace_;

			// swig
			enumDef.IsDefinedBySWIG = namespace_.Contains("ace.swig");

			foreach(var member in enumSyntax.Members)
			{
				var def = ParseEnumMember(member, semanticModel);
				enumDef.Members.Add(def);
			}

			definitions.Enums.Add(enumDef);
		}

		EnumMemberDef ParseEnumMember(EnumMemberDeclarationSyntax syntax, SemanticModel semanticModel)
		{
			EnumMemberDef dst = new EnumMemberDef();

			// 名称
			dst.Name = syntax.Identifier.ValueText;
			dst.Internal = syntax;

			return dst;
		}
	}
}
