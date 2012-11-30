namespace CaveTube.CaveTalk.ViewModel {
	using System;
	using System.Linq;
	using System.Windows.Input;
	using CaveTube.CaveTalk.Model;
	using CaveTube.CaveTalk.Utils;
	using Microsoft.Win32;

	public sealed class GeneralOptionViewModel : OptionBaseViewModel {
		private Config config;

		public SafeObservable<Int32> FontSizeList { get; private set; }

		public Int32 FontSize {
			get { return this.config.FontSize; }
			set {
				this.config.FontSize = value;
				base.OnPropertyChanged("FontSize");
			}
		}

		public Boolean TopMost {
			get { return this.config.TopMost; }
			set {
				this.config.TopMost = value;
				base.OnPropertyChanged("TopMost");
			}
		}

		public Boolean NoticeLiveClose {
			get { return this.config.NoticeLiveClose; }
			set {
				this.config.NoticeLiveClose = value;
				base.OnPropertyChanged("NoticeLiveClose");
			}
		}

		public GeneralOptionViewModel() {
			this.config = Config.GetConfig();

			this.FontSizeList = new SafeObservable<Int32>();
			new[] { 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 30, 36, 42 }.ForEach(size => {
				this.FontSizeList.Add(size);
			});
		}

		internal override void Save() {
			this.config.Save();
		}
	}
}
