using System;
using System.Collections.Generic;

namespace LL
{
	class Program
	{
		static void Main(string[] args)
		{
			var fa = CharFA.Parse("fu(ba+r|baz)","woo!");
			var dfa = fa.ToDfa();
			var closure = dfa.FillClosure();
			var subset = closure[0].ClonePathTo(closure[5]);
			subset.RenderToFile(@"..\..\..\fa.jpg");
			Console.WriteLine(dfa.IsLiteral);
			Console.WriteLine(subset.IsLiteral);
			
		}
		static void _DoParse()
		{
			EbnfDocument ebnf;
			EbnfDocument._TryParse(new EbnfParser(new EbnfTokenizer(new FileReaderEnumerable(@"..\..\..\ebnf.ebnf"))), out ebnf);
			Console.WriteLine(ebnf);
		}
		
	}
}
