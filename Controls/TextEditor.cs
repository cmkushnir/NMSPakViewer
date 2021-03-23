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
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace NMS
{
	public partial class TextEditor : avalon.TextEditor
	{
		static TextEditor ()
		{
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(TextEditor), new FrameworkPropertyMetadata(typeof(TextEditor))
			);
		}

		protected static System.Windows.Media.FontFamily s_font =
			new System.Windows.Media.FontFamily("Consolas")
		;

		//...........................................................

		public TextEditor ()
		:	base ()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch;
			VerticalAlignment   = VerticalAlignment.Stretch;

			IsReadOnly = true;  // all derived are viewers, none allow edit

			avalon.Search.SearchPanel.Install(TextArea);

			Background = SystemColors.ControlLightBrush;
			FontFamily = s_font;
			FontSize   = 14;
			ShowLineNumbers = true;
			Options.EnableHyperlinks = true;
			Options.WordWrapIndentation = 4;
			Options.InheritWordWrapIndentation = true;
		}
	}
}

//=============================================================================
