using WebSocketSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace WebSocketClientTest
{


	[TestClass()]
	public class ExtTest {


		private TestContext testContextInstance;

		public TestContext TestContext {
			get {
				return testContextInstance;
			}
			set {
				testContextInstance = value;
			}
		}

		#region 追加のテスト属性
		// 
		//テストを作成するときに、次の追加属性を使用することができます:
		//
		//クラスの最初のテストを実行する前にコードを実行するには、ClassInitialize を使用
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//クラスのすべてのテストを実行した後にコードを実行するには、ClassCleanup を使用
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//各テストを実行する前にコードを実行するには、TestInitialize を使用
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//各テストを実行した後にコードを実行するには、TestCleanup を使用
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		[TestMethod()]
		public void AreNotEqualDoTest() {
			string expected = string.Empty; // TODO: 適切な値に初期化してください
			string actual = string.Empty; // TODO: 適切な値に初期化してください
			Func<string, string, string> func = null; // TODO: 適切な値に初期化してください
			string ret = string.Empty; // TODO: 適切な値に初期化してください
			string retExpected = string.Empty; // TODO: 適切な値に初期化してください
			bool expected1 = false; // TODO: 適切な値に初期化してください
			bool actual1;
			actual1 = Ext.AreNotEqualDo(expected, actual, func, out ret);
			Assert.AreEqual(retExpected, ret);
			Assert.AreEqual(expected1, actual1);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void EqualsWithSaveToTest() {
			int asByte = 0; // TODO: 適切な値に初期化してください
			char c = '\0'; // TODO: 適切な値に初期化してください
			IList<byte> dist = null; // TODO: 適切な値に初期化してください
			bool expected = false; // TODO: 適切な値に初期化してください
			bool actual;
			actual = Ext.EqualsWithSaveTo(asByte, c, dist);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void GenerateKeyTest() {
			Random rand = null; // TODO: 適切な値に初期化してください
			int space = 0; // TODO: 適切な値に初期化してください
			uint expected = 0; // TODO: 適切な値に初期化してください
			uint actual;
			actual = Ext.GenerateKey(rand, space);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void GeneratePrintableASCIIwithoutSPandNumTest() {
			Random rand = null; // TODO: 適切な値に初期化してください
			char expected = '\0'; // TODO: 適切な値に初期化してください
			char actual;
			actual = Ext.GeneratePrintableASCIIwithoutSPandNum(rand);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void GenerateSecKeyTest() {
			Random rand = null; // TODO: 適切な値に初期化してください
			uint key = 0; // TODO: 適切な値に初期化してください
			uint keyExpected = 0; // TODO: 適切な値に初期化してください
			string expected = string.Empty; // TODO: 適切な値に初期化してください
			string actual;
			actual = Ext.GenerateSecKey(rand, out key);
			Assert.AreEqual(keyExpected, key);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void InitializeWithPrintableASCIITest() {
			byte[] bytes = null; // TODO: 適切な値に初期化してください
			Random rand = null; // TODO: 適切な値に初期化してください
			byte[] expected = null; // TODO: 適切な値に初期化してください
			byte[] actual;
			actual = Ext.InitializeWithPrintableASCII(bytes, rand);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void IsValidTest() {
			string[] response = null; // TODO: 適切な値に初期化してください
			byte[] expectedCR = null; // TODO: 適切な値に初期化してください
			byte[] actualCR = null; // TODO: 適切な値に初期化してください
			string message = string.Empty; // TODO: 適切な値に初期化してください
			string messageExpected = string.Empty; // TODO: 適切な値に初期化してください
			bool expected = false; // TODO: 適切な値に初期化してください
			bool actual;
			actual = Ext.IsValid(response, expectedCR, actualCR, out message);
			Assert.AreEqual(messageExpected, message);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void TimesTest() {
			int n = 0; // TODO: 適切な値に初期化してください
			Action act = null; // TODO: 適切な値に初期化してください
			Ext.Times(n, act);
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}
	}
}
