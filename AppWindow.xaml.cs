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

using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using avalon = ICSharpCode.AvalonEdit;

//=============================================================================

namespace NMS
{
	public partial class AppWindow : Window
	{
		protected PAK.Entry.Data m_data;
		protected UIElement      m_viewer;

		//...........................................................

		public AppWindow ()
		{
			InitializeComponent();

			App.Current.PakTreeBuilt += Current_PakTreeBuilt;
			LayoutUpdated            += AppWindow_LayoutUpdated;

			PakCombobox.ItemsSource       = App.Current.PAK_List;  // mod and game .pak files
			PakCombobox.SelectionChanged += PakCombobox_SelectionChanged;

			EntryBreadcrumb.SelectionChanged += EntryBreadcrumb_SelectionChanged;

			Save.Click += Save_Click;
		}

		//...........................................................

		protected void Current_PakTreeBuilt ( object SENDER, PAK.Entry.Node ROOT )
		{
			App.Current.Dispatcher.Invoke( () => {
				EntryBreadcrumb.ItemsSource = App.Current.PAK_Tree;  // = ROOT
			});
		}

		//...........................................................

		protected void AppWindow_LayoutUpdated ( object SENDER, System.EventArgs ARGS )
		{
			PakCombobox    .MaxDropDownHeight = Viewer.ActualHeight - 4;
			EntryBreadcrumb.MaxDropDownHeight = Viewer.ActualHeight - 4;
		}

		//...........................................................

		protected void PakCombobox_SelectionChanged ( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var file = ARGS.AddedItems.Count < 1 ? null : ARGS.AddedItems[0] as PAK.File;
			EntryBreadcrumb.ItemsSource =
				string.IsNullOrEmpty(file?.Name) ?
				App.Current.PAK_Tree :  // null until fully built
				file.EntryTree
			;
		}

		//...........................................................

		protected void EntryBreadcrumb_SelectionChanged ( object SENDER, IPathNode SELECTED )
		{
			Viewer.Children.Clear();
			Save.IsEnabled = false;

			var info   = SELECTED?.Tag as PAK.Entry.Info;
			  m_data   = PAK.Entry.Data.New(info);
			var viewer = m_data?.ViewerControl;

			if( viewer != null ) {
				Viewer.Children.Add(viewer);
				Save.IsEnabled = true;
			}
		}

		//...........................................................

		protected void Save_Click ( object SENDER, RoutedEventArgs ARGS )
		{
			var info  = EntryBreadcrumb.Selected?.Tag as PAK.Entry.Info;
			if( info == null ) return;

			var dialog = new SaveFileDialog {
				FileName = info.Name,
				Filter   = string.Format("{0}|*{1}",
					info.Extension.TrimStart('.'),
					info.Extension
				),
				FilterIndex = 0
			};
			if( info.Extension == ".MBIN" ) {  // hack
				dialog.Filter += "|EXML|*.EXML";
			}
			if( dialog.ShowDialog() == false ) return;

			if( dialog.FilterIndex > 0 ) {  // hack, .EXML
				var mbin  = PAK.Entry.Data.New(info) as PAK.MBIN.Data;
				if( mbin != null ) File.WriteAllText(dialog.FileName, mbin.EXML);
			}
			else using( var file = File.Create(dialog.FileName) ) {
				info.Extract().CopyTo(file);
			}
		}
	}
}

//=============================================================================
