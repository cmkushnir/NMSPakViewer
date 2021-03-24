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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

//=============================================================================

namespace NMS
{
	public partial class AppWindow : Window
	{
		protected string m_GamePath;

		protected List<PAK.File> m_Game_PAK_List;
		protected List<PAK.File> m_Mod_PAK_List;

		public List<PAK.File> PAK_List = new List<PAK.File>();
		public PAK.Entry.Node PAK_Tree;

		public delegate void PakTreeBuiltEventHandler ( object SENDER, PAK.Entry.Node ROOT );
		public event         PakTreeBuiltEventHandler PakTreeBuilt;

		//...........................................................

		public AppWindow ()
		: base()
		{
			InitializeComponent();

			PakTreeBuilt  += Current_PakTreeBuilt;
			LayoutUpdated += AppWindow_LayoutUpdated;

			PakCombobox.ItemsSource       = PAK_List;  // mod and game .pak files
			PakCombobox.SelectionChanged += PakCombobox_SelectionChanged;

			EntryBreadcrumb.SelectionChanged += EntryBreadcrumb_SelectionChanged;

			Copy.Click  += Copy_Click;
			Save.Click  += Save_Click;
			About.Click += About_Click;

			{   // add null item as first .pak, use full tree if selected
				var pak = new PAK.File(null);
				PAK_List.Add(pak);
			}
			foreach( var pak in ModPakFiles ) PAK_List.Add(pak);
			foreach( var pak in GamePakFiles ) PAK_List.Add(pak);

			// start task to build combined tree of meta-data from all game .pak files,
			// don't wait for it to finish so we can show ui to user right-away.
			Task.Run(() => {
				var root = new PAK.Entry.Node();

				foreach( var pak in GamePakFiles ) {
					var entries  = pak.EntryList;
					if( entries != null )
						for( int entry = 1; entry < entries.Count; ++entry ) {
							if( entries[entry].Path.EndsWith(".BIN")
							||  entries[entry].Path.EndsWith(".SPV")  // ~46K
							||  entries[entry].Path.EndsWith(".WEM")  // ~manyK
							) continue;
							root.Insert(entries[entry].Path, entries[entry]);  // add tree node
						}
				}

				PAK_Tree = root;
				PakTreeBuilt?.Invoke(this, PAK_Tree);
			});
		}

		//...........................................................

		/// <summary>
		/// https://steamdb.info/ - ID for No Man's Sky == 275850
		/// HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\Apps\275850\
		///   Installed == 1|0
		///   Name      == "No Man's Sky"
		/// HKEY_CURRENT_USER\SOFTWARE\Valve\Steam\
		///   SteamPath == "g:/steam"
		/// g:/steam/steamapps/appmanifest_275850.acf - json like, but not json
		///   search for "installdir", followed by "No Man's Sky" (path)
		/// SteamGamePath == <SteamPath>/steamapps/common/<installdir>
		/// </summary>
		/// <returns>False if not installed as a Steam game.</returns>
		protected bool LoadSteamGamePath ()
		{
			m_GamePath = "";

			// is it: i) an owned Steam game, ii) installed
			var reg  = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps\275850");
			if( reg == null || ((int?)reg.GetValue("Installed")).GetValueOrDefault(0) == 0 ) return false;

			reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
			var steam = reg.GetValue("SteamPath") as string;
			if( steam.IsNullOrEmpty() ) return false;

			var manifest = System.IO.File.OpenText(steam + "/steamapps/appmanifest_275850.acf")?.ReadToEnd();
			if( manifest.IsNullOrEmpty() ) return false;

			var index_start = manifest.IndexOf("installdir");
			index_start = manifest.IndexOf('\"', index_start + 11);
			var index_end   = manifest.IndexOf('\"', ++index_start);

			// assume either '<folder>' or '<drive>:<path><folder>' format,
			// if former then assume in steamapps/common, else assume fully qualified path.
			m_GamePath = manifest.Substring(index_start, index_end - index_start);
			if( !m_GamePath.Contains(':') ) {
				m_GamePath = steam + "/steamapps/common/" + m_GamePath + "/";
			}
			if( !Directory.Exists(m_GamePath + "GAMEDATA") ) m_GamePath = "";

			return !m_GamePath.IsNullOrEmpty();
		}

		//...........................................................

		protected bool LoadGOGGamePath ()
		{
			m_GamePath = "";

			// todo: get GOG game path

			return !m_GamePath.IsNullOrEmpty();
		}

		//...........................................................

		protected bool PromptGamePath ()
		{
			var dialog = new NMS.Controls.SelectFolderDialog();
			dialog.Description.Content = @"Select No Man's Sky game folder.";

			if( dialog.ShowDialog() == true ) {
				m_GamePath = dialog.DirectoryInfo?.FullName.Replace('\\', '/') + '/';
				if( !Directory.Exists(m_GamePath + "GAMEDATA") ) m_GamePath = "";
			}

			return !m_GamePath.IsNullOrEmpty();
		}

		//...........................................................

		protected bool LoadGamePath ()
		{
			var key  = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\HelloGames\NoMansSky");
			if( key == null ) m_GamePath = "";
			else {
				m_GamePath = key.GetValue("InstallDir") as string;
				if( !Directory.Exists(m_GamePath + "GAMEDATA") ) m_GamePath = "";
			}
			return !m_GamePath.IsNullOrEmpty();
		}

		//...........................................................

		protected void SaveGamePath ()
		{
			if( !Directory.Exists(m_GamePath + "GAMEDATA") ) m_GamePath = "";
			var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\HelloGames\NoMansSky", true);
			if( key != null ) key.SetValue("InstallDir", m_GamePath);
		}

		//...........................................................

		public string GamePath {
			get {
				if( m_GamePath == null && !LoadGamePath() ) {
					if( LoadSteamGamePath() ||
						LoadGOGGamePath()   ||
						// no support for XBOX GamePass PC version
						PromptGamePath()
					) SaveGamePath();
				}
				return m_GamePath;
			}
		}

		//...........................................................

		public List<PAK.File> GamePakFiles {
			get {
				if( m_Game_PAK_List == null ) {
					m_Game_PAK_List  = new List<PAK.File>();
					if( GamePath.IsNullOrEmpty() ) return m_Game_PAK_List;

					var pak_path = GamePath + "GAMEDATA/PCBANKS/";
					if( !Directory.Exists(pak_path) ) return m_Game_PAK_List;

					var load_pak_actions = new List<Action>();
					foreach( var path in System.IO.Directory.EnumerateFiles(pak_path, "*.pak") ) {
						load_pak_actions.Add(() => {
							var pak = new PAK.File(path);
							Monitor.Enter(m_Game_PAK_List);
							m_Game_PAK_List.Add(pak);
							Monitor.Exit(m_Game_PAK_List);
						});
					}
					Parallel.Invoke(load_pak_actions.ToArray());
					m_Game_PAK_List.Sort();
				}
				return m_Game_PAK_List;
			}
		}

		//...........................................................

		public List<PAK.File> ModPakFiles {
			get {
				if( m_Mod_PAK_List == null ) {
					m_Mod_PAK_List  = new List<PAK.File>();
					if( GamePath.IsNullOrEmpty() ) return m_Mod_PAK_List;

					var pak_path = GamePath + "GAMEDATA/PCBANKS/MODS/";
					if( !Directory.Exists(pak_path) ) return m_Game_PAK_List;

					var load_pak_actions = new List<Action>();
					foreach( var path in System.IO.Directory.EnumerateFiles(pak_path, "*.pak") ) {
						load_pak_actions.Add(() => {
							var pak = new PAK.File(path);
							Monitor.Enter(m_Mod_PAK_List);
							m_Mod_PAK_List.Add(pak);
							Monitor.Exit(m_Mod_PAK_List);
						});
					}
					Parallel.Invoke(load_pak_actions.ToArray());
					m_Mod_PAK_List.Sort();
				}
				return m_Mod_PAK_List;
			}
		}

		//...........................................................

		protected void Current_PakTreeBuilt ( object SENDER, PAK.Entry.Node ROOT )
		{
			App.Current.Dispatcher.Invoke(() => {
				EntryBreadcrumb.ItemsSource = PAK_Tree;  // = ROOT
			});
		}

		//...........................................................

		protected void AppWindow_LayoutUpdated ( object SENDER, System.EventArgs ARGS )
		{
			PakCombobox.MaxDropDownHeight = Viewer.ActualHeight - 4;
			EntryBreadcrumb.MaxDropDownHeight = Viewer.ActualHeight - 4;
		}

		//...........................................................

		protected void PakCombobox_SelectionChanged ( object SENDER, SelectionChangedEventArgs ARGS )
		{
			var file = ARGS.AddedItems.Count < 1 ? null : ARGS.AddedItems[0] as PAK.File;
			EntryBreadcrumb.ItemsSource =
				string.IsNullOrEmpty(file?.Name) ?
				PAK_Tree :  // null until fully built
				file.EntryTree
			;
		}

		//...........................................................

		protected void EntryBreadcrumb_SelectionChanged ( object SENDER, IPathNode SELECTED )
		{
			Viewer.Children.Clear();
			Save.IsEnabled = false;

			var info   = SELECTED?.Tag as PAK.Entry.Info;
			var data   = PAK.Entry.Data.New(info);
			var viewer = data?.ViewerControl;

			if( viewer != null ) {
				Viewer.Children.Add(viewer);
				Save.IsEnabled = true;
			}
		}

		//...........................................................

		protected void Copy_Click ( object SENDER, RoutedEventArgs ARGS )
		{
			var path = EntryBreadcrumb.Selected?.Path;
			Clipboard.SetText(path ?? "");
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

		//...........................................................

		protected void About_Click ( object SENDER, RoutedEventArgs ARGS )
		{
			Process.Start(new ProcessStartInfo(@"https://github.com/cmkushnir/NMSPakViewer"));
			ARGS.Handled = true;
		}
	}
}

//=============================================================================
