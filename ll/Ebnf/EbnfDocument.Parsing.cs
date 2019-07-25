using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LL
{
	partial class EbnfDocument
	{
		static IList<EbnfMessage> _TryParseExpression(ParseNode pn,out EbnfExpression result)
		{
			result = null;
			Debug.Assert(EbnfParser.expression == pn.SymbolId, "Not positioned on expression");
			var msgs = new List<EbnfMessage>();

			if (1==pn.Children.Count)
			{
				switch(pn.Children[0].SymbolId)
				{
					case EbnfParser.symbol:
						break;
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
						var s = attrnode.Children[2].Value;
						if (!ParseContext.Create(s).TryParseJsonValue(out attrval))
							attrval = null;
					}
					prod.Attributes.Add(attrname, attrval);
					++i;
				}
				while (EbnfParser.eq != pn.Children[i].SymbolId)
					++i;
				++i;
				var seq = new List<EbnfExpression>();
				while(EbnfParser.semi!=pn.Children[i].SymbolId)
				{
					if(EbnfParser.expression==pn.Children[i].SymbolId)
					{
						EbnfExpression expr;
						msgs.AddRange(_TryParseExpression(pn.Children[i], out expr));
					}
					++i;
				}
			}
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
					}
				}
			}
			return msgs;
		}
	}
}
