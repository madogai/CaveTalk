namespace CaveTube.CaveTubeClient {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;
	using System.Threading.Tasks;

	internal static class WebClientUtil {
		public static WebClient CreateInstance() {
			var client = new WebClient();
			client.Encoding = Encoding.UTF8;
			client.Headers.Add(HttpRequestHeader.UserAgent, "CaveChatClient");
			return client;
		}
	}
}
