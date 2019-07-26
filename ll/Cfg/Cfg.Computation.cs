using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LL
{
	partial class Cfg
	{
		public IList<IList<string>> ExpandRights(IList<IList<string>> rights)
		{
			var result = new List<IList<string>>(rights);
			for(int ic=rights.Count,i=0;i<ic;++i)
			{
				var right = rights[i];
				for(int jc=right.Count,j=0;j<jc;++j)
				{
					var sym = right[j];
					if(IsNonTerminal(sym))
					{
						result.Remove(right);
						var ntr = FillNonTerminalRules(sym);
						for(int kc=ntr.Count,k=0;k<kc;++k)
						{
							var ntrr = ntr[k];
							var newRight = new List<string>(right.Replace(sym, ntrr.Right));
							if (!result.Contains(newRight, OrderedCollectionEqualityComparer<string>.Default))
								result.Add(newRight);
						}
						break;
					}
				}
			}
			return result;
		}
		public IList<IList<string>> ExpandRights(IList<IList<string>> rights,int toK, int maxIterations=10)
		{
			if (0 == toK) return rights;
			var again = true;
			while (again)
			{
				again = false;
				for(int ic=rights.Count,i=0;i<ic;++i)
				{
					var right = rights[i];
					for(int jc=Math.Min(toK,right.Count),j=0;j<jc;++j)
					{
						var sym = right[j];
						if(IsNonTerminal(sym))
						{
							again = true;
							break;
						}
					}
				}
				if (again)
					rights = ExpandRights(rights);
				--maxIterations;
				if (1> maxIterations)
					break;
			}
			var result = new List<IList<string>>(rights.Count);
			for(int ic = rights.Count,i=0;i<ic;++i)
			{
				var right = rights[i].Range(0,toK);
				var r = new List<string>(right);
				if (!result.Contains(r,OrderedCollectionEqualityComparer<string>.Default))
					result.Add(r);
				
			}
			return result;
		}
		public IList<CfgRule> FillReferencesToSymbol(string symbol, IList<CfgRule> result = null)
		{
			if (null == result)
				result = new List<CfgRule>();
			for (int ic = Rules.Count,i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (rule.Right.Contains(symbol))
					if (!result.Contains(rule))
						result.Add(rule);
			}
			return result;
		}
		bool _AreDistinct(IList<IList<string>> left,IList<IList<string>> right)
		{
			for(int ic=left.Count,i=0;i<ic;++i)
				if (right.Contains(left[i], OrderedCollectionEqualityComparer<string>.Default))
					return false;
			return true;
		}
		public int GetK(CfgRule left,CfgRule right,int maxK = 20)
		{
			var lleft = new List<IList<string>>();
			lleft.Add(left.Right);
			var lright = new List<IList<string>>();
			lright.Add(right.Right);
			for(int i = 1;i<maxK;++i)
			{
				if (_AreDistinct(ExpandRights(lleft, i), ExpandRights(lright, i)))
					return i;
			}
			return -1;
		}
		public IDictionary<string,ICollection<(CfgRule,IList<string> Symbols)>> FillPredictK(int k, IDictionary<string, ICollection<(CfgRule, IList<string> Symbols)>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<(CfgRule, IList<string> Symbols)>>();
			if (1 > k)
				k = 1;
			// first add the terminals to the result
			foreach (var t in _EnumTerminals())
			{
				var l = new List<(CfgRule Rule, IList<string> Symbols)>();
				l.Add((null, new List<string>(new string[] { t })));
				result.Add(t, l);
			}
			// now for each rule, find every first right hand side and add it to the rule's left non-terminal result
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				ICollection<(CfgRule Rule, IList<string> Symbols)> col;
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<(CfgRule Rule, IList<string> Symbols)>();
					result.Add(rule.Left, col);
				}
				if (!rule.IsNil)
				{
					var e = (rule, new List<string>(new string[] { rule.Right[0] }));
					if (!col.Contains(e))
						col.Add(e);
				}
				else
				{
					// when it's nil, we represent that with a null
					(CfgRule Rule, IList<string> Symbols) e = (rule, null);
					if (!col.Contains(e))
						col.Add(e);
				}
			}
			return result;
		}
		/// <summary>
		/// Computes the predict table, which contains a collection of terminals and associated rules for each non-terminal.
		/// The terminals represent the terminals that will first appear in the non-terminal.
		/// </summary>
		/// <param name="result">The predict table</param>
		/// <returns>The result</returns>
		public IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> FillPredict(IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<(CfgRule Rule, string Symbol)>>();
			_FillPredictNT(result);
			try
			{


				// finally, for each non-terminal N we still have in the firsts, resolve FIRSTS(N)
				var done = false;
				while (!done)
				{
					done = true;
					foreach (var kvp in result)
					{
						foreach (var item in new List<(CfgRule Rule, string Symbol)>(kvp.Value))
						{
							if (IsNonTerminal(item.Symbol))
							{
								done = false;
								kvp.Value.Remove(item);
								foreach (var f in result[item.Symbol])
									kvp.Value.Add((item.Rule, f.Symbol));
							}
						}
					}
				}
			}
			catch(InvalidOperationException)
			{
				throw new CfgException("This operation cannot be performed because the grammar is left recursive.");
			}
			return result;
		}
		IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> _FillPredictNT(IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<(CfgRule Rule, string Symbol)>>();
			// first add the terminals to the result
			foreach (var t in _EnumTerminals())
			{
				var l = new List<(CfgRule Rule, string Symbol)>();
				l.Add((null, t));
				result.Add(t, l);
			}
			// now for each rule, find every first right hand side and add it to the rule's left non-terminal result
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				ICollection<(CfgRule Rule, string Symbol)> col;
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<(CfgRule Rule, string Symbol)>();
					result.Add(rule.Left, col);
				}
				if (!rule.IsNil)
				{
					var e = (rule, rule.Right[0]);
					if (!col.Contains(e))
						col.Add(e);
				}
				else
				{
					// when it's nil, we represent that with a null
					(CfgRule Rule, string Symbol) e = (rule, null);
					if (!col.Contains(e))
						col.Add(e);
				}
			}
			return result;
		}
		/// <summary>
		/// Computes the predict table, which contains a collection of terminals and associated rules for each non-terminal.
		/// The terminals represent the terminals that will first appear in the non-terminal.
		/// </summary>
		/// <param name="result">The predict table</param>
		/// <returns>The result</returns>
		public IDictionary<string, ICollection<string>> FillFirsts(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			_FillFirstsNT(result);
			// finally, for each non-terminal N we still have in the firsts, resolve FIRSTS(N)
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var kvp in result)
				{
					foreach (var item in new List<string>(kvp.Value))
					{
						if (IsNonTerminal(item))
						{
							done = false;
							kvp.Value.Remove(item);
							foreach (var f in result[item])
								kvp.Value.Add(f);
						}
					}
				}
			}

			return result;
		}
		internal IDictionary<string, ICollection<string>> _FillFirstsNT(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();
			// first add the terminals to the result
			foreach (var t in _EnumTerminals())
			{
				var l = new List<string>();
				l.Add(t);
				result.Add(t, l);
			}
			// now for each rule, find every first right hand side and add it to the rule's left non-terminal result
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				ICollection<string> col;
				if (!result.TryGetValue(rule.Left, out col))
				{
					col = new HashSet<string>();
					result.Add(rule.Left, col);
				}
				if (!rule.IsNil)
				{
					var e = rule.Right[0];
					if (!col.Contains(e))
						col.Add(e);
				}
				else
				{
					// when it's nil, we represent that with a null
					if (!col.Contains(null))
						col.Add(null);
				}
			}
			
			return result;
		}
		public IDictionary<string, ICollection<string>> FillFollows(IDictionary<string, ICollection<string>> result = null)
		{
			if (null == result)
				result = new Dictionary<string, ICollection<string>>();

			// we'll need the predict table
			var predict = FillPredict();

			var ss = StartSymbol;
			for (int ic = Rules.Count, i = -1; i < ic; ++i)
			{
				// here we augment the grammar by inserting START' -> START #EOS as the first rule.
				var rule = (-1 < i) ? Rules[i] : new CfgRule(GetTransformId(ss), ss, "#EOS");
				ICollection<string> col;

				// traverse the rule looking for symbols that follow non-terminals
				if (!rule.IsNil)
				{
					var jc = rule.Right.Count;
					for (var j = 1; j < jc; ++j)
					{
						var r = rule.Right[j];
						var target = rule.Right[j - 1];
						if (IsNonTerminal(target))
						{
							if (!result.TryGetValue(target, out col))
							{
								col = new HashSet<string>();
								result.Add(target, col);
							}
							foreach (var f in predict[r])
							{
								if (null != f.Symbol)
								{
									if (!col.Contains(f.Symbol))
										col.Add(f.Symbol);
								}
								else
								{
									if (!col.Contains(f.Rule.Left))
										col.Add(f.Rule.Left);
								}
							}
						}
					}

					var rr = rule.Right[jc - 1];
					if (IsNonTerminal(rr))
					{
						if (!result.TryGetValue(rr, out col))
						{
							col = new HashSet<string>();
							result.Add(rr, col);
						}
						if (!col.Contains(rule.Left))
							col.Add(rule.Left);
					}
				}
				else // rule is nil
				{
					// what follows is the rule's left nonterminal itself
					if (!result.TryGetValue(rule.Left, out col))
					{
						col = new HashSet<string>();
						result.Add(rule.Left, col);
					}

					if (!col.Contains(rule.Left))
						col.Add(rule.Left);
				}
			}
			// below we look for any non-terminals in the follows result and replace them
			// with their follows, so for example if N appeared, N would be replaced with 
			// the result of FOLLOW(N)
			var done = false;
			while (!done)
			{
				done = true;
				foreach (var kvp in result)
				{
					foreach (var item in new List<string>(kvp.Value))
					{
						if (IsNonTerminal(item))
						{
							done = false;
							kvp.Value.Remove(item);
							foreach (var f in result[item])
								kvp.Value.Add(f);

							break;
						}
					}
				}
			}
			return result;
		}
		

		IList<IList<string>> _Explode(IList<IList<string>> left, IList<IList<string>> right)
		{
			if (null== right||0==right.Count)
				return left;
			else if (null==left||0==left.Count)
				return right;
			var result = new List<IList<string>>();
			for(int ic=left.Count,i=0;i<ic;++i)
			{
				var leftList = left[i];
				for(int jc=right.Count,j=0;j<jc;++j)
				{
					var rightList = right[j];
					var finalList = new List<string>(ic+jc);
					for(int kc=leftList.Count,k=0;k<kc;++k)
						finalList.Add(leftList[k]);
					for (int kc = rightList.Count, k = 0; k < kc; ++k)
						finalList.Add(rightList[k]);
					var oec = OrderedCollectionEqualityComparer<string>.Default;
					var found = false;
					for (int kc = result.Count, k = 0; k < kc; ++k)
					{
						if (oec.Equals(result[k], finalList))
						{
							found = true;
							break;
						}
					}
					if(!found)
						result.Add(finalList);
				}
			}
			return result;
		}
		

		public IList<string> FillClosure(string symbol,IList<string> result=null)
		{
			if (null == result)
				result = new List<string>();
			else if (result.Contains(symbol))
				return result;
			var rules = FillNonTerminalRules(symbol);
			if (0!=rules.Count) // non-terminal
			{
				if (!result.Contains(symbol))
					result.Add(symbol);
				for(int ic=rules.Count,i=0;i<ic;++i)
				{
					var rule = rules[i];
					for(int jc=rule.Right.Count,j=0;j<jc;++j)
						FillClosure(rule.Right[j], result);
				}
			} else if(IsSymbol(symbol))
			{
				// make sure this is a terminal
				if (!result.Contains(symbol))
					result.Add(symbol);
			}
			return result;
		}
	}
}
