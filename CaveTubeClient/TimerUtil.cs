namespace CaveTube.CaveTubeClient {
	using System;
	using System.Threading;

	internal static class TimerUtil {
		public static void SetTimeout(Int32 timeout, Action act) {
			Timer timer = null;
			timer = new Timer(_ => {
				timer.Dispose();
				act();
			}, null, timeout, Timeout.Infinite);
		}
	}
}