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

//=============================================================================

namespace NMS.PAK.Entry
{
	/// <summary>
	/// Meta-data for a (compressed) entry (file) in a NMS .pak file.
	/// </summary>
	public class Info : IComparable<Info>
	{
		protected string m_path      = "";  // full dir/.../name.ext
		protected string m_name      = "";  // no dir, extension
		protected string m_extension = "";  // if !Extension then assume is directory 

		public readonly PAK.File File;

		public readonly  int Id;      // ordinal of EntryInfo in IO.PAK.m_entries
		public readonly uint Index;   // index of first block for Item in PAK.m_blocks
		public readonly long Offset;  // offset in PAK file where Item block data starts
		public readonly long Length;  // length of uncompressed Item

		//...........................................................

		public Info ( PAK.File FILE, int ID = 0, uint INDEX = 0, long OFFSET = 0, long LENGTH = 0 )
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
}

//=============================================================================
