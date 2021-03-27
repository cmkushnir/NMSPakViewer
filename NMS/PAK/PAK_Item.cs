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
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;

//=============================================================================

namespace cmk.NMS.PAK.Item
{
	/// <summary>
	/// Meta-data for a (compressed) entry (file) in a NMS .pak file.
	/// </summary>
	public class Info : System.IComparable<Info>
	{
		protected string m_path      = "";  // full dir/.../name.ext
		protected string m_name      = "";  // no dir, extension
		protected string m_extension = "";  // if !Extension then assume is directory 

		public readonly cmk.NMS.PAK.File File;

		public readonly  int Id;      // ordinal of EntryInfo in IO.PAK.m_entries
		public readonly uint Index;   // index of first block for Item in PAK.m_blocks
		public readonly long Offset;  // offset in PAK file where Item block data starts
		public readonly long Length;  // length of uncompressed Item

		//...........................................................

		public Info ( cmk.NMS.PAK.File FILE, int ID = 0, uint INDEX = 0, long OFFSET = 0, long LENGTH = 0 )
		{
			File   = FILE;
			Id     = ID;
			Index  = INDEX;
			Offset = OFFSET;
			Length = LENGTH;
		}

		//...........................................................

		public string Path {
			get { return m_path; }
			set {
				m_extension = "";
				m_name      = "";
				m_path      = value?.TrimStart('/', '\\');

				if( m_path == null ) m_path = "";
				if( m_path.Length < 1 ) return;

				m_path = m_path.Replace("\\", "/").ToUpper().Trim();

				// some NMS files have multiple '.' in name e.g. 'GCDEBUGOPTIONS.GLOBAL.MBIN'
				// parse as: m_name = 'GCDEBUGOPTIONS.GLOBAL', m_extension = '.MBIN'
				m_name      = System.IO.Path.GetFileNameWithoutExtension(m_path);
				m_extension = System.IO.Path.GetExtension(m_path);
			}
		}

		public string Name {
			get { return m_name; }
		}

		public string Extension {
			get { return m_extension; }
		}

		//...........................................................

		public Stream Extract ()
		{
			return File?.Extract(this);
		}

		//...........................................................

		public override string ToString ()
		{
			return m_name;
		}

		public int CompareTo ( Info RHS )
		{
			return string.Compare(m_path, RHS?.m_path);
		}
	}

	//=========================================================================

	/// <summary>
	/// Tree node wrapping meta-data for a .pak entry.
	/// </summary>
	public class Node : cmk.PathNode<cmk.NMS.PAK.Item.Info, Node>
	{
		public Node (
			Node   PARENT = null,
			string PATH   = "",
			Info   ENTRY  = null
		)
		:	base ( PARENT, PATH, ENTRY )
		{
		}
	}

	//=========================================================================

	public class Data : System.IComparable<Data>
	{
		public readonly cmk.NMS.PAK.Item.Info Info;
		protected Stream                    m_raw;

		//...........................................................

		protected Data ( cmk.NMS.PAK.Item.Info INFO, Stream RAW )
		{
			Info  = INFO;
			m_raw = RAW;
		}

		//...........................................................

		public static Data New ( cmk.NMS.PAK.Item.Info INFO )
		{
			if( INFO == null ) return null;

			var raw  = INFO.Extract();
			if( raw == null ) return null;

			switch( INFO.Extension ) {
				case ".TXT":  return new TXT .Data(INFO, raw);
				case ".CSV":  return new CSV .Data(INFO, raw);
				case ".JSON": return new JSON.Data(INFO, raw);
				case ".XML":  return new XML .Data(INFO, raw);
				case ".LUA":  return new LUA .Data(INFO, raw);
				case ".MBIN": return new MBIN.Data(INFO, raw);
				case ".DDS":  return new DDS .Data(INFO, raw);
			}
			if( INFO.Path.EndsWith(".MBIN.PC") ) {
				return new MBIN.Data(INFO, raw);
			}

			return new Data(INFO, raw);
		}

		//...........................................................

		public bool IsMod {
			get {
				var    path = Info.File?.Path??"";
				return path.Contains("\\MODS\\");
			}
		}

		public bool IsGame {
			get { return !IsMod; }
		}

		//...........................................................

		public Data GameData {
			get {
				if( !IsMod ) return null;

				// spin if still merging game .pak manifests
				while( cmk.NMS.PakViewer.App.Current.PakTree == null ) Thread.Sleep(10);

				// PakTree only has game files, not mod files
				var node = cmk.NMS.PakViewer.App.Current.PakTree.Search(Info.Path);
				var info = node?.Tag;

				return Data.New(info);
			}
		}

		//...........................................................

		protected virtual UIElement ViewerControl_ {
			get { return null; }  // derived must override
		}

		public UIElement ViewerControl {		
			get {
				try {
					Mouse.OverrideCursor = Cursors.Wait;
					return ViewerControl_;
				}
				catch   { return null; }
				finally { Mouse.OverrideCursor = null; }
			}
		}

		//...........................................................

		public Stream Raw {
			get { return m_raw; }
			set { m_raw = value; }
		}

		//...........................................................

		public bool SaveTo ( string PATH )
		{
			if( Info == null || Info.Path.IsNullOrEmpty() || PATH.IsNullOrEmpty() ) return false;

			var stream = System.IO.File.Create(PATH, 0,
				System.IO.FileOptions.SequentialScan |
				System.IO.FileOptions.WriteThrough
			);
			m_raw?.CopyTo(stream);
			stream.Close();

			return System.IO.File.Exists(PATH);
		}

		//...........................................................

		public override string ToString ()
		{
			return Info?.Name;
		}

		int IComparable<Data>.CompareTo ( Data RHS )
		{
			if( Info == RHS?.Info ) return  0;
			if( Info == null )      return -1;
			return Info.CompareTo(RHS?.Info);
		}
	}
}

//=============================================================================
