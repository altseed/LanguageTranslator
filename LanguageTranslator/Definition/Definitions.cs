﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LanguageTranslator.Definition
{
    enum AccessLevel
    {
        Public, Protected, Private, Internal, ProtectedInternal
    }

    class Definitions
    {
        public List<EnumDef> Enums = new List<EnumDef>();
        public List<ClassDef> Classes = new List<ClassDef>();
        public List<StructDef> Structs = new List<StructDef>();
        public List<InterfaceDef> Interfaces = new List<InterfaceDef>();

		public object Find(TypeSpecifier typeSpecifier)
		{
			if(typeSpecifier is SimpleType)
			{
				var t = typeSpecifier as SimpleType;
				if(t.TypeKind == SimpleTypeKind.Class)
				{
					return Classes.Where(_ => _.Name == t.TypeName && _.Namespace == _.Namespace).FirstOrDefault();
				}
			}

			return null;
		}

		public void AddDefault()
		{
			{
				ClassDef c = new ClassDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "List";

				{
					MethodDef m = new MethodDef();
					m.Name = "Add";
					m.Parameters.Add(new ParameterDef() { Name = "item" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Remove";
					m.Parameters.Add(new ParameterDef() { Name = "item" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Clear";
					c.Methods.Add(m);
				}

				{
					PropertyDef m = new PropertyDef();
					m.Name = "Count";
					c.Properties.Add(m);
				}
				c.IsDefinedDefault = true;
				Classes.Add(c);
			}

			{
				LinkedList<int> v = new LinkedList<int>();

				ClassDef c = new ClassDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "LinkedList";

				{
					MethodDef m = new MethodDef();
					m.Name = "AddLast";
					m.Parameters.Add(new ParameterDef() { Name = "value" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Remove";
					m.Parameters.Add(new ParameterDef() { Name = "value" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Contains";
					m.Parameters.Add(new ParameterDef() { Name = "value" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Clear";
					c.Methods.Add(m);
				}

				c.IsDefinedDefault = true;
				Classes.Add(c);
			}

			{
				Queue<int> v = new Queue<int>();

				ClassDef c = new ClassDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "Queue";

				{
					MethodDef m = new MethodDef();
					m.Name = "Enqueue";
					m.Parameters.Add(new ParameterDef() { Name = "item" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Dequeue";
					c.Methods.Add(m);
				}

				{
					PropertyDef m = new PropertyDef();
					m.Name = "Count";
					c.Properties.Add(m);
				}

				c.IsDefinedDefault = true;
				Classes.Add(c);
			}

			{
				Dictionary<int, int> v = new Dictionary<int, int>();
				ClassDef c = new ClassDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "Dictionary";

				{
					MethodDef m = new MethodDef();
					m.Name = "Add";
					m.Parameters.Add(new ParameterDef() { Name = "key" });
					m.Parameters.Add(new ParameterDef() { Name = "value" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "ContainsKey";
					m.Parameters.Add(new ParameterDef() { Name = "key" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Remove";
					m.Parameters.Add(new ParameterDef() { Name = "key" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Clear";
					c.Methods.Add(m);
				}

				{
					PropertyDef m = new PropertyDef();
					m.Name = "Values";
					c.Properties.Add(m);
				}

				c.IsDefinedDefault = true;
				Classes.Add(c);
			}

			{
				SortedList<int, int> v = new SortedList<int, int>();
				ClassDef c = new ClassDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "SortedList";

				{
					MethodDef m = new MethodDef();
					m.Name = "ContainsKey";
					m.Parameters.Add(new ParameterDef() { Name = "key" });
					c.Methods.Add(m);
				}

				c.IsDefinedDefault = true;
				Classes.Add(c);
			}

			{
				StructDef c = new StructDef();
				c.Namespace = "System.Collections.Generic";
				c.Name = "KeyValuePair";

				{
					PropertyDef m = new PropertyDef();
					m.Name = "Key";
					c.Properties.Add(m);
				}

				{
					PropertyDef m = new PropertyDef();
					m.Name = "Value";
					c.Properties.Add(m);
				}

				c.IsDefinedDefault = true;

				Structs.Add(c);
			}

			{
				ClassDef c = new ClassDef();
				c.Namespace = "System";
				c.Name = "Console";

				{
					MethodDef m = new MethodDef();
					m.Name = "WriteLine";
					m.Parameters.Add(new ParameterDef() { Name = "value" });
					c.Methods.Add(m);
				}
			}

			{
				ClassDef c = new ClassDef();
				c.Namespace = "System";
				c.Name = "Math";

				{
					MethodDef m = new MethodDef();
					m.Name = "Sqrt";
					m.Parameters.Add(new ParameterDef() { Name = "d" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Sin";
					m.Parameters.Add(new ParameterDef() { Name = "a" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Cos";
					m.Parameters.Add(new ParameterDef() { Name = "d" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Atan2";
					m.Parameters.Add(new ParameterDef() { Name = "y" });
					m.Parameters.Add(new ParameterDef() { Name = "x" });

					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Tan";
					m.Parameters.Add(new ParameterDef() { Name = "a" });
					c.Methods.Add(m);
				}

				{
					MethodDef m = new MethodDef();
					m.Name = "Exp";
					m.Parameters.Add(new ParameterDef() { Name = "d" });
					c.Methods.Add(m);
				}

				c.IsDefinedDefault = true;
				Classes.Add(c);
			}
		}
    }

    class EnumDef
    {
        public string Namespace = string.Empty;
        public string Name = string.Empty;
        public string Brief = string.Empty;
        public List<EnumMemberDef> Members = new List<EnumMemberDef>();

        public bool IsDefinedBySWIG = false;

        public override string ToString()
        {
            return string.Format("EnumDef {0}", Name);
        }
    }

    class EnumMemberDef
    {
        public string Name = string.Empty;
        public string Brief = string.Empty;

        public Expression Value = null;

        public override string ToString()
        {
            return string.Format("EnumMemberDef {0}", Name);
        }

        /// <summary>
        /// パーサー内部処理用
        /// </summary>
        internal Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax Internal = null;
    }

    class TypeParameterDef
    {
        public string Name = string.Empty;
        public bool IsConstraintedAsValueType = false;
        public bool IsConstraintedAsReferenceType = false;
        public List<TypeSpecifier> BaseTypeConstraints = new List<TypeSpecifier>();

        public override string ToString()
        {
            return string.Format("TypeParameterDef {0}", Name);
        }
    }

	interface ITypeParameters
	{
		List<TypeParameterDef> TypeParameters { get; }
	}


	abstract class TypeDef : 
		ITypeParameters
    {
        public AccessLevel AccessLevel { get; set; }
        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<TypeSpecifier> BaseTypes { get; protected set; }
        public List<TypeParameterDef> TypeParameters { get; protected set; }

        public List<ConstructorDef> Constructors { get; protected set; }
        public List<DestructorDef> Destructors { get; protected set; }
        public List<MethodDef> Methods { get; protected set; }
        public List<PropertyDef> Properties { get; protected set; }
        public List<FieldDef> Fields { get; protected set; }
        public List<OperatorDef> Operators { get; protected set; }

		public string UserCode { get; set; }

        public TypeDef()
        {
            Namespace = "";
            Name = "";
            BaseTypes = new List<TypeSpecifier>();
            TypeParameters = new List<TypeParameterDef>();
            Methods = new List<MethodDef>();
            Properties = new List<PropertyDef>();
            Fields = new List<FieldDef>();
            Operators = new List<OperatorDef>();
            Constructors = new List<ConstructorDef>();
            Destructors = new List<DestructorDef>();

			UserCode = "";
        }
    }

    class ClassDef : TypeDef
    {
        public bool IsAbstract { get; set; }
        public string Brief { get; set; }

        public ClassDef()
        {
            IsAbstract = false;
            Brief = "";
        }

        public override string ToString()
        {
            return string.Format("ClassDef {0}", Name);
        }

        public bool IsDefinedBySWIG = false;
		public bool IsDefinedDefault = false;
		public bool IsExported = true;
    }

    class StructDef : TypeDef
    {
        // BaseTypeはダミー

        public string Brief { get; set; }

        /// <summary>
        /// パーサー内部処理用
        /// </summary>
        internal Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax Internal = null;

        public StructDef()
        {
            BaseTypes = null;
            Brief = "";
        }

        public override string ToString()
        {
            return string.Format("StructDef {0}", Name);
        }

		public bool IsDefinedDefault = false;
    }

    class InterfaceDef : TypeDef
    {
        // Fields, Operatorsはダミー

        public string Brief { get; set; }

        public InterfaceDef()
        {
            Fields = null;
            Operators = null;
            Constructors = null;
            Destructors = null;
            Brief = "";
        }

        public override string ToString()
        {
            return string.Format("InterfaceDef {0}", Name);
        }
    }
}
