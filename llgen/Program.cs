using System;
using System.IO;
using System.Reflection;
namespace LL
{
	partial class Program
	{		
		static void _PrintUsage()
		{
			var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetModules()[0].Name);
			Console.Error.WriteLine("Usage: {0} [<grammarfile> [<outputfile>]] [/namespace <namespace>] [/language <language>]",name);
			Console.Error.WriteLine();
			Console.Error.WriteLine("  <grammarfile>\tThe grammar file to use (or stdin)");
			Console.Error.WriteLine("  <outputfile>\tThe file to write (or stdout)");
			Console.Error.WriteLine("  <namespace>\tThe namespace to generate the code under (or none)");
			Console.Error.WriteLine("  <language>\tThe .NET language to generate the code for (or draw from filename or C#)");
		}
		static int Main(string[] args)
		{
			string grammarFile = null;
			string outFile = null;
			string @namespace = null;
			string language = null;// "c#";
			var optIndex = -1;
			for (var i=0;i<args.Length;++i)
			{
				if("--help"==args[i] || "/?"==args[i] || "/help"==args[i])
				{
					_PrintUsage();
					return 0;
				}
				if (args[i].StartsWith("/"))
				{
					optIndex = i;
					if(i==args.Length-1)
					{
						_PrintUsage();
						return 1;
					}
					switch (args[i])
					{
						case "/language":
							++i;
							language = args[i];
							break;
						case "/namespace":
							++i;
							@namespace= args[i];
							break;
						default:
							_PrintUsage();
							return 1;
					}
				}
				else
				{
					if (-1 != optIndex)
					{
						_PrintUsage();
						return 1;
					}
					if (0 == i)
						grammarFile = args[i];
					else if (1 == i)
						outFile = args[i];
					else
					{
						_PrintUsage();
						return 1;
					}

				}
			}

			string inp;
			if (string.IsNullOrEmpty(grammarFile))
				inp = Console.In.ReadToEnd();
			else
			{
				using (var sr = File.OpenText(grammarFile))
					inp = sr.ReadToEnd();
			}
			var ebnf = EbnfDocument.Parse(inp);			
			var hasErrors = false;
			foreach (var msg in ebnf.Validate(false))
			{
				Console.Error.WriteLine(string.Concat("EBNF ",msg.ToString()));
				if (EbnfErrorLevel.Error == msg.ErrorLevel)
					hasErrors = true;
			}
			var cfg = ebnf.ToCfg();
			foreach(var msg in cfg.PrepareLL1(false))
			{
				Console.Error.WriteLine(string.Concat("CFG ",msg.ToString()));
				if (CfgErrorLevel.Error == msg.ErrorLevel)
					hasErrors = true;
			}
			Console.Error.WriteLine();
			Console.Error.WriteLine(cfg);
			if (!hasErrors)
			{
				if (!string.IsNullOrEmpty(outFile))
				{
					if (null == language)
					{
						language = Path.GetExtension(outFile).Substring(1);
						if ("" == language)
							language = null;
					}
					using (var fw = new StreamWriter(File.Open(outFile, FileMode.OpenOrCreate)))
					{
						fw.BaseStream.SetLength(0);
						LLCodeGenerator.WriteParserAndGeneratorClassesTo(ebnf, cfg, @namespace, inp,Path.GetFileNameWithoutExtension(outFile), language, fw);
					}
				} else
					LLCodeGenerator.WriteParserAndGeneratorClassesTo(ebnf, cfg, @namespace, inp,cfg.StartSymbol, language, Console.Out);
				return 0;
			}
			return 1;
			
		}
	}
}
