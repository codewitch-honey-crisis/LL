using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LL
{
	class Program
	{
		static void _PrintUsage()
		{
			var name = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetModules()[0].Name);
			Console.Error.WriteLine("Usage: {0} <grammarfile>", name);
			Console.Error.WriteLine();
			Console.Error.WriteLine("  <grammarfile>\tThe grammar file to check, or stdin");
		}
		static int Main(string[] args)
		{
			if(1<args.Length)
			{
				_PrintUsage();
				return 1;
			}
			EbnfDocument ebnf;
			if (1 == args.Length)
				ebnf = EbnfDocument.ReadFrom(args[0]);
			else
				ebnf = EbnfDocument.ReadFrom(Console.In);
			ebnf.Validate(true);
			ebnf.Prepare(true);
			var cfg = ebnf.ToCfg();
			cfg.PrepareLL1(false);
			foreach(var conflict in cfg.FillConflicts())
			{
				switch(conflict.Kind)
				{
					case CfgConflictKind.FirstFirst:
						Console.WriteLine("First first conflict on {0} between rules:",conflict.Symbol);
						Console.WriteLine("\t{0}", conflict.Rule1);
						Console.Write("\t{0} k = ", conflict.Rule2);
						Console.WriteLine(cfg.GetK(conflict.Rule1, conflict.Rule2, 5));
						break;
					case CfgConflictKind.FirstFollows:
						Console.WriteLine("First follows conflict on {0} between rules:", conflict.Symbol);
						Console.WriteLine("\t{0}", conflict.Rule1);
						Console.Write("\t{0} k = ", conflict.Rule2);
						Console.WriteLine(cfg.GetK(conflict.Rule1, conflict.Rule2, 5));
						break;
				}
			}

			return 0;
		}
	}
}
