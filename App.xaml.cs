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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

//=============================================================================

namespace NMS
{
	public partial class App : Application
	{
		protected string m_GamePath;

		protected List<PAK.File> m_Game_PAK_List = new List<PAK.File>();
		protected List<PAK.File> m_Mod_PAK_List  = new List<PAK.File>();

		public List<PAK.File> PAK_List = new List<PAK.File>();
		public PAK.Entry.Node PAK_Tree;

		public delegate void PakTreeBuiltEventHandler ( object SENDER, PAK.Entry.Node ROOT );
		public event         PakTreeBuiltEventHandler PakTreeBuilt;

		//...........................................................

		public App ()
		{
			{	// add null item as first .pak, use full tree if selected
				var item = new PAK.File(null);
				PAK_List.Add(item);
			}

			// parallel load meta-data for all .pak files
			var load_pak_actions = new List<Action>();
			foreach( var path in ModPakFiles ) {
				load_pak_actions.Add(() => {
					var item = new PAK.File(path);

					Monitor.Enter(m_Mod_PAK_List);
					m_Mod_PAK_List.Add(item);
					Monitor.Exit(m_Mod_PAK_List);

					Monitor.Enter(PAK_List);
					PAK_List.Add(item);
					Monitor.Exit(PAK_List);
				});
			}
			foreach( var path in GamePakFiles ) {
				load_pak_actions.Add( () => {
					var item = new PAK.File(path);

					Monitor.Enter(m_Game_PAK_List);
					m_Game_PAK_List.Add(item);
					Monitor.Exit(m_Game_PAK_List);

					Monitor.Enter(PAK_List);
					PAK_List.Add(item);
					Monitor.Exit(PAK_List);
				});
			}
			Parallel.Invoke(load_pak_actions.ToArray());

			// start task to build combined tree of meta-data from all game .pak files,
			// don't wait for it to finish so we can show ui to user right-away.
			Task.Run( () => {
				var root = new PAK.Entry.Node();

				foreach( var pak in m_Game_PAK_List ) {
					var entries  = pak.EntryList;
					if( entries != null )
					for( int entry = 1; entry < entries.Count; ++entry ) {
						if( entries[entry].Path.EndsWith(".BIN")
						||	entries[entry].Path.EndsWith(".SPV")  // ~46K
						||	entries[entry].Path.EndsWith(".WEM")  // ~manyK
						) continue;
						root.Insert(entries[entry].Path, entries[entry]);  // add tree node
					}
				}

				PAK_Tree = root;
				PakTreeBuilt(this, PAK_Tree);
			});
		}

		//...........................................................

		public static new App Current {
			get {
				return Application.Current as App;
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
		///   search for "installdir", followed by "No Man's Sky"
		/// SteamGamePath == <SteamPath>/steamapps/common/<installdir>
		/// </summary>
		/// <returns>Null if not installed as a Steam game.</returns>
		protected static string SteamGamePath ()
		{
			// is it: i) an owned Steam game, ii) installed
			var reg  = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\Apps\275850");
			if( reg == null || ((int?)reg.GetValue("Installed")).GetValueOrDefault(0) == 0 ) return null;

			reg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
			var steam_path = reg.GetValue("SteamPath") as string;
			if( steam_path.IsNullOrEmpty() ) return null;

			var manifest = System.IO.File.OpenText(steam_path + "/steamapps/appmanifest_275850.acf")?.ReadToEnd();
			if( manifest.IsNullOrEmpty() ) return null;

			var index_start = manifest.IndexOf("installdir");
			    index_start = manifest.IndexOf('\"', index_start + 11);
			var index_end   = manifest.IndexOf('\"', ++index_start);

			var game_path = manifest.Substring(index_start, index_end - index_start);

			return steam_path + "/steamapps/common/" + game_path + "/";
		}

		//...........................................................

		protected static string GOGGamePath ()
		{
			// todo: get GOG game path
			return null;
		}

		//...........................................................

		public string GamePath {
			get {
				if( m_GamePath.IsNullOrEmpty() ) {
					                                 m_GamePath = SteamGamePath();
					if( m_GamePath.IsNullOrEmpty() ) m_GamePath = GOGGamePath();
					// no support for XBOX GamePass PC version
					if( m_GamePath.IsNullOrEmpty() ) m_GamePath = @"g:/games/No Man's Sky/";
				}
				return m_GamePath;
			}
			set {
				var path = new IO.Directory(value);
				if( path.Exists() ) {
					m_GamePath = path.Path;
				}
			}
		}

		//...........................................................

		public IEnumerable<string> GamePakFiles {
			get {
				return System.IO.Directory.EnumerateFiles(GamePath + "GAMEDATA/PCBANKS/", "*.pak");
			}
		}

		public IEnumerable<string> ModPakFiles {
			get {
				return System.IO.Directory.EnumerateFiles(GamePath + "GAMEDATA/PCBANKS/MODS/", "*.pak");
			}
		}
	}
}

//=============================================================================
