using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
namespace LL
{
	class Program
	{
		static void _PrintUsage()
		{
			var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetModules()[0].Name);
			Console.Error.WriteLine("Usage: {0} <grammarfile> <inputfile1> ... <inputfileN>", name);
			Console.Error.WriteLine();
			Console.Error.WriteLine("  <grammarfile>\tThe grammar file to use");
			Console.Error.WriteLine("  <inputfile1/N>\tThe file(s) to test with");
		}
		static int Main(string[] args)
		{
			if(2>args.Length)
			{
				_PrintUsage();
				return 1;
			}
			var ebnf = EbnfDocument.ReadFrom(args[0]);
			var cfg = ebnf.ToCfg();
			cfg.PrepareLL1();
			var lexer = ebnf.ToLexer(cfg);
			var parser1 = new DebugLL1Parser(cfg, null);
			parser1.ShowHidden = true; // won't work without these
			var parser2 = cfg.ToLL1Parser(null);
			parser2.ShowHidden = true;
			var failed = false;
			for (var i = 1; i < args.Length; ++i)
			{
				Console.WriteLine("For \"{0}\"...", args[i]);
				string input;
				using (var sr = File.OpenText(args[i]))
					input = sr.ReadToEnd();
				var tokenizer = new DebugTokenizer(cfg, lexer, input);
				parser1.Restart(tokenizer);
				var pass = _TestParser(parser1, input);
				Console.WriteLine("Debug Test {0}", pass? "passed" : "failed");
				parser2.Restart(tokenizer);
				if (!pass)
					failed = true;
				pass = _TestParser(parser2, input);
				Console.WriteLine("Table Test {0}", pass? "passed" : "failed");
				if (!pass)
					failed = true;
			}
			return failed ? 1 : 0;

		}
		
		static bool _TestParser(Parser parser,string input)
		{
			var sb = new StringBuilder();
			while(parser.Read())
			{
				// uncomment this if you want to see the run
				//Console.Error.WriteLine("{0}: {1} {2}", parser.NodeType, parser.Symbol, parser.Value);
				switch(parser.NodeType)
				{
					case ParserNodeType.Terminal:
					case ParserNodeType.Error:
						sb.Append(parser.Value);
						break;
				}
			}
			return sb.ToString() == input;
		}
	}
}
