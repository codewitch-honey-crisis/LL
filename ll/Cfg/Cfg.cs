using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LL
{
	/// <summary>
	/// Represents a Context-Free-Grammar or CFG, which is a collection of <see cref="CfgRule"/> entries and a start symbol.
	/// </summary>
	/// <remarks>This class implements value semantics</remarks>
	public partial class Cfg : IEquatable<Cfg>, ICloneable
	{
		/// <summary>
		/// Attribute sets represent sideband data about terminals and non-terminals like hidden or collapsed status, or recording the start symbol.
		/// </summary>
		/// <remarks>These are not shown in the string representation.</remarks>
		public AttributeSetDictionary AttributeSets { get; } = new AttributeSetDictionary();
		/// <summary>
		/// The start symbol. If not set, the first non-terminal is used.
		/// </summary>
		public string StartSymbol
		{
			get {
				foreach (var sattr in AttributeSets)
					if (sattr.Value.Start)
						return sattr.Key;
				if(0<Rules.Count)
					return Rules[0].Left;
				return null;
			}
			set {
				foreach (var sattr in AttributeSets)
				{
					if (sattr.Value.Start)
						sattr.Value.Remove("start");
				}
				if (null!=value)
				{
					if (!IsSymbol(value))
						throw new KeyNotFoundException("The specified symbol does not exist");
					AttributeSets.SetAttribute(value, "start", true);
				}
			}
		}
		/// <summary>
		/// The rules
		/// </summary>
		public IList<CfgRule> Rules { get; } = new List<CfgRule>();

		/// <summary>
		/// Provides a string representation of the grammar
		/// </summary>
		/// <returns>A string representing the grammar</returns>
		public override string ToString()
			=> ToString(null);
		/// <summary>
		/// Provides a string representation of the grammar
		/// </summary>
		/// <param name="format">The format specifier. null, "", "r" or "rules" for the default format, or "y" or "yacc" for YACC format.</param>
		/// <returns>A string representing the grammar</returns>
		public string ToString(string format)
		{
			var sb = new StringBuilder();
			if (string.IsNullOrEmpty(format) || "r"==format || "rules"==format)
			{
				for (int ic = Rules.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Rules[i].ToString());
			} else if("y"==format || "yacc"==format)
			{
				sb.AppendLine("%%");
				foreach(var nt in _EnumNonTerminals())
				{
					var nt2 = nt.Replace("'", "_");
					var rules = FillNonTerminalRules(nt);
					if(0<rules.Count) // sanity check
					{
						sb.Append(nt2);
						var delim = ":";
						for(int ic = rules.Count,i=0;i<ic;++i)
						{
							var rule = rules[i];
							sb.Append(delim);
							for(int jc=rule.Right.Count,j=0;j<jc;++j)
							{
								sb.Append(" ");
								sb.Append(rule.Right[j].Replace("'","_"));
							}
							delim = Environment.NewLine+"\t|";
						}
						sb.AppendLine(";");
					}
				}
			}
			return sb.ToString();
		}
		
		public HashSet<IList<string>> FillSubstrings(int k,HashSet<IList<string>> result =null)
		{
			if (0 >= k) k = 1;
			if (null == result)
				result = new HashSet<IList<string>>(OrderedCollectionEqualityComparer<string>.Default);
			for (int ic=Rules.Count, i = 0;i<ic;++i)
			{
				var rule = Rules[i];

				if (k == 1 && rule.IsNil)
				{
					var l = new List<string>();
					l.Add(null);
					result.Add(l);
				}
				else
				{
					for (int jc = rule.Right.Count-k+1, j = 0;j<jc;++j)
					{
						result.Add(new List<string>(rule.Right.Range(j, k)));
					}

					
				}
			}
			return result;
		}
		
		
		/// <summary>
		/// Gets a unique id that is a variation of the passed in id. T would become T'
		/// </summary>
		/// <param name="id">The id to transform</param>
		/// <returns>A unique transform id</returns>
		public string GetTransformId(string id)
		{
			var iid = id;
			var syms = FillSymbols();
			var i = 1;
			while (true)
			{
				var s = string.Concat(iid, "'");
				if (!syms.Contains(s))
					return s;
				++i;
				iid = string.Concat(id, i.ToString());
			}
		}
		
		
		public string GetUniqueId(string id)
		{
			var names = new HashSet<string>(_EnumSymbols());
			var s = id;
			if (!names.Contains(s))
				return s;
			var i = 2;
			s = string.Concat(id, i.ToString());
			while (names.Contains(s))
			{
				++i;
				s = string.Concat(id, i.ToString());
			}
			return s;
		}
		public static Cfg Parse(IEnumerable<char> @string) => _Parse(ParseContext.Create(@string));
		public static Cfg ReadFrom(TextReader reader) => _Parse(ParseContext.Create(reader));
		public static Cfg ReadFrom(string filename) {
			using (var pc= ParseContext.CreateFromFile(filename))
				return _Parse(pc);
		}
		static Cfg _Parse(ParseContext pc)
		{
			var result = new Cfg();
			pc.EnsureStarted();
			while(-1!=pc.Current)
				result.Rules.Add(_ParseRule(pc));
			
			return result;
		}
		static CfgRule _ParseRule(ParseContext pc)
		{
			var result = new CfgRule();
			pc.TrySkipWhiteSpace();
			pc.ClearCapture();
			pc.TryReadUntil(false, ' ', '\t', '\r', '\n', '\f', '\v','-');
			result.Left = pc.Capture;
			pc.TrySkipWhiteSpace();
			pc.Expecting('-');
			pc.Advance();
			pc.Expecting('>');
			pc.Advance();
			while (-1 != pc.Current && '\n' != pc.Current)
			{
				pc.TrySkipWhiteSpace();
				pc.ClearCapture();
				pc.TryReadUntil(false, ' ', '\t', '\r', '\n','\f', '\v');
				result.Right.Add(pc.Capture);
			}
			pc.TrySkipWhiteSpace();
			return result;
		}
		/// <summary>
		/// Makes a simple lexer where each terminal is its own literal value.
		/// </summary>
		/// <returns>A lexer suitable for lexing the grammar</returns>
		public CharFA ToSimpleLexer()
		{
			var result = new CharFA();
			foreach(var t in _EnumTerminals())
				if("#ERROR"!=t && "#EOS"!=t)
					result.EpsilonTransitions.Add(CharFA.Literal(t, t));
			result = result.ToDfa();
			return result;
		}
		public Parser ToLL1Parser(IEnumerable<Token> tokenizer=null)
		{
			var parseTable = ToLL1ParseTable();
			var syms = new List<string>();
			FillSymbols(syms);
			var nodeFlags = new int[syms.Count];
			for (var i = 0; i < nodeFlags.Length; ++i)
			{
				var o = AttributeSets.GetAttribute(syms[i], "hidden", false);
				if (o is bool && (bool)o)
					nodeFlags[i] |= 2;
				o = AttributeSets.GetAttribute(syms[i], "collapsed", false);
				if (o is bool && (bool)o)
					nodeFlags[i] |= 1;
			}
			var attrSets = new KeyValuePair<string, object>[syms.Count][];
			for (var i = 0; i < attrSets.Length; i++)
			{
				AttributeSet attrs;
				if (AttributeSets.TryGetValue(syms[i], out attrs))
				{
					attrSets[i] = new KeyValuePair<string, object>[attrs.Count];
					var j = 0;
					foreach (var attr in attrs)
					{
						attrSets[i][j] = new KeyValuePair<string, object>(attr.Key, attr.Value);
						++j;
					}
				}
				else
					attrSets[i] = null;// new KeyValuePair<string, object>[0];
			}
			var initCfg = new int[] { GetIdOfSymbol(StartSymbol), FillNonTerminals().Count };
			return new LL1TableParser(parseTable.ToLL1Array(syms), initCfg, syms.ToArray(), nodeFlags, attrSets, tokenizer);
			
		}
		#region Value Semantics
		/// <summary>
		/// Indicates whether the CFG is exactly equivelant to the specified CFG
		/// </summary>
		/// <param name="rhs">The CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false.</returns>
		public bool Equals(Cfg rhs)
		{
			if (!CollectionUtility.Equals(this.Rules, rhs.Rules))
				return false;
			if (AttributeSets.Count != rhs.AttributeSets.Count)
				return false;
			foreach (var attrs in AttributeSets)
			{
				AttributeSet d;
				if (!rhs.AttributeSets.TryGetValue(attrs.Key, out d))
				{
					if (d.Count != attrs.Value.Count)
						return false;
					foreach (var attr in attrs.Value)
					{
						object o;
						if (!d.TryGetValue(attr.Key, out o) || !Equals(o, attr.Value))
							return false;
					}
				}
			}
			return true;
		}
		/// <summary>
		/// Indicates whether the CFG is exactly equivelant to the specified CFG
		/// </summary>
		/// <param name="obj">The CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as Cfg);
		}
		/// <summary>
		/// Gets a hashcode that represents this CFG
		/// </summary>
		/// <returns>A hashcode that represents this CFG</returns>
		public override int GetHashCode()
		{
			var result = CollectionUtility.GetHashCode(Rules);
			foreach (var attrs in AttributeSets)
			{
				result ^= attrs.Key.GetHashCode();
				foreach (var attr in attrs.Value)
				{
					result ^= attr.Key.GetHashCode();
					if (null != attr.Value)
						result ^= attr.Value.GetHashCode();
				}
			}
			return result;
		}
		/// <summary>
		/// Indicates whether the two CFGs are exactly equivelent
		/// </summary>
		/// <param name="lhs">The first CFG to compare</param>
		/// <param name="rhs">The second CFG to compare</param>
		/// <returns>True if the CFGs are equal, otherwise false</returns>
		public static bool operator ==(Cfg lhs, Cfg rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether the two CFGs are not equal
		/// </summary>
		/// <param name="lhs">The first CFG to compare</param>
		/// <param name="rhs">The second CFG to compare</param>
		/// <returns>True if the CFGs are not equal, or false if they are equal</returns>
		public static bool operator !=(Cfg lhs, Cfg rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null)) return true;
			return !lhs.Equals(rhs);
		}

		/// <summary>
		/// Performs a deep clone of the CFG
		/// </summary>
		/// <returns>A new CFG equal to this CFG</returns>
		public Cfg Clone()
		{
			var result = new Cfg();
			var ic = Rules.Count;
			for (var i = 0; i < ic; ++i)
				result.Rules.Add(Rules[i].Clone());
			foreach (var attrs in AttributeSets)
			{
				var d = new AttributeSet();
				result.AttributeSets.Add(attrs.Key, d);
				foreach (var attr in attrs.Value)
					d.Add(attr.Key, attr.Value);
			}
			return result;
		}
		object ICloneable.Clone() { return Clone(); }
		#endregion

	}
}
