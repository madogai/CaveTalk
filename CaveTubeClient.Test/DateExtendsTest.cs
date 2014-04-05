namespace CaveTube.CaveTalk.Test.Utils {
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using CaveTube.CaveTubeClient;

	[TestClass]
	public class DateExtendsTest {

		[TestMethod]
		public void ToDateTime_正常() {
			// arrange
			var jsTime = 946652400000;

			// act
			var act = DateExtends.ToDateTime(jsTime);

			// assert
			Assert.AreEqual(new DateTime(2000, 1, 1), act);
		}

		[TestMethod]
		public void ToUnixEpoch_正常() {
			// arrange
			var date = new DateTime(2000, 1, 1);

			// act
			var act = date.ToUnixEpoch();

			// assert
			Assert.AreEqual(946652400000, act);
		}

		[TestMethod]
		public void ToUnixEpoch_小数点以下切り捨て() {
			// arrange
			var date = new DateTime(2000, 1, 1, 0, 0, 0, 999);

			// act
			var act = date.ToUnixEpoch();

			// assert
			Assert.AreEqual(946652400999, act);
		}
	}
}
