using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	/// <summary>
	/// The tokenizer breaks input into lexical units that can be fed to a parser. Abstractly, they are essentially
	/// a series of regular expressions, each one tagged to a symbol. As the input is scanned, the tokenizer reports
	/// the symbol for each matched chunk, along with the matching value and location information. It's a regex runner.
	/// </summary>
	/// <remarks>The heavy lifting here is done by the <see cref="DebugTokenEnumerator"/> class. This just provides a for-each interface over the tokenization process.</remarks>
	public class DebugTokenizer : IEnumerable<Token>
	{
		Cfg _cfg;
		CharFA _lexer;
		IEnumerable<char> _input;
		IDictionary<string, string> _blockEnds;
		IDictionary<string, int> _symbolIds;
		public DebugTokenizer(Cfg cfg, CharFA lexer,IEnumerable<char> input)
		{
			_cfg = cfg;
			_lexer = lexer;
			_input = input;
			// we use the blockEnd attribute in the lexer to enable things like block comments and XML CDATA sections
			_PopulateAttrs();
		}
		void _PopulateAttrs()
		{
			var syms = _cfg.FillSymbols();
			_symbolIds = new Dictionary<string, int>();
			for(int ic=syms.Count,i=0;i<ic;++i)
				_symbolIds.Add(syms[i], i);
				
			_blockEnds = new Dictionary<string,string>();
			foreach(var s in _cfg.AttributeSets.Keys)
				if(!_cfg.IsNonTerminal(s))
				{
					var be = _cfg.AttributeSets.GetAttribute(s, "blockEnd") as string;
					if(null!=be)
						_blockEnds.Add(s, be);
				}
		}
		public IEnumerator<Token> GetEnumerator()
			=> new DebugTokenEnumerator(_lexer,_symbolIds, _blockEnds, _input);
		
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
	/// <summary>
	/// The token enumerator is the core of the lexing engine. It uses a composite FA macine to match text against one of several "regular expression" patterns.
	/// </summary>
	class DebugTokenEnumerator : IEnumerator<Token>
	{
		IDictionary<string, int> _symbolIds;
		IDictionary<string, string> _blockEnds;
		// our underlying input enumerator - works on strings or char arrays
		IEnumerator<char> _input;
		// location information
		long _position;
		int _line;
		int _column;
		// an integer we use so we can tell if the enumeration is started or running, or past the end.
		int _state;
		// this holds the current token we're on.
		Token _token;
		// this holds the initial NFA states we're in so we can quickly return here.
		// it's the starting point for all matches. It comes from _lexer.FillEpsilonClosure()
		ICollection<CharFA> _initialStates;

		// the lexer is a composite "regular expression" with tagged symbols for each one.
		CharFA _lexer;
		// this holds our current value
		StringBuilder _buffer;
		public DebugTokenEnumerator(CharFA lexer,IDictionary<string,int> symbolIds,IDictionary<string,string> blockEnds,IEnumerable<char> @string)
		{
			_lexer = lexer;
			_symbolIds = symbolIds;
			_blockEnds = blockEnds;
			_input = @string.GetEnumerator();
			_buffer = new StringBuilder();
			_initialStates = _lexer.FillEpsilonClosure();
			_state = -1;
			_line = 1;
			_column = 1;
			_position = 0;
		}
		public Token Current { get { return _token; } }
		object IEnumerator.Current => Current;
		
		public void Dispose()
		{
			_state = -3;
			_input.Dispose();
		}
		public bool MoveNext()
		{
			switch(_state)
			{
				case -3:
					throw new ObjectDisposedException(GetType().FullName);
				case -2:
					if(_token.Symbol!="#EOS")
					{
						_state = -2;
						goto case 0;
					}
					return false;
				case -1:
				case 0:
					_token = new Token();
					// store our current location before we advance
					_token.Column = _column;
					_token.Line = _line;
					_token.Position = _position;
					// this is where the real work happens:
					_token.Symbol = _Lex();
					// store our value and length from the lex
					_token.Value = _buffer.ToString();
					_token.Length = _buffer.Length;
					_token.SymbolId = _symbolIds[_token.Symbol];
					return true;
				default:
					return false;
			}
			
		}
		/// <summary>
		/// This is where the work happens
		/// </summary>
		/// <returns>The symbol that was matched. members _state _line,_column,_position,_buffer and _input are also modified.</returns>
		string _Lex()
		{
			string acc;
			var states = _initialStates;
			_buffer.Clear();
			switch (_state)
			{
				case -1: // initial
					if (!_MoveNextInput())
					{
						_state = -2;
						acc = _GetAcceptingSymbol(states);
						if (null != acc)
							return acc;
						else
							return "#ERROR";
					}
					_state = 0; // running
					break;
				case -2: // end of stream
					return "#EOS";
			}
			// Here's where we run most of the match. FillMove runs one interation of the NFA state machine.
			// We match until we can't match anymore (greedy matching) and then report the symbol of the last 
			// match we found, or an error ("#ERROR") if we couldn't find one.
			while (true)
			{
				var next = CharFA.FillMove(states, _input.Current);
				if (0 == next.Count) // couldn't find any states
					break;
				_buffer.Append(_input.Current);

				states = next;
				if (!_MoveNextInput())
				{
					// end of stream
					_state = -2;
					acc = _GetAcceptingSymbol(states);
					if (null != acc) // do we accept?
						return acc;
					else
						return "#ERROR";
				}
			}
			acc = _GetAcceptingSymbol(states);
			if (null != acc) // do we accept?
			{
				string be;
				if(_blockEnds.TryGetValue(acc,out be) && !string.IsNullOrEmpty(be as string))
				{
					// we have to resolve our blockends. This is tricky. We break out of the FA 
					// processing and instead we loop until we match the block end. We have to 
					// be very careful when we match only partial block ends and we have to 
					// handle the case where there's no terminating block end.
					var more = true;
					while (more)
					{
						while (more)
						{
							if (_input.Current != be[0])
							{
								_buffer.Append(_input.Current);
								more = _MoveNextInput();
								if (!more)
									return "#ERROR";
								break;
							}
							else
							{
								var i = 0;
								var found = true;
								while (i < be.Length && _input.Current == be[i])
								{
									if (!(more=_MoveNextInput()))
									{
										++i;
										found = false;
										if (i < be.Length)
											acc = "#ERROR";
										break;
									}
									++i;
									
								}
								if (be.Length != i)
									found = false;
								if (!found)
								{
									_buffer.Append(be.Substring(0, i));
								}
								else
								{
									more = false;
									_buffer.Append(be);
									break;
								}
								if (found)
								{
									more = _MoveNextInput();
									if (!more)
										break;
								}
							}
							
						}
					}
				}
				return acc;
			}
			else
			{
				// handle the error condition
				_buffer.Append(_input.Current);
				if (!_MoveNextInput())
					_state = -2;
				return "#ERROR";
			}
		}
		/// <summary>
		/// Advances the input, and tracks location information
		/// </summary>
		/// <returns>True if the underlying MoveNext returned true, otherwise false.</returns>
		bool _MoveNextInput()
		{
			if (_input.MoveNext())
			{
				if (-1 != _state)
				{
					++_position;
					if ('\n' == _input.Current)
					{
						_column = 1;
						++_line;
					}
					else
						++_column;
				}
				return true;
			}
			else if (0==_state)
			{
				++_position;
				++_column;
			}
			_state = -2;
			return false;
		}
		/// <summary>
		/// Finds if any of our states has an accept symbol and if so, returns it
		/// </summary>
		/// <param name="states">The states to check</param>
		/// <returns>The first symbol found or null if none were found</returns>
		static string _GetAcceptingSymbol(IEnumerable<CharFA> states)
		{
			foreach (var fa in states)
				if (null != fa.AcceptingSymbol)
					return fa.AcceptingSymbol;
			return null;
		}
		public void Reset()
		{
			_input.Reset();
			_state = -1;
			_line = 1;
			_column = 1;
			_position = 0;
			_token = default(Token);
		}
	}
}
