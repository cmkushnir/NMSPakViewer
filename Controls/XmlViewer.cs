//=============================================================================
/*
MIT License

Copyright (c) 2021 Chris Kushnir

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
//=============================================================================

using System;
using System.Windows;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk.Controls
{
	public partial class XmlViewer : cmk.Controls.TextViewer
	{
		protected static avalon.Highlighting.IHighlightingDefinition s_highlighter =
			avalon.Highlighting.HighlightingManager.Instance.GetDefinitionByExtension(".xml")
		;
		public static avalon.Folding.XmlFoldingStrategy FoldingStrategy =
			new avalon.Folding.XmlFoldingStrategy {
				ShowAttributesWhenFolded = true
			}
		;

		protected avalon.Folding.FoldingManager m_folding_manager;

		//...........................................................

		public XmlViewer ()
		:	base ()
		{
			SyntaxHighlighting = s_highlighter;
			m_folding_manager  = avalon.Folding.FoldingManager.Install(TextArea);
		}

		//...........................................................

		public avalon.Folding.FoldingManager FoldingManager {
			get { return m_folding_manager; }
		}

		//...........................................................

		protected override void OnLoaded ( object SENDER, RoutedEventArgs ARGS ) 
		{
			base.OnLoaded(SENDER, ARGS);
			FoldingStrategy.UpdateFoldings(m_folding_manager, Document);
		}
	}
}

//=============================================================================
