using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public class EbnfOptionalExpression : EbnfUnaryExpression, IEquatable<EbnfOptionalExpression>, ICloneable
	{
		public EbnfOptionalExpression(EbnfExpression expression) { Expression = expression; }
		public EbnfOptionalExpression() { }
		public override bool IsTerminal => false;

		public override IList<IList<string>> ToDisjunctions(EbnfDocument parent,Cfg cfg)
		{
			var l = new List<IList<string>>();
			if (null != Expression) {
				l.AddRange(Expression.ToDisjunctions(parent,cfg));
				var ll = new List<string>();
				if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
					l.Add(ll);
			}
			return l;
		}
		public override CharFA ToFA(EbnfDocument parent, Cfg cfg)
		{
			if (null == Expression)
				return null;
			return CharFA.Optional(Expression.ToFA(parent, cfg), (null == parent) ? "" : parent.GetContainingIdForExpression(this));
		}
		public EbnfOptionalExpression Clone() {
			var result = new EbnfOptionalExpression(Expression);
			result.SetLocationInfo(Line, Column, Position);
			return result;
		}
		object ICloneable.Clone() => Clone();
		public bool Equals(EbnfOptionalExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Equals(Expression, rhs.Expression);
		}
		public override bool Equals(object obj) => Equals(obj as EbnfOptionalExpression);
		public override int GetHashCode()
		{
			if (null != Expression) return Expression.GetHashCode();
			return 0;
		}
		public static bool operator ==(EbnfOptionalExpression lhs, EbnfOptionalExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(EbnfOptionalExpression lhs, EbnfOptionalExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		public override string ToString()
		{
			if (null == Expression) return "[ ]";
			return string.Concat("[ ", Expression.ToString(), " ]");
		} 
	}
}
