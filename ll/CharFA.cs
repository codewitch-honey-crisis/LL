using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LL
{
	// can't used named tuples since CodeDom doesn't really support them
	// (int Accept, (char[] Ranges, int Destination)[])
	using CharDfaEntry = KeyValuePair<int, KeyValuePair<string, int>[]>;
	/// <summary>
	/// Represents a remedial bare bones "regular expression" engine.
	/// </summary>
	/// <remarks>There's just enough here to make it work, not to make it fast or fancy.</remarks>
	public class CharFA : ICloneable
	{
		public CharFA(string accepting = null)
		{
			AcceptingSymbol = accepting;
		}
		/// <summary>
		/// The symbol to return when this state accepts, or null if it does not accept.
		/// </summary>
		public string AcceptingSymbol { get; set; } = null;
		/// <summary>
		/// The input transitions
		/// </summary>
		public IDictionary<char, CharFA> Transitions { get; } = new _TrnsDic();
		/// <summary>
		/// The epsilon transitions (transitions on no input)
		/// </summary>
		public ICollection<CharFA> EpsilonTransitions { get; } = new List<CharFA>();
		class _ExpFA
		{
			internal readonly IDictionary<_ExpFA, string> Transitions = new Dictionary<_ExpFA, string>();
		}
		public bool IsFinal {
			get { return 0 == Transitions.Count && 0 == EpsilonTransitions.Count; }
		}
		public bool IsLoop {
			get { return FillDescendants().Contains(this); }
		}
		public bool IsNeutral {
			get { return 0 == Transitions.Count && 1 == EpsilonTransitions.Count; }
		}
		
		public override string ToString()
		{
			var dfa = ToDfa();
			var sb = new StringBuilder();
			dfa._AppendTo(sb, new List<CharFA>());
			return sb.ToString();
		}
		void _AppendTo(StringBuilder sb, ICollection<CharFA> visited)
		{
			if (null != visited)
			{
				if (visited.Contains(this))
				{
					sb.Append("*");
					return;
				}
				visited.Add(this);
			}

			//var sb = new StringBuilder();
			var trgs = FillInputTransitionRangesGroupedByState();
			var delim = "";
			bool isAccepting = null != AcceptingSymbol;
			if (1 < trgs.Count)
				sb.Append("(");
			foreach (var trg in trgs)
			{
				sb.Append(delim);
				//sb.Append("(");
				if (1 == trg.Value.Count && 1 == trg.Value[0].Length)
					_AppendRangeTo(sb, trg.Value[0]);
				else
				{
					sb.Append("[");
					foreach (var rng in trg.Value)
						_AppendRangeTo(sb, rng);
					sb.Append("]");
				}
				trg.Key._AppendTo(sb, new List<CharFA>(visited));
				//sb.Append(")");
				delim = "|";
			}
			if (1 < trgs.Count)
				sb.Append(")");
			if (isAccepting && !IsFinal && !IsLoop)
				sb.Append("?");
		}
		/// <summary>
		/// Computes the set of all states reachable from this state, including itself. Puts the result in the <paramref name="result"/> field amd returns the same collection."/>
		/// </summary>
		/// <param name="result">The collection to fill, or null for one to be created</param>
		/// <returns>Either <paramref name="result"/> or a new collection filled with the result of the closure computation.</returns>
		public IList<CharFA> FillClosure(IList<CharFA> result = null)
		{
			if (null == result)
				result = new List<CharFA>();
			if (!result.Contains(this))
			{
				result.Add(this);
				foreach (var fa in Transitions.Values)
					fa.FillClosure(result);
				foreach (var fa in EpsilonTransitions)
					fa.FillClosure(result);
			}
			return result;
		}
		/// <summary>
		/// Computes the set of all states reachable from this state. Puts the result in the <paramref name="result"/> field amd returns the same collection."/>
		/// </summary>
		/// <param name="result">The collection to fill, or null for one to be created</param>
		/// <returns>Either <paramref name="result"/> or a new collection filled with the result of the closure computation.</returns>
		public IList<CharFA> FillDescendants(IList<CharFA> result = null)
		{
			if (null == result)
				result = new List<CharFA>();
			foreach (var fa in Transitions.Values)
				fa.FillClosure(result);
			foreach (var fa in EpsilonTransitions)
				fa.FillClosure(result);
			return result;
		}
		/// <summary>
		/// Computes the set of all states reachable from this state on no input, including itself. Puts the result in the <paramref name="result"/> field amd returns the same collection."/>
		/// </summary>
		/// <param name="result">The collection to fill, or null for one to be created</param>
		/// <returns>Either <paramref name="result"/> or a new collection filled with the result of the epsilon closure computation.</returns>
		public IList<CharFA> FillEpsilonClosure(IList<CharFA> result = null)
		{
			if (null == result)
				result = new List<CharFA>();
			if (!result.Contains(this))
			{
				result.Add(this);
				foreach (var fa in EpsilonTransitions)
					fa.FillEpsilonClosure(result);
			}
			return result;
		}
		public static IList<CharFA> FillEpsilonClosure(IEnumerable<CharFA> states, IList<CharFA> result = null)
		{
			if (null == result)
				result = new List<CharFA>();
			foreach (var fa in states)
				fa.FillEpsilonClosure(result);
			return result;
		}
		/// <summary>
		/// Creates a clone of this FA state
		/// </summary>
		/// <returns>A new FA that is equal to this FA</returns>
		public CharFA Clone()
		{
			var closure = FillClosure();
			var nclosure = new CharFA[closure.Count];
			for (var i = 0; i < nclosure.Length; i++)
			{
				nclosure[i] = new CharFA();
				nclosure[i].AcceptingSymbol = closure[i].AcceptingSymbol;
			}
			for (var i = 0; i < nclosure.Length; i++)
			{
				var t = nclosure[i].Transitions;
				var e = nclosure[i].EpsilonTransitions;
				foreach (var trns in closure[i].Transitions)
				{
					var id = closure.IndexOf(trns.Value);
					t.Add(trns.Key, nclosure[id]);
				}
				foreach (var trns in closure[i].EpsilonTransitions)
				{
					var id = closure.IndexOf(trns);
					e.Add(nclosure[id]);
				}
			}
			return nclosure[0];
		}
		public bool IsLiteral {
			get {
				var closure = FillClosure();
				int ic = closure.Count, i = 0;
				for (;i<ic;++i)
				{
					var fa = closure[i];
					if (!(fa.IsNeutral || fa.IsFinal || (0 == fa.EpsilonTransitions.Count && 1 == fa.Transitions.Count)))
						break;
				}
				return (i == ic);
			}
		}
		public CharFA ClonePath(CharFA to)
		{
			var closure = FillClosure();
			var nclosure = new CharFA[closure.Count];
			for (var i = 0; i < nclosure.Length; i++)
			{
				nclosure[i] = new CharFA();
				nclosure[i].AcceptingSymbol = closure[i].AcceptingSymbol;
			}
			for (var i = 0; i < nclosure.Length; i++)
			{
				var t = nclosure[i].Transitions;
				var e = nclosure[i].EpsilonTransitions;
				foreach (var trns in closure[i].Transitions)
				{
					if(trns.Value.FillClosure().Contains(to)) {
						var id = closure.IndexOf(trns.Value);

						t.Add(trns.Key, nclosure[id]);
					}
				}
				foreach (var trns in closure[i].EpsilonTransitions)
				{
					if (trns.FillClosure().Contains(to))
					{
						var id = closure.IndexOf(trns);
						e.Add(nclosure[id]);
					}
				}
			}
			return nclosure[0];

		}
		public sealed class DotGraphOptions
		{
			/// <summary>
			/// The resolution, in dots-per-inch to render at
			/// </summary>
			public int Dpi { get; set; } = 300;
			/// <summary>
			/// The prefix used for state labels
			/// </summary>
			public string StatePrefix { get; set; } = "q";

			/// <summary>
			/// If non-null, specifies a debug render using the specified input string.
			/// </summary>
			/// <remarks>The debug render is useful for tracking the transitions in a state machine</remarks>
			public IEnumerable<char> DebugString { get; set; } = null;
			/// <summary>
			/// If non-null, specifies the source NFA from which this DFA was derived - used for debug view
			/// </summary>
			public CharFA DebugSourceNfa { get; set; } = null;
		}
		/// <summary>
		/// Writes a Graphviz dot specification to the specified <see cref="TextWriter"/>
		/// </summary>
		/// <param name="writer">The writer</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public void WriteDotTo(TextWriter writer, DotGraphOptions options = null)
		{
			_WriteDotTo(FillClosure(), writer, options);
		}
		/// <summary>
		/// Writes a Graphviz dot specification of the specified closure to the specified <see cref="TextWriter"/>
		/// </summary>
		/// <param name="closure">The closure of all states</param>
		/// <param name="writer">The writer</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		static void _WriteDotTo(IList<CharFA> closure, TextWriter writer, DotGraphOptions options = null)
		{
			if (null == options) options = new DotGraphOptions();
			string spfx = null == options.StatePrefix ? "q" : options.StatePrefix;
			writer.WriteLine("digraph FA {");
			writer.WriteLine("rankdir=LR");
			writer.WriteLine("node [shape=circle]");
			var finals = new List<CharFA>();
			var neutrals = new List<CharFA>();
			var accepting = new List<CharFA>();
			foreach (var ffa in closure)
			{
				if (null != ffa.AcceptingSymbol)
					accepting.Add(ffa);
				if (0 == ffa.Transitions.Count && 0 == ffa.EpsilonTransitions.Count && null == ffa.AcceptingSymbol)
					finals.Add(ffa);
			}
			IList<CharFA> fromStates = null;
			IList<CharFA> toStates = null;
			char tchar = default(char);
			toStates = closure[0].FillEpsilonClosure();
			if (null != options.DebugString)
			{
				foreach (char ch in options.DebugString)
				{
					fromStates = FillEpsilonClosure(toStates, null);
					tchar = ch;
					toStates = CharFA.FillMove(fromStates, ch);
					if (0 == toStates.Count)
						break;

				}
			}
			if (null != toStates)
			{
				toStates = FillEpsilonClosure(toStates, null);
			}
			int i = 0;
			foreach (var ffa in closure)
			{
				if (!finals.Contains(ffa))
				{
					if (null != ffa.AcceptingSymbol)
						accepting.Add(ffa);
					else if (0 == ffa.Transitions.Count && 1 == ffa.EpsilonTransitions.Count)
						neutrals.Add(ffa);
				}
				var rngGrps = ffa.FillInputTransitionRangesGroupedByState(null);
				foreach (var rngGrp in rngGrps)
				{
					var di = closure.IndexOf(rngGrp.Key);
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(di.ToString());
					writer.Write(" [label=\"");
					var sb = new StringBuilder();
					foreach (CharRange range in rngGrp.Value)
						_AppendRangeTo(sb, range);

					if (sb.Length != 1 || " " == sb.ToString())
					{
						writer.Write('[');
						writer.Write(_EscapeLabel(sb.ToString()));
						writer.Write(']');
					}
					else
						writer.Write(_EscapeLabel(sb.ToString()));
					writer.WriteLine("\"]");
				}
				// do epsilons
				foreach (var fffa in ffa.EpsilonTransitions)
				{
					writer.Write(spfx);
					writer.Write(i);
					writer.Write("->");
					writer.Write(spfx);
					writer.Write(closure.IndexOf(fffa));
					writer.WriteLine(" [style=dashed,color=gray]");
				}


				++i;
			}
			string delim = "";
			i = 0;
			foreach (var ffa in closure)
			{
				writer.Write(spfx);
				writer.Write(i);
				writer.Write(" [");
				if (null != options.DebugString)
				{
					if (null != toStates && toStates.Contains(ffa))
					{
						writer.Write("color=green,");
					}
					if (null != fromStates && fromStates.Contains(ffa) && (null == toStates || !toStates.Contains(ffa)))
					{
						writer.Write("color=darkgreen,");
					}
				}
				writer.Write("label=<");
				writer.Write("<TABLE BORDER=\"0\"><TR><TD>");
				writer.Write(spfx);
				writer.Write("<SUB>");
				writer.Write(i);
				writer.Write("</SUB></TD></TR>");

				if (null != ffa.AcceptingSymbol)
				{
					writer.Write("<TR><TD>");
					writer.Write(Convert.ToString(ffa.AcceptingSymbol).Replace("\"", "&quot;"));
					writer.Write("</TD></TR>");

				}
				writer.Write("</TABLE>");
				writer.Write(">");
				bool isfinal = false;
				if (accepting.Contains(ffa) || (isfinal = finals.Contains(ffa)))
					writer.Write(",shape=doublecircle");
				if (isfinal || neutrals.Contains(ffa))
				{
					if ((null == fromStates || !fromStates.Contains(ffa)) &&
						(null == toStates || !toStates.Contains(ffa)))
					{
						writer.Write(",color=gray");
					}
				}
				writer.WriteLine("]");
				++i;
			}
			delim = "";
			if (0 < accepting.Count)
			{
				foreach (var ntfa in accepting)
				{
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle]");
			}
			delim = "";
			if (0 < neutrals.Count)
			{
				delim = "";
				foreach (var ntfa in neutrals)
				{
					if ((null == fromStates || !fromStates.Contains(ntfa)) &&
						(null == toStates || !toStates.Contains(ntfa))
						)
					{
						writer.Write(delim);
						writer.Write(spfx);
						writer.Write(closure.IndexOf(ntfa));
						delim = ",";
					}
				}
				writer.WriteLine(" [color=gray]");
				delim = "";
				if (null != fromStates)
				{
					foreach (var ntfa in neutrals)
					{
						if (fromStates.Contains(ntfa) && (null == toStates || !toStates.Contains(ntfa)))
						{
							writer.Write(delim);
							writer.Write(spfx);
							writer.Write(closure.IndexOf(ntfa));
							delim = ",";
						}
					}

					writer.WriteLine(" [color=darkgreen]");
				}
				if (null != toStates)
				{
					delim = "";
					foreach (var ntfa in neutrals)
					{
						if (toStates.Contains(ntfa))
						{
							writer.Write(delim);
							writer.Write(spfx);
							writer.Write(closure.IndexOf(ntfa));
							delim = ",";
						}
					}
					writer.WriteLine(" [color=green]");
				}


			}
			delim = "";
			if (0 < finals.Count)
			{
				foreach (var ntfa in finals)
				{
					writer.Write(delim);
					writer.Write(spfx);
					writer.Write(closure.IndexOf(ntfa));
					delim = ",";
				}
				writer.WriteLine(" [shape=doublecircle,color=gray]");
			}

			writer.WriteLine("}");

		}
		public static void _AppendRangeTo(StringBuilder builder, CharRange range)
		{
			_AppendRangeCharTo(builder, range.First);
			if (0 == range.Last.CompareTo(range.First)) return;
			if (range.Last == range.First + 1) // spit out 1 length ranges as two chars
			{
				_AppendRangeCharTo(builder, range.Last);
				return;
			}
			builder.Append('-');
			_AppendRangeCharTo(builder, range.Last);
		}
		static void _AppendRangeCharTo(StringBuilder builder, char rangeChar)
		{
			switch (rangeChar)
			{
				case '-':
				case '\\':
					builder.Append('\\');
					builder.Append(rangeChar);
					return;
				case '\t':
					builder.Append("\\t");
					return;
				case '\n':
					builder.Append("\\n");
					return;
				case '\r':
					builder.Append("\\r");
					return;
				case '\0':
					builder.Append("\\0");
					return;
				case '\f':
					builder.Append("\\f");
					return;
				case '\v':
					builder.Append("\\v");
					return;
				case '\b':
					builder.Append("\\b");
					return;
				default:
					if (!char.IsLetterOrDigit(rangeChar) && !char.IsSeparator(rangeChar) && !char.IsPunctuation(rangeChar) && !char.IsSymbol(rangeChar))
					{

						builder.Append("\\u");
						builder.Append(unchecked((ushort)rangeChar).ToString("x4"));

					}
					else
						builder.Append(rangeChar);
					break;
			}
		}
		static string _EscapeLabel(string label)
		{
			if (string.IsNullOrEmpty(label)) return label;

			string result = label.Replace("\\", @"\\");
			result = result.Replace("\"", "\\\"");
			result = result.Replace("\n", "\\n");
			result = result.Replace("\r", "\\r");
			result = result.Replace("\0", "\\0");
			result = result.Replace("\v", "\\v");
			result = result.Replace("\t", "\\t");
			result = result.Replace("\f", "\\f");
			return result;
		}
		/// <summary>
		/// Renders Graphviz output for this machine to the specified file
		/// </summary>
		/// <param name="filename">The output filename. The format to render is indicated by the file extension.</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		public void RenderToFile(string filename, DotGraphOptions options = null)
		{
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			string ext = Path.GetExtension(filename);
			if (0 == string.Compare(".png", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "png";
			else if (0 == string.Compare(".jpg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "jpg";
			else if (0 == string.Compare(".bmp", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "bmp";
			else if (0 == string.Compare(".svg", ext, StringComparison.InvariantCultureIgnoreCase))
				args += "svg";
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			args += " -o\"" + filename + "\"";

			var psi = new ProcessStartInfo("dot", args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true
			};
			using (var proc = Process.Start(psi))
			{
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				proc.WaitForExit();
			}

		}
		/// <summary>
		/// Renders Graphviz output for this machine to a stream
		/// </summary>
		/// <param name="format">The output format. The format to render can be any supported dot output format. See dot command line documation for details.</param>
		/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
		/// <returns>A stream containing the output. The caller is expected to close the stream when finished.</returns>
		public Stream RenderToStream(string format, bool copy = false, DotGraphOptions options = null)
		{
			if (null == options)
				options = new DotGraphOptions();
			string args = "-T";
			args += string.Concat(" ", format);
			if (0 < options.Dpi)
				args += " -Gdpi=" + options.Dpi.ToString();

			var psi = new ProcessStartInfo("dot", args)
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			};
			using (var proc = Process.Start(psi))
			{
				WriteDotTo(proc.StandardInput, options);
				proc.StandardInput.Close();
				if (!copy)
					return proc.StandardOutput.BaseStream;
				else
				{
					MemoryStream stm = new MemoryStream();
					proc.StandardOutput.BaseStream.CopyTo(stm);
					proc.StandardOutput.BaseStream.Close();
					proc.WaitForExit();
					return stm;
				}
			}
		}
		object ICloneable.Clone() => Clone();
		/// <summary>
		/// Creates an FA that matches a literal string
		/// </summary>
		/// <param name="string">The string to match</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA machine that will match this literal</returns>
		public static CharFA Literal(IEnumerable<char> @string, string accept = "")
		{
			var result = new CharFA();
			var current = result;
			foreach (char ch in @string)
			{
				current.AcceptingSymbol = null;
				var fa = new CharFA();
				fa.AcceptingSymbol = accept;
				current.Transitions.Add(ch, fa);
				current = fa;
			}
			return result;
		}
		/// <summary>
		/// Creates an FA that will match any one of a set of a characters
		/// </summary>
		/// <param name="set">The set of characters that will be matched</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>An FA that will match the specified set</returns>
		public static CharFA Set(IEnumerable<char> set, string accept = "")
		{
			var result = new CharFA();
			var final = new CharFA();
			final.AcceptingSymbol = accept;
			foreach (char ch in set)
				result.Transitions.Add(ch, final);
			return result;
		}
		/// <summary>
		/// Creates an FA that will match any one of a set of a characters
		/// </summary>
		/// <param name="ranges">The set ranges of characters that will be matched</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>An FA that will match the specified set</returns>
		public static CharFA Set(IEnumerable<CharRange> ranges, string accept = "")
		{
			var result = new CharFA();
			var final = new CharFA();
			final.AcceptingSymbol = accept;
			foreach (char ch in CharRange.ExpandRanges(ranges))
				result.Transitions.Add(ch, final);
			return result;
		}
		/// <summary>
		/// Creates a new FA that is a concatenation of two other FA expressions
		/// </summary>
		/// <param name="exprs">The FAs to concatenate</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that is the concatenation of the specified FAs</returns>
		public static CharFA Concat(IEnumerable<CharFA> exprs, string accept = "")
		{
			CharFA left = null;
			var right = left;
			foreach (var val in exprs)
			{
				if (null == val) continue;
				var nval = val.Clone();
				if (null == left)
				{
					left = nval;
					continue;
				}
				else if (null == right)
					right = nval;
				else
					_Concat(right, nval);

				_Concat(left, right);
			}
			right.FirstAcceptingState.AcceptingSymbol = accept;
			return left;
		}
		static void _Concat(CharFA lhs, CharFA rhs)
		{
			var f = lhs.FirstAcceptingState;
			f.EpsilonTransitions.Add(rhs);
			f.AcceptingSymbol = null;
		}
		/// <summary>
		/// Creates a new FA that matche any one of the FA expressions passed
		/// </summary>
		/// <param name="exprs">The expressions to match</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that will match the union of the FA expressions passed</returns>
		public static CharFA Or(IEnumerable<CharFA> exprs, string accept = "")
		{
			var result = new CharFA();
			var final = new CharFA();
			final.AcceptingSymbol = accept;
			foreach (var fa in exprs)
			{
				var nfa = fa.Clone();
				result.EpsilonTransitions.Add(nfa);
				var nffa = nfa.FirstAcceptingState;
				nffa.AcceptingSymbol = null;
				nffa.EpsilonTransitions.Add(final);

			}
			return result;
		}
		/// <summary>
		/// Creates a new FA that will match a repetition of one or more of the specified FA expression
		/// </summary>
		/// <param name="expr">The expression to repeat</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that matches the specified FA one or more times</returns>
		public static CharFA Repeat(CharFA expr, string accept = "")
		{
			var result = new CharFA();
			var final = new CharFA();
			var e = expr.Clone();
			final.AcceptingSymbol = accept;
			var afa = e.FirstAcceptingState;
			afa.AcceptingSymbol = null;
			afa.EpsilonTransitions.Add(final);
			afa.EpsilonTransitions.Add(result);
			result.EpsilonTransitions.Add(e);
			return result;
		}
		/// <summary>
		/// Creates a new FA that matches the specified FA expression or empty
		/// </summary>
		/// <param name="expr">The expression to make optional</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that will match the specified expression or empty</returns>
		public static CharFA Optional(CharFA expr, string accept = "")
		{
			var result = expr.Clone();
			var f = result.FirstAcceptingState;
			f.AcceptingSymbol = accept;
			result.EpsilonTransitions.Add(f);
			return result;
		}
		/// <summary>
		/// Creates a new FA that will match a repetition of zero or more of the specified FA expressions
		/// </summary>
		/// <param name="expr">The expression to repeat</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that matches the specified FA zero or more times</returns>
		public static CharFA Kleene(CharFA expr, string accept = "")
		{
			return Optional(Repeat(expr), accept);
		}
		/// <summary>
		/// Returns the first state that accepts from a given FA, or null if none do.
		/// </summary>
		public CharFA FirstAcceptingState {
			get {
				foreach (var fa in FillClosure())
					if (null != fa.AcceptingSymbol)
						return fa;
				return null;
			}
		}
		/// <summary>
		/// Fills a collection with the result of moving each of the specified <paramref name="states"/> by the specified input.
		/// </summary>
		/// <param name="states">The states to examine</param>
		/// <param name="input">The input to use</param>
		/// <param name="result">The states that are now entered as a result of the move</param>
		/// <returns><paramref name="result"/> or a new collection if it wasn't specified.</returns>
		public static IList<CharFA> FillMove(IEnumerable<CharFA> states, char input, IList<CharFA> result = null)
		{
			if (null == result) result = new List<CharFA>();
			foreach (var fa in FillEpsilonClosure(states))
			{
				// examine each of the states reachable from this state on no input

				CharFA ofa;
				// see if this state has this input in its transitions
				if (fa.Transitions.TryGetValue(input, out ofa))
					foreach (var efa in ofa.FillEpsilonClosure())
						if (!result.Contains(efa)) // if it does, add it if it's not already there
							result.Add(efa);
			}
			return result;
		}

		public CharDfaEntry[] ToDfaTable(IDictionary<string, int> symbolLookup = null)
		{
			var dfa = ToDfa();
			var closure = dfa.FillClosure();
			if (symbolLookup == null)
			{
				symbolLookup = new Dictionary<string, int>();
				var i = 0;
				for (int jc = closure.Count, j = 0; j < jc; ++j)
				{
					var fa = closure[j];
					if (null != fa.AcceptingSymbol && !symbolLookup.ContainsKey(fa.AcceptingSymbol))
					{
						symbolLookup.Add(fa.AcceptingSymbol, i);
						++i;
					}
				}
			}
			// (int Accept, (char[] Ranges, int Destination)[])[]
			var result = new CharDfaEntry[closure.Count];
			for (var i = 0; i < result.Length; i++)
			{
				var fa = closure[i];
				var trgs = fa.FillInputTransitionRangesGroupedByState();
				var trns = new KeyValuePair<string, int>[trgs.Count];
				var j = 0;

				foreach (var trg in trgs)
				{
					trns[j] = new KeyValuePair<string, int>(
						CharRange.ToPackedString(trg.Value),
						closure.IndexOf(trg.Key));

					++j;
				}
				result[i] = new CharDfaEntry(
					(null != fa.AcceptingSymbol) ? symbolLookup[fa.AcceptingSymbol] : -1,
					trns);

			}
			return result;
		}

		/// <summary>
		/// Returns a <see cref="IDictionary{FA,IList{KeyValuePair{Char,Char}}}"/>, keyed by state, that contains all of the outgoing local input transitions, expressed as a series of ranges
		/// </summary>
		/// <param name="result">The <see cref="IDictionary{FA,IList{CharRange}}"/> to fill, or null to create one.</param>
		/// <returns>A <see cref="IDictionary{FA,IList{CharRange}}"/> containing the result of the query</returns>
		public IDictionary<CharFA, IList<CharRange>> FillInputTransitionRangesGroupedByState(IDictionary<CharFA, IList<CharRange>> result = null)
		{
			if (null == result)
				result = new Dictionary<CharFA, IList<CharRange>>();
			// using the optimized dictionary we have little to do here.
			foreach (var trns in (IDictionary<CharFA, ICollection<char>>)Transitions)
			{
				result.Add(trns.Key, new List<CharRange>(CharRange.GetRanges(trns.Value)));
			}
			return result;
		}
		public CharFA ToDfa()
		{
			// The DFA states are keyed by the set of NFA states they represent.
			var dfaMap = new Dictionary<List<CharFA>, CharFA>(_SetComparer.Default);

			var unmarked = new HashSet<CharFA>();

			// compute the epsilon closure of the initial state in the NFA
			var states = new List<CharFA>();

			FillEpsilonClosure(states);

			// create a new state to represent the current set of states. If one 
			// of those states is accepting, set this whole state to be accepting.
			CharFA dfa = new CharFA();
			var al = new List<string>();
			foreach (var fa in states)
				if (null != fa.AcceptingSymbol)
					if (!al.Contains(fa.AcceptingSymbol))
						al.Add(fa.AcceptingSymbol);
			int ac = al.Count;
			if (1 == ac)
				dfa.AcceptingSymbol = al[0];
			else if (1 < ac)
				dfa.AcceptingSymbol = string.Join("|", al); // hang on to the multiple symbols


			CharFA result = dfa; // store the initial state for later, so we can return it.

			// add it to the dfa map
			dfaMap.Add(states, dfa);

			// add it to the unmarked states, signalling that we still have work to do.
			unmarked.Add(dfa);
			bool done = false;
			while (!done)
			{
				done = true;
				HashSet<List<CharFA>> mapKeys = new HashSet<List<CharFA>>(dfaMap.Keys, _SetComparer.Default);
				foreach (List<CharFA> mapKey in mapKeys)
				{
					dfa = dfaMap[mapKey];
					if (unmarked.Contains(dfa))
					{
						// when we get here, mapKey represents the epsilon closure of our 
						// current dfa state, which is indicated by kvp.Value

						// build the transition list for the new state by combining the transitions
						// from each of the old states

						// retrieve every possible input for these states
						HashSet<char> inputs = new HashSet<char>();
						foreach (CharFA state in mapKey)
						{
							var dtrns = (IDictionary<CharFA, ICollection<char>>)state.Transitions;
							foreach (var trns in dtrns)
							{
								foreach (var inp in trns.Value)
									inputs.Add(inp);
							}

						}

						foreach (var input in inputs)
						{
							var acc = new List<string>();
							List<CharFA> ns = new List<CharFA>();
							foreach (var state in mapKey)
							{
								CharFA dst = null;
								if (state.Transitions.TryGetValue(input, out dst))
								{
									foreach (var d in dst.FillEpsilonClosure())
									{
										if (null != d.AcceptingSymbol)
											if (!acc.Contains(d.AcceptingSymbol))
												acc.Add(d.AcceptingSymbol);
										if (!ns.Contains(d))
											ns.Add(d);
									}
								}
							}

							CharFA ndfa;
							if (!dfaMap.TryGetValue(ns, out ndfa))
							{
								ndfa = new CharFA();
								ac = acc.Count;
								if (1 == ac)
									ndfa.AcceptingSymbol = acc[0];
								else if (1 < ac)
									ndfa.AcceptingSymbol = string.Join("|", acc);
								else
									ndfa.AcceptingSymbol = null;



								dfaMap.Add(ns, ndfa);
								unmarked.Add(ndfa);
								done = false;
							}
							dfa.Transitions.Add(input, ndfa);
						}
						unmarked.Remove(dfa);
					}
				}
			}
			return result;
		}
		class _TrnsDic : IDictionary<char, CharFA>, IDictionary<CharFA, ICollection<char>>
		{
			IDictionary<CharFA, ICollection<char>> _inner = new ListDictionary<CharFA, ICollection<char>>();

			public CharFA this[char key] {
				get {
					foreach (var trns in _inner)
					{
						if (trns.Value.Contains(key))
							return trns.Key;
					}
					throw new KeyNotFoundException();
				}
				set {
					Remove(key);
					ICollection<char> hs;
					if (_inner.TryGetValue(value, out hs))
					{
						hs.Add(key);
					}
					else
					{
						hs = new HashSet<char>();
						hs.Add(key);
						_inner.Add(value, hs);
					}
				}
			}

			public ICollection<char> Keys {
				get {
					return new _KeysCollection(_inner);
				}

			}

			sealed class _KeysCollection : ICollection<char>
			{
				IDictionary<CharFA, ICollection<char>> _inner;
				public _KeysCollection(IDictionary<CharFA, ICollection<char>> inner)
				{
					_inner = inner;
				}
				public int Count {
					get {
						var result = 0;
						foreach (var val in _inner.Values)
							result += val.Count;
						return result;
					}
				}
				void _ThrowReadOnly() { throw new NotSupportedException("The collection is read-only."); }
				public bool IsReadOnly => true;

				public void Add(char item)
				{
					_ThrowReadOnly();
				}

				public void Clear()
				{
					_ThrowReadOnly();
				}

				public bool Contains(char item)
				{
					foreach (var val in _inner.Values)
						if (val.Contains(item))
							return true;
					return false;
				}

				public void CopyTo(char[] array, int arrayIndex)
				{
					var si = arrayIndex;
					foreach (var val in _inner.Values)
					{
						val.CopyTo(array, si);
						si += val.Count;
					}
				}

				public IEnumerator<char> GetEnumerator()
				{
					foreach (var val in _inner.Values)
						foreach (var ch in val)
							yield return ch;
				}

				public bool Remove(char item)
				{
					_ThrowReadOnly();
					return false;
				}

				IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			}
			sealed class _ValuesCollection : ICollection<CharFA>
			{
				IDictionary<CharFA, ICollection<char>> _inner;
				public _ValuesCollection(IDictionary<CharFA, ICollection<char>> inner)
				{
					_inner = inner;
				}
				public int Count {
					get {
						var result = 0;
						foreach (var val in _inner.Values)
							result += val.Count;
						return result;
					}
				}
				void _ThrowReadOnly() { throw new NotSupportedException("The collection is read-only."); }
				public bool IsReadOnly => true;

				public void Add(CharFA item)
				{
					_ThrowReadOnly();
				}

				public void Clear()
				{
					_ThrowReadOnly();
				}

				public bool Contains(CharFA item)
				{
					return _inner.Keys.Contains(item);
				}

				public void CopyTo(CharFA[] array, int arrayIndex)
				{
					var si = arrayIndex;
					foreach (var trns in _inner)
					{
						foreach (var ch in trns.Value)
						{
							array[si] = trns.Key;
							++si;
						}
					}
				}

				public IEnumerator<CharFA> GetEnumerator()
				{
					foreach (var trns in _inner)
						foreach (var ch in trns.Value)
							yield return trns.Key;
				}

				public bool Remove(CharFA item)
				{
					_ThrowReadOnly();
					return false;
				}

				IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			}
			public ICollection<CharFA> Values { get { return new _ValuesCollection(_inner); } }

			public int Count {
				get {
					var result = 0;
					foreach (var trns in _inner)
						result += trns.Value.Count;
					return result;
				}
			}

			ICollection<CharFA> IDictionary<CharFA, ICollection<char>>.Keys { get { return _inner.Keys; } }
			ICollection<ICollection<char>> IDictionary<CharFA, ICollection<char>>.Values { get { return _inner.Values; } }
			int ICollection<KeyValuePair<CharFA, ICollection<char>>>.Count { get { return _inner.Count; } }
			public bool IsReadOnly { get { return _inner.IsReadOnly; } }

			ICollection<char> IDictionary<CharFA, ICollection<char>>.this[CharFA key] { get { return _inner[key]; } set { _inner[key] = value; } }

			public void Add(char key, CharFA value)
			{
				if (ContainsKey(key))
					throw new InvalidOperationException("The key is already present in the dictionary.");
				ICollection<char> hs;
				if (_inner.TryGetValue(value, out hs))
				{
					hs.Add(key);
				}
				else
				{
					hs = new HashSet<char>();
					hs.Add(key);
					_inner.Add(value, hs);
				}
			}

			public void Add(KeyValuePair<char, CharFA> item)
			{
				Add(item.Key, item.Value);
			}

			public void Clear()
			{
				_inner.Clear();
			}

			public bool Contains(KeyValuePair<char, CharFA> item)
			{
				ICollection<char> hs;
				return _inner.TryGetValue(item.Value, out hs) && hs.Contains(item.Key);
			}

			public bool ContainsKey(char key)
			{
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
						return true;
				}
				return false;
			}

			public void CopyTo(KeyValuePair<char, CharFA>[] array, int arrayIndex)
			{
				((IEnumerable<KeyValuePair<char, CharFA>>)this).CopyTo(array, arrayIndex);
			}

			public IEnumerator<KeyValuePair<char, CharFA>> GetEnumerator()
			{
				foreach (var trns in _inner)
					foreach (var ch in trns.Value)
						yield return new KeyValuePair<char, CharFA>(ch, trns.Key);
			}

			public bool Remove(char key)
			{
				CharFA rem = null;
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
					{
						trns.Value.Remove(key);
						if (0 == trns.Value.Count)
						{
							rem = trns.Key;
							break;
						}
						return true;
					}
				}
				if (null != rem)
				{
					_inner.Remove(rem);
					return true;
				}
				return false;
			}

			public bool Remove(KeyValuePair<char, CharFA> item)
			{
				ICollection<char> hs;
				if (_inner.TryGetValue(item.Value, out hs))
				{
					if (hs.Contains(item.Key))
					{
						if (1 == hs.Count)
							_inner.Remove(item.Value);
						else
							hs.Remove(item.Key);
						return true;
					}
				}
				return false;
			}

			public bool TryGetValue(char key, out CharFA value)
			{
				foreach (var trns in _inner)
				{
					if (trns.Value.Contains(key))
					{
						value = trns.Key;
						return true;
					}
				}
				value = null;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			void IDictionary<CharFA, ICollection<char>>.Add(CharFA key, ICollection<char> value)
			{
				_inner.Add(key, value);
			}

			bool IDictionary<CharFA, ICollection<char>>.ContainsKey(CharFA key)
			{
				return _inner.ContainsKey(key);
			}

			bool IDictionary<CharFA, ICollection<char>>.Remove(CharFA key)
			{
				return _inner.Remove(key);
			}

			bool IDictionary<CharFA, ICollection<char>>.TryGetValue(CharFA key, out ICollection<char> value)
			{
				return _inner.TryGetValue(key, out value);
			}

			void ICollection<KeyValuePair<CharFA, ICollection<char>>>.Add(KeyValuePair<CharFA, ICollection<char>> item)
			{
				_inner.Add(item);
			}
			bool ICollection<KeyValuePair<CharFA, ICollection<char>>>.Contains(KeyValuePair<CharFA, ICollection<char>> item)
			{
				return _inner.Contains(item);
			}

			void ICollection<KeyValuePair<CharFA, ICollection<char>>>.CopyTo(KeyValuePair<CharFA, ICollection<char>>[] array, int arrayIndex)
			{
				_inner.CopyTo(array, arrayIndex);
			}

			bool ICollection<KeyValuePair<CharFA, ICollection<char>>>.Remove(KeyValuePair<CharFA, ICollection<char>> item)
			{
				return _inner.Remove(item);
			}

			IEnumerator<KeyValuePair<CharFA, ICollection<char>>> IEnumerable<KeyValuePair<CharFA, ICollection<char>>>.GetEnumerator()
			{
				return _inner.GetEnumerator();
			}
		}
		// compares several types of state collections or dictionaries used by FA
		sealed class _SetComparer : IEqualityComparer<IList<CharFA>>, IEqualityComparer<ICollection<CharFA>>, IEqualityComparer<IDictionary<char, CharFA>>
		{
			// ordered comparison
			public bool Equals(IList<CharFA> lhs, IList<CharFA> rhs)
			{
				return lhs.Equals<CharFA>(rhs);
			}
			// unordered comparison
			public bool Equals(ICollection<CharFA> lhs, ICollection<CharFA> rhs)
			{
				return lhs.Equals<CharFA>(rhs);
			}
			public bool Equals(IDictionary<char, CharFA> lhs, IDictionary<char, CharFA> rhs)
			{
				return lhs.Equals<KeyValuePair<char, CharFA>>(rhs);
			}
			public bool Equals(IDictionary<CharFA, ICollection<char>> lhs, IDictionary<CharFA, ICollection<char>> rhs)
			{
				if (lhs.Count != rhs.Count) return false;
				if (ReferenceEquals(lhs, rhs))
					return true;
				else if (ReferenceEquals(null, lhs) || ReferenceEquals(null, rhs))
					return false;
				using (var xe = lhs.GetEnumerator())
				using (var ye = rhs.GetEnumerator())
					while (xe.MoveNext() && ye.MoveNext())
					{
						if (xe.Current.Key != ye.Current.Key)
							return false;
						if (!CollectionUtility.Equals(xe.Current.Value, ye.Current.Value))
							return false;
					}
				return true;
			}
			public int GetHashCode(IList<CharFA> lhs)
			{
				return lhs.GetHashCode<CharFA>();
			}
			public int GetHashCode(ICollection<CharFA> lhs)
			{
				return lhs.GetHashCode<CharFA>();
			}
			public int GetHashCode(IDictionary<char, CharFA> lhs)
			{
				return lhs.GetHashCode<KeyValuePair<char, CharFA>>();
			}
			public static readonly _SetComparer Default = new _SetComparer();
		}
		static char _ReadRangeChar(IEnumerator<char> e)
		{
			char ch;
			if ('\\' != e.Current || !e.MoveNext())
			{
				return e.Current;
			}
			ch = e.Current;
			switch (ch)
			{
				case 't':
					ch = '\t';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case '0':
					ch = '\0';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'x':
					if (!e.MoveNext())
						throw new ExpectingException("Expecting input for escape \\x");
					ch = e.Current;
					byte x = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)x);
						return ch;
					}
					x *= 0x10;
					x += _FromHexChar(e.Current);
					ch = unchecked((char)x);
					break;
				case 'u':
					if (!e.MoveNext())
						throw new ExpectingException("Expecting input for escape \\u");
					ch = e.Current;
					ushort u = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					ch = unchecked((char)u);
					break;
				default: // return itself
					break;
			}
			return ch;
		}
		static byte _FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		static int _ParseEscape(ParseContext pc)
		{
			if ('\\' != pc.Current)
				return -1;
			if (-1 == pc.Advance())
				return -1;
			switch (pc.Current)
			{
				case 't':
					pc.Advance();
					return '\t';
				case 'n':
					pc.Advance();
					return '\n';
				case 'r':
					pc.Advance();
					return '\r';
				case 'x':
					if (-1 == pc.Advance())
						return 'x';
					byte b = _FromHexChar((char)pc.Current);
					b <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)b);
					b |= _FromHexChar((char)pc.Current);
					return unchecked((char)b);
				case 'u':
					if (-1 == pc.Advance())
						return 'u';
					ushort u = _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					return unchecked((char)u);
				default:
					int i = pc.Current;
					pc.Advance();
					return (char)i;
			}
		}
		static IEnumerable<CharRange> _ParseRanges(IEnumerable<char> charRanges)
		{
			using (var e = charRanges.GetEnumerator())
			{
				var skipRead = false;

				while (skipRead || e.MoveNext())
				{
					skipRead = false;
					char first = _ReadRangeChar(e);
					if (e.MoveNext())
					{
						if ('-' == e.Current)
						{
							if (e.MoveNext())
								yield return new CharRange(first, _ReadRangeChar(e));
							else
								yield return new CharRange('-', '-');
						}
						else
						{
							yield return new CharRange(first, first);
							skipRead = true;
							continue;

						}
					}
					else
					{
						yield return new CharRange(first, first);
						yield break;
					}
				}
			}
			yield break;
		}
		static IEnumerable<CharRange> _ParseRanges(IEnumerable<char> charRanges, bool normalize)
		{
			if (!normalize)
				return _ParseRanges(charRanges);
			else
			{
				var result = new List<CharRange>(_ParseRanges(charRanges));
				CharRange.NormalizeRangeList(result);
				return result;
			}
		}
		/// <summary>
		/// Parses a regular expresion from the specified string
		/// </summary>
		/// <param name="string">The string</param>
		/// <param name="accepting">The symbol reported when accepting the specified expression</param>
		/// <returns>A new machine that matches the regular expression</returns>
		public static CharFA Parse(IEnumerable<char> @string, string accepting = "") => _Parse(ParseContext.Create(@string), accepting);
		/// <summary>
		/// Parses a regular expresion from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The text reader</param>
		/// <param name="accepting">The symbol reported when accepting the specified expression</param>
		/// <returns>A new machine that matches the regular expression</returns>
		public static CharFA Parse(TextReader reader, string accepting = "") => _Parse(ParseContext.Create(reader), accepting);
		static CharFA _Parse(ParseContext pc, string accepting)
		{
			var result = new CharFA();
			if (null == accepting) accepting = "";
			result.AcceptingSymbol = accepting;
			CharFA f, next;
			int ch;
			pc.EnsureStarted();
			var current = result;
			while (true)
			{
				switch (pc.Current)
				{
					case -1:
						return result;
					case '.':
						pc.Advance();
						f = current.FirstAcceptingState;

						current = CharFA.Set(new CharRange[] { new CharRange(char.MinValue, char.MaxValue) }, accepting);
						switch (pc.Current)
						{
							case '*':
								current = CharFA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = CharFA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = CharFA.Optional(current, accepting);
								pc.Advance();
								break;

						}
						f.AcceptingSymbol = null;
						f.EpsilonTransitions.Add(current);
						break;
					case '\\':
						if (-1 != (ch = _ParseEscape(pc)))
						{
							next = null;
							switch (pc.Current)
							{
								case '*':
									next = new CharFA();
									next.Transitions.Add((char)ch, new CharFA(accepting));
									next = CharFA.Kleene(next, accepting);
									pc.Advance();
									break;
								case '+':
									next = new CharFA();
									next.Transitions.Add((char)ch, new CharFA(accepting));
									next = CharFA.Repeat(next, accepting);
									pc.Advance();
									break;
								case '?':
									next = new CharFA();
									next.Transitions.Add((char)ch, new CharFA(accepting));
									next = CharFA.Optional(next, accepting);
									pc.Advance();
									break;
								default:
									current = current.FirstAcceptingState;
									current.AcceptingSymbol = null;
									current.Transitions.Add((char)ch, new CharFA(accepting));
									break;
							}
							if (null != next)
							{
								current = current.FirstAcceptingState;
								current.AcceptingSymbol = null;
								current.EpsilonTransitions.Add(next);
								current = next;
							}
						}
						else
						{
							pc.Expecting(); // throw an error
							return null; // doesn't execute
						}
						break;
					case ')':
						return result;
					case '(':
						pc.Advance();
						pc.Expecting();
						f = current.FirstAcceptingState;
						current = _Parse(pc, accepting);
						pc.Expecting(')');
						pc.Advance();
						switch (pc.Current)
						{
							case '*':
								current = CharFA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = CharFA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = CharFA.Optional(current, accepting);
								pc.Advance();
								break;
						}
						var ff = f.FirstAcceptingState;
						ff.EpsilonTransitions.Add(current);
						ff.AcceptingSymbol = null;
						//f = CharFA.Concat(new CharFA[] { f, current },accepting);
						break;
					case '|':
						if (-1 != pc.Advance())
						{
							current = _Parse(pc, accepting);
							result = CharFA.Or(new CharFA[] { result, current }, accepting);
						}
						else
						{
							current = current.FirstAcceptingState;
							result = CharFA.Optional(result, accepting);
						}
						break;
					case '[':
						pc.ClearCapture();
						pc.Advance();
						pc.Expecting();
						bool not = false;
						if ('^' == pc.Current)
						{
							not = true;
							pc.Advance();
							pc.Expecting();
						}
						pc.TryReadUntil(']', '\\', false);
						pc.Expecting(']');
						pc.Advance();

						var r = (!not && "." == pc.Capture) ?
							new CharRange[] { new CharRange(char.MinValue, char.MaxValue) } :
							_ParseRanges(pc.Capture, true);
						if (not)
							r = CharRange.NotRanges(r);
						f = current.FirstAcceptingState;
						current = CharFA.Set(r, accepting);
						switch (pc.Current)
						{
							case '*':
								current = CharFA.Kleene(current, accepting);
								pc.Advance();
								break;
							case '+':
								current = CharFA.Repeat(current, accepting);
								pc.Advance();
								break;
							case '?':
								current = CharFA.Optional(current, accepting);
								pc.Advance();
								break;

						}
						f.AcceptingSymbol = null;
						f.EpsilonTransitions.Add(current);
						break;
					default:
						ch = pc.Current;
						pc.Advance();
						next = null;
						switch (pc.Current)
						{
							case '*':
								next = new CharFA();
								next.Transitions.Add((char)ch, new CharFA(accepting));
								next = CharFA.Kleene(next, accepting);
								pc.Advance();
								break;
							case '+':
								next = new CharFA();
								next.Transitions.Add((char)ch, new CharFA(accepting));
								next = CharFA.Repeat(next, accepting);
								pc.Advance();
								break;
							case '?':
								next = new CharFA();

								next.Transitions.Add((char)ch, new CharFA(accepting));
								next = CharFA.Optional(next, accepting);
								pc.Advance();
								break;
							default:
								current = current.FirstAcceptingState;
								current.AcceptingSymbol = null;
								current.Transitions.Add((char)ch, new CharFA(accepting));
								break;
						}
						if (null != next)
						{
							current = current.FirstAcceptingState;
							current.AcceptingSymbol = null;
							current.EpsilonTransitions.Add(next);
							current = next;
						}
						break;
				}
			}
		}
	}
}
