using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using UnityAtoms.BaseAtoms;
using System.Reflection;

namespace RoslynTest
{
    internal class SOGenerator
    {
        static Dictionary<Type, string> _typeConverter = new()
        {
            {typeof(float), "float"},
            {typeof(int),   "int"},
            //{typeof(long),"long"},
            {typeof(string),"string"}
        };

        static Dictionary<Type, Type> _soConverter = new Dictionary<Type, Type>()
        {
            {typeof(float),     typeof(FloatVariable)   },
            {typeof(int),       typeof(IntVariable)     },
            //{typeof(long),    typeof(IntVariable)     },
            {typeof(string),    typeof(StringVariable)  },
        };

        [MenuItem("SOGenerator/GenerateCode")]
        public static void GenerateCode()
        {
            var t = typeof(IPlayerProfile);
            string className = GetSOClassName(t);

            var @namespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("SOGenerator").NormalizeWhitespace());
            @namespace = @namespace.AddUsings(
                 SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(UnityEngine))),
                 SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"{nameof(UnityAtoms)}.{nameof(UnityAtoms.BaseAtoms)}")),
                 SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(System)))
                );
            var classDeclaration = SyntaxFactory.ClassDeclaration(className);
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            classDeclaration = classDeclaration.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(nameof(ScriptableObject))),
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(t.Name))
                ); ;

            AttributeSyntax attribute = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName(nameof(CreateAssetMenuAttribute).Replace("Attribute", "")),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.NameEquals(nameof(CreateAssetMenuAttribute.fileName)),
                            null,
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(className))),
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.NameEquals(nameof(CreateAssetMenuAttribute.menuName)),
                            null,
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal($"SOGenerator/{className}")))
                    })));

            classDeclaration = classDeclaration.AddAttributeLists(
                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

            foreach (var p in t.GetProperties())
            {
                string varName = ConvertToPrivateFieldConvention(p.Name);
                string soTypeName = _soConverter[p.PropertyType].Name;
                var variable = SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName(soTypeName))
                        .AddVariables(SyntaxFactory.VariableDeclarator(varName));

                var field = SyntaxFactory.FieldDeclaration(variable);
                field = field.AddAttributeLists(
                        SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                SyntaxFactory.Attribute(
                                    SyntaxFactory.IdentifierName(nameof(SerializeField))))));

                var property = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(
                        _typeConverter[p.PropertyType]
                    ),
                    p.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"return {varName}.{nameof(IntVariable.Value)};"))),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"{varName}.{nameof(IntVariable.Value)} = value;")))
                        );
                members.Add(field);
                members.Add(property);
            }

            classDeclaration = classDeclaration.AddMembers(members.ToArray());
            @namespace = @namespace.AddMembers(classDeclaration);

            var code = @namespace
                .NormalizeWhitespace()
                .ToFullString();

            // Output new code to the console.
            Debug.Log(code);
            string path = $"Assets/Scripts/{className}.cs";
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();
        }

        [MenuItem("SOGenerator/GenerateAssets")]
        public static void GenerateAssets()
        {
            var t = typeof(IPlayerProfile);
            string className = GetSOClassName(typeof(IPlayerProfile));

            var parentObjectAsset = ScriptableObject.CreateInstance(className);
            var soType = parentObjectAsset.GetType();
            foreach (var p in t.GetProperties())
            {
                var fieldAsset = ScriptableObject.CreateInstance(_soConverter[p.PropertyType]);
                fieldAsset.name = p.Name;
               
                string path = $"Assets/Variables/{className}/{p.Name}.asset";
                PathUtil.CreatePath(path);
                AssetDatabase.CreateAsset(fieldAsset, path);
                var fieldName = ConvertToPrivateFieldConvention(p.Name);
                var fieldInfo = soType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfo.SetValue(parentObjectAsset, fieldAsset);
            }

            string parentObjectPath = $"Assets/Variables/{className}.asset";
            AssetDatabase.CreateAsset(parentObjectAsset, parentObjectPath);
            AssetDatabase.Refresh();
        }

        public static string GetSOClassName(Type interfaceType)
        {
            return "SO" + (interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name);
        }

        public static string ConvertToPrivateFieldConvention(string fieldName)
        {
            return $"_{Char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1)}";
        }
    }
}