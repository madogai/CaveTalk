using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebSocketSharp;

namespace WebSocketClientTest {
	[TestClass()]
	public class WebSocketTest {
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

		[TestMethod]
		public void HandShakeTest() {
			var nameSpace = "socket.io";
			var protocolVersion = "1";

			var uri = new Uri("http://ws.vmhost:3000/socket.io/1/");
			var wc = new WebClient();
			var handshakeUrl = String.Format("http://{0}:{1}/{2}/{3}", uri.Host, uri.Port, nameSpace, protocolVersion);
			var response = wc.DownloadString(handshakeUrl);
			var infos = Regex.Split(response, ":");
			var sessionId = infos.ElementAtOrDefault(0);
			var heartbeatText = infos.ElementAtOrDefault(1);
			var heartbeat = String.IsNullOrEmpty(heartbeatText) == false ? (Int32.Parse(heartbeatText) * 1000) : 0;
			var timoutText = infos.ElementAtOrDefault(2);
			var timeout = String.IsNullOrEmpty(timoutText) == false ? (Int32.Parse(timoutText) * 1000) : (25 * 1000);

			wc.Dispose();
			var webSocketUrl = String.Format("ws://{0}:{1}/{2}/{3}/websocket/{4}", uri.Host, uri.Port, nameSpace, protocolVersion, sessionId);
			var target = new WebSocket(webSocketUrl);
			target.Connect();


		}
	}
}
