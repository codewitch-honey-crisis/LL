using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public sealed class LookaheadEnumerable<T> : IEnumerable<T>
	{
		IEnumerable<T> _inner;
		public LookaheadEnumerable(IEnumerable<T> inner) { _inner = inner; }
		public LookaheadEnumerator<T> GetEnumerator()
			=> new LookaheadEnumerator<T>(_inner.GetEnumerator());
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
			=> GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
	public sealed class LookaheadEnumerator<T> : IEnumerator<T>
	{
		public LookaheadEnumerator(IEnumerator<T> inner)
		{
			_input = new Queue<T>();
			_inner = inner;
		}
		bool _skipNext;
		IEnumerator<T> _inner;
		Queue<T> _input;
		public T Current { get { return _input.Peek(); } }
		object System.Collections.IEnumerator.Current => Current;

		public bool MoveNext()
		{
			if (0 == _input.Count)
			{
				_skipNext = false;
				return _EnsureQueue();
			} 
			if(!_skipNext)
				_input.Dequeue();
			_skipNext = false;
			return _EnsureQueue();
		}
		public bool EnsureQueued(int count = 1)
		{
			if (1 > count) count = 1;
			while (_input.Count < count && _inner.MoveNext())
				_input.Enqueue(_inner.Current);
			
			return _input.Count >= count;

		}
		public bool TryPeekOne(int lookAhead, out T value)
		{
			value = default(T);
			if (0 > lookAhead)
				lookAhead = 0;
			if(!EnsureQueued(lookAhead + 1))
				return false;
			int i = 0;
			foreach(var result in _input)
			{
				if (i == lookAhead)
				{
					value = result;
					break;
				}
				++i;
			}
			return true;
		}
		public T PeekOne(int lookAhead)
		{
			T result;
			if (!TryPeekOne(lookAhead, out result))
				throw new InvalidOperationException("The cursor could not be positioned to the requested location.");
			return result;
		}
		public T[] Peek(int count)
		{
			var list = new List<Token>();
			while(_input.Count<count && _inner.MoveNext())
				_input.Enqueue(_inner.Current);

			if (count >= _input.Count)
			{
				_skipNext = true;
				return _input.ToArray();
			}
			var result = new T[count];
			var i = 0;
			foreach(var item in _input)
			{
				result[i] = item;
				++i;
				if (result.Length==i)
					break;
			}
			_skipNext = true;
			return result;
		}
		public IEnumerable<T> Bookmark { get {
				return new _Bookmarker(this);
			}
		}
		sealed class _Bookmarker : IEnumerable<T>
		{
			LookaheadEnumerator<T> _outer;
			public _Bookmarker(LookaheadEnumerator<T> outer) { _outer = outer; }

			public IEnumerator<T> GetEnumerator()
				=> new _BookmarkEnum(_outer);

			IEnumerator IEnumerable.GetEnumerator()
				=> GetEnumerator();
		}
		sealed class _BookmarkEnum : IEnumerator<T>
		{
			int _index;
			LookaheadEnumerator<T> _outer;
			public _BookmarkEnum(LookaheadEnumerator<T> outer) { _outer = outer; Reset(); }

			public T Current { get { return _outer.PeekOne(_index); } }
			object IEnumerator.Current => Current;

			public void Dispose() {}

			public bool MoveNext()
			{
				++_index;
				if (!_outer.EnsureQueued(_index + 1))
					return false;
				return true;
			}

			public void Reset()
			{
				_index = -1;
			}
		}
		public void Reset()
		{
			_inner.Reset();
			_skipNext = false;
			_input.Clear();
		}
		private bool _EnsureQueue()
		{
			if (0 == _input.Count)
			{
				if (!_inner.MoveNext())
					return false;
				_input.Enqueue(_inner.Current);
				return true;
			}
			return true;
		}
		public void Dispose()
		{
			_inner.Dispose();
			_input.Clear();
		}
	}
}
