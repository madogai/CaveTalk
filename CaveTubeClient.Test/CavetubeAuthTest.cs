using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CaveTube.CaveTubeClient;

namespace CaveTubeClient.Test {
	[TestClass]
	public class CavetubeAuthTest {
		[TestMethod]
		public void Login_成功() {
			// arrange

			// act
			var actual = CavetubeAuth.Login("CaveTalk", "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.IsFalse(String.IsNullOrEmpty(actual));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_ユーザー名なし() {
			// arrange

			// act
			CavetubeAuth.Login(String.Empty, "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_パスワードなし() {
			// arrange

			// act
			var actual = CavetubeAuth.Login("CaveTalk", String.Empty, "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Login_開発者キーなし() {
			// arrange

			// act
			var actual = CavetubeAuth.Login("CaveTalk", "cavetalk", String.Empty);

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		public void Logout_成功() {
			// arrange

			// act
			var actual = CavetubeAuth.Logout("CaveTalk", "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.AreEqual(true, actual);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_ユーザー名なし() {
			// arrange

			// act
			var actual = CavetubeAuth.Logout(String.Empty, "cavetalk", "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_パスワードなし() {
			// arrange

			// act
			var actual = CavetubeAuth.Logout("CaveTalk", String.Empty, "C9A8B58F00CB4E88A8C884CD9C19B868");

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Logout_開発者キーなし() {
			// arrange

			// act
			var actual = CavetubeAuth.Logout("CaveTalk", "cavetalk", String.Empty);

			// assert
			Assert.Fail("例外が発生しませんでした。");
		}
	}
}
