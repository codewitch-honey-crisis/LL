using System;
using System.IO;
using System.Reflection;

namespace LL
{
	class Program
	{
		static void _PrintUsage()
		{
			var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetModules()[0].Name);
			Console.Error.WriteLine("Usage: {0} <grammarfile> <inputfile>", name);
			Console.Error.WriteLine();
			Console.Error.WriteLine("  <grammarfile>\tThe grammar file to use");
			Console.Error.WriteLine("  <inputfile>\tThe file to parse");
		}
		static int Main(string[] args)
		{
			var cfg = Cfg.Parse(
				"S -> A b\n"+
				"A -> b\n"+
				"A ->\n");
			cfg.StartSymbol = "S";
			Console.WriteLine(cfg);
			foreach (var msg in cfg.PrepareLL1(false))
			{
				Console.WriteLine(msg);
			}
			Console.WriteLine(cfg);
			cfg.ToLL1ParseTable();
			return 0;
			if(2!=args.Length)
			{
				_PrintUsage();
				return 1;
			}
			var ebnf = EbnfDocument.ReadFrom(args[0]);
			var hasErrors = false;
			foreach (var msg in ebnf.Validate(false))
			{
				if (EbnfErrorLevel.Error == msg.ErrorLevel)
				{
					hasErrors = true;
					Console.Error.WriteLine(msg);
				}
			}
			cfg = ebnf.ToCfg();
			foreach(var msg in cfg.PrepareLL1(false))
			{
				if(CfgErrorLevel.Error==msg.ErrorLevel)
				{
					hasErrors = true;
					Console.Error.WriteLine(msg);
				}
			}
			if(!hasErrors)
			{
				var tokenizer = ebnf.ToTokenizer(cfg, new FileReaderEnumerable(args[1]));

				using (var parser = cfg.ToLL1Parser(tokenizer))
					while (ParserNodeType.EndDocument != parser.NodeType)
						Console.WriteLine(parser.ParseSubtree());
				
				return 0;
				
			}
			return 1;
		}
	}
}
