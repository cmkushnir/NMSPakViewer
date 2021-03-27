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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using avalon = ICSharpCode.AvalonEdit;
using diffplex = DiffPlex.DiffBuilder;

//=============================================================================

namespace cmk.Controls
{
	public partial class TextDiffer : System.Windows.Controls.UserControl
	{
		protected avalon.TextEditor m_viewerL,
									m_viewerR;

		protected TextDifferBackgroundRenderer m_renderL,
		                                       m_renderR;

		protected diffplex.Model.SideBySideDiffModel m_model;

		//...........................................................

		public TextDiffer ( string LHS, string RHS )
		:	base ()
		{
			InitializeComponent();

			ScrollBarV.Orientation   = Orientation.Vertical;
			ScrollBarV.ValueChanged += ScrollBarVChanged;

			m_model = diffplex.SideBySideDiffBuilder.Diff(LHS, RHS, true, false);

			// derived or caller must call ConstructViewers to complete construction
		}

		//...........................................................

		public void ConstructViewers<EDITOR_T> ()
		where EDITOR_T : avalon.TextEditor, new()
		{
			m_viewerL = new EDITOR_T();  PanelL.Children.Add(m_viewerL);
			m_viewerR = new EDITOR_T();  PanelR.Children.Add(m_viewerR);

			// disable folding:
			// can't listen for folding events, can't sync foldings between viewers
			// todo: more generic solution if add folding to types other than xml.
			var viewerL  = m_viewerL as XmlViewer;
			var viewerR  = m_viewerR as XmlViewer;
			if( viewerL != null && viewerR != null ) {
				avalon.Folding.FoldingManager.Uninstall(viewerL.FoldingManager);
				avalon.Folding.FoldingManager.Uninstall(viewerR.FoldingManager);
			}

			m_viewerL.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			m_viewerL.VerticalScrollBarVisibility   = ScrollBarVisibility.Hidden;

			m_viewerR.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			m_viewerR.VerticalScrollBarVisibility   = ScrollBarVisibility.Hidden;

			foreach( var line in m_model.OldText.Lines ) {
				var text = line?.Text + System.Environment.NewLine;
				m_viewerL.AppendText(text);
			}
			foreach( var line in m_model.NewText.Lines ) {
				var text = line?.Text + System.Environment.NewLine;
				m_viewerR.AppendText(text);
			}

			m_renderL = new TextDifferBackgroundRenderer(m_viewerL);
			m_renderR = new TextDifferBackgroundRenderer(m_viewerR);

			m_renderL.Lines = m_model.OldText.Lines;
			m_renderR.Lines = m_model.NewText.Lines;

			m_viewerL.TextArea.TextView.BackgroundRenderers.Add(m_renderL);
			m_viewerR.TextArea.TextView.BackgroundRenderers.Add(m_renderR);

			m_viewerL.TextArea.TextView.VisualLinesChanged  += ViewerL_VScrollChanged;
			m_viewerL.TextArea.TextView.ScrollOffsetChanged += ViewerL_HScrollChanged;
			m_viewerL.LayoutUpdated                         += ViewerL_LayoutUpdated;

			m_viewerR.TextArea.TextView.VisualLinesChanged  += ViewerR_VScrollChanged;
			m_viewerR.TextArea.TextView.ScrollOffsetChanged += ViewerR_HScrollChanged;

			Loaded += OnLoaded;
		}

		//...........................................................

		protected void OnLoaded ( object SENDER, System.Windows.RoutedEventArgs ARGS )
		{
			ScrollBarV_Init();
		}

		//...........................................................

		protected void ViewerL_LayoutUpdated ( object SENDER, EventArgs ARGS )
		{
			ScrollBarV_Init();
		}

		//...........................................................

		protected void ScrollBarV_Init ()
		{
			ScrollBarV.Minimum      = 0;
			ScrollBarV.Maximum      = m_viewerL.ExtentHeight - m_viewerL.ViewportHeight;
			ScrollBarV.ViewportSize = m_viewerL.ViewportHeight;
			ScrollBarV.SmallChange  = m_viewerL.TextArea.TextView.DefaultLineHeight;
			ScrollBarV.LargeChange  = m_viewerL.ViewportHeight;
		}

		//...........................................................

		protected void ScrollBarVChanged ( object SENDER, System.Windows.RoutedPropertyChangedEventArgs<double> ARGS )
		{
			var sender = SENDER as ScrollBar;
			var offset = sender.Value;
			m_viewerL.ScrollToVerticalOffset(offset);
			m_viewerR.ScrollToVerticalOffset(offset);
		}

		//...........................................................

		protected void ViewerL_VScrollChanged ( object SENDER, System.EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			ScrollBarV.Value = sender.VerticalOffset;
		}

		//...........................................................

		protected void ViewerL_HScrollChanged ( object SENDER, System.EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			var offsetL = sender.HorizontalOffset  / (m_viewerL.ExtentWidth - m_viewerL.ViewportWidth);  // as %
			var offsetR =                  offsetL * (m_viewerR.ExtentWidth - m_viewerR.ViewportWidth);
			m_viewerR.ScrollToHorizontalOffset(offsetR);
		}

		//...........................................................

		protected void ViewerR_VScrollChanged ( object SENDER, System.EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			ScrollBarV.Value = sender.VerticalOffset;
		}

		//...........................................................

		protected void ViewerR_HScrollChanged ( object SENDER, System.EventArgs ARGS )
		{
			var sender = SENDER as avalon.Rendering.TextView;
			var offsetR = sender.HorizontalOffset  / (m_viewerR.ExtentWidth - m_viewerR.ViewportWidth);  // as %
			var offsetL =                  offsetR * (m_viewerL.ExtentWidth - m_viewerL.ViewportWidth);
			m_viewerL.ScrollToHorizontalOffset(offsetL);
		}
	}
}

//=============================================================================
