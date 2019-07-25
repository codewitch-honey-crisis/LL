using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{

	public abstract class EbnfBinaryExpression :EbnfExpression
	{
		public EbnfExpression Left { get; set; } = null;
		public EbnfExpression Right { get; set; } = null;
	}
}
