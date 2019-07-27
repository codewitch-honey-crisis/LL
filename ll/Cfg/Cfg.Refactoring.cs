using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	partial class Cfg
	{
		static bool _HasFirstFirstConflicts(IList<CfgConflict> conflicts)
		{
			for(int ic=conflicts.Count, i = 0;i<ic;++i)
				if (CfgConflictKind.FirstFirst == conflicts[i].Kind)
					return true;
			return false;
		}
		static bool _HasFirstFollowsConflicts(IList<CfgConflict> conflicts)
		{
			for (int ic = conflicts.Count, i = 0; i < ic; ++i)
				if (CfgConflictKind.FirstFollows == conflicts[i].Kind)
					return true;
			return false;
		}
		public IList<CfgMessage> EliminateUnderivableRules()
		{
			var result = new List<CfgMessage>();
			var ss = StartSymbol;
			if (null == ss)
			{
				result.Add(new CfgMessage(CfgErrorLevel.Warning, -1, "No start symbol set. Nothing done."));
				return result;
			}
			var closure = FillClosure(ss);
			foreach (var rule in new List<CfgRule>(Rules))
				if (!closure.Contains(rule.Left))
				{
					Rules.Remove(rule);
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Rule ", rule, " removed because it was not referenced.")));
				}
			return result;
		}
		public IList<CfgMessage> PrepareLL1(bool throwIfErrors=true)
		{
			var result = new List<CfgMessage>();
			Cfg old = this;
			// if 10 times doesn't sort out this grammar it's not LL(1)
			// the math is such that we don't know unless we try
			// and the tries can go on forever.
			for (int i = 0; i < 10; ++i)
			{
				if (IsDirectlyLeftRecursive)
					result.AddRange(EliminateLeftRecursion());
				var cc = FillConflicts();
				if (_HasFirstFollowsConflicts(cc))
					result.AddRange(EliminateFirstFollowsConflicts());
				cc = FillConflicts();
				if(_HasFirstFirstConflicts(cc))
					result.AddRange(EliminateFirstFirstConflicts());
				//result.AddRange(EliminateUnderivableRules());
				cc = FillConflicts();
				if (0 == cc.Count && !IsDirectlyLeftRecursive)
					break;
				if (old.Equals(this))
					break;
				old = Clone();
			}
			if (IsDirectlyLeftRecursive)
				result.Add(new CfgMessage(CfgErrorLevel.Error, -1, "Grammar is unresolvably and directly left recursive and cannot be parsed with an LL parser."));
			//else if (IsLeftRecursive())
			//	result.Add(new CfgMessage(CfgErrorLevel.Error, -1, "Grammar is unresolvably and indirectly left recursive and cannot be parsed with an LL parser."));
			var fc = FillConflicts();
			foreach (var f in fc)
				result.Add(new CfgMessage(CfgErrorLevel.Error, -1, string.Format("Grammar has unresolvable first-{0} conflict between {1} and {2} on symbol {3}",f.Kind==CfgConflictKind.FirstFirst?"first":"follows",f.Rule1,f.Rule2,f.Symbol)));
			FillValidate(throwIfErrors, result);
			return result;
		}
		public IList<CfgMessage> EliminateFirstFirstConflicts()
		{
			var result = new List<CfgMessage>();
			foreach (var nt in new List<string>(_EnumNonTerminals()))
			{
				var rules = FillNonTerminalRules(nt);
				var rights = new List<IList<string>>();
				foreach (var rule in rules)
					rights.Add(rule.Right);
				while (true)
				{
					var pfx = rights.GetLongestCommonPrefix();
					if (pfx.IsNullOrEmpty())
						break;
					// obv first first conflict
					var nnt = GetTransformId(nt);

					var suffixes = new List<IList<string>>();
					foreach (var rule in rules)
					{
						if (rule.Right.StartsWith(pfx))
						{
							rights.Remove(rule.Right);
							suffixes.Add(new List<string>(rule.Right.Range(pfx.Count)));
							Rules.Remove(rule);
							result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Removed rule {0} because it is part of a first-first conflict.", rule)));

						}
					}

					var newRule = new CfgRule(nt);
					newRule.Right.AddRange(pfx);
					newRule.Right.Add(nnt);
					
					if (!Rules.Contains(newRule))
						Rules.Add(newRule);
					result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Added rule {0} to resolve first-first conflict.", newRule)));
					foreach (var suffix in suffixes)
					{
						newRule = new CfgRule(nnt);
						newRule.Right.AddRange(suffix);
						
						if (!Rules.Contains(newRule))
							Rules.Add(newRule);
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Added rule {0} to resolve first-first conflict.", newRule)));
					}
					
					AttributeSets.SetAttribute(nnt, "collapsed", true);

				}
			}
			return result;
		}
		public IList<CfgMessage> EliminateFirstFollowsConflicts()
		{
			var result = new List<CfgMessage>();
			var conflicts = FillConflicts();
			for(int ic=conflicts.Count,i=0;i<ic;++i)
			{
				var conflict = conflicts[i];
				if(CfgConflictKind.FirstFollows==conflict.Kind)
				{
					if(conflict.Rule1.IsNil || conflict.Rule2.IsNil)
					{
						var rule = conflict.Rule1.IsNil ? conflict.Rule1 : conflict.Rule2;
						// we might be able to do something about this.
						var refs = FillReferencesToSymbol(rule.Left);
						var ntr = FillNonTerminalRules(rule.Left);
						for (int jc=refs.Count,j=0;j<jc;++j)
						{
							for(int kc=ntr.Count,k=0;k<kc;++k)
							{
								var ntrr = ntr[k];
								var r = refs[j];
								var rr = new CfgRule(r.Left, r.Right.Replace(rule.Left, ntrr.Right));
								if(!Rules.Contains(rr))
									Rules.Add(rr);
								result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Added rule ", rr.ToString(), " to resolve first-follows conflict.")));
							}
							result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Removed rule ", refs[j].ToString(), " to resolve first-follows conflict.")));
							Rules.Remove(refs[j]);
						}
						for (int jc = ntr.Count, j = 0; j < jc; ++j)
						{
							Rules.Remove(ntr[j]);
							result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Removed rule ", ntr[j].ToString(), " to resolve first-follows conflict.")));

						}
					}
					
				}
			}
			return result;
		}
		bool _IsLeftRecursive(CfgRule rule, ICollection<CfgRule> visited)
		{
			if (null == visited)
			{
				if (0 < rule.Right.Count)
				{
					return Equals(rule.Left, rule.Right[0]);
				}
				return false;
			}
			if (!visited.Contains(rule))
				visited.Add(rule);
			else
				return true;

			if (0 < rule.Right.Count)
			{
				if (Equals(rule.Left, rule.Right[0]))
					return true;
				foreach (var r in Rules)
				{
					if (Equals(r.Left, rule.Right[0]))
						if (_IsLeftRecursive(r, visited))
							return true;
				}
			}
			return false;
		}
		bool _IsIndirectlyLeftRecursive(CfgRule rule)
		{
			if (_IsLeftRecursive(rule, null))
				return false;
			if (FillLeftDescendants(rule).Contains(rule.Left))
				return true;
			return false;
		}
		public IList<CfgMessage> EliminateLeftRecursion()
		{

			var result = new List<CfgMessage>();
			var done = false;
			while (!done)
			{
				done = true;
				var ic = Rules.Count;
				for (var i = 0; i < ic; ++i)
				{
					var rule = Rules[i];
					if (rule.IsDirectlyLeftRecursive)
					{
						Rules.Remove(rule);
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Removed rule {0} because it is directly left recursive.", rule)));

						var newId = GetTransformId(rule.Left);

						var col = new List<string>();
						var c = rule.Right.Count;
						for (var j = 1; j < c; ++j)
							col.Add(rule.Right[j]);
						col.Add(newId);
						AttributeSets.SetAttribute(newId, "collapsed", true);
						var newRule = new CfgRule(newId);
						newRule.Right.AddRange(col);
						if (!Rules.Contains(newRule))
							Rules.Add(newRule);
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Added rule {1} to replace rule {0}", rule, newRule)));

						var rr = new CfgRule(newId);
						if (!Rules.Contains(rr))
							Rules.Add(rr);
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Format("Added rule {1} to replace rule {0}", rule, rr)));

						foreach (var r in Rules)
						{
							if (Equals(r.Left, rule.Left))
							{
								if (!r.IsDirectlyLeftRecursive)
								{
									r.Right.Add(newId);
								}
							}
						}
						
					
					}
					else if (_IsIndirectlyLeftRecursive(rule))
					{
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Rule ", rule, " modified because it was indirectly left recursive.")));
						Rules.Remove(rule);
						var jc = rule.Right.Count;
						var append = new List<string>(jc - 1);
						for (var j = 1; j < jc; ++j)
							append.Add(rule.Right[j]);
						// do indirect left recursion elimination.
						// first make it directly left recursive.
						var dstRules = FillNonTerminalRules(rule.Right[0]);
						foreach (var drule in dstRules)
						{
							var newRule = new CfgRule(rule.Left);
							// now add the stuff from the dst rule;
							newRule.Right.AddRange(drule.Right);
							newRule.Right.AddRange(append);
							if (!Rules.Contains(newRule))
								Rules.Add(newRule);
							done = false;
							var nt = GetTransformId(rule.Left);
							var allRules = FillNonTerminalRules(rule.Left);
							foreach (var ar in allRules)
							{
								// Section 2.3, 3.2
								// TODO: This needs lots more testing

								if (ar.IsNil || !Equals(ar.Right[0], rule.Left))
								{
									var nar = new CfgRule(rule.Left);
									nar.Right.AddRange(ar.Right);
									nar.Right.Add(nt);
									if (!Rules.Contains(nar))
										Rules.Add(nar);
									Rules.Remove(ar);
								}
								else
								{
									ar.Right.RemoveAt(0);
									ar.Left = nt;
									ar.Right.Add(nt);
									var nr2 = new CfgRule(nt);
									if (!Rules.Contains(nr2))
										Rules.Add(nr2);
								}
								//}

							}

							result.AddRange(EliminateUnderivableRules());

							break;

						}
						if (!done)
							break;
					}
				}
			}
			return result;
		}
		
	}
}
