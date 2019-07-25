using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LL
{
	using CharDfaEntry = KeyValuePair<int, KeyValuePair<string, int>[]>;

	public static class LLCodeGenerator
	{
		static void _FillPreamble(string preamble,CodeNamespace ns)
		{

			TextReader tr = new StringReader(preamble);
			string line;
			while(null!=(line=tr.ReadLine()))
				ns.Comments.Add(new CodeCommentStatement(string.Concat("  ",line)));
		}
		static CodeTypeDeclaration _CreateTokenizerClass(EbnfDocument ebnf,Cfg cfg,string name)
		{
			var lexer = ebnf.ToLexer(cfg);
			var sm = new Dictionary<string, int>();
			var ii = 0;
			var syms = new List<string>();

			cfg.FillSymbols(syms);
			var tt = new List<string>(syms);
			for (int jc = tt.Count, j = 0; j < jc; ++j)
				if (cfg.IsNonTerminal(tt[j]))
					tt[j] = null;

			foreach (var sym in syms)
			{
				sm.Add(sym, ii);
				++ii;
			}

			var bes = new string[syms.Count];
			for (ii = 0; ii < bes.Length; ii++)
				bes[ii] = cfg.AttributeSets.GetAttribute(syms[ii], "blockEnd", null) as string;
			var dfaTable = lexer.ToDfaTable(sm);
			var result = new CodeTypeDeclaration();
			result.Name = name;
			result.BaseTypes.Add(typeof(TableTokenizer));
			result.Attributes = MemberAttributes.FamilyOrAssembly;
			CodeMemberField f;
			foreach(var t in tt)
			{
				if(null!=t)
				{
					f = new CodeMemberField();
					f.Attributes = MemberAttributes.Const | MemberAttributes.Public;
					f.Name = t.Replace("#", "_").Replace("'", "_").Replace("<", "_").Replace(">", "_");
					f.Type = new CodeTypeReference(typeof(int));
					f.InitExpression = CodeDomUtility.Serialize(cfg.GetIdOfSymbol(t));
					result.Members.Add(f);
				}
			}

			f = new CodeMemberField();
			f.Name = "_Symbols";
			f.Type = new CodeTypeReference(typeof(string[]));
			f.Attributes = MemberAttributes.Static;
			f.InitExpression = CodeDomUtility.Serialize(tt.ToArray());
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Name = "_BlockEnds";
			f.Type = new CodeTypeReference(typeof(string[]));
			f.Attributes = MemberAttributes.Static;
			f.InitExpression = CodeDomUtility.Serialize(bes);
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Name = "_DfaTable";
			f.Type = new CodeTypeReference(typeof(CharDfaEntry[]));
			f.Attributes = MemberAttributes.Static;
			f.InitExpression = CodeDomUtility.Serialize(dfaTable);
			result.Members.Add(f);

			var ctor = new CodeConstructor();
			ctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEnumerable<char>), "input"));
			ctor.BaseConstructorArgs.AddRange(new CodeExpression[] {
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_DfaTable"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_Symbols"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_BlockEnds"),
				new CodeArgumentReferenceExpression("input")
			});
			ctor.Attributes = MemberAttributes.Public;
			result.Members.Add(ctor);
			return result;
		}
		static CodeTypeDeclaration _CreateParserClass(Cfg cfg,string name)
		{
			var sm = new Dictionary<string, int>();
			var ii = 0;
			var syms = new List<string>();

			cfg.FillSymbols(syms);
		

			foreach (var sym in syms)
			{
				sm.Add(sym, ii);
				++ii;
			}
			var pt = cfg.ToLL1ParseTable();
			var ipt = pt.ToLL1Array(syms);
			var nodeFlags = new int[syms.Count];
			for (var i = 0; i < nodeFlags.Length; ++i)
			{
				var o = cfg.AttributeSets.GetAttribute(syms[i], "hidden", false);
				if (o is bool && (bool)o)
					nodeFlags[i] |= 2;
				o = cfg.AttributeSets.GetAttribute(syms[i], "collapsed", false);
				if (o is bool && (bool)o)
					nodeFlags[i] |= 1;
			}
			var attrSets = new KeyValuePair<string, object>[syms.Count][];
			for (ii = 0; ii < attrSets.Length; ii++)
			{
				AttributeSet attrs;
				if (cfg.AttributeSets.TryGetValue(syms[ii], out attrs))
				{
					attrSets[ii] = new KeyValuePair<string, object>[attrs.Count];
					var j = 0;
					foreach (var attr in attrs)
					{
						attrSets[ii][j] = new KeyValuePair<string, object>(attr.Key, attr.Value);
						++j;
					}
				}
				else
					attrSets[ii] = null;// new KeyValuePair<string, object>[0];
			}
			

			var result = new CodeTypeDeclaration();
			result.Name = name;
			result.Attributes = MemberAttributes.FamilyOrAssembly;
			result.BaseTypes.Add(typeof(LL1TableParser));
			CodeMemberField f;
			foreach (var s in syms)
			{
				if (null != s)
				{
					f = new CodeMemberField();
					f.Attributes = MemberAttributes.Const | MemberAttributes.Public;
					f.Name = s.Replace("#", "_").Replace("'", "_").Replace("<", "_").Replace(">", "_");
					f.Type = new CodeTypeReference(typeof(int));
					f.InitExpression = CodeDomUtility.Serialize(cfg.GetIdOfSymbol(s));
					result.Members.Add(f);
				}
			}
			f = new CodeMemberField();
			f.Attributes = MemberAttributes.Static;
			f.Name = "_Symbols";
			f.Type = new CodeTypeReference(typeof(string[]));
			f.InitExpression = CodeDomUtility.Serialize(syms.ToArray());
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Attributes = MemberAttributes.Static;
			f.Name = "_ParseTable";
			f.Type = new CodeTypeReference(typeof(int[][][]));
			f.InitExpression = CodeDomUtility.Serialize(ipt);
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Attributes = MemberAttributes.Static;
			f.Name = "_InitCfg";
			f.Type = new CodeTypeReference(typeof(int[]));
			f.InitExpression = CodeDomUtility.Serialize(new int[] { cfg.GetIdOfSymbol(cfg.StartSymbol), cfg.FillNonTerminals().Count });
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Attributes = MemberAttributes.Static;
			f.Name = "_NodeFlags";
			f.Type = new CodeTypeReference(typeof(int[]));
			f.InitExpression = CodeDomUtility.Serialize(nodeFlags);
			result.Members.Add(f);

			f = new CodeMemberField();
			f.Attributes = MemberAttributes.Static;
			f.Name = "_AttributeSets";
			f.Type = new CodeTypeReference(attrSets.GetType());
			f.InitExpression = CodeDomUtility.Serialize(attrSets);
			result.Members.Add(f);

			var ctor = new CodeConstructor();
			ctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEnumerable<Token>), "tokenizer"));
			ctor.BaseConstructorArgs.AddRange(new CodeExpression[] {
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_ParseTable"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_InitCfg"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_Symbols"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_NodeFlags"),
				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(result.Name), "_AttributeSets"),
				new CodeArgumentReferenceExpression("tokenizer")
			});
			ctor.Attributes = MemberAttributes.Public;
			result.Members.Add(ctor);
			return result;
		}
		public static void WriteTokenizerClassTo(EbnfDocument ebnf,Cfg cfg,string name,string language,TextWriter writer)
		{
			if (string.IsNullOrEmpty(language))
				language = "cs";
			var cdp = CodeDomProvider.CreateProvider(language);
			var tokenizer = _CreateTokenizerClass(ebnf, cfg, name);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			cdp.GenerateCodeFromType(tokenizer, writer, opts);
		}
		public static void WriteParserClassTo(Cfg cfg, string name, string language, TextWriter writer)
		{
			if (string.IsNullOrEmpty(language))
				language = "cs";
			var cdp = CodeDomProvider.CreateProvider(language);
			var parser = _CreateParserClass(cfg, name);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			cdp.GenerateCodeFromType(parser, writer, opts);
		}
		public static void WriteParserAndGeneratorClassesTo(EbnfDocument ebnf,Cfg cfg, string @namespace, string preamble,string name,string language, TextWriter writer)
		{
			if (string.IsNullOrEmpty(language))
				language = "cs";
			var cdp = CodeDomProvider.CreateProvider(language);
			var tokenizer = _CreateTokenizerClass(ebnf,cfg, string.Concat(name, "Tokenizer"));
			var parser = _CreateParserClass(cfg, string.Concat(name,"Parser"));
			var ccu = new CodeCompileUnit();
			var cns = new CodeNamespace(@namespace);
			ccu.Namespaces.Add(cns);
			if(!string.IsNullOrEmpty(preamble))
				_FillPreamble(preamble, cns);
			
			cns.Types.Add(tokenizer);
			cns.Types.Add(parser);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			cdp.GenerateCodeFromCompileUnit(ccu, writer, opts);
		}
	}
}
