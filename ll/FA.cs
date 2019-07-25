using System;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	/// <summary>
	/// Represents a generic FA state machine
	/// </summary>
	public sealed class FA<TInput,TAccept> : ICloneable
		where TInput : IEquatable<TInput>
	{
		public bool IsAccepting { get; set; } = false;
		public TAccept Accept { get; set; } = default(TAccept);
		public IDictionary<TInput, FA<TInput,TAccept>> Transitions { get; } = new Dictionary<TInput, FA<TInput,TAccept>>();
		public ICollection<FA<TInput,TAccept>> EpsilonTransitions { get; } = new HashSet<FA<TInput,TAccept>>();
		/// <summary>
		/// Computes the set of all states reachable from this state, including itself. Puts the result in the <paramref name="result"/> field amd returns the same collection."/>
		/// </summary>
		/// <param name="result">The collection to fill, or null for one to be created</param>
		/// <returns>Either <paramref name="result"/> or a new collection filled with the result of the closure computation.</returns>
		public IList<FA<TInput,TAccept>> FillClosure(IList<FA<TInput,TAccept>> result = null)
		{
			if (null == result)
				result = new List<FA<TInput,TAccept>>();
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
		/// Computes the set of all states reachable from this state on no input, including itself. Puts the result in the <paramref name="result"/> field amd returns the same collection."/>
		/// </summary>
		/// <param name="result">The collection to fill, or null for one to be created</param>
		/// <returns>Either <paramref name="result"/> or a new collection filled with the result of the epsilon closure computation.</returns>
		public ICollection<FA<TInput,TAccept>> FillEpsilonClosure(ICollection<FA<TInput,TAccept>> result = null)
		{
			if (null == result)
				result = new List<FA<TInput,TAccept>>();
			if (!result.Contains(this))
			{
				result.Add(this);
				foreach (var fa in EpsilonTransitions)
					fa.FillEpsilonClosure(result);
			}
			return result;
		}
		/// <summary>
		/// Creates a clone of this FA state
		/// </summary>
		/// <returns>A new FA that is equal to this FA</returns>
		public FA<TInput,TAccept> Clone()
		{
			var closure = FillClosure();
			var nclosure = new FA<TInput,TAccept>[closure.Count];
			for (var i = 0; i < nclosure.Length; i++)
			{
				nclosure[i] = new FA<TInput,TAccept>();
				nclosure[i].Accept = closure[i].Accept;
				nclosure[i].IsAccepting = closure[i].IsAccepting;
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
		object ICloneable.Clone()
			=> Clone();
		/// <summary>
		/// Returns the first state that accepts from a given FA, or null if none do.
		/// </summary>
		public FA<TInput,TAccept> FirstAcceptingState {
			get {
				foreach (var fa in FillClosure())
					if (fa.IsAccepting)
						return fa;
				return null;
			}
		}
		public FA<TInput,TAccept> ToDfa()
		{
			// The DFA states are keyed by the set of NFA states they represent.
			var dfaMap = new Dictionary<List<FA<TInput,TAccept>>, FA<TInput,TAccept>>(_SetComparer.Default);

			var unmarked = new HashSet<FA<TInput,TAccept>>();

			// compute the epsilon closure of the initial state in the NFA
			var states = new List<FA<TInput,TAccept>>();

			FillEpsilonClosure(states);

			// create a new state to represent the current set of states. If one 
			// of those states is accepting, set this whole state to be accepting.
			FA<TInput,TAccept> dfa = new FA<TInput,TAccept>();
			var al = new List<TAccept>();
			foreach (var fa in states)
				if (fa.IsAccepting)
					if (!al.Contains(fa.Accept))
						al.Add(fa.Accept);
			int ac = al.Count;
			if (1 == ac)
			{
				dfa.Accept = al[0];
				dfa.IsAccepting = true;
			}
			else if (1 < ac)
			{
				// TODO: Give a detailed error message of the conflict
				throw new InvalidOperationException("Ambiguity in the FA.");
			}


			FA<TInput,TAccept> result = dfa; // store the initial state for later, so we can return it.

			// add it to the dfa map
			dfaMap.Add(states, dfa);

			// add it to the unmarked states, signalling that we still have work to do.
			unmarked.Add(dfa);
			bool done = false;
			while (!done)
			{
				done = true;
				HashSet<List<FA<TInput,TAccept>>> mapKeys = new HashSet<List<FA<TInput,TAccept>>>(dfaMap.Keys, _SetComparer.Default);
				foreach (List<FA<TInput,TAccept>> mapKey in mapKeys)
				{
					dfa = dfaMap[mapKey];
					if (unmarked.Contains(dfa))
					{
						// when we get here, mapKey represents the epsilon closure of our 
						// current dfa state, which is indicated by kvp.Value

						// build the transition list for the new state by combining the transitions
						// from each of the old states

						// retrieve every possible input for these states
						HashSet<TInput> inputs = new HashSet<TInput>();
						foreach (FA<TInput,TAccept> state in mapKey)
						{
							foreach (var inp in state.Transitions.Keys)
								inputs.Add(inp);
							
						}

						foreach (var input in inputs)
						{
							var acc = new List<TAccept>();
							List<FA<TInput,TAccept>> ns = new List<FA<TInput,TAccept>>();
							foreach (var state in mapKey)
							{
								FA<TInput,TAccept> dst = null;
								if (state.Transitions.TryGetValue(input, out dst))
								{
									foreach (var d in dst.FillEpsilonClosure())
									{
										if (d.IsAccepting)
											if (!acc.Contains(d.Accept))
												acc.Add(d.Accept);
										if (!ns.Contains(d))
											ns.Add(d);
									}
								}
							}

							FA<TInput,TAccept> ndfa;
							if (!dfaMap.TryGetValue(ns, out ndfa))
							{
								ndfa = new FA<TInput,TAccept>();
								ac = acc.Count;
								if (1 == ac)
								{
									ndfa.Accept = acc[0];
									ndfa.IsAccepting = true;
								}
								else if (1 < ac)
								{
									// TODO: Give a detailed error message of the conflict
									throw new InvalidOperationException("Ambiguity in the FA.");
								}
								else
									ndfa.Accept = default(TAccept);



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
		/// <summary>
		/// Creates an FA that matches a literal string
		/// </summary>
		/// <param name="string">The string to match</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA machine that will match this literal</returns>
		public static FA<TInput,TAccept> Literal(IEnumerable<TInput> @string, TAccept accept = default(TAccept))
		{
			var result = new FA<TInput,TAccept>();
			var current = result;
			foreach (TInput ch in @string)
			{
				current.Accept= default(TAccept);
				current.IsAccepting = false;
				var fa = new FA<TInput,TAccept>();
				fa.Accept = accept;
				fa.IsAccepting = true;
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
		public static FA<TInput,TAccept> Set(IEnumerable<TInput> set, TAccept accept = default(TAccept))
		{
			var result = new FA<TInput,TAccept>();
			var final = new FA<TInput,TAccept>();
			final.Accept= accept;
			final.IsAccepting = true;
			foreach (TInput ch in set)
				result.Transitions.Add(ch, final);
			return result;
		}
		/// <summary>
		/// Creates a new FA that is a concatenation of two other FA expressions
		/// </summary>
		/// <param name="exprs">The FAs to concatenate</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that is the concatenation of the specified FAs</returns>
		public static FA<TInput, TAccept> Concat(IEnumerable<FA<TInput, TAccept>> exprs, TAccept accept = default(TAccept))
		{
			FA<TInput,TAccept> left = null;
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
			var fas = right.FirstAcceptingState;
			fas.Accept= accept;
			fas.IsAccepting = true;
			return left;
		}
		static void _Concat(FA<TInput,TAccept> lhs, FA<TInput,TAccept> rhs)
		{
			var f = lhs.FirstAcceptingState;
			lhs.FirstAcceptingState.EpsilonTransitions.Add(rhs);
			f.Accept = default(TAccept);
			f.IsAccepting = false;
		}
		/// <summary>
		/// Creates a new FA that matche any one of the FA expressions passed
		/// </summary>
		/// <param name="exprs">The expressions to match</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that will match the union of the FA expressions passed</returns>
		public static FA<TInput,TAccept> Or(IEnumerable<FA<TInput,TAccept>> exprs, TAccept accept = default(TAccept))
		{
			var result = new FA<TInput,TAccept>();
			var final = new FA<TInput,TAccept>();
			final.Accept = accept;
			final.IsAccepting = true;
			foreach (var fa in exprs)
			{
				fa.EpsilonTransitions.Add(fa);
				var nfa = fa.Clone();
				var nffa = fa.FirstAcceptingState;
				nfa.FirstAcceptingState.EpsilonTransitions.Add(final);
				nffa.Accept = default(TAccept);
				nffa.IsAccepting = false;
			}
			return result;
		}
		/// <summary>
		/// Creates a new FA that will match a repetition of one or more of the specified FA expression
		/// </summary>
		/// <param name="expr">The expression to repeat</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that matches the specified FA one or more times</returns>
		public static FA<TInput,TAccept> Repeat(FA<TInput,TAccept> expr, TAccept accept = default(TAccept))
		{
			var result = expr.Clone();

			result.FirstAcceptingState.EpsilonTransitions.Add(result);
			var fas = result.FirstAcceptingState;
			fas.Accept = accept;
			fas.IsAccepting=true;
			return result;
		}
		/// <summary>
		/// Creates a new FA that matches the specified FA expression or empty
		/// </summary>
		/// <param name="expr">The expression to make optional</param>
		/// <param name="accept">The symbol to accept</param>
		/// <returns>A new FA that will match the specified expression or empty</returns>
		public static FA<TInput,TAccept> Optional(FA<TInput,TAccept> expr,TAccept accept = default(TAccept))
		{
			var result = expr.Clone();
			var f = result.FirstAcceptingState;
			f.Accept = accept;
			f.IsAccepting = true;
			result.EpsilonTransitions.Add(f);
			return result;
		}
		/// <summary>
		/// Fills a collection with the result of moving each of the specified <paramref name="states"/> by the specified input.
		/// </summary>
		/// <param name="states">The states to examine</param>
		/// <param name="input">The input to use</param>
		/// <param name="result">The states that are now entered as a result of the move</param>
		/// <returns><paramref name="result"/> or a new collection if it wasn't specified.</returns>
		public static ICollection<FA<TInput,TAccept>> FillMove(IEnumerable<FA<TInput,TAccept>> states, TInput input, ICollection<FA<TInput,TAccept>> result = null)
		{
			if (null == result) result = new List<FA<TInput,TAccept>>();
			foreach (var fa in FillEpsilonClosure(states))
			{
				// examine each of the states reachable from this state on no input
				
				FA<TInput,TAccept> ofa;
				// see if this state has this input in its transitions
				if (fa.Transitions.TryGetValue(input, out ofa))
					foreach(var efa in ofa.FillEpsilonClosure())
						if (!result.Contains(efa)) // if it does, add it if it's not already there
							result.Add(efa);
			}
			return result;
		}
		public static IList<FA<TInput,TAccept>> FillEpsilonClosure(IEnumerable<FA<TInput,TAccept>> states, IList<FA<TInput,TAccept>> result = null)
		{
			if (null == result)
				result = new List<FA<TInput,TAccept>>();
			foreach (var fa in states)
				fa.FillEpsilonClosure(result);
			return result;
		}
		// compares several types of state collections or dictionaries used by FA
		sealed class _SetComparer : IEqualityComparer<IList<FA<TInput,TAccept>>>, IEqualityComparer<ICollection<FA<TInput,TAccept>>>, IEqualityComparer<IDictionary<TInput, FA<TInput,TAccept>>>
		{
			// ordered comparison
			public bool Equals(IList<FA<TInput,TAccept>> lhs, IList<FA<TInput,TAccept>> rhs)
			{
				return lhs.Equals<FA<TInput,TAccept>>(rhs);
			}
			// unordered comparison
			public bool Equals(ICollection<FA<TInput,TAccept>> lhs, ICollection<FA<TInput,TAccept>> rhs)
			{
				return lhs.Equals<FA<TInput,TAccept>>(rhs);
			}
			public bool Equals(IDictionary<TInput, FA<TInput,TAccept>> lhs, IDictionary<TInput, FA<TInput,TAccept>> rhs)
			{
				return lhs.Equals<KeyValuePair<TInput, FA<TInput,TAccept>>>(rhs);
			}
			public bool Equals(IDictionary<FA<TInput,TAccept>, ICollection<TInput>> lhs, IDictionary<FA<TInput,TAccept>, ICollection<TInput>> rhs)
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
			public int GetHashCode(IList<FA<TInput,TAccept>> lhs)
			{
				return lhs.GetHashCode<FA<TInput,TAccept>>();
			}
			public int GetHashCode(ICollection<FA<TInput,TAccept>> lhs)
			{
				return lhs.GetHashCode<FA<TInput,TAccept>>();
			}
			public int GetHashCode(IDictionary<TInput, FA<TInput,TAccept>> lhs)
			{
				return lhs.GetHashCode<KeyValuePair<TInput, FA<TInput,TAccept>>>();
			}
			public static readonly _SetComparer Default = new _SetComparer();
		}
	}
}
