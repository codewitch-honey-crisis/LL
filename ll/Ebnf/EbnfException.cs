using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public sealed class EbnfException : Exception
	{
		public IList<EbnfMessage> Messages { get; }
		public EbnfException(string message, int errorCode = -1, int line = 0, int column = 0, long position = -1) :
			this(new EbnfMessage[] { new EbnfMessage(EbnfErrorLevel.Error, errorCode, message, line, column, position) })
		{ }
		static string _FindMessage(IEnumerable<EbnfMessage> messages)
		{
			var l = new List<EbnfMessage>(messages);
			if (null == messages) return "";
			int c = 0;
			foreach (var m in l)
			{
				if (EbnfErrorLevel.Error == m.ErrorLevel)
				{
					if (1 == l.Count)
						return m.ToString();
					return string.Concat(m, " (multiple messages)");
				}
				++c;
			}
			foreach (var m in messages)
				return m.ToString();
			return "";
		}
		public EbnfException(IEnumerable<EbnfMessage> messages) : base(_FindMessage(messages))
		{
			Messages = new List<EbnfMessage>(messages);
		}
		public static void ThrowIfErrors(IEnumerable<EbnfMessage> messages)
		{
			if (null == messages) return;
			foreach (var m in messages)
				if (EbnfErrorLevel.Error == m.ErrorLevel)
					throw new EbnfException(messages);
		}
	}
}
