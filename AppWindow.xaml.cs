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

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PakViewer
{
	public partial class AppWindow : System.Windows.Window
	{

		//...........................................................

		public AppWindow ()
		:	base ()
		{
			InitializeComponent();

			LayoutUpdated += AppWindow_LayoutUpdated;

			PakCombobox.ItemsSource       = App.Current.PakFiles;  // load mod and game .pak files
			PakCombobox.SelectionChanged += OnPakComboboxSelectionChanged;

			ItemBreadcrumb.SelectionChanged += OnItemBreadcrumbSelectionChanged;

			Copy.Click   += OnCopyClick;
			Save.Click   += OnSaveClick;
			GitHub.Click += OnGitHubClick;

			App.Current.PakTreeBuilt += OnPakTreeBuilt;
			var task = App.Current.BuildPakTree();
		}

		//...........................................................

		protected void AppWindow_LayoutUpdated ( object SENDER, System.EventArgs ARGS )
		{
			PakCombobox    .MaxDropDownHeight = Viewer.ActualHeight - 4;
			ItemBreadcrumb.MaxDropDownHeight = Viewer.ActualHeight - 4;
		}

		//...........................................................

		protected void OnPakTreeBuilt ( object SENDER, PAK.Item.Node ROOT )
		{
			App.Current.PakTreeBuilt -= OnPakTreeBuilt;

			// if ItemBreadcrumb.ItemsSource != null then user already picked a specific mod to view
			if( ItemBreadcrumb.ItemsSource == null )
			App.Current.Dispatcher.Invoke(() => {
				ItemBreadcrumb.ItemsSource = App.Current.PakTree;  // = ROOT
			});
		}

		//...........................................................

		protected void OnPakComboboxSelectionChanged ( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var file = ARGS.AddedItems.Count < 1 ? null : ARGS.AddedItems[0] as PAK.File;
			ItemBreadcrumb.ItemsSource =
				string.IsNullOrEmpty(file?.Name) ?
				App.Current.PakTree :  // null until fully built
				file.EntryTree
			;
		}

		//...........................................................

		protected void OnItemBreadcrumbSelectionChanged ( object SENDER, cmk.IPathNode SELECTED )
		{
			Viewer.Children.Clear();

			var info   = SELECTED?.Tag as PAK.Item.Info;
			var data   = PAK.Item.Data.New(info);
			var viewer = data?.ViewerControl;

			ItemBreadcrumb.ToolTip = info?.File?.Name;

			Save.IsEnabled = data?.Raw != null;

			if( viewer != null ) Viewer.Children.Add(viewer);			
		}

		//...........................................................

		protected void OnCopyClick ( object SENDER, RoutedEventArgs ARGS )
		{
			var path = ItemBreadcrumb.Selected?.Path;
			Clipboard.SetText(path ?? "");
		}

		//...........................................................

		protected void OnSaveClick ( object SENDER, RoutedEventArgs ARGS )
		{
			var info  = ItemBreadcrumb.Selected?.Tag as PAK.Item.Info;
			if( info == null ) return;

			var dialog = new SaveFileDialog {
				FileName = info.Name,
				Filter   = string.Format("{0}|*{1}",
					info.Extension.TrimStart('.'),
					info.Extension
				),
			};
			if( info.Extension ==  ".MBIN" ||
				info.Path.EndsWith(".MBIN.PC")
			)	dialog.Filter += "|EXML|*.EXML";

			if( dialog.ShowDialog() == false ) return;

			if( dialog.FileName.EndsWith(".EXML") ) {
				var mbin  = PAK.Item.Data.New(info) as PAK.MBIN.Data;
				if( mbin != null ) File.WriteAllText(dialog.FileName, mbin.EXML);
			}
			else using( var file = File.Create(dialog.FileName) ) {
				info.Extract().CopyTo(file);
			}
		}

		//...........................................................

		protected void OnGitHubClick ( object SENDER, RoutedEventArgs ARGS )
		{
			Process.Start(new ProcessStartInfo(@"https://github.com/cmkushnir/NMSPakViewer"));
			ARGS.Handled = true;
		}
	}
}

//=============================================================================
