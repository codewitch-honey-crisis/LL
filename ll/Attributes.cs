using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LL
{
	public sealed class AttributeSetDictionary : IDictionary<string,AttributeSet>
	{
		IDictionary<string, AttributeSet> _inner = new Dictionary<string, AttributeSet>();

		public object GetAttribute(string symbol,string attribute,object @default = null)
		{
			AttributeSet attrs;
			if(_inner.TryGetValue(symbol,out attrs))
			{
				object o;
				if (attrs.TryGetValue(attribute, out o))
					return o;
			}
			return @default;
		}
		public void SetAttribute(string symbol, string attribute, object value)
		{
			AttributeSet attrs;
			if (!_inner.TryGetValue(symbol, out attrs))
			{
				attrs = new AttributeSet();
				_inner.Add(symbol, attrs);
			}
			attrs[attribute] = value;
		}

		public AttributeSet this[string key] { get => _inner[key]; set => _inner[key] = value; }

		public ICollection<string> Keys => _inner.Keys;

		public ICollection<AttributeSet> Values => _inner.Values;

		public int Count => _inner.Count;

		public bool IsReadOnly => _inner.IsReadOnly;

		public void Add(string key, AttributeSet value)
		{
			_inner.Add(key, value);
		}

		public void Add(KeyValuePair<string, AttributeSet> item)
		{
			_inner.Add(item);
		}

		public void Clear()
		{
			_inner.Clear();
		}

		public bool Contains(KeyValuePair<string, AttributeSet> item)
		{
			return _inner.Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return _inner.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, AttributeSet>[] array, int arrayIndex)
		{
			_inner.CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, AttributeSet>> GetEnumerator()
		{
			return _inner.GetEnumerator();
		}

		public bool Remove(string key)
		{
			return _inner.Remove(key);
		}

		public bool Remove(KeyValuePair<string, AttributeSet> item)
		{
			return _inner.Remove(item);
		}

		public bool TryGetValue(string key, out AttributeSet value)
		{
			return _inner.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _inner.GetEnumerator();
		}
	}
	public sealed class AttributeSet : IDictionary<string,object>
	{
		IDictionary<string, object> _inner = new Dictionary<string, object>();

		public bool Start {
			get {
				object o;
				if (_inner.TryGetValue("start", out o) && o is bool)
					return (bool)o;
				return false;
			}
			set {
				_inner["start"] = value;
			}
		}
		public bool Hidden {
			get {
				object o;
				if (_inner.TryGetValue("hidden", out o) && o is bool)
					return (bool)o;
				return false;
			}
			set {
				_inner["hidden"] = value;
			}
		}
		public bool Collapse {
			get {
				object o;
				if (_inner.TryGetValue("collapsed", out o) && o is bool)
					return (bool)o;
				return false;
			}
			set {
				_inner["collapsed"] = value;
			}
		}
		public bool Terminal {
			get {
				object o;
				if (_inner.TryGetValue("terminal", out o) && o is bool)
					return (bool)o;
				return false;
			}
			set {
				_inner["terminal"] = value;
			}
		}
		public object this[string key] { get => _inner[key]; set => _inner[key] = value; }

		public ICollection<string> Keys => _inner.Keys;

		public ICollection<object> Values => _inner.Values;

		public int Count => _inner.Count;

		public bool IsReadOnly => _inner.IsReadOnly;

		public void Add(string key, object value)
		{
			_inner.Add(key, value);
		}

		public void Add(KeyValuePair<string, object> item)
		{
			_inner.Add(item);
		}

		public void Clear()
		{
			_inner.Clear();
		}

		public bool Contains(KeyValuePair<string, object> item)
		{
			return _inner.Contains(item);
		}

		public bool ContainsKey(string key)
		{
			return _inner.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			_inner.CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return _inner.GetEnumerator();
		}

		public bool Remove(string key)
		{
			return _inner.Remove(key);
		}

		public bool Remove(KeyValuePair<string, object> item)
		{
			return _inner.Remove(item);
		}

		public bool TryGetValue(string key, out object value)
		{
			return _inner.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _inner.GetEnumerator();
		}
	}
}
