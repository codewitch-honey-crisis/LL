using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	/// <summary>
	/// Represents the base class for all EBNF expressions
	/// </summary>
	/// <remarks>Make sure derived classes implement <see cref="ICloneable"/> and value semantics</remarks>
	public abstract class EbnfExpression 
	{
		public abstract IList<IList<string>> ToDisjunctions(EbnfDocument parent,Cfg cfg);
		/// <summary>
		/// Indicates whether or not the expression represents a terminal
		/// </summary>
		public abstract bool IsTerminal {get; }
		public static implicit operator EbnfExpression(string rhs)
		{
			return new EbnfRefExpression(rhs);
		}
		public void SetLocationInfo(int line, int column, long position)
		{
			Line = line;
			Column = column;
			Position = position;
		}
		public int Line { get; private set; }
		public int Column { get; private set; }
		public long Position { get; private set; }

		public abstract CharFA ToFA(EbnfDocument parent, Cfg cfg);
	}
}
