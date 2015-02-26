namespace CaveTube.CaveTalk.Test.Utils {
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using CaveTube.CaveTubeClient;
	using Newtonsoft.Json.Linq;

	[TestClass]
	public class JsonNetTest {

		[TestMethod]
		public void Dynamicで存在しないキーを検証() {
			// arrange
			dynamic json = JObject.Parse(@"{}");

			// act

			// assert
			Assert.IsTrue(json.hoge == null);
		}

		[TestMethod]
		public void キャストの検証() {
			// arrange
			dynamic json = JObject.Parse(@"{""hoge"": 1, ""fuga"": 1.3}");

			// act
			Int32 hoge = json.hoge ?? 0;
			Int32 fuga = json.fuga ?? 0;
			Int32 piyo = json.piyo ?? 0;

			// assert
			Assert.AreEqual(hoge, 1);
			Assert.AreEqual(fuga, 1);
			Assert.AreEqual(piyo, 0);
		}
	}
}
