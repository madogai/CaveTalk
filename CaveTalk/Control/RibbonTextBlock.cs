using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Windows.Controls.Ribbon;

namespace CaveTube.CaveTalk.Control {
	public class RibbonTextBlock : System.Windows.Controls.Control {

		public String Label {
			get { return (String)GetValue(LabelProperty); }
			set { SetValue(LabelProperty, value); }
		}

		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register("Label", typeof(String), typeof(RibbonTextBlock), new UIPropertyMetadata(String.Empty));

		public String Text {
			get { return (String)GetValue(LabelProperty); }
			set { SetValue(LabelProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(String), typeof(RibbonTextBlock), new UIPropertyMetadata(String.Empty));

		/// <summary>
		/// CornerRadius of the RibbonTextBox
		/// </summary>
		public CornerRadius CornerRadius {
			get { return RibbonControlService.GetCornerRadius(this); }
			set { RibbonControlService.SetCornerRadius(this, value); }
		}

		/// <summary>
		/// DependencyProperty for CornerRadius
		/// </summary>
		public static readonly DependencyProperty CornerRadiusProperty =
			RibbonControlService.CornerRadiusProperty.AddOwner(typeof(RibbonTextBlock));

		/// <summary>
		///     Size definition to apply to this control when it's placed in a QuickAccessToolBar.
		/// </summary>
		public RibbonControlSizeDefinition QuickAccessToolBarControlSizeDefinition {
			get { return RibbonControlService.GetQuickAccessToolBarControlSizeDefinition(this); }
			set { RibbonControlService.SetQuickAccessToolBarControlSizeDefinition(this, value); }
		}

		/// <summary>
		///     DependencyProperty for QuickAccessToolBarControlSizeDefinition property.
		/// </summary>
		public static readonly DependencyProperty QuickAccessToolBarControlSizeDefinitionProperty =
			RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner(typeof(RibbonTextBlock));

		static RibbonTextBlock() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTextBlock), new FrameworkPropertyMetadata(typeof(RibbonTextBlock)));
		}


	}
}
