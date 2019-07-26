using System;
using System.Collections.Generic;

namespace LL
{
	class Program
	{
		static void Main(string[] args)
		{
			EbnfDocument ebnf;
			EbnfDocument._TryParse(new EbnfParser(new EbnfTokenizer(new FileReaderEnumerable(@"..\..\..\ebnf.ebnf"))), out ebnf);
			Console.WriteLine(ebnf);
			
		}
		
	}
}
