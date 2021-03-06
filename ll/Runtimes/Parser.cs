﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	/// <summary>
	/// An enumeration indicating the node types of the parser
	/// </summary>
	public enum ParserNodeType
	{
		/// <summary>
		/// Indicates the initial state.
		/// </summary>
		Initial = 0,
		/// <summary>
		/// Parser is on a non-terminal
		/// </summary>
		NonTerminal = 1,
		/// <summary>
		/// Parser is ending a non-terminal node
		/// </summary>
		EndNonTerminal = 2,
		/// <summary>
		/// Parser is on a terminal node
		/// </summary>
		Terminal = 3,
		/// <summary>
		/// Parser is on an error node
		/// </summary>
		Error = 4,
		/// <summary>
		/// The parser is at the end of the document
		/// </summary>
		EndDocument = 5
	}
	public abstract class Parser : IDisposable
	{
		public bool ShowHidden { get; set; } = false;
		public bool ShowCollapsed { get; set; } = false;
		public abstract ParserNodeType NodeType { get; }
		public abstract string Symbol { get; }
		public abstract int SymbolId { get; }
		public abstract string Value { get; }
		public abstract int Line { get;  }
		public abstract int Column { get; }
		public abstract long Position { get; }
		public abstract bool Read();
		public abstract void Restart();
		public abstract void Restart(IEnumerable<Token> tokenizer);
		public abstract void Close();
		public abstract bool IsHidden { get; }
		public abstract bool IsCollapsed { get; }
		public abstract object GetAttribute(string name, object @default = null);
		void IDisposable.Dispose() { Close(); }
		
		/// <summary>
		/// Parses the from the current position into a parse tree. This will read an entire sub-tree.
		/// </summary>
		/// <param name="trimEmpties">Remove non-terminal nodes that have no terminals</param>
		/// <returns>A <see cref="ParseNode"/> representing the parse tree. The reader's cursor is advanced.</returns>
		public virtual ParseNode ParseSubtree(bool trimEmpties = false)
		{
			if (!Read())
				return null;
			var nn = NodeType;
			if (ParserNodeType.EndNonTerminal == nn)
				return null;

			var result = new ParseNode();
			result.IsHidden = IsHidden;
			result.IsCollapsed = IsCollapsed;
			if (ParserNodeType.NonTerminal == nn)
			{
				result.Symbol = Symbol;
				result.SymbolId = SymbolId;
				while (true)
				{
					var k = ParseSubtree(trimEmpties);
					if (null != k)
					{
						if (!trimEmpties || ((null != k.Value) || 0 < k.Children.Count))
							result.Children.Add(k);
					}
					else
						break;
				}

				return result;
			}
			else if (ParserNodeType.Terminal == nn)
			{
				result.SetLocationInfo(Line, Column, Position);
				result.Symbol = Symbol;
				result.SymbolId = SymbolId;
				result.Value = Value;
				return result;
			}
			else if (ParserNodeType.Error == nn)
			{
				result.SetLocationInfo(Line, Column, Position);
				result.Symbol = Symbol;
				result.SymbolId = SymbolId;
				result.Value = Value;
				return result;
			}
			return null;
		}
	}
}
