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
using System.Threading.Tasks;
using Microsoft.Win32;

//=============================================================================

namespace cmk.NMS.PakViewer
{
	public partial class App : System.Windows.Application
	{
		protected string m_GamePath;

		protected List<cmk.NMS.PAK.File> m_Game_PAK_List,
										 m_Mod_PAK_List,
										 m_PAK_List;

		public cmk.NMS.PAK.Item.Node  PakTree;

		public delegate void PakTreeBuiltEventHandler ( object SENDER, cmk.NMS.PAK.Item.Node ROOT );
		public event         PakTreeBuiltEventHandler PakTreeBuilt;

		//...........................................................

		public App ()
		:	base ()
		{
		}

		//...........................................................

		public static new App Current {
			get {
				return System.Windows.Application.Current as App;
			}
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
			var dialog = new cmk.Controls.SelectFolderDialog();
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

		public List<cmk.NMS.PAK.File> GamePakFiles {
			get {
				if( m_Game_PAK_List == null ) {
					var list = new List<cmk.NMS.PAK.File>();
					var path = GamePath + "GAMEDATA/PCBANKS/";
					if( Directory.Exists(path) ) {
						foreach( var file in System.IO.Directory.EnumerateFiles(path, "*.pak") ) {
							list.Add(new cmk.NMS.PAK.File(file));
						}
						list.Sort();
					}
					m_Game_PAK_List = list;
				}
				return m_Game_PAK_List;
			}
		}

		//...........................................................

		public List<cmk.NMS.PAK.File> ModPakFiles {
			get {
				if( m_Mod_PAK_List == null ) {
					var list = new List<cmk.NMS.PAK.File>();
					var path = GamePath + "GAMEDATA/PCBANKS/MODS/";
					if( Directory.Exists(path) ) {
						foreach( var file in System.IO.Directory.EnumerateFiles(path, "*.pak") ) {
							list.Add(new cmk.NMS.PAK.File(file));
						}
						list.Sort();
					}
					m_Mod_PAK_List = list;
				}
				return m_Mod_PAK_List;
			}
		}

		//...........................................................

		public List<cmk.NMS.PAK.File> PakFiles {
			get {
				if( m_PAK_List == null ) {
					var list = new List<cmk.NMS.PAK.File>();

					foreach( var pak in ModPakFiles ) list.Add(pak);

					// add null item, use full tree if selected
					list.Add(new cmk.NMS.PAK.File(null));

					foreach( var pak in GamePakFiles ) list.Add(pak);

					m_PAK_List = list;
				}
				return m_PAK_List;
			}
		}

		//...........................................................

		public Task BuildPakTree ()
		{
			// start task to build combined tree of meta-data from all game .pak files,
			// don't wait for it to finish so we can show ui to user right-away.
			return Task.Run(() => {
				var root = new cmk.NMS.PAK.Item.Node();

				foreach( var pak in App.Current.GamePakFiles ) {
					var entries  = pak.EntryList;
					if( entries != null )
						for( int entry = 1; entry < entries.Count; ++entry ) {
							if( entries[entry].Path.EndsWith(".BIN")
							||  entries[entry].Path.EndsWith(".SPV")  // ~46K
							||  entries[entry].Path.EndsWith(".WEM")  // ~manyK
							)	continue;
							root.Insert(entries[entry].Path, entries[entry]);  // add tree node
						}
				}

				PakTree = root;
				PakTreeBuilt?.Invoke(this, PakTree);
			});
		}
	}
}

//=============================================================================
