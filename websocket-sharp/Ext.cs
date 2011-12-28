#region MIT License
/**
 * Ext.cs
 *
 * The MIT License
 *
 * Copyright (c) 2010 sta.blockhead
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

namespace WebSocketSharp {
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public static class Ext {
		public static Boolean AreNotEqualDo(this String expected, String actual, Func<String, String, String> func, out String ret) {
			if (expected != actual) {
				ret = func(expected, actual);
				return true;
			}

			ret = String.Empty;
			return false;
		}

		public static Boolean EqualsWithSaveTo(this Int32 asByte, Char c, IList<Byte> dist) {
			Byte b = (Byte)asByte;
			dist.Add(b);
			return b == Convert.ToByte(c);
		}

		public static UInt32 GenerateKey(this Random rand, Int32 space) {
			UInt32 max = (UInt32)(0xffffffff / space);

			Int32 upper16 = (Int32)((max & 0xffff0000) >> 16);
			Int32 lower16 = (Int32)(max & 0x0000ffff);

			return ((UInt32)rand.Next(upper16 + 1) << 16) + (UInt32)rand.Next(lower16 + 1);
		}

		public static Char GeneratePrintableASCIIwithoutSPandNum(this Random rand) {
			Int32 ascii = rand.Next(2) == 0 ? rand.Next(33, 48) : rand.Next(58, 127);
			return Convert.ToChar(ascii);
		}

		public static String GenerateSecKey(this Random rand, out UInt32 key) {
			Int32 space = rand.Next(1, 13);
			Int32 ascii = rand.Next(1, 13);

			key = rand.GenerateKey(space);

			Int64 mKey = key * space;
			List<Char> secKey = new List<Char>(mKey.ToString().ToCharArray());

			Int32 buf = 0;
			for (var i = 0; i < ascii; i++) {
				buf = rand.Next(secKey.Count + 1);
				secKey.Insert(i, rand.GeneratePrintableASCIIwithoutSPandNum());
			}

			for (var i = 0; i < space; i++) {
				buf = rand.Next(1, secKey.Count);
				secKey.Insert(buf, ' ');
			}

			return new String(secKey.ToArray());
		}

		public static Byte[] InitializeWithPrintableASCII(this Byte[] bytes, Random rand) {
			for (var i = 0; i < bytes.Length; i++) {
				bytes[i] = (Byte)rand.Next(32, 127);
			}

			return bytes;
		}

		public static Boolean IsValid(this String[] response, Byte[] expectedCR, Byte[] actualCR, out String message) {
			String expectedCRtoHexStr = BitConverter.ToString(expectedCR);
			String actualCRtoHexStr = BitConverter.ToString(actualCR);

			Func<String, Func<String, String, String>> func = s => (e, a) => String.Format("Invalid {0} response: {1}", s, a);

			Func<String, String, String> func1 = func("handshake");
			Func<String, String, String> func2 = func("challenge");

			String msg;
			if ("HTTP/1.1 101 WebSocket Protocol Handshake".AreNotEqualDo(response[0], func1, out msg) ||
				"Upgrade: WebSocket".AreNotEqualDo(response[1], func1, out msg) ||
				"Connection: Upgrade".AreNotEqualDo(response[2], func1, out msg) ||
				expectedCRtoHexStr.AreNotEqualDo(actualCRtoHexStr, func2, out msg)) {
				message = msg;
				return false;
			}

			message = String.Empty;
			return true;
		}

		public static void Times(this Int32 n, Action act) {
			for (var i = 0; i < n; i++) {
				act();
			}
		}
	}
}
