using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BdBuilder
{
	public class Program
	{
		public static void Main(string[] args)
		{
			args = args.SelectMany(i => i.Split('"').Select(j=>j.Trim())).ToArray();

            Console.WriteLine(string.Join(" ", args));

			var targetDirectory = new DirectoryInfo(args[0]);
			var rootNameSpace = args[1];

			foreach (var file in targetDirectory.GetFiles("*.feature", SearchOption.AllDirectories))
			{
				Console.WriteLine($"Transpiling: {file.Name}");

				Task.Run(async () =>
				{
					await TranspileFile(file, rootNameSpace);
				}).GetAwaiter().GetResult();
			}
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
			var text = info.ReadAllText().Split('\r');

			var scenarios = new List<Tuple<string, List<string>>>();

			var currentScenario = new List<string>();
			var currentName = "";

			for (var i = 0; i < text.Length; i++)
			{
				var line = text[i];

				if (line.StartsWith("\n\t"))
				{
					currentScenario.Add(line.TrimStart('\n', '\t'));
				}
				else
				{
					if (line.Trim() == "")
						continue;

					if (currentName != "")
					{
						scenarios.Add(new Tuple<string, List<string>>(currentName, currentScenario.ToList()));
						currentScenario.Clear();
					}

					currentName = line.Trim();
				}
			}
			scenarios.Add(new Tuple<string, List<string>>(currentName, currentScenario));

			var name = text[0];
			var steps = text.Skip(1).Select(i => i.Trim()).Where(i => i != "");

			var methCode = new List<string>
			{
				"using Microsoft.VisualStudio.TestTools.UnitTesting;",
				"using System;",

				$"namespace {rootNameSpace} {{"
			};

			var className = info.Name.Replace(".feature", "");

			methCode.Add($"[TestClass]");
			methCode.Add($"public class {className} {{");

			foreach (var scenario in scenarios)
			{
				methCode.Add($"[TestMethod]");
				methCode.Add($"public void {scenario.Item1.ToCamelCase()}() {{");

				methCode.Add("var step = new Steps();");

				foreach (var line in scenario.Item2.Select(i => GetStepCall(i)))
				{
					methCode.Add(line);
				}

				methCode.Add($"}}");
			}

			methCode.Add($"}}");
			methCode.Add($"}}");

			var newFile = Path.ChangeExtension(info.FullName, ".feature.cs");

			File.WriteAllText(newFile, string.Join(Environment.NewLine, methCode));
		}
	}

	public static class Extensions
	{
		public static string ReadAllText(this FileInfo info)
		{
			return File.ReadAllText(info.FullName);
		}

		public static string ToCamelCase(this string str)
		{
			TextInfo textInfo = new CultureInfo("en-GB", false).TextInfo;

			return textInfo.ToTitleCase(str).Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
		}
	}
}