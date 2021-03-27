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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using avalon   = ICSharpCode.AvalonEdit;
using diffplex = DiffPlex.DiffBuilder;

//=============================================================================

namespace cmk.Controls
{
	public class TextDifferBackgroundRenderer : avalon.Rendering.IBackgroundRenderer
	{
		private avalon.TextEditor m_editor;

		public List<diffplex.Model.DiffPiece> Lines;

		public Brush ModifiedBrush,
					 DeletedBrush,
		             InsertedBrush,
					 PaddingBrush;  // other file added or removed lines

		//...........................................................

		public TextDifferBackgroundRenderer ( avalon.TextEditor EDITOR )
		{
			m_editor = EDITOR;

			var background = m_editor.Background as SolidColorBrush;
			var color      = background.Color;

			ModifiedBrush = new LinearGradientBrush(
				Color.FromRgb(0x88, 0xff, 0xff),
				color, 0.0
			);
			DeletedBrush = new LinearGradientBrush(
				Color.FromRgb(0xff, 0xcc, 0xcc),
				color, 0.0
			);
			InsertedBrush = new LinearGradientBrush(
				Color.FromRgb(0xcc, 0xff, 0xcc),
				color, 0.0
			);
			PaddingBrush = new LinearGradientBrush(
				Color.FromRgb(0xd4, 0xd4, 0xd4),
				color, 0.0
			);
		}

		//...........................................................

		public avalon.Rendering.KnownLayer Layer {
			get { return avalon.Rendering.KnownLayer.Selection; }
		}

		//...........................................................

		public void Draw ( avalon.Rendering.TextView VIEW, DrawingContext CONTEXT )
		{
			if( m_editor.Document == null ) return;

			VIEW.EnsureVisualLines();
			if( m_editor.ExtentWidth < 1 ) return;  // happens on init

			// draw full-width, not just text width
			var size = new Size(
				m_editor.ExtentWidth,
				VIEW.DefaultLineHeight
			);

			for( var i = 0; i < Lines.Count; ++i ) {
				var   diff  = Lines[i];
				Brush brush = null;

				if( diff.Text == null ) brush = PaddingBrush;
				else                    switch( diff.Type ) {
					case diffplex.Model.ChangeType.Modified: brush = ModifiedBrush; break;
					case diffplex.Model.ChangeType.Deleted:  brush = DeletedBrush;  break;
					case diffplex.Model.ChangeType.Inserted: brush = InsertedBrush; break;
				}
				if( brush == null ) continue;

				var line = m_editor.Document.GetLineByNumber(i + 1);

				// usually only 1 rect per line, but may have more
				foreach( var rect in avalon.Rendering.BackgroundGeometryBuilder.GetRectsForSegment(VIEW, line) ) {
					var extents = new Rect(rect.Location, size);
					CONTEXT.DrawRectangle(brush, null, extents);
				}
			}
		}
	}
}

//=============================================================================
