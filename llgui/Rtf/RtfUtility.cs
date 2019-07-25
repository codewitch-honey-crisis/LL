using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
namespace LL
{

	static partial class RtfUtility
	{
		public static string ToColorTable(params Color[] colors) => ToColorTable((IEnumerable<Color>)colors);
		public static string ToColorTable(IEnumerable<Color> colors)
		{
			var sb = new StringBuilder();
			sb.Append("{\\colortbl");
			foreach(var c in colors)
			{
				sb.Append("\\red");
				sb.Append(c.R);
				sb.Append("\\green");
				sb.Append(c.G);
				sb.Append("\\blue");
				sb.Append(c.B);
				sb.Append(";");
			}
			sb.Append("}");
			return sb.ToString();
		}
		public static string Escape(IEnumerable<char> @string)
		{
			var sb = new StringBuilder();
			foreach(char ch in @string)
			{
				if('{'==ch || '}'==ch || '\\'==ch)
				{
					sb.Append("\\'");
					sb.Append(((int)ch).ToString("x2"));
				}
				else if(ch<128)
				{
					if (char.IsLetterOrDigit(ch) || char.IsPunctuation(ch))
						sb.Append(ch);
					else
					{
						sb.Append("\\'");
						sb.Append(((int)ch).ToString("x2"));
					}
				} else
				{
					sb.Append("\\u");
					sb.Append(((int)ch).ToString("x4"));
					sb.Append("?");
				}
			}
			return sb.ToString();
		}
	}
}
