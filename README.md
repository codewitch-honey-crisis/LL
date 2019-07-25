# LL: A parser generator playground - supersedes Newt

This repository contains several projects

* **ll** - the main runtime library, includes entire API and DOM for creating and rendering parsers and tables

* **llrt** - the minimal runtime necessary to use the generated parsers. Do not include both this and ll

* **llgen** - a tool to generate a parser (vb generation is broken due to a bug in Microsoft's VBCodeProvider, C# works great)

* **lltree** - a tool to render an ascii parse tree for a given grammar and input file

* **llvstudio** - a custom tool "LL" used in visual studio 2017 (and 2019?) that can render like llgen does

* **llgui** - a work in progress gui for editing EBNF. Doesn't quite work yet

* **lltest** - one of my tests written as a command line utility.

The *DebugLL1Parser* and *DebugTokenizer* classes use only strings for internal information so seeing what they do in the debugger is easy. I use these to prototype changes to the parser and lexer algorithms before baking those changes into the *LL1TableParser* and *TableTokenizer* classes.

Cfg and Ebnf are both pretty enormous and contain APIs for manipulating CFGs and EBNF documents. The latter exposes a full DOM object model.
