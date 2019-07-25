using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	/// <summary>
	/// Represents a grammar production
	/// </summary>
	/// <remarks>This class implements value semantics</remarks>

	public class EbnfProduction : IEquatable<EbnfProduction>, ICloneable
	{
		public EbnfProduction(EbnfExpression expression) { Expression = expression; }
		public EbnfProduction() { }
		/// <summary>
		/// Indicates the grammar attributes for the production
		/// </summary>
		public AttributeSet Attributes { get; } = new AttributeSet();
		/// <summary>
		/// Indicates the root expression for the production
		/// </summary>
		public EbnfExpression Expression { get; set; } = null;
		public bool IsTerminal {
			get {
				if (null != Expression && Expression.IsTerminal)
					return true;
				if (Attributes.Terminal)
					return true;
				return false;
			}
		}
		public void SetPositionInfo(int line, int column, long position)
		{
			Line = line;
			Column = column;
			Position = position;
		}
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }
		public EbnfProduction Clone()
		{
			var prod = new EbnfProduction();
			foreach(var attr in Attributes)
				prod.Attributes.Add(attr.Key, attr.Value);
			prod.Expression = ((ICloneable)Expression).Clone() as EbnfExpression;
			prod.SetPositionInfo(Line, Column, Position);
			return prod;
		}
		object ICloneable.Clone() => Clone();
		public override string ToString()
		{
			var sb = new StringBuilder();
			if(0<Attributes.Count)
			{
				var delim = "<";
				foreach(var attr in Attributes)
				{
					sb.Append(delim);
					sb.Append(attr.Key);
					_AppendAttrVal(attr.Value, sb);
					delim = ",";
				}
				sb.Append(">");
			}
			sb.Append("= ");
			sb.Append(null != Expression ? Expression.ToString() : "");
			sb.Append(";");
			return sb.ToString();
		}
		void _AppendAttrVal(object value, StringBuilder sb)
		{
			if (value is bool)
			{
				if (!(bool)value)
				{
					sb.Append("=false");
				}
			}
			else if (value is string)
			{

				sb.Append("=\"");
				sb.Append(((string)value).Replace("\"", "\\\""));
				sb.Append('\"');
			}
			else if (value is char)
			{
				sb.Append("=\"");
				sb.Append(Convert.ToString(value).Replace("\"", "\\\""));
				sb.Append('\"');
			}
			else
			{
				sb.Append('=');
				sb.Append(value);
			}
		}

		public bool Equals(EbnfProduction rhs)
		{
			if (ReferenceEquals(this, rhs)) return true;
			if (ReferenceEquals(null, rhs)) return false;
			if (rhs.Attributes.Count != Attributes.Count) return false;
			if (!Equals(rhs.Expression, Expression)) return false;
			foreach (var attr in Attributes)
			{
				object o;
				if (!rhs.Attributes.TryGetValue(attr.Key, out o) || !Equals(o, attr.Value))
					return false;
			}
			return true;
		}
		public override bool Equals(object obj) => Equals(obj as EbnfProduction);

		public override int GetHashCode()
		{
			var result = 0;
			if (null != Expression)
				result ^= Expression.GetHashCode();
			foreach(var attr in Attributes)
			{
				result ^= attr.Key.GetHashCode();
				if (null != attr.Value)
					result ^= attr.Value.GetHashCode();
			}
			return result;
		}
		
		public static bool operator==(EbnfProduction lhs,EbnfProduction rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(EbnfProduction lhs, EbnfProduction rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}

	}
}
