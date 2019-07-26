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
		/// <summary>
		/// Usage: lltree $grammarfile $inputfile
		/// </summary>
		/// <param name="args">The grammar file and the input file to parse</param>
		/// <returns></returns>
		static int Main(string[] args)
		{
			if (2!=args.Length)
			{
				_PrintUsage();
				return 1;
			}
			// read the ebnf document from the file.
			var ebnf = EbnfDocument.ReadFrom(args[0]);
			var hasErrors = false;
			// here we validate the document and print any
			// validation errors to the console.
			foreach (var msg in ebnf.Validate(false))
			{
				if (EbnfErrorLevel.Error == msg.ErrorLevel)
				{
					hasErrors = true;
					Console.Error.WriteLine(msg);
				}
			}

			foreach (var msg in ebnf.Prepare(false))
			{
				if (EbnfErrorLevel.Error == msg.ErrorLevel)
				{
					hasErrors = true;
					Console.Error.WriteLine(msg);
				}
			}

			// even if we have errors, we keep going.

			// create a CFG from the EBNF document
			var cfg = ebnf.ToCfg();

			// we have to prepare a CFG to be parsable by an LL(1)
			// parser. This means removing left recursion, and 
			// factoring out first-first and first-follows conflicts
			// where possible.
			// here we do that, and print any errors we encounter.
			foreach(var msg in cfg.PrepareLL1(false))
			{
				if(CfgErrorLevel.Error==msg.ErrorLevel)
				{
					hasErrors = true;
					Console.Error.WriteLine(msg);
				}
			}
			// if we don't have errors let's set up our parse.
			if(!hasErrors)
			{
				// the tokenizer is created from the EBNF document becase
				// it has the terminal definitions, unlike the CFG,
				// see https://www.codeproject.com/Articles/5162249/How-to-Make-an-LL-1-Parser-Lesson-1

				// The FileReaderEnumerable takes a filename and exposes IEnumerable<char> from
				// them. Tokenizers expect IEnumerable<char> (typically a string or char array)
				var tokenizer = ebnf.ToTokenizer(cfg, new FileReaderEnumerable(args[1]));

				// now create our parser. and since the parser *might* return multiple parse trees
				// in some cases, we keep reading until the end of document, calling ParseSubtree()
				// each time to get the result back as a ParseNode tree. We then take those nodes and
				// write them to the console via an implicit call to their ToString method
				using (var parser = cfg.ToLL1Parser(tokenizer))
					while (ParserNodeType.EndDocument != parser.NodeType)
						Console.WriteLine(parser.ParseSubtree());
				
				return 0;
				
			}
			return 1;
		}
	}
}
