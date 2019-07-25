using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{

	public class EbnfRegexExpression : EbnfExpression,IEquatable<EbnfRegexExpression>, ICloneable
	{
		public EbnfRegexExpression() { }
		public EbnfRegexExpression(string value) { Value = value; }
		public override bool IsTerminal => true;
		public string Value { get; set; } = null;
		public override IList<IList<string>> ToDisjunctions(EbnfDocument parent,Cfg cfg)
		{
			foreach (var prod in parent.Productions)
			{
				if (Equals(prod.Value.Expression, this))
				{
					var l = new List<IList<string>>();
					var ll = new List<string>();
					l.Add(ll);
					ll.Add(prod.Key);
					return l;
				}
			}
			throw new InvalidOperationException("The terminal was not declared.");
		}
		public override CharFA ToFA(EbnfDocument parent, Cfg cfg)
		{
			return CharFA.Parse(Value,(null==parent)?"":parent.GetIdForExpression(this));
		}
		public EbnfRegexExpression Clone() {
			var result = new EbnfRegexExpression(Value);
			result.SetPositionInfo(Line, Column, Position);
			return result;
		}
		object ICloneable.Clone() => Clone();
		public bool Equals(EbnfRegexExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Equals(Value, rhs.Value);
		}
		public override bool Equals(object obj) => Equals(obj as EbnfRegexExpression);
		public override int GetHashCode()
		{
			if (null != Value) return Value.GetHashCode();
			return 0;
		}
		public static bool operator ==(EbnfRegexExpression lhs, EbnfRegexExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(EbnfRegexExpression lhs, EbnfRegexExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		public override string ToString()
		{
			return string.Concat("\'",Value.Replace("\'","\\\'"), "\'");
		}
	}
}
