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

using System.Windows.Controls;

//=============================================================================

namespace NMS.Controls
{
	public partial class Breadcrumb : UserControl
	{
		protected IPathNode m_items_source;

		public delegate void SelectionChangedEventHandler ( object SENDER, IPathNode SELECTED );
		public event         SelectionChangedEventHandler SelectionChanged;

		//...........................................................

		public Breadcrumb ()
		:	base ()
		{
			InitializeComponent();
			Root.Tag               = 1;  // 1-based index
			Root.IsEditable        = false;
			Root.IsReadOnly        = true;
			Root.Padding           = new System.Windows.Thickness(4, 0, 4, 0);
			Root.SelectionChanged += Combobox_SelectionChanged;
		}

		//...........................................................

		public double MaxDropDownHeight {
			get { return Root.MaxDropDownHeight; }
			set {
				if( Root.MaxDropDownHeight != value ) {
					// Root is Panel.Children[0] 
					foreach( ComboBox cb in Panel.Children ) {
						cb.MaxDropDownHeight = value;
					}
				}
			}
		}

		//...........................................................

		public IPathNode ItemsSource {
			get {
				return m_items_source;
			}
			set {
				if( value == m_items_source ) return;

				var old_selected = Selected;
				m_items_source   = value;
				Root.ItemsSource = m_items_source?.Items;

				// try to reselect in new source (tree)
				Selected = old_selected;
			}
		}

		//...........................................................

		protected IPathNode SelectedChild ( int INDEX )
		{
			if( INDEX < 0 || INDEX >= Panel.Children.Count ) return null;
			var child = Panel.Children[INDEX] as ComboBox;
			return child.SelectedItem as IPathNode;
		}

		//...........................................................

		public IPathNode Selected {
			get {
				// if last child has a selected item then
				// Combobox_SelectionChanged determined there was no more path parts.
				// if last child has no selected item then
				// Combobox_SelectionChanged has populated it with the next path part options.
				var node  = SelectedChild(Panel.Children.Count - 1);  // file
				if( node == null ) {
					node  = SelectedChild(Panel.Children.Count - 2);  // dir
				}
				return node;
			}
			set {
				if( Selected == value ) return;

				var nodes = value?.PathNodes;
				if( nodes.IsNullOrEmpty() ) {
					Root.SelectedIndex = -1;  // triggers Combobox_SelectionChanged
					return;
				}

				for( int i = 0; i < nodes.Count; ++i ) {
					// previous iteration failed to create next Combobox
					if( i >= Panel.Children.Count ) break;

					var child = Panel.Children[i] as ComboBox;
					child.SelectedItem = nodes[i];  // triggers Combobox_SelectionChanged
				}
			}
		}

		//...........................................................

		protected void Combobox_SelectionChanged ( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var sender = SENDER as ComboBox;
			var index  = (int)sender.Tag;

			while( Panel.Children.Count > index ) {
				Panel.Children.RemoveAt(Panel.Children.Count - 1);
			}

			var selected = sender.SelectedItem as IPathNode;
			SelectionChanged.Invoke(this, selected);

			if( selected?.Items == null ) return;

			var next = new ComboBox {
				ItemsSource       = selected.Items,
				Tag               = index + 1,
				IsEditable        = false,
				IsReadOnly        = true,
				SelectedIndex     = -1,
				MaxDropDownHeight = Root.MaxDropDownHeight,
				Padding           = new System.Windows.Thickness(4, 0, 4, 0)
			};
			next.SelectionChanged += Combobox_SelectionChanged;
			Panel.Children.Add(next);
		}
	}
}

//=============================================================================
