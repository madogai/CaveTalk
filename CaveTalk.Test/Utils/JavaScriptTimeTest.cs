﻿namespace CaveTalk.Test.Utils {
	using System;
	using System.Text;
	using System.Collections.Generic;
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using CaveTalk.Utils;

	[TestClass]
	public class JavaScriptTimeTest {

		[TestMethod]
		public void ToDateTime_正常() {
			// arrange
			var jsTime = 946652400000d;

			// act
			var act = JavaScriptTime.ToDateTime(jsTime, TimeZoneKind.Japan);

			// assert
			Assert.AreEqual(new DateTime(2000, 1, 1), act);
		}

		[TestMethod]
		public void ToDouble_正常() {
			// arrange
			var date = new DateTime(2000, 1, 1);

			// act
			var act = JavaScriptTime.ToDouble(date, TimeZoneKind.Japan);

			// assert
			Assert.AreEqual(946652400000, act);
		}
	}
}
