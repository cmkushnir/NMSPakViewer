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

//=============================================================================

namespace NMS.IO
{
	public class Item : IComparable<Item>
	{
		protected string m_path = "";  // full dir/.../name.ext
		protected string m_name = "";  // no dir, extension

		//...........................................................

		public Item ( string PATH = null )
		{
			Path = PATH;
		}

		//...........................................................

		public virtual string Path {
			get { return m_path; }
			set {
				m_name = "";
				m_path = value?.Replace("/", "\\")?.Trim();  // keep case
				if( m_path == null ) m_path = "";
				if( m_path.Length < 1 ) return;

				// assume it's a directory if no extension.
				// ... not valid in a general solution but ok for NMS.
				if( System.IO.Path.GetExtension(m_path).Length < 1 &&
					!m_path.EndsWith("\\")
				)	 m_path += '\\';
			}
		}

		//...........................................................

		public string Name {
			get { return m_name; }
		}

		//...........................................................

		public virtual bool Exists ()
		{
			return false;
		}

		public virtual bool EnsureExists ()
		{
			return false;
		}

		//...........................................................

		public override string ToString ()
		{
			return Name;
		}

		int IComparable<Item>.CompareTo ( Item RHS )
		{
			return string.Compare(m_path, RHS?.m_path);
		}
	}

	//=========================================================================

	public class File : Item
	{
		protected string m_extension = "";  // if !Extension then assume is directory 

		//...........................................................

		public File ( string PATH )
		: base(PATH)
		{
		}

		//...........................................................

		public override string Path {
			set {
				m_extension = "";
				base.Path   = value;

				// some NMS files have multiple '.' in name e.g. 'GCDEBUGOPTIONS.GLOBAL.MBIN'
				// parse as: m_name = 'GCDEBUGOPTIONS.GLOBAL', m_extension = '.MBIN'
				m_name      = System.IO.Path.GetFileNameWithoutExtension(m_path);
				m_extension = System.IO.Path.GetExtension(m_path);
			}
		}

		public string Extension {
			get { return m_extension; }
		}

		//...........................................................

		public override bool Exists ()
		{
			return System.IO.File.Exists(m_path);
		}

		public override bool EnsureExists ()
		{
			if( Exists()  )              return true;
			if( m_path.IsNullOrEmpty() ) return false;

			System.IO.File.Create(m_path, 0,
				System.IO.FileOptions.SequentialScan |
				System.IO.FileOptions.WriteThrough
			);

			return Exists();
		}
	}

	//=========================================================================

	public class Directory : Item
	{
		public Directory ( string PATH )
		: base(PATH)
		{
		}

		//...........................................................

		public override string Path {
			set {
				base.Path = value;
				m_name    = System.IO.Path.GetDirectoryName(m_path);
			}
		}

		//...........................................................

		public override bool Exists ()
		{
			return System.IO.Directory.Exists(m_path);
		}

		public override bool EnsureExists ()
		{
			if( Exists() )               return true;
			if( m_path.IsNullOrEmpty() ) return false;
				
			System.IO.Directory.CreateDirectory(m_path);

			return Exists();
		}

		//...........................................................

		public IEnumerable<string> Files ( string PATTERN = null )
		{
			return System.IO.Directory.EnumerateFiles(m_path, PATTERN);
		}

		public IEnumerable<string> Directories ( string PATTERN = null )
		{
			return System.IO.Directory.EnumerateDirectories(m_path, PATTERN);
		}
	}
}

//=============================================================================
