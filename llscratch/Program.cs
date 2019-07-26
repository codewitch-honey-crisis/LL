using System;
using System.Collections.Generic;

namespace LL
{
	class Program
	{
		static void Main(string[] args)
		{
			var ebnf = EbnfDocument.ReadFrom(@"..\..\..\ebnf.ebnf");
			var cfg = ebnf.ToCfg();
			cfg.PrepareLL1(false);
			Console.WriteLine(cfg.ToString());

			var conflicts = cfg.FillConflicts();
			foreach(var conflict in conflicts)
			{
				if (CfgConflictKind.FirstFirst == conflict.Kind)
				{
					Console.Write("Rule {0} conflicts with {1} on {2}, k-value of ", conflict.Rule1, conflict.Rule2, conflict.Symbol);
					Console.WriteLine(cfg.GetK(conflict.Rule1, conflict.Rule2));
				}
			}
			
		}
		static void _WriteRights(IList<IList<string>> rights)
		{
			foreach (var right in rights)
			{
				var delim = "{ ";
				foreach (var sym in right)
				{
					Console.Write(delim);
					Console.Write(sym);
					delim = ", ";
				}
				Console.WriteLine(" }");
			}
		}
	}
}
