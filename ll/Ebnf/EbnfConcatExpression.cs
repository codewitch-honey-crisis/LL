using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public class EbnfConcatExpression : EbnfBinaryExpression,IEquatable<EbnfConcatExpression>, ICloneable
	{
		public EbnfConcatExpression(EbnfExpression left,params EbnfExpression[] right)
		{
			if (null == right) right = new EbnfExpression[] { null };
			Left = left;
			for(var i = 0;i<right.Length;++i)
			{
				if (Right == null)
					Right = right[i];
				else
					Right = new EbnfConcatExpression(Right, right[i]);
			}
		}
		public EbnfConcatExpression() { }
		public override bool IsTerminal => false;
		public override IList<IList<string>> ToDisjunctions(EbnfDocument parent,Cfg cfg)
		{
			var l = new List<IList<string>>();
			if(null==Right)
			{
				if (null == Left) return l;
				foreach(var ll in Left.ToDisjunctions(parent,cfg))
					l.Add(new List<string>(ll));
				return l;
			} else if(null==Left)
			{
				foreach (var ll in Right.ToDisjunctions(parent,cfg))
					l.Add(new List<string>(ll));
				return l;
			}
			foreach(var ll in Left.ToDisjunctions(parent,cfg)) { 
				foreach(var ll2 in Right.ToDisjunctions(parent,cfg))
				{
					var ll3 = new List<string>();
					ll3.AddRange(ll);
					ll3.AddRange(ll2);
					if (!l.Contains(ll3, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll3);
				}
			}
			return l;
		}
		public override CharFA ToFA(EbnfDocument parent, Cfg cfg)
		{
			string sym = "";
			if (null != parent)
				sym = parent.GetContainingIdForExpression(this);
			if (null == Right)
			{
				if (null == Left) return null;
				var fa = Left.ToFA(parent, cfg);
				fa.FirstAcceptingState.AcceptingSymbol = sym;
				return fa;
			} else if (null == Left)
			{
				var fa =Right.ToFA(parent, cfg);
				fa.FirstAcceptingState.AcceptingSymbol = sym;
				return fa;
			}
				
			return CharFA.Concat(new CharFA[] { Left.ToFA(parent, cfg), Right.ToFA(parent, cfg) },sym);
		}
		public EbnfConcatExpression Clone()
		{
			var result = new EbnfConcatExpression(null != Left ? ((ICloneable)Left).Clone() as EbnfExpression : null, null != Right ? ((ICloneable)Right).Clone() as EbnfExpression : null);
			result.SetLocationInfo(Line, Column, Position);
			return result;
		}
		object ICloneable.Clone() => Clone();

		public bool Equals(EbnfConcatExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Equals(Left, rhs.Left) && Equals(Right,rhs.Right);
		}
		public override bool Equals(object obj) => Equals(obj as EbnfConcatExpression);
		public override int GetHashCode()
		{
			var result = 0;
			if (null != Left) result =Left.GetHashCode();
			if (null != Right) result ^= Right.GetHashCode();
			return result;
		}
		public static bool operator ==(EbnfConcatExpression lhs, EbnfConcatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(EbnfConcatExpression lhs, EbnfConcatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		public override string ToString()
		{
			if (null == Left) return (null != Right) ? Right.ToString() : "";
			if (null == Right) return Left.ToString();
			string l, r;
			var o = Left as EbnfOrExpression;
			if (null != o)
				l = string.Concat("( ", o.ToString(), " )");
			else
				l = Left.ToString();
			o = Right as EbnfOrExpression;
			if (null != o)
				r = string.Concat("( ", o.ToString(), " )");
			else
				r = Right.ToString();
			return string.Concat(l, " ", r);
		}
	}
}
