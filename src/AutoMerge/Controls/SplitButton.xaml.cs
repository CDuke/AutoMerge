using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

namespace AutoMerge.Controls
{
	/// <summary>
	/// Interaction logic for SplitButton.xaml
	/// </summary>
	public partial class SplitButton : Button
	{
		public static readonly DependencyProperty DropDownMenuCommandProperty;
		public static readonly DependencyProperty DropDownMenuProperty;
		public static readonly DependencyProperty DropDownMenuCommandParameterProperty;
		public static readonly DependencyProperty ShowArrowProperty;
		private bool _dropDownOpen;

		public bool ShowArrow
		{
			get
			{
				return (bool)GetValue(ShowArrowProperty);
			}
			set
			{
				SetValue(ShowArrowProperty, value);
			}
		}

		public ContextMenu DropDownMenu
		{
			get
			{
				return (ContextMenu)GetValue(DropDownMenuProperty);
			}
			set
			{
				SetValue(DropDownMenuProperty, value);
			}
		}
		
		public ICommand DropDownMenuCommand
		{
			get
			{
				return (ICommand)GetValue(DropDownMenuCommandProperty);
			}
			set
			{
				SetValue(DropDownMenuCommandProperty, value);
			}
		}

		public object DropDownMenuCommandParameter
		{
			get
			{
				return GetValue(DropDownMenuCommandParameterProperty);
			}
			set
			{
				SetValue(DropDownMenuCommandParameterProperty, value);
			}
		}

		static SplitButton()
		{
			DropDownMenuCommandProperty
				= DependencyProperty.Register("DropDownMenuCommand", typeof(ICommand), typeof(SplitButton), new FrameworkPropertyMetadata(null, OnDropDownMenuCommandChanged));

			DropDownMenuProperty
				= DependencyProperty.Register("DropDownMenu", typeof(ContextMenu), typeof(SplitButton), new PropertyMetadata(OnDropDownMenuChanged));

			DropDownMenuCommandParameterProperty
				= DependencyProperty.Register("DropDownMenuCommandParameter", typeof(object), typeof(SplitButton));

			ShowArrowProperty
				= DependencyProperty.Register("ShowArrow", typeof(bool), typeof(SplitButton), new PropertyMetadata(true));
		}

		public SplitButton()
		{
			InitializeComponent();
			KeyDown += SplitButton_KeyDown;
		}

		private static void OnDropDownMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var splitButton = d as SplitButton;
			if (splitButton == null)
				return;

			var contextMenu1 = e.OldValue as ContextMenu;
			if (contextMenu1 != null)
			{
				contextMenu1.Opened -= splitButton.OnContextMenuOpened;
				contextMenu1.Closed -= splitButton.OnContextMenuClosed;
			}

			var contextMenu2 = e.NewValue as ContextMenu;
			if (contextMenu2 == null)
				return;
			contextMenu2.Opened += splitButton.OnContextMenuOpened;
			contextMenu2.Closed += splitButton.OnContextMenuClosed;
		}

		private static void OnDropDownMenuCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var splitButton = d as SplitButton;
			if (splitButton == null)
				return;
			splitButton.OnDropDownMenuCommandChanged((ICommand)e.OldValue, (ICommand)e.NewValue);
		}

		private void OnDropDownMenuCommandChanged(ICommand oldCommand, ICommand newCommand)
		{
			if (oldCommand != null)
				oldCommand.CanExecuteChanged -= OnCanExecuteChanged;
			if (newCommand != null)
				newCommand.CanExecuteChanged += OnCanExecuteChanged;
			UpdateCanExecute();
		}

		private void OnCanExecuteChanged(object sender, EventArgs e)
		{
			UpdateCanExecute();
		}

		private void UpdateCanExecute()
		{
			IsEnabled = DropDownMenuCommand != null && DropDownMenuCommand.CanExecute(DropDownMenuCommandParameter);
		}

		private void SplitButton_KeyDown(object sender, KeyEventArgs e)
		{
			if (!e.Handled && e.Key == Key.Down)
			{
				e.Handled = true;
				ToggleDropDown();
			}
			if ((e.Handled || e.Key != Key.Right) && e.Key != Key.Left)
				return;
			e.Handled = true;
			if (!IsFocused || e.Key != Key.Right)
				return;
			var uiElement = Template.FindName("SplitButtonDropDownButton", this) as UIElement;
			if (uiElement == null)
				return;
			uiElement.Focus();
		}

		private void SplitButtonDropDownButton_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Handled)
				return;
			if (e.Key == Key.Return || e.Key == Key.Space || e.Key == Key.Down)
			{
				e.Handled = true;
				ToggleDropDown();
			}
			else if (e.Key == Key.Left)
			{
				e.Handled = true;
				Focus();
			}
			else
			{
				if (e.Key != Key.Right)
					return;
				e.Handled = true;
			}
		}

		private void SplitButtonDropDownButton_Click(object sender, RoutedEventArgs e)
		{
			if (e.Handled)
				return;
			e.Handled = true;
			ToggleDropDown();
		}

		private void ToggleDropDown()
		{
			var dropDownMenuCommand = DropDownMenuCommand;
			if (dropDownMenuCommand != null)
			{
				if (!dropDownMenuCommand.CanExecute(null))
					return;
				var point = PointToScreen(new Point(0.0, ActualHeight));
				if (DropDownMenuCommandParameter != null)
					dropDownMenuCommand.Execute(new DropDownLink.DropDownMenuCommandParameters
					{
						Point = point,
						Parameter = DropDownMenuCommandParameter
					});
				else
					dropDownMenuCommand.Execute(point);
			}
			else
			{
				if (DropDownMenu == null)
					return;
				if (_dropDownOpen)
				{
					DropDownMenu.IsOpen = false;
				}
				else
				{
					DropDownMenu.FontFamily = FontFamily;
					DropDownMenu.FontSize = FontSize;
					DropDownMenu.PlacementTarget = this;
					DropDownMenu.Placement = PlacementMode.Bottom;
					DropDownMenu.IsOpen = true;
				}
			}
		}

		private void OnContextMenuOpened(object sender, RoutedEventArgs e)
		{
			_dropDownOpen = true;
		}

		private void OnContextMenuClosed(object sender, RoutedEventArgs e)
		{
			_dropDownOpen = false;
		}
	}
}
