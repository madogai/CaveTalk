namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Configuration;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using CaveTube.CaveTubeClient;
	using NLog;

	public sealed class AboutBoxViewModel : ViewModelBase {
		private Logger logger = LogManager.GetCurrentClassLogger();

		public DateTime Version {
			get {
				return DateTime.Parse(ConfigurationManager.AppSettings["version"]);
			}
		}
	}
}
