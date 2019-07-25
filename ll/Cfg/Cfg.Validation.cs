using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	partial class Cfg
	{
		public IList<CfgMessage> FillValidate(bool throwIfErrors=false,IList<CfgMessage> result = null)
		{
			if (null == result)
				result = new List<CfgMessage>();
			_FillValidateAttributes(result);
			_FillValidateRules(result);
			if(throwIfErrors)
				CfgException.ThrowIfErrors(result);
			return result;
		}
		public IList<CfgLLConflict> FillLLConflicts(IList<CfgLLConflict> result = null)
		{
			if (null == result)
				result = new List<CfgLLConflict>();
			// build a temporary parse table to check for conflicts
			var predict = FillPredict();
			var follows = FillFollows();
			//var pt = new LLParseTable();
			foreach (var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgRule>();
				foreach (var f in predict[nt])
				{
					if (null != f.Symbol)
					{
						CfgRule r;
						if (d.TryGetValue(f.Symbol, out r) && r != f.Rule)
						{
							var cf = new CfgLLConflict(CfgConflictKind.FirstFirst, r, f.Rule, f.Symbol);
							if (!result.Contains(cf))
								result.Add(cf);
						}
						else
							d.Add(f.Symbol, f.Rule);
					}
					else
					{
						foreach (var ff in follows[nt])
						{
							CfgRule r;
							if (d.TryGetValue(ff, out r) && r != f.Rule)
							{
								var cf = new CfgLLConflict(CfgConflictKind.FirstFollows, r, f.Rule, ff);
								if (!result.Contains(cf))
									result.Add(cf);
							}
							else
								d.Add(ff, f.Rule);
						}
					}
				}
			}
			return result;
		}
		IList<CfgMessage> _FillValidateRules(IList<CfgMessage> result)
		{
			if(null==result)
				result = new List<CfgMessage>();
			var ic = Rules.Count;
			if (0 == ic)
				result.Add(new CfgMessage(CfgErrorLevel.Error, -1, "Grammar has no rules"));
			
			var dups = new HashSet<CfgRule>();
			for (var i = 0;i<ic;++i)
			{
				var rule = Rules[i];
				// LL specific
				if (rule.IsDirectlyLeftRecursive)
					result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Rule is directly left recursive on line ", (i + 1).ToString(), ":", rule.ToString())));
				if (rule.Left.IsNullOrEmpty())
					result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Rule has empty left hand side on line ", (i + 1).ToString(), ":", rule.ToString())));
				else if("#ERROR"==rule.Left ||"#EOS"==rule.Left)
					result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Rule has reserved terminal on left hand side on line ", (i + 1).ToString(), ":", rule.ToString())));
				for (int jc = rule.Right.Count, j = 0; j > jc; ++j)
					if (rule.Right[j].IsNullOrEmpty())
						result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Rule has empty symbols on the right hand side on line ", (i + 1).ToString(), ":", rule.ToString())));
					else if ("#ERROR" == rule.Right[j] || "#EOS" == rule.Right[j])
						result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Rule has reserved terminal on right hand side on line ", (i + 1).ToString(), ":", rule.ToString())));
				
				for (var j = 0; j < ic; ++j)
					if (i != j && Rules[j] == rule && dups.Add(rule))
						result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat("Duplicate rule on line ", (i + 1).ToString(), ":", rule.ToString())));
				
			}
			var closure = FillClosure(StartSymbol);
			var syms = FillSymbols();
			ic = syms.Count;
			for (var i=0;i<ic;++i)
			{
				var sym = syms[i];
				if (!closure.Contains(sym))
				{
					var found = false;
					if(!IsNonTerminal(sym))
						if ("#EOS"==sym || "#ERROR"==sym || (bool)AttributeSets.GetAttribute(sym, "hidden", false))
							found = true;
					if(!found)
						result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Concat("Unreachable symbol \"", sym, "\"")));
				}
			}
			// build a temporary parse table to check for conflicts
			var predict = FillPredict();
			var follows = FillFollows();
			//var pt = new LLParseTable();
			foreach(var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgRule>();
				foreach(var f in predict[nt])
				{
					if(null!=f.Symbol)
					{
						CfgRule r;
						if (d.TryGetValue(f.Symbol, out r) && r!=f.Rule)
						{
							result.Add(new CfgMessage(CfgErrorLevel.Warning, -1,
								string.Format(
									"Rule {0} at line {1} has a FIRST FIRST conflict with rule {2} on symbol {3} at line {4} and will require additional lookahead",
									f.Rule,
									Rules.IndexOf(f.Rule) + 1,
									r,
									f.Symbol,
									Rules.IndexOf(r) + 1)));

						}
						else
							d.Add(f.Symbol, f.Rule);
					} else
					{
						foreach(var ff in follows[nt])
						{
							CfgRule r;
							if (d.TryGetValue(ff, out r) && r!=f.Rule)
							{
								
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1,
								string.Format(
									"Rule {0} at line {1} has a FIRST FOLLOW conflict with rule {2} on symbol {3} at line {4} and will require additional lookahead",
									f.Rule,
									Rules.IndexOf(f.Rule) + 1,
									r,
									ff,
									Rules.IndexOf(r) + 1)));
								
							}
							else
								d.Add(ff, f.Rule);
						}
					}
				}
			}
			return result;
		}
		IList<CfgMessage> _FillValidateAttributes(IList<CfgMessage> result)
		{
			if(null==result)
				result = new List<CfgMessage>();
			string start = null;
			foreach(var attrs in AttributeSets)
			{
				if(!IsSymbol(attrs.Key))
					result.Add(new CfgMessage(CfgErrorLevel.Warning,-1,string.Concat("Attributes declared on a symbol \"",attrs.Key,"\" that is not in the grammar")));
				foreach(var attr in attrs.Value)
				{
					string s;
					var p = string.Concat("On \"" , attrs.Key , "\": ");
					switch (attr.Key)
					{
						case "start":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "start attribute expects a bool value and will be ignored")));
							if(null!=start)
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "start attribute was already specified on \"",start,"\" and this declaration will be ignored")));
							else
								start = attrs.Key;
							continue;
						case "hidden":
							if(!(attr.Value is bool))
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p,"hidden attribute expects a bool value and will be ignored")));
							continue;
						case "terminal":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "terminal attribute expects a bool value and will be ignored")));
							continue;
						case "collapsed":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p,"collapse attribute expects a bool value and will be ignored")));
							continue;
						case "blockEnd":
							if (IsNonTerminal(attrs.Key))
								result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute cannot be specified on a non-terminal and will be ignored")));
							else
							{
								s = attr.Value as string;
								if (!(attr.Value is string))
									result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute expects a string value and will be ignored")));
								else if (string.IsNullOrEmpty(s))
									result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute expects a non-empty string value and will be ignored")));
							}
							continue;
						case "followsConflict":
							s = attr.Value as string;
							switch(s)
							{
								case "error":
								case "first":
								case "last":
									break;
								default:
									result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p,"followsError attribute expects \"error\", \"first\", or \"last\" and will revert to \"error\".")));
									break;
							}
							continue;
					}
					result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat(p,"Unknown attribute \"", attr.Key, "\" will be ignored")));
				}

			}
			if(null==start)
				result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, string.Concat("start attribute was not specified and the first non-terminal in the grammar (\"",StartSymbol,"\") will be used")));
			return result;
		}
	}
}
