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

using System.Windows;
using System.Windows.Media;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace cmk.Controls
{
	public partial class TextViewer : avalon.TextEditor
	{
		static TextViewer ()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(TextViewer), new FrameworkPropertyMetadata(typeof(TextViewer))
			);
		}

		protected static System.Windows.Media.FontFamily s_font =
			new System.Windows.Media.FontFamily("Consolas")
		;

		protected avalon.Search.SearchPanel m_search;

		//...........................................................

		public TextViewer ()
		:	base ()
		{
			IsReadOnly = true;

			HorizontalAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment   = VerticalAlignment  .Stretch;

			m_search = avalon.Search.SearchPanel.Install(TextArea);
			m_search.MarkerBrush = Brushes.Yellow;

			Background      = SystemColors.ControlLightBrush;
			FontFamily      = s_font;
			FontSize        = 14;
			ShowLineNumbers = true;

			Options.EnableHyperlinks           = true;
			Options.WordWrapIndentation        = 4;
			Options.InheritWordWrapIndentation = true;

			Loaded += OnLoaded;
		}

		//...........................................................

		protected virtual void OnLoaded ( object SENDER, RoutedEventArgs ARGS )
		{
			m_search.Open();
			m_search.Reactivate();
		}
	}
}

//=============================================================================
