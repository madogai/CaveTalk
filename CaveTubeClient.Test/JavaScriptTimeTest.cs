namespace CaveTube.CaveTalk.Test.Utils {
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using CaveTube.CaveTubeClient;

	[TestClass]
	public class JavaScriptTimeTest {

		[TestMethod]
		public void ToDateTime_正常() {
			// arrange
			var jsTime = 946652400000d;

			// act
			var act = JavaScriptTime_Accessor.ToDateTime(jsTime, TimeZoneKind.Japan);

			// assert
			Assert.AreEqual(new DateTime(2000, 1, 1), act);
		}

		[TestMethod]
		public void ToDouble_正常() {
			// arrange
			var date = new DateTime(2000, 1, 1);

			// act
			var act = JavaScriptTime_Accessor.ToDouble(date, TimeZoneKind.Japan);

			// assert
			Assert.AreEqual(946652400000, act);
		}

		[TestMethod]
		public void ToDouble_小数点以下切り捨て() {
			// arrange
			var date = new DateTime(2000, 1, 1, 0, 0, 0, 999);

			// act
			var act = JavaScriptTime_Accessor.ToDouble(date, TimeZoneKind.Japan);

			// assert
			Assert.AreEqual(946652400000, act);
		}
	}
}
