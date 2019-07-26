using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LL
{
	partial class EbnfDocument
	{
		static IList<EbnfMessage> _TryParseExpressions(ParseNode parent,int firstChildIndex,out EbnfExpression result)
		{
			var msgs = new List<EbnfMessage>();
			result = null;
			var or = new List<IList<EbnfExpression>>();
			var seq = new List<EbnfExpression>();
			for(int ic = parent.Children.Count,i=firstChildIndex;i<ic;++i)
			{
				EbnfExpression expr;
				var pn = parent.Children[i];
				switch(pn.SymbolId)
				{
					case EbnfParser.expression:
						msgs.AddRange(_TryParseExpression(parent.Children[i], out expr));
						seq.Add(expr);
						break;
					case EbnfParser.or:
						or.Add(seq);
						seq = new List<EbnfExpression>();
						break;
					default:
						break;
				}
			}
			result = null;
			for(int ic=or.Count,i=0;i<ic;++i)
			{
				var s = or[i];
				EbnfExpression e = null;
				switch(s.Count)
				{
					case 0:
						e = null;
						break;
					case 1:
						e = s[0];
						break;
					default:
						for(int jc=s.Count,j=0;j<jc;++j)
						{

						}
						break;
				}
			}
			return msgs;
		}
		static IList<EbnfMessage> _TryParseExpression(ParseNode pn,out EbnfExpression result)
		{
			result = null;
			Debug.Assert(EbnfParser.expression == pn.SymbolId, "Not positioned on expression");
			var msgs = new List<EbnfMessage>();

			if (1==pn.Children.Count)
			{
				var c = pn.Children[0];
				if (EbnfParser.symbol == c.SymbolId)
				{
					var cc = c.Children[0];
					//TODO: parse the regular expressions and literals to make sure they're valid.
					switch (cc.SymbolId)
					{
						case EbnfParser.identifier:
							result = new EbnfRefExpression(cc.Value);
							return msgs;
						case EbnfParser.regex:
							result = new EbnfRegexExpression(cc.Value);
							return msgs;
						case EbnfParser.literal:
							result = new EbnfLiteralExpression(cc.Value);
							return msgs;
						case EbnfParser.lbrace:
							msgs.AddRange(_TryParseExpressions(c, 1, out result));
							result = new EbnfRepeatExpression(result);
							if (EbnfParser.rbracePlus == c.Children[c.Children.Count - 1].SymbolId)
								((EbnfRepeatExpression)result).IsOptional = false;
							return msgs;
						case EbnfParser.lbracket:
							msgs.AddRange(_TryParseExpressions(c, 1, out result));
							result = new EbnfOptionalExpression(result);
							return msgs;
						default:
							break;
					}
				}
			}
			return msgs;			
		}
		static IList<EbnfMessage> _TryParseProduction(ParseNode pn, out KeyValuePair<string,EbnfProduction> result)
		{
			Debug.Assert(EbnfParser.production == pn.SymbolId, "Not positioned on production");
			var msgs = new List<EbnfMessage>();
			string name = pn.Children[0].Value;
			var prod = new EbnfProduction();
			prod.SetPositionInfo(pn.Line, pn.Column, pn.Position);
			if(EbnfParser.lt==pn.Children[1].SymbolId)
			{
				var i = 2;
				while(EbnfParser.gt!=pn.Children[i].SymbolId)
				{
					var attrnode = pn.Children[i];
					var attrname = attrnode.Children[0].Value;
					var attrval = (object)true;
					if (3==attrnode.Children.Count)
					{
						var s = attrnode.Children[2].Children[0].Value;
						if (!ParseContext.Create(s).TryParseJsonValue(out attrval))
							attrval = null;
					}
					prod.Attributes.Add(attrname, attrval);
					++i;
					if (EbnfParser.comma == pn.Children[i].SymbolId)
						++i;
				}
				while (EbnfParser.eq != pn.Children[i].SymbolId)
					++i;
				++i;
				var ors = new List<IList<EbnfExpression>>();
				var seq = new List<EbnfExpression>();
				while(EbnfParser.semi!=pn.Children[i].SymbolId)
				{
					if(EbnfParser.expression==pn.Children[i].SymbolId)
					{
						EbnfExpression expr;
						msgs.AddRange(_TryParseExpression(pn.Children[i], out expr));
						seq.Add(expr);
					} else if(EbnfParser.or==pn.Children[i].SymbolId)
					{
						ors.Add(seq);
						seq = new List<EbnfExpression>();
					}
					++i;
				}
			}
			result = new KeyValuePair<string, EbnfProduction>(name, prod);
			return msgs;
		}
		public static IList<EbnfMessage> _TryParse(EbnfParser parser, out EbnfDocument result)
		{
			result = null;
			var msgs = new List<EbnfMessage>();
			ParseNode gn = null;
			var hasErrors = false;
			do
			{
				var pn = parser.ParseSubtree();
				if (null != pn)
				{
					var das = pn.FillDescendantsAndSelf();
					foreach (var p in das)
					{
						if (EbnfParser._ERROR == p.SymbolId)
						{
							hasErrors = true;
							msgs.Add(new EbnfMessage(EbnfErrorLevel.Error, -1, string.Concat("Syntax error in EBNF document. Unrecognized: ", p.Value), p.Line, p.Column, p.Position));
						}
					}
					if (EbnfParser.grammar == pn.SymbolId)
						gn = pn;
				}
			} while (ParserNodeType.EndDocument!=parser.NodeType);
			if(null==gn)
			{
				hasErrors = true;
				msgs.Add(new EbnfMessage(EbnfErrorLevel.Error, -1, "No productions were found.", 1, 1, 0));
			}
			if(!hasErrors)
			{
				result = new EbnfDocument();
				foreach(var pn in gn.Children)
				{
					if(EbnfParser.production==pn.SymbolId)
					{
						KeyValuePair<string, EbnfProduction> prod;
						msgs.AddRange(_TryParseProduction(pn,out prod));
						result.Productions.Add(prod);
					}
				}
			}
			
			return msgs;
		}
	}
}
