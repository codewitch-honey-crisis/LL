using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public enum CfgConflictKind
	{
		FirstFirst=0,
		FirstFollows=1
	}
	public sealed class CfgConflict : IEquatable<CfgConflict>, ICloneable
	{
		public CfgConflict(CfgConflictKind kind,CfgRule rule1,CfgRule rule2,string symbol)
		{
			Kind = kind;
			Rule1 = rule1 ?? throw new ArgumentNullException("rule1");
			Rule2 = rule2 ?? throw new ArgumentNullException("rule2");
			Symbol = symbol;
		}
		public CfgConflictKind Kind { get; } 
		public CfgRule Rule1 { get; }
		public CfgRule Rule2 { get; }
		public string Symbol { get; }
		public bool Equals(CfgConflict rhs)
		{
			if (ReferenceEquals(this, rhs)) return true;
			if (ReferenceEquals(null, rhs)) return false;
			if (Equals(Kind, rhs.Kind) && Equals(Symbol,rhs.Symbol))
			{
				return (Equals(Rule1, rhs.Rule1) && Equals(Rule2, rhs.Rule2))
					|| (Equals(Rule1, rhs.Rule2) && Equals(Rule2, rhs.Rule1)); 
					
			}
			return false;
		}
		public override bool Equals(object obj)
			=> Equals(obj as CfgConflict);
		public override int GetHashCode()
		{
			var result = 0;
			result ^= Kind.GetHashCode();
			result ^= Rule1.GetHashCode();
			result ^= Rule2.GetHashCode();
			result ^= Symbol.GetHashCode();
			return result;
		}
		public static bool operator ==(CfgConflict lhs, CfgConflict rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(CfgConflict lhs, CfgConflict rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
				return true;
			return !lhs.Equals(rhs);
		}
		public CfgConflict Clone()
		{
			return new CfgConflict(Kind, Rule1,Rule2,Symbol);
		}
		object ICloneable.Clone() => Clone();
	}
}
