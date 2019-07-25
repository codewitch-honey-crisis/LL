using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	partial class Cfg
	{
		/// <summary>
		/// Enumerates all of the non-terminals in the CFG
		/// </summary>
		/// <returns>All non-terminals in the CFG</returns>
		IEnumerable<string> _EnumNonTerminals()
		{
			var seen = new HashSet<string>();
			// for each rule in the CFG, yield the left hand side if it hasn't been returned already
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (seen.Add(rule.Left))
					yield return rule.Left;
			}
		}
		public IList<string> FillNonTerminals(IList<string> result = null)
		{
			if (null == result) result = new List<string>();
			// for each rule in the CFG, add the left hand side if it hasn't been added already
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (!result.Contains(rule.Left))
					result.Add(rule.Left);
			}
			return result;
		}
		/// <summary>
		/// Enumerates each of the terminals in the CFG, as well as #EOS and #ERROR
		/// </summary>
		/// <returns>An enumeration containing each terminal</returns>
		IEnumerable<string> _EnumTerminals()
		{
			// gather the non-terminals into a collection
			var nts = new HashSet<string>();
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
				nts.Add(Rules[i].Left);
			var seen = new HashSet<string>();
			// just scan through the rules looking for anything that isn't a non-terminal
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
				{
					string r = rule.Right[j];
					if (!nts.Contains(r) && seen.Add(r))
						yield return r;
				}
			}
			// now scan through the attributes looking for any terminals that weren't explicitely in the grammar (hidden terminals)
			foreach (var s in AttributeSets.Keys)
				if (!IsNonTerminal(s))
					if (seen.Add(s))
						yield return s;
			// add EOS and error
			yield return "#EOS";
			yield return "#ERROR";
		}
		public IList<string> FillTerminals(IList<string> result = null)
		{
			if (null == result) result = new List<string>();
			// fetch the non-terminals into a collection
			var nts = new HashSet<string>();
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
				nts.Add(Rules[i].Left);
			// just scan through the rules looking for anything that isn't a non-terminal
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
				{
					string r = rule.Right[j];
					if (!nts.Contains(r) && !result.Contains(r))
						result.Add(r);
				}
			}
			// now scan through the attributes looking for any terminals that weren't explicitely in the grammar (hidden terminals)
			foreach (var s in AttributeSets.Keys)
				if (!IsNonTerminal(s))
					if (!result.Contains(s))
						result.Add(s);

			// add EOS and error
			if (!result.Contains("#EOS"))
				result.Add("#EOS");
			if (!result.Contains("#ERROR"))
				result.Add("#ERROR");
			return result;
		}
		/// <summary>
		/// Enumerates the non-terminals, followed by the terminals in the CFG
		/// </summary>
		/// <returns>An enumeration of all symbols in the CFG, including #EOS and #ERROR</returns>
		IEnumerable<string> _EnumSymbols()
		{
			foreach (var nt in _EnumNonTerminals())
				yield return nt;
			foreach (var t in _EnumTerminals())
				yield return t;
		}
		public IList<string> FillSymbols(IList<string> result = null)
		{
			if (null == result)
				result = new List<string>();
			FillNonTerminals(result);
			FillTerminals(result);
			return result;
		}
		/// <summary>
		/// Indicates whether the specified symbol is a non-terminal
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <returns>True if the symbol is a non-terminal, otherwise false.</returns>
		public bool IsNonTerminal(string symbol)
		{
			foreach (var nt in _EnumNonTerminals())
				if (Equals(nt, symbol))
					return true;
			return false;
		}
		/// <summary>
		/// Indicates whether the specified symbol is a symbol in the grammar
		/// </summary>
		/// <param name="symbol">The symbol</param>
		/// <returns>True if the symbol is a non-terminal, otherwise false.</returns>
		public bool IsSymbol(string symbol)
		{
			foreach (var nt in _EnumSymbols())
				if (Equals(nt, symbol))
					return true;
			return false;
		}
		public bool IsDirectlyLeftRecursive 
			{
			get {
				for (int ic = Rules.Count, i = 0; i < ic; ++i)
					if (Rules[i].IsDirectlyLeftRecursive)
						return true;
				return false;
			}
		}
		public IList<CfgRule> FillNonTerminalRules(string symbol, IList<CfgRule> result = null)
		{
			if (null == result)
				result = new List<CfgRule>();
			for (int ic = Rules.Count, i = 0; i < ic; ++i)
			{
				var rule = Rules[i];
				if (rule.Left == symbol)
					result.Add(rule);
			}
			return result;
		}
		public bool IsNillable(string nonTerminal)
		{
			foreach (var rule in FillNonTerminalRules(nonTerminal))
				if (rule.IsNil)
					return true;
			return false;
		}

		public int GetIdOfSymbol(string symbol)
		{
			var i = 0;
			foreach(var sym in _EnumSymbols())
			{
				if (sym == symbol)
					return i;
				++i;
			}
			return -1;
		}
		public string GetSymbolOfId(int id)
		{
			var i = 0;
			foreach (var sym in _EnumSymbols())
			{
				if (id == i)
					return sym;
				++i;
			}
			return null;
		}
	}
}
