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

namespace NMS
{
	public static class IList_x
	{
		public static bool IsNullOrEmpty (
			this string STRING
		)
		{
			return string.IsNullOrEmpty(STRING);
		}

		public static bool IsNullOrEmpty<OBJECT_T> (
			this IList<OBJECT_T> LIST
		)
		{
			return LIST == null || LIST.Count < 1;
		}

		public static int BinarySearchInsertIndex<OBJECT_T, KEY_T> (
			this IList<OBJECT_T>       LIST,
			KEY_T                      KEY,
			Func<OBJECT_T, KEY_T, int> COMPARE
		)
		{
			if( LIST == null || COMPARE == null ) return -1;

			int min = 0;
			int max = LIST.Count - 1;

			while( min <= max ) {
				int mid = (min + max) / 2;
				int c = COMPARE(LIST[mid], KEY);
				if( c == 0 ) return mid;
				if( c <  0 ) min = mid + 1;
				else         max = mid - 1;
			}

			return min >= 0 ? min : max;
		}

		public static int BinarySearchIndex<OBJECT_T, KEY_T> (
			this IList<OBJECT_T>       LIST,
			KEY_T                      KEY,
			Func<OBJECT_T, KEY_T, int> COMPARE
		)
		{
			if( LIST == null || COMPARE == null ) return -1;

			int min = 0;
			int max = LIST.Count - 1;

			while( min <= max ) {
				int mid = (min + max) / 2;
				int c = COMPARE(LIST[mid], KEY);
				if( c == 0 ) return mid;
				if( c <  0 ) min = mid + 1;
				else max = mid - 1;
			}

			return -1;
		}

		public static OBJECT_T BinarySearchObject<OBJECT_T, KEY_T> (
			this IList<OBJECT_T>       LIST,
			KEY_T                      KEY,
			Func<OBJECT_T, KEY_T, int> COMPARE,
			OBJECT_T                   DEFAULT = default
		)
		{
			var    index = BinarySearchIndex(LIST, KEY, COMPARE);
			return index < 0 ? DEFAULT : LIST[index];
		}
	}
}

//=============================================================================
