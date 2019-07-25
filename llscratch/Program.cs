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
			IList<IList<string>> rights = new List<IList<string>>();
			rights.Add(cfg.Rules[0].Right);
			_WriteRights(rights);
			for(var i = 0; i<10;++i)
			{
				rights = cfg.ExpandRights(rights);
			}
			_WriteRights(rights);


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
