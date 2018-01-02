using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BdBuilder
{
    public class Templates
    {
        public static string Code =>
            @"	using Microsoft.VisualStudio.TestTools.UnitTesting;

				namespace Template {}
			";
    }

    public class CompileFeatureFile : Microsoft.Build.Utilities.Task
    {
        [Microsoft.Build.Framework.Output]
        public string OutputFile { get; set; }

        public string FileName { get; set; }

        public string RootNameSpace { get; set; }

        public override bool Execute()
        {
            var fileInfo = new FileInfo(FileName);

            try
            {
                Log.LogMessage(Microsoft.Build.Framework.MessageImportance.High, $"BdBuilder -> Compiling {fileInfo.Name} into {fileInfo.Name}.cs");
            }
            catch (Exception)
            {
                
            }
               
            OutputFile = Path.ChangeExtension(fileInfo.FullName, ".feature.cs");

            System.Threading.Tasks.Task.Run(async () =>
            {
                await TranspileFile(fileInfo, RootNameSpace);

            }).GetAwaiter().GetResult();

            return true;
        }

        public static string GetStepCall(string line)
        {
            var replacements = new List<string> { "x", "y", "z", "i", "j" };

            MatchCollection matches;

            var count = 0;
            var args = new List<Tuple<string, string>>();

            do
            {
                matches = new Regex("['‘\"](.*?)['’\"]").Matches(line);

                if (matches.Count == 0)
                    break;

                var firstGroup = matches[0];

                var val = firstGroup.Value;

                var replacement = replacements[count % replacements.Count];

                line = line.Replace(val, replacement);
                val = val.Trim('\'', '‘', '’', '"');

                args.Add(new Tuple<string, string>($"\"{val}\"", replacement));
                count++;
            }
            while (matches.Count > 0);

            var argStr = string.Join(",", args.Select(j => $"{j.Item2}: {j.Item1}"));

            var function = $"step.{line.ToCamelCase()}({argStr});".Trim();

            return function;
        }

        public async static Task TranspileFile(FileInfo info, string rootNameSpace)
        {
            var text = info.ReadAllText().Split('\r', '\n').Where(i => i.Trim() != "").Select(i => i.Replace("   ", "\t")).ToList();

            var tests = new List<Tuple<string, List<string>>>();

            if (text.Count == 0)
                return;

            do
            {
                var title = text.ElementAt(0);
                var stepsToAdd = text.Skip(1).TakeWhile(i => i.StartsWith("\t")).Select(i => i.Trim()).ToList();

                tests.Add(new Tuple<string, List<string>>(title, stepsToAdd));

                text = text.Skip(stepsToAdd.Count() + 1).ToList();
            }
            while (text.Count > 0);
 
            var code = Templates.Code;

            var tree = CSharpSyntaxTree.ParseText(code);

            var root = await tree.GetRootAsync().ConfigureAwait(false) as CompilationUnitSyntax;

            // Get the namespace declaration.
            var oldNamespace = root.Members.Single(m => m is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;

            // Create a new namespace declaration.
            var newNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(rootNameSpace)).NormalizeWhitespace();

            var publicModifiers = SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) });

            var classDeclaration = SyntaxFactory.ClassDeclaration(info.Name.Replace(".feature", "")).WithModifiers(publicModifiers);

            foreach (var test in tests)
            {
                var name = test.Item1;
                var steps = test.Item2;

                var methodToInsert = GetMethodDeclarationSyntax(returnTypeName: "void", methodName: name.ToCamelCase()).WithModifiers(publicModifiers);

                var methCode = new List<string>();

                methCode.Add("var step = new Steps();");

                methCode = methCode.Concat(steps.Select(i =>
                {
                    var replacements = new List<string> { "x", "y", "z", "i", "j" };

                    MatchCollection matches;
                    var count = 0;

                    var args = new List<Tuple<string, string>>();

                    do
                    {
                        matches = new Regex("['‘\"](.*?)['’\"]").Matches(i);

                        if (matches.Count == 0)
                            break;

                        var firstGroup = matches[0];

                        var val = firstGroup.Value;
                        var replacement = replacements[count % replacements.Count];

                        i = i.Replace(val, replacement);

                        val = val.Trim('\'', '‘', '’', '"');

                        args.Add(new Tuple<string, string>($"\"{val}\"", replacement));

                        count++;
                    }
                    while (matches.Count > 0);

                    var argStr = string.Join(",", args.Select(j => $"{j.Item2}: {j.Item1}"));

                    var function = $"step.{i.ToCamelCase()}({argStr});".Trim();

                    return function;
                })).ToList();

                foreach (var meth in methCode)
                {
                    methodToInsert = methodToInsert.AddBodyStatements(SyntaxFactory.ParseStatement(meth).NormalizeWhitespace());
                }

                methodToInsert = methodToInsert.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestMethod")))));

                classDeclaration = classDeclaration.AddMembers(methodToInsert);
            }

            classDeclaration = classDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestClass")))));

            // Add the class declarations to the new namespace.
            newNamespace = newNamespace.AddMembers(classDeclaration);

            // Replace the oldNamespace with the newNamespace and normailize.
            root = root.ReplaceNode(oldNamespace, newNamespace).NormalizeWhitespace();

            string newCode = root.ToFullString();
            
            var newFile = Path.ChangeExtension(info.FullName, ".feature.cs");

            File.WriteAllText(newFile, newCode);
        }

        public static MethodDeclarationSyntax GetMethodDeclarationSyntax(string returnTypeName, string methodName, string[] parameterTypes = null, string[] paramterNames = null)
        {
            var parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(GetParametersList(parameterTypes, paramterNames)));
            return SyntaxFactory.MethodDeclaration(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                          modifiers: SyntaxFactory.TokenList(),
                          returnType: SyntaxFactory.ParseTypeName(returnTypeName),
                          explicitInterfaceSpecifier: null,
                          identifier: SyntaxFactory.Identifier(methodName),
                          typeParameterList: null,
                          parameterList: parameterList,
                          constraintClauses: SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                          body: null,
                          semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private static IEnumerable<ParameterSyntax> GetParametersList(string[] parameterTypes, string[] paramterNames)
        {
            for (int i = 0; i < parameterTypes?.Length; i++)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                                                         modifiers: SyntaxFactory.TokenList(),
                                                         type: SyntaxFactory.ParseTypeName(parameterTypes[i]),
                                                         identifier: SyntaxFactory.Identifier(paramterNames[i]),
                                                         @default: null);
            }
        }
    }

    public static class Extensions
    {
        public static string ReadAllText(this FileInfo info)
        {
            return File.ReadAllText(info.FullName);
        }

        public static IEnumerable<char> CharsToTitleCase(string s)
        {
            bool newWord = true;
            foreach (char c in s)
            {
                if (newWord) { yield return Char.ToUpper(c); newWord = false; }
                else yield return Char.ToLower(c);
                if (c == ' ') newWord = true;
            }
        }

        public static string ToCamelCase(this string str)
        {
            var conv = new string(CharsToTitleCase(str).ToArray());

            return conv.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
        }
    }
}