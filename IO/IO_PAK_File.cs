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
using System.IO;
using zlib = ICSharpCode.SharpZipLib.Zip.Compression;

//=============================================================================

namespace NMS.PAK
{
	/// <summary>
	/// Note: all values are big-endian.
	/// See: 'https://www.psdevwiki.com/ps3/PlayStation_archive_(PSARC)'
	/// </summary>
	public class File : NMS.IO.File
	{
		protected static readonly int s_cmp_type_zlib = 0x7a6c6962;  // ['z', 'l', 'i', 'b'] big-endian
		protected static readonly int s_cmp_type_lzma = 0x6c7a6d61;  // ['l', 'z', 'm', 'a'] big-endian

		// LoadHeader
		protected  int m_cmp_type       = s_cmp_type_zlib;  // "zlib" (NMS) or "lzma"
		protected  int m_toc_length     =     0;  // includes 32 byte header + TOC list + block size list
		protected  int m_toc_entry_size =    30;  // default is 30 bytes (NMS) - 16 bytes MD5 hash, 4 bytes index of 1st block, 5 bytes entry length, 5 bytes offset of of 1st block in .pak
		protected  int m_toc_entries    =     0;  // the manifest is always included as the first entry, it has no id or path
		protected  int m_block_size     = 65536;  // size of uncompressed block, compressed blocks are this size or smaller, default is 65536 bytes (NMS) 
		protected uint m_archive_flags  =     1;  // 0 = relative paths (default), 1 = ignorecase (NMS), 2 = absolute

		// LoadTOC
		// we need the list because there are 2 parts to parse when reading a .pak file
		// the toc header which gives everything except the names of the files,
		// then later get get the names.  we need the names to build the tree.
		protected List<PAK.Entry.Info> m_entries;  // list of EntryInfo meta-data
		protected      PAK.Entry.Node  m_root;     // tree of EntryInfo meta-data

		// LoadBlocks
		protected int     m_block_count = 0;  // m_blocks[m_block_count]
		protected long [] m_blocks;           // block length table, each entry contains compressed size of a block

		//...........................................................

		/// <summary>
		/// Construct a new .pak file wrapper.
		/// </summary>
		/// <param name="PATH">Full path to .pak file on disk.</param>
		public File ( string PATH )
		: base(PATH)
		{
		}

		//...........................................................

		/// <summary>
		/// Get an uncompressed blob from PAK_STREAM using the meta-data in ENTRY.
		/// </summary>
		/// <returns>A MemoryStream if possible, otherwise a FileStream to a auto-delete temp file.</returns>
		protected Stream Decompress ( Stream PAK_STREAM, PAK.Entry.Info ENTRY )
		{
			if( PAK_STREAM == null ) return null;

			Stream entry;
			if( ENTRY.Length < Int32.MaxValue ) {
				entry = new MemoryStream((int)ENTRY.Length);
			}
			else {
				var temp = System.IO.Path.GetTempFileName();
				entry    = new FileStream(temp,
					FileMode.Open,
					FileAccess.ReadWrite,
					FileShare.None,
					m_block_size,
					FileOptions.DeleteOnClose
				);
			}

			// m_block_size is the max decompressed size of a block.
			// each compressed block will be this size or smaller.
			var   compressed = new byte [m_block_size];
			var decompressed = new byte [m_block_size];
			var inflater     = new zlib.Inflater();
			// using System.IO.Compression.DeflateStream.Read fails to parse the data,
			// even though it supposidly uses zlib behind the scenes.
			// todo: assume used it incorrectly, would prefer using built-in API instead of nuget zlib.

			var index  = ENTRY.Index;   // index of first block for ENTRY
			var offset = ENTRY.Offset;  // where the first block ( m_blocks[index]) starts

			// e.g. ENTRY: Index = 7, Offset = 123456, Length = 123456:
			// - m_blocks[7] = 12345, m_blocks[8] = 6789  (example compressed sizes)
			//   these would (likely) decompress to: 65536 and 57920 bytes = 123456 bytes Length.
			// - offset = 123456 is where m_blocks[7] 12345 bytes of compressed data starts,
			//   m_blocks[8]  6789 bytes of compressed data will immediately follow
			//   m_blocks[7] 12345 bytes, and so on.

			for( ; entry.Length < ENTRY.Length; ++index ) {
				PAK_STREAM.Position = offset;

				// current block is uncompressed full block, just copy to output
				if( m_blocks[index] == 0 ) {
					PAK_STREAM.Read(compressed, 0, m_block_size);
					entry.Write(compressed, 0, m_block_size);
					offset += m_block_size;
					continue;
				}

				PAK_STREAM.Read(compressed, 0, (int)m_blocks[index]);  // read compressed data for current block
				offset += m_blocks[index];                             // update offset for next block

				// if it's not an uncompressed full block we MUST assume it's a 'compressed' block (has compression header).
				// we have no way to differentiate a compression header from valid uncompressed data.
				//
				// original psarc.exe code assumes that if the compressed block does not
				// start with 0x78da then it's an uncompressed partial block - ugghhh.
				//
				// https://stackoverflow.com/questions/9050260/what-does-a-zlib-header-look-like
				// Level | ZLIB  | GZIP
				// 1     | 78 01 | 1F 8B - No Compression/low
				// 2     | 78 5E | 1F 8B - Fastest Compression
				// 3     | 78 5E | 1F 8B
				// 4     | 78 5E | 1F 8B
				// 5     | 78 5E | 1F 8B
				// 6     | 78 9C | 1F 8B - Default Compression
				// 7     | 78 DA | 1F 8B - Best Compression (Slowest)
				// 8     | 78 DA | 1F 8B
				// 9     | 78 DA | 1F 8B
				//
				if( BitConverter.IsLittleEndian ) Array.Reverse(compressed, 0x00, 2);
				var is_compressed = BitConverter.ToUInt16(compressed, 0);
				if( BitConverter.IsLittleEndian ) Array.Reverse(compressed, 0x00, 2);

				if( is_compressed != 0x7801 &&
					is_compressed != 0x785e &&
					is_compressed != 0x789c &&
					is_compressed != 0x78da
				) {
					// MessageBox.Show("Surprise, uncompressed partial ... maybe");
					// ... turns out these exist
					entry.Write(compressed, 0, (int)m_blocks[index]);
					continue;
				}

				// ??? wtf ???
				// for blocks that inflate to a full block size (65536) we end up with 4 bytes
				// in RemainingInput after first Inflate call, the second call results in 0 bytes inflated.
				// the resulting data in uncompressed_file seems good though, no missing data.
				// if there was padding added by Deflate i'd expect it to be consumed by first Inflate.
				// maybe it's the 4 byte adler checksum ???
				inflater.SetInput(compressed, 0, (int)m_blocks[index]);

				var length = inflater.Inflate(decompressed);
				entry.Write(decompressed, 0, length);

				inflater.Reset();
			}

			entry.Position = 0;
			return entry;
		}

		//...........................................................

		/// <summary>
		/// Read first 32 bytes of .pak to get meta-data needed to parse rest of file.
		/// </summary>
		protected bool LoadHeader ( Stream PAK_STREAM )
		{
			PAK_STREAM.Position = 0;

			var buffer  = new byte[0x20];
			var length  = PAK_STREAM.Read(buffer, 0, 0x20);
			if( length != 0x20 ) return false;  // read failed

			// all values big-endian
			if( BitConverter.IsLittleEndian ) {
				Array.Reverse(buffer, 0x00, 4);
				Array.Reverse(buffer, 0x04, 2);
				Array.Reverse(buffer, 0x06, 2);
				Array.Reverse(buffer, 0x08, 4);
				Array.Reverse(buffer, 0x0c, 4);
				Array.Reverse(buffer, 0x10, 4);
				Array.Reverse(buffer, 0x14, 4);
				Array.Reverse(buffer, 0x18, 4);
				Array.Reverse(buffer, 0x1c, 4);
			}

			// file identifier
			var magic  = BitConverter.ToUInt32(buffer, 0);
			if( magic != 0x50534152 ) return false;  // PSAR - PlayStation ARchive

			// file version
			var ver_maj = BitConverter.ToUInt16(buffer, 0x04);  // NMS: 1
			var ver_min = BitConverter.ToUInt16(buffer, 0x06);  // NMS: 4
			if( ver_maj != 1 || ver_min != 4 )  return false;  // unexpected version

			// compression method
			var cmp_type  = BitConverter.ToUInt32(buffer, 0x08);
			if( cmp_type != s_cmp_type_zlib &&
				cmp_type != s_cmp_type_lzma
			)	return false;  // unexpected compression type

			// table of contents info
			m_toc_length     = BitConverter. ToInt32(buffer, 0x0c);
			m_toc_entry_size = BitConverter. ToInt32(buffer, 0x10);
			m_toc_entries    = BitConverter. ToInt32(buffer, 0x14);
			m_block_size     = BitConverter. ToInt32(buffer, 0x18);
			m_archive_flags  = BitConverter.ToUInt32(buffer, 0x1c);

			// 30 = md5 hash (16 bytes) + index (4 bytes) + length (5 bytes) + offset (5 bytes)
			if( m_toc_entry_size != 30 ) return false;

			return true;
		}

		//...........................................................

		/// <summary>
		/// Read table of contents block from .pak.
		/// For each entry this contains: starting offset, block index, and uncompressed size, but not path.
		/// </summary>
		protected bool LoadTOC ( Stream PAK_STREAM )
		{
			PAK_STREAM.Position = 0x20;  // skip 32 byte header

			// read entire TOC into buffer
			var buffer  = new byte [(m_toc_entries * m_toc_entry_size) + 8];
			var length  = PAK_STREAM.Read(buffer, 0, m_toc_entries * m_toc_entry_size);
			if( length != m_toc_entries * m_toc_entry_size ) return false;  // read failed

			const ulong mask_40bits = 0x000000ffffffffff;

			m_entries = new List<PAK.Entry.Info>(m_toc_entries);

			for( int entry = 0, offset = 0; entry < m_toc_entries; ++entry, offset += m_toc_entry_size ) {
				var entry_offset = offset + (m_toc_entry_size - (4 + 5 + 5));  // skip (md5) hash

				if( BitConverter.IsLittleEndian ) {
					Array.Reverse(buffer, entry_offset,  4);     // 4 bytes           - index
					Array.Reverse(buffer, entry_offset + 4, 5);  // 5 bytes (40 bits) - length
					Array.Reverse(buffer, entry_offset + 9, 5);  // 5 bytes (40 bits) - offset
				}

				var item_index  = BitConverter.ToUInt32(buffer, entry_offset);
				var item_length = BitConverter.ToUInt64(buffer, entry_offset + 4);
				var item_offset = BitConverter.ToUInt64(buffer, entry_offset + 9);
				item_length &= mask_40bits;  // we read 8 bytes but only want least-sig 5
				item_offset &= mask_40bits;  // we read 8 bytes but only want least-sig 5

				m_entries.Add( new PAK.Entry.Info(this,
					entry, item_index, (long)item_offset, (long)item_length
				));
			}

			return true;
		}

		//...........................................................

		/// <summary>
		/// Read compressed-block-size list from .pak.
		/// </summary>
		protected bool LoadBlocks ( Stream PAK_STREAM )
		{
			PAK_STREAM.Position = 0x20 + (m_toc_entries * m_toc_entry_size);  // skip header and toc

			// calc # of bytes needed to represent an entry block size, will be 2, 3, or 4 bytes.
			// e.g. if m_block_size is <= 65536 then only need 2 bytes to represent entry block size.
			byte block_bytes =   1;
			var  accum       = 256;
			do {
				++block_bytes;
				accum *= 256;
			}	while( accum < m_block_size );

			// m_toc_length includes header, toc and block size list.
			// if we subtract headers and toc lengths then remainder is block size list.
			m_block_count = (m_toc_length - (int)PAK_STREAM.Position) / block_bytes;
			m_blocks      = new long[m_block_count];

			var buffer = new byte [(m_block_count * block_bytes) + 4];
			var length = PAK_STREAM.Read(buffer, 0, m_block_count * block_bytes);
			if( length != m_block_count * block_bytes ) return false;  // read failed

			if( BitConverter.IsLittleEndian ) {
				for( int i = 0, offset = 0; i < m_block_count; i++, offset += block_bytes ) {
					Array.Reverse(buffer, offset, block_bytes);
				}
			}

			switch( block_bytes ) {
				case 2: {
					for( int i = 0, offset = 0; i < m_block_count; i++, offset += block_bytes ) {
						m_blocks[i] = BitConverter.ToUInt16(buffer, offset);
					}
					break;
				}
				case 3: {
					const uint mask_24bits = 0x00ffffff;
					for( int i = 0, offset = 0; i < m_block_count; i++, offset += block_bytes ) {
						var block_size  = BitConverter.ToUInt32(buffer, offset);
						block_size &= mask_24bits;  // we read 4 bytes but only want least-sig 3
						m_blocks[i] = block_size;
					}
					break;
				}
				case 4: {
					for( int i = 0, offset = 0; i < m_block_count; i++, offset += block_bytes ) {
						m_blocks[i] = BitConverter.ToUInt32(buffer, offset);
					}
					break;
				}
			}

			return true;
		}

		//...........................................................

		/// <summary>
		/// The maifest (list of contained file paths) is always the first entry.
		/// It has no Id or path.  It is compressed like all other contained entries.
		/// The uncompressed data is a big array of characters.
		/// Each path, other than the last, is terminated by '\n'.
		/// The last path is terminated by the end of the blob.
		/// A general solution would treat these char as UTF8,
		/// for NMS we assume ASCII to simplfy the code.
		/// </summary>
		protected bool LoadManifest ( Stream PAK_STREAM )
		{
			var manifest  = Decompress(PAK_STREAM, m_entries[0]);
			if( manifest == null ) return false;

			var path = new char [1024];  // no path should exceed this size

			// decompressed_stream is a big char array where the name of each entry
			// is separated by '\n'. starting entry at 1, manifest has no name.
			for( int entry = 1, length = 0; entry < m_entries.Count; ++entry, length = 0 ) {
				var data = manifest.ReadByte();  // data == -1 at end of stream
				while( data >= 0 && data != '\n' ) {
					path[length++] = (char)data;
					data = manifest.ReadByte();
				}
				m_entries[entry].Path = new string(path, 0, length);  // update list entry
			}
			m_entries.Sort();

			return true;
		}

		//...........................................................

		protected bool BuildTree ()
		{
			if( m_root    != null ) return true;
			if( m_entries == null ) return false;

			m_root = new PAK.Entry.Node();

			for( int entry = 1; entry < m_entries.Count; ++entry ) {
				m_root.Insert(m_entries[entry].Path, m_entries[entry]);  // add tree node
			}

			return true;
		}

		//...........................................................

		protected bool Load ()
		{
			if( m_root != null )         return true;
			if( m_path.IsNullOrEmpty() ) return false;

			var pak_stream = System.IO.File.OpenRead(m_path);
			if( pak_stream == null ) return false;

			return
				LoadHeader  (pak_stream) &&
				LoadTOC     (pak_stream) &&  // creates m_entries
				LoadBlocks  (pak_stream) &&
				LoadManifest(pak_stream) &&  // assigns m_entries[i].Path, adds m_entries to m_root
				BuildTree()
			;
		}

		//...........................................................

		/// <summary>
		/// Load-on-demand a list of meta-data for the contained items.
		/// </summary>
		public List<PAK.Entry.Info> EntryList {
			get {
				if( m_entries == null ) Load();
				return m_entries;
			}
		}

		//...........................................................

		/// <summary>
		/// Load-on-demand a tree of meta-data for the contained items.
		/// </summary>
		public PAK.Entry.Node EntryTree {
			get {
				if( m_root == null ) Load();
				return m_root;
			}
		}

		//...........................................................

		/// <summary>
		/// Uncompress the contained Item.
		/// </summary>
		/// <param name="ENTRY">Meta-data describing contained entry to extract.</param>
		/// <returns>Wraper around MemoryStrem if possible, else delete-on-close FileStream.</returns>
		public Stream Extract ( PAK.Entry.Info ENTRY )
		{
			return Path.IsNullOrEmpty() || ENTRY?.File != this ? null :
				Decompress(System.IO.File.OpenRead(Path), ENTRY)
			;
		}
	}
}

//=============================================================================
