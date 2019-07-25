using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public enum CfgErrorLevel
	{
		Message = 0,
		Warning = 1,
		Error = 2
	}
	public sealed class CfgMessage
	{
		public CfgMessage(CfgErrorLevel errorLevel, int errorCode, string message)
		{
			ErrorLevel = errorLevel;
			ErrorCode = errorCode;
			Message = message;
		}
		public CfgErrorLevel ErrorLevel { get; private set; }
		public int ErrorCode { get; private set; }
		public string Message { get; private set; }
		public override string ToString()
		{
			if (-1 != ErrorCode)
				return string.Format("{0}: {1} code {2}",
					ErrorLevel, Message, ErrorCode);
			return string.Format("{0}: {1}",
					ErrorLevel, Message);
		}
	}
}
