using System;
using System.Collections.Generic;

namespace LL
{
	class Program
	{
		static void Main(string[] args)
		{
			var fa = CharFA.Parse("fuba[rz]+","woo!");
			var dfa = fa.ToDfa();
			dfa.RenderToFile(@"..\..\..\fa.jpg");
			Console.WriteLine(fa.ToString());
			
			
		}
		static void _DoParse()
		{
			EbnfDocument ebnf;
			EbnfDocument._TryParse(new EbnfParser(new EbnfTokenizer(new FileReaderEnumerable(@"..\..\..\ebnf.ebnf"))), out ebnf);
			Console.WriteLine(ebnf);
		}
		
	}
}
