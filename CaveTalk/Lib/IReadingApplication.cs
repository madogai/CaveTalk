namespace CaveTube.CaveTalk.Lib {

	using System;

	public interface IReadingApplicationClient : IDisposable {

		String ApplicationName { get; }

		Boolean IsConnect { get; }

		Boolean Add(String text);
	}
}