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
			
			or.Add(seq);
			result = null;
			switch(or.Count)
			{
				case 0:
					result = null;
					break;
				case 1:
					result = _SeqToExpression(or[0]);
					break;
				default:
					var state = 0;
					var oe = new EbnfOrExpression();
					for (int ic = or.Count, i = 0; i < ic; ++i)
					{
						switch(state)
						{
							case 0:
								oe.Left = _SeqToExpression(or[i]);
								state = 1;
								break;
							case 1:
								oe.Right = _SeqToExpression(or[i]);
								state = 2;
								break;
							case 2:
								oe = new EbnfOrExpression(oe, null);
								oe.Right = _SeqToExpression(or[i]);
								break;
						}
					}
					//if (0 == state || 2 == state)
					//	result = oe.Left;
					//else
						result = oe;
					break;
			}
			
			return msgs;
		}
		static EbnfExpression _SeqToExpression(IList<EbnfExpression> seq)
		{
			EbnfExpression result;
			switch (seq.Count)
			{
				case 0:
					result = null;
					break;
				case 1:
					result = seq[0];
					break;
				default:
					var ce = new EbnfConcatExpression();
					for (int jc = seq.Count, j = 0; j < jc; ++j)
					{
						if (null == ce.Left)
							ce.Left = seq[j];
						else if (null == ce.Right)
							ce.Right = seq[j];
						if (null != ce.Right)
							ce = new EbnfConcatExpression(ce, null);
					}
					if (null == ce.Right)
						result = ce.Left;
					else
						result = ce;
					break;
			}
			return result;
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
					ParseContext pc;
					var cc = c.Children[0];
					//TODO: parse the regular expressions and literals to make sure they're valid.
					switch (cc.SymbolId)
					{
						case EbnfParser.identifier:
							result = new EbnfRefExpression(cc.Value);
							return msgs;
						case EbnfParser.regex:
							pc = ParseContext.Create(cc.Value);
							pc.EnsureStarted();
							pc.Advance(); 
							pc.TryReadUntil('\'', '\\', false);
							result = new EbnfRegexExpression(pc.GetCapture());
							return msgs;
						case EbnfParser.literal:
							pc = ParseContext.Create(cc.Value);
							pc.EnsureStarted();
							pc.Advance();
							pc.TryReadUntil('\"', '\\', false);
							result = new EbnfLiteralExpression(pc.GetCapture());
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
						case EbnfParser.lparen:
							msgs.AddRange(_TryParseExpressions(c, 1, out result));
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
			if ("production" == name) Debugger.Break();
			var prod = new EbnfProduction();
			prod.SetPositionInfo(pn.Line, pn.Column, pn.Position);
			var i = 0;
			if (EbnfParser.lt == pn.Children[1].SymbolId)
			{
				i = 2;
				while (EbnfParser.gt != pn.Children[i].SymbolId)
				{
					var attrnode = pn.Children[i];
					var attrname = attrnode.Children[0].Value;
					var attrval = (object)true;
					if (3 == attrnode.Children.Count)
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
				++i;
			}
			++i;
			EbnfExpression e;
			_TryParseExpressions(pn, i, out e);
			prod.Expression = e;
			
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
