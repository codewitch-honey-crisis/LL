using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{

	public abstract class EbnfUnaryExpression : EbnfExpression
	{
		public EbnfExpression Expression { get; set; } = null;
	}
}
