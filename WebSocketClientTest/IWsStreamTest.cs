using WebSocketSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WebSocketClientTest
{


	[TestClass()]
	public class IWsStreamTest {


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


		internal virtual IWsStream CreateIWsStream() {
			// TODO: 適切な具象クラスをインスタンス化します。
			IWsStream target = null;
			return target;
		}

		[TestMethod()]
		public void CloseTest() {
			IWsStream target = CreateIWsStream(); // TODO: 適切な値に初期化してください
			target.Close();
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void ReadTest() {
			IWsStream target = CreateIWsStream(); // TODO: 適切な値に初期化してください
			byte[] buffer = null; // TODO: 適切な値に初期化してください
			int offset = 0; // TODO: 適切な値に初期化してください
			int size = 0; // TODO: 適切な値に初期化してください
			int expected = 0; // TODO: 適切な値に初期化してください
			int actual;
			actual = target.Read(buffer, offset, size);
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void ReadByteTest() {
			IWsStream target = CreateIWsStream(); // TODO: 適切な値に初期化してください
			int expected = 0; // TODO: 適切な値に初期化してください
			int actual;
			actual = target.ReadByte();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void WriteTest() {
			IWsStream target = CreateIWsStream(); // TODO: 適切な値に初期化してください
			byte[] buffer = null; // TODO: 適切な値に初期化してください
			int offset = 0; // TODO: 適切な値に初期化してください
			int count = 0; // TODO: 適切な値に初期化してください
			target.Write(buffer, offset, count);
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void WriteByteTest() {
			IWsStream target = CreateIWsStream(); // TODO: 適切な値に初期化してください
			byte value = 0; // TODO: 適切な値に初期化してください
			target.WriteByte(value);
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}
	}
}
