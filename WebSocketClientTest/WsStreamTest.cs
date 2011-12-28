using WebSocketSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace WebSocketClientTest {
	[TestClass()]
	public class WsStreamTest {


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


		public void WsStreamConstructorTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream);
			Assert.Inconclusive("TODO: ターゲットを確認するためのコードを実装してください");
		}

		[TestMethod()]
		public void WsStreamConstructorTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで WsStreamConstructorTestHelper<T>() を呼び出" +
					"してください。");
		}

		public void CloseTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
			target.Close();
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void CloseTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで CloseTestHelper<T>() を呼び出してください。");
		}

		public void DisposeTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
			target.Dispose();
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void DisposeTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで DisposeTestHelper<T>() を呼び出してください。");
		}

		public void ReadTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
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
		public void ReadTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで ReadTestHelper<T>() を呼び出してください。");
		}

		public void ReadByteTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
			int expected = 0; // TODO: 適切な値に初期化してください
			int actual;
			actual = target.ReadByte();
			Assert.AreEqual(expected, actual);
			Assert.Inconclusive("このテストメソッドの正確性を確認します。");
		}

		[TestMethod()]
		public void ReadByteTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで ReadByteTestHelper<T>() を呼び出してください。");
		}

		public void WriteTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
			byte[] buffer = null; // TODO: 適切な値に初期化してください
			int offset = 0; // TODO: 適切な値に初期化してください
			int count = 0; // TODO: 適切な値に初期化してください
			target.Write(buffer, offset, count);
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void WriteTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで WriteTestHelper<T>() を呼び出してください。");
		}

		public void WriteByteTestHelper<T>()
			where T : Stream {
			T innerStream = default(T); // TODO: 適切な値に初期化してください
			WsStream<T> target = new WsStream<T>(innerStream); // TODO: 適切な値に初期化してください
			byte value = 0; // TODO: 適切な値に初期化してください
			target.WriteByte(value);
			Assert.Inconclusive("値を返さないメソッドは確認できません。");
		}

		[TestMethod()]
		public void WriteByteTest() {
			Assert.Inconclusive("T の型の制約を満たす適切な型パラメーターが見つかりません。適切な型パラメーターで WriteByteTestHelper<T>() を呼び出してください。");
		}
	}
}
