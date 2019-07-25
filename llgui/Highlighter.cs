using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace LL
{
	public partial class Highlighter : RichTextBox
	{

		private IContainer components;
		bool _isColorizing = false;
		public Parser Parser { get; set; }
		public Highlighter() : base()
		{
			InitializeComponent();
			Multiline = true;
			AcceptsTab = true;
		}
		class _InputEnumerable :IEnumerable<char>
		{
			Highlighter _outer;
			public _InputEnumerable(Highlighter outer) { _outer = outer; }

			public IEnumerator<char> GetEnumerator() { return new _InputEnumerator(_outer); }
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
		class _InputEnumerator : IEnumerator<char>
		{
			Highlighter _outer;
			IEnumerator<char> _inner;
			public _InputEnumerator(Highlighter outer) { _outer = outer; Reset(); }

			public char Current => _inner.Current;
			object IEnumerator.Current => _inner.Current;

			public void Dispose() { if (null != _inner) _inner.Dispose(); }

			public bool MoveNext()
				=> _inner.MoveNext();

			public void Reset()
			{
				if (null != _inner) _inner.Dispose();
				_inner = _outer.Text.GetEnumerator();
			}
		}
		public IEnumerable<char> Input { get { return new _InputEnumerable(this); } }
		protected override void OnTextChanged(EventArgs e)
		{
			_Colorize(Text);
			base.OnTextChanged(e);
			
		}
		void _Colorize(string text)
		{
			if (_isColorizing)
				return;
			var spos = this.SelectionStart;
			var atEnd = spos >= text.Length-1;
			_isColorizing = true;
			if(null!=Parser)
			{
				var sb = new StringBuilder();
				var cols = new List<Color>();
				cols.Add(Color.Black);
				cols.Add(Color.DarkRed);
				Parser.Restart();
				var colStack = new Stack<int>();
				colStack.Push(0);
				while (Parser.Read())
				{
					switch (Parser.NodeType) {
						case ParserNodeType.NonTerminal:
							var c = Parser.GetAttribute("color") as string;
							if (!string.IsNullOrEmpty(c))
							{
								System.Diagnostics.Debug.WriteLine(c);
								Color color = Color.FromName(c);
								var i = cols.IndexOf(color);
								if (0 > i)
								{
									i = cols.Count;
									cols.Add(color);
									
								}
								colStack.Push(i);
							}
							break;
						case ParserNodeType.EndNonTerminal:
							if(0<colStack.Count)
								colStack.Pop();
							break;
						case ParserNodeType.Terminal:
						case ParserNodeType.Error:
							if (ParserNodeType.Error == Parser.NodeType)
							{
								sb.Append("\\cf1\\ulwave");
							}
							else
							{
								var pushed = false;
								var cc = Parser.GetAttribute("color") as string;
								if (!string.IsNullOrEmpty(cc))
								{
									Color color = Color.FromName(cc);
									var i = cols.IndexOf(color);
									if (0 > i)
									{
										i = cols.Count;
										cols.Add(color);

									}
									pushed = true;
									colStack.Push(i);
								}
								if (0 < colStack.Count)
								{
									sb.Append("\\cf");
									sb.Append(colStack.Peek());
								}
								else
									sb.Append("\\cf0");
								if (pushed)
									colStack.Pop();
							}
							sb.Append(RtfUtility.Escape(Parser.Value));
							if(ParserNodeType.Error== Parser.NodeType)
							{
								sb.Append("\\ul0");
							}
							break;
					}
				}
				
				Rtf = string.Concat("{\\rtf1", RtfUtility.ToColorTable(cols.ToArray()), sb.ToString(),"}");
			}
			if (!atEnd)
				SelectionStart = spos;
			else
				SelectionStart = Text.Length + 1;
			_isColorizing = false;
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.SuspendLayout();
			// 
			// colorizeTimer
			// 
			this.ResumeLayout(false);

		}

		
	}
}
