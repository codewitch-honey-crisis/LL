using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	partial class Cfg
	{
		public CfgLL1ParseTable ToLL1ParseTable()
		{
			// Here we populate the outer dictionary with one non-terminal for each key
			// we populate each inner dictionary with the result terminals and associated 
			// rules of the predict tables except in the case where the predict table 
			// contains null. In that case, we use the follows to get the terminals and 
			// the rule associated with the null predict in order to compute the inner 
			// dictionary. The conflict resolution tables are always empty for LL(1)
			var predict = FillPredict();
			var follows = FillFollows();
			var exmsgs = new List<CfgMessage>();
			var result = new CfgLL1ParseTable();
			foreach (var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgLL1ParseTableEntry>();
				foreach (var f in predict[nt])
					if (null != f.Symbol)
					{
						CfgLL1ParseTableEntry re;
						re.ConflictTable = null;
						re.Rule = f.Rule;
						CfgLL1ParseTableEntry or;
						if (d.TryGetValue(f.Symbol, out or))
						{
							exmsgs.Add(new CfgMessage(CfgErrorLevel.Error,1,
										string.Format(
											"FIRST FIRST conflict between {0} and {1} on {2}",
											or.Rule,
											f.Rule,
											f.Symbol)));
						} else
							d.Add(f.Symbol, re);
					}
					else
					{
						var ff = follows[nt];
						foreach (var fe in ff)
						{
							CfgLL1ParseTableEntry or;
							if (d.TryGetValue(fe, out or))
							{
								// we can override conflict handling with the followsConflict
								// attribute. If specified (first/last/error - error is default) it will choose
								// the first or last rule respectively.
								var fc = AttributeSets.GetAttribute(nt, "followsConflict", "error") as string;
								if ("error" == fc)
									exmsgs.Add(new CfgMessage(CfgErrorLevel.Error,2,
										string.Format(
											"FIRST FOLLOWS conflict between {0} and {1} on {2}",
											or.Rule,
											f.Rule,
											fe)));
								else if ("last" == fc)
									d[fe] = new CfgLL1ParseTableEntry(f.Rule);
							}
							else
							{
								d.Add(fe, new CfgLL1ParseTableEntry(f.Rule));
							}
						}
					}

				result.Add(nt, d);
			}
			CfgException.ThrowIfErrors(exmsgs);
			return result;
		}
		CfgLL1ParseTable _MakeConflictTable(CfgRule left,CfgRule right,int index)
		{
			var result = new CfgLL1ParseTable();
			throw new NotImplementedException();
			//return result;
		}
		
		public CfgLL1ParseTable ToLLkParseTable()
		{
			// Here we populate the outer dictionary with one non-terminal for each key
			// we populate each inner dictionary with the result terminals and associated 
			// rules of the predict tables except in the case where the predict table 
			// contains null. In that case, we use the follows to get the terminals and 
			// the rule associated with the null predict in order to compute the inner 
			// dictionary. The conflict resolution tables are created as needed. Basically
			// they are nested parse tables for additional lookahead resolution.
			// The parser can use these to further resolve conflicts.
			var predict = FillPredict();
			var follows = FillFollows();
			var result = new CfgLL1ParseTable();
			foreach (var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgLL1ParseTableEntry>();
				foreach (var f in predict[nt])
					if (null != f.Symbol)
					{
						CfgLL1ParseTableEntry or;
						if (d.TryGetValue(f.Symbol, out or))
						{
							if(null!=or.ConflictTable)
							{

							}
							else if (null != or.Rule)
							{
								var pt = new CfgLL1ParseTable();
								var dd = new Dictionary<string, CfgRule>();
								
								throw new CfgException(
											string.Format(
												"FIRST FIRST conflict between {0} and {1} on {2}",
												or.Rule,
												f.Rule,
												f.Symbol));
							}
						}
						else
						{
							or.ConflictTable = null;
							or.Rule = f.Rule;
							d.Add(f.Symbol, or);
						}
					}
					else
					{
						var ff = follows[nt];
						foreach (var fe in ff)
						{
							CfgLL1ParseTableEntry or;
							if (d.TryGetValue(fe, out or))
							{
								// we can override conflict handling with the followsConflict
								// attribute. If specified (first/last/error - error is default) it will choose
								// the first or last rule respectively.
								var fc = AttributeSets.GetAttribute(nt, "followsConflict", "error") as string;
								if ("error" == fc)
									throw new CfgException(
										string.Format(
											"FIRST FOLLOWS conflict between {0} and {1} on {2}",
											or.Rule,
											f.Rule,
											fe));
								else if ("last" == fc)
									d[fe] = new CfgLL1ParseTableEntry(f.Rule);
							}
							else
							{
								d.Add(fe, new CfgLL1ParseTableEntry(f.Rule));
							}
						}
					}

				result.Add(nt, d);
			}
			return result;
		}
		/// <summary>
		/// Creates a finite state machine that represents the LL(k) parsing rules
		/// </summary>
		/// <returns></returns>
		public FA<string, CfgRule> ToLLkFsm()
		{
			var predict = FillPredict();
			var follows = FillFollows();
			var nts = FillNonTerminals();
			var result = new FA<string, CfgRule>();
			for (int ic = nts.Count, i = 0; i < ic; i++)
			{
				var nt = nts[i];
				var fa = new FA<string, CfgRule>();
				result.Transitions.Add(nt, fa);
				foreach (var p in predict[nt])
				{
					if (null != p.Symbol)
					{
						var ffa = new FA<string, CfgRule>();
						ffa.IsAccepting = true;
						ffa.Accept = p.Rule;
						FA<string, CfgRule> fad;
						if (fa.Transitions.TryGetValue(p.Symbol, out fad))
						{
							System.Diagnostics.Debugger.Break();
							fa.Transitions.Remove(p.Symbol);
							//
							// FIRST FIRST conflict
						}
						else
						{
							fa.Transitions.Add(p.Symbol, ffa);
						}
					}
					else
					{
						foreach (var f in follows[nt])
						{
							var ffa = new FA<string, CfgRule>();
							ffa.IsAccepting = true;
							ffa.Accept = p.Rule;
							FA<string, CfgRule> fad;
							if (fa.Transitions.TryGetValue(f, out fad))
							{
								System.Diagnostics.Debugger.Break();
								// FIRST FOLLOW conflict
							}
							fa.Transitions.Add(f, ffa);
						}
					}
				}
			}
			return result;
		}

		void _AddState(
			(CfgRule Rule, string Symbol) f,
			int ruleIndex,
			FA<string, CfgRule> fa,
			IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> predict,
			IDictionary<string, ICollection<string>> follows)
		{
			FA<string, CfgRule> ffa;
			if (fa.Transitions.TryGetValue(f.Symbol, out ffa))
			{
				// FIRST/FIRST Conflict
				var rOld = ffa.FirstAcceptingState.Accept;
				var rNew = f.Rule;
				var inp = f.Symbol;
				var ntr1 = 1 < rOld.Right.Count - ruleIndex ? rOld.Right[ruleIndex + 1] : null;
				var ntr2 = 1 < rNew.Right.Count - ruleIndex ? rNew.Right[ruleIndex + 1] : null;
				if (null == ntr1 && null == ntr2) // grammar is actually ambiguous
					throw new Exception(string.Format("FIRST FIRST conflict between {0} and {1} on {2}.", rOld, rNew, inp));

				if (ntr1 == ntr2)
				{
					//throw new NotImplementedException("Haven't done k=3+ yet");
					foreach (var ff in predict[ntr1])
					{
						if (null != ff.Symbol)
							_AddState((rOld, ff.Symbol), ruleIndex + 1, ffa, predict, follows);
						else
						{
							foreach (var fff in follows[ntr1])
							{
								_AddState((rOld, fff), ruleIndex + 1, ffa, predict, follows);
							}
						}
					}
					foreach (var ff in predict[ntr2])
					{
						if (null != ff.Symbol)
							_AddState((rNew, ff.Symbol), ruleIndex + 1, ffa, predict, follows);
						else
						{
							foreach (var fff in follows[ntr2])
							{
								_AddState((rNew, fff), ruleIndex + 1, ffa, predict, follows);
							}
						}
					}
					//_AddState((rNew, ntr2), ruleIndex + 1, ffa, predict, follows);
					return;
				}
				else if (null == ntr1)
				{
					if (rOld.Right.Count == ruleIndex)
					{
						ffa.IsAccepting = true;
						ffa.Accept = rOld;
					}
				}
				else if (null == ntr2)
				{
					if (rNew.Right.Count == ruleIndex)
					{
						ffa.IsAccepting = true;
						ffa.Accept = rNew;
					}
				}
				if (null != ntr1)
				{
					foreach (var ff in predict[ntr1])
					{
						if (null != ff.Symbol)
						{
							_AddState((rOld, ff.Symbol), ruleIndex + 1, ffa, predict, follows);
							/*var fffa = new FA<string, CfgRule>();
							fffa.IsAccepting = true;
							fffa.Accept = r1;
							ffa.Transitions.Add(ff.Symbol, fffa);*/
						}
						else
						{
							foreach (var fff in follows[ntr1])
							{
								_AddState((rOld, fff), ruleIndex + 1, ffa, predict, follows);
								/*var fffa = new FA<string, CfgRule>();
								fffa.IsAccepting = true;
								fffa.Accept = r1;
								ffa.Transitions.Add(fff, fffa);*/
							}
						}
					}
				}
				if (null != ntr2)
				{
					foreach (var ff in predict[ntr2])
					{
						if (null != ff.Symbol)
						{
							_AddState((rNew, ff.Symbol), ruleIndex + 1, ffa, predict, follows);
							/*var fffa = new FA<string, CfgRule>();
							fffa.IsAccepting = true;
							fffa.Accept = r2;
							ffa.Transitions.Add(ff.Symbol, fffa);*/
						}
						else
						{
							foreach (var fff in follows[ntr2])
							{
								_AddState((rNew, fff), ruleIndex + 1, ffa, predict, follows);
								/*var fffa = new FA<string, CfgRule>();
								fffa.IsAccepting = true;
								fffa.Accept = r2;
								ffa.Transitions.Add(fff, fffa);*/
							}
						}
					}
				}
			}
			else
			{
				ffa = new FA<string, CfgRule>();
				ffa.IsAccepting = true;
				ffa.Accept = f.Rule;
				fa.Transitions.Add(f.Symbol, ffa);
			}
		}
		/// <summary>
		/// Creates an LL(*) parse table from the configuration
		/// </summary>
		/// <returns>A nested dictionary representing the parse table</returns>
		public IDictionary<string, IDictionary<string, ICollection<CfgRule>>> ToLLStarParseTable()
		{
			// Here we populate the outer dictionary with one non-terminal for each key
			// we populate each inner dictionary with the result terminals and associated 
			// rules of the predict tables except in the case where the predict table 
			// contains null. In that case, we use the follows to get the terminals and 
			// the rule associated with the null predict in order to compute the inner 
			// dictionary
			var predict = FillPredict();
			var follows = FillFollows();
			var result = new Dictionary<string, IDictionary<string, ICollection<CfgRule>>>();
			foreach (var nt in _EnumNonTerminals())
			{
				var d = new Dictionary<string, ICollection<CfgRule>>();
				foreach (var f in predict[nt])
				{
					ICollection<CfgRule> col;
					if (null != f.Symbol)
					{
						if (!d.TryGetValue(f.Symbol, out col))
						{
							col = new List<CfgRule>();
							d.Add(f.Symbol, col);
						}
						if (!col.Contains(f.Rule))
							col.Add(f.Rule);
					}
					else
					{
						var ff = follows[nt];
						foreach (var fe in ff)
						{
							if (!d.TryGetValue(fe, out col))
							{
								col = new List<CfgRule>();
								d.Add(fe, col);
							}
							if (!col.Contains(f.Rule))
								col.Add(f.Rule);
						}
					}
				}
				result.Add(nt, d);
			}
			return result;
		}
	}
}
