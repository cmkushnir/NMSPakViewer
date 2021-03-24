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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

//=============================================================================

namespace NMS.Controls
{
	public partial class SelectFolderDialog : Window
	{
		protected DirectoryInfo m_directory_info;

		//...........................................................

		public SelectFolderDialog ()
		:	base ()
		{
			InitializeComponent();

			OK.Click     += OnOK;
			Cancel.Click += OnCancel;

			LoadDrives();
		}

		//...........................................................

		public DirectoryInfo DirectoryInfo {
			get { return m_directory_info; }
		}

		//...........................................................

		protected TreeViewItem NewItem ( DirectoryInfo INFO )
		{
			var item   = new TreeViewItem {
				Header = INFO.Name,
				Tag    = INFO
			};
			item.Expanded += OnExpanded;
			return item;
		}

		//...........................................................

		protected void LoadDrives ()
		{
			foreach( var drive in DriveInfo.GetDrives() ) {
				try   { if( drive.TotalSize == 0 ) continue; }
				catch { continue; }  // e.g. CDRom w/ no disk

				DirectoryInfo sub;
				try   { sub = new DirectoryInfo(drive.Name); }
				catch { continue; }

				var item   = NewItem(sub);
				var index  = Tree.Items.Add(item);
				if( index >= 0 ) LoadBranch(item);
			}
		}

		//...........................................................

		protected void LoadBranch ( TreeViewItem ITEM )
		{
			if( ITEM.Items.Count > 0 ) return;

			var folder = ITEM.Tag as DirectoryInfo;
			IEnumerable<DirectoryInfo> subs;

			try   { subs = folder.EnumerateDirectories(); }
			catch { return; }  // e.g. access denined 'c:\System Volume Information'

			foreach( var sub in subs ) {
				if( sub.Name.First() == '$' ) continue;  // recycle bin, ...

				var item = NewItem(sub);
				ITEM.Items.Add(item);
			}
		}

		//...........................................................

		protected void OnExpanded ( object SENDER, RoutedEventArgs ARGS )
		{
			var sender = SENDER as TreeViewItem;
			foreach( TreeViewItem item in sender.Items ) {
				LoadBranch(item);
			}
		}

		//...........................................................

		protected void OnOK ( object SENDER, RoutedEventArgs ARGS )
		{
			var item = Tree.SelectedItem as TreeViewItem;
			var info = item?.Tag as DirectoryInfo;
			if( info == null ) DialogResult = false;
			else {
				m_directory_info = info;
				DialogResult     = true;
			}
			Close();
		}

		//...........................................................

		protected void OnCancel ( object SENDER, RoutedEventArgs ARGS )
		{
			DialogResult = false;
			Close();
		}
	}
}

//=============================================================================
