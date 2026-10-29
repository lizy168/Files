// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.App.Dialogs;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Edits a layout template, opened from <see cref="LayoutPage"/>.
	/// </summary>
	public sealed partial class LayoutTemplatePage : Page
	{
		public LayoutTemplateEditorViewModel ViewModel { get; private set; } = null!;

		public LayoutTemplatePage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.Parameter is LayoutTemplateEditorViewModel viewModel)
			{
				ViewModel = viewModel;
				Bindings.Update();
			}
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			ViewModel?.Save();
		}

		private void LayoutBreadcrumb_Click(Hyperlink sender, HyperlinkClickEventArgs args)
		{
			GoBack();
		}

		private void GoBack()
		{
			if (Frame.CanGoBack)
				Frame.GoBack(new SuppressNavigationTransitionInfo());
		}

		private async void Rename_Click(object sender, RoutedEventArgs e)
		{
			if (await LayoutTemplateDialogs.AskNameAsync(Strings.RenameTemplate.GetLocalizedResource(), Strings.Rename.GetLocalizedResource(), ViewModel.DisplayName) is { } name)
				ViewModel.Name = name;
		}

		private void UseForFileTypes_Toggled(object sender, RoutedEventArgs e)
		{
			if (sender is ToggleSwitch toggleSwitch)
				ViewModel.SetUsedForFileTypes(toggleSwitch.IsOn);
		}

		private async void DeleteCard_Click(object sender, RoutedEventArgs e)
		{
			if (!await LayoutTemplateDialogs.ConfirmDeleteAsync(ViewModel.DisplayName))
				return;

			ViewModel.Delete();
			GoBack();
		}

		private void PropertyPickerFlyout_Opening(object sender, object e)
		{
			if (sender is Flyout { Content: FrameworkElement { Tag: LayoutPropertyPickerViewModel picker } content })
			{
				picker.Filter(null);
				if (content is Grid grid && grid.Children.OfType<TextBox>().FirstOrDefault() is { } searchBox)
					searchBox.Text = string.Empty;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void PropertyPickerSearch_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is TextBox { Tag: LayoutPropertyPickerViewModel picker } searchBox)
				picker.Filter(searchBox.Text);
		}

		private void PropertyPicker_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (sender is ListView { Tag: LayoutPropertyPickerViewModel picker } && e.ClickedItem is LayoutPropertyChoice choice)
				picker.Choose(choice);

			SortPickerFlyout.Hide();
			GroupPickerFlyout.Hide();
		}

		private void AddColumnFlyout_Opening(object sender, object e)
		{
			if (ViewModel.Columns is { } columns)
				columns.SearchQuery = string.Empty;
		}

		private void AvailableColumns_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is ColumnTreeItem { IsGroup: false } column)
				ViewModel.Columns?.AddColumns([column.Id]);
		}

		private void ColumnsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			if (e.Items.OfType<ColumnListItem>().Any(column => column.IsPinned))
				e.Cancel = true;
		}

		private void ColumnsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			ViewModel.Columns?.KeepPinnedColumnFirst();
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void MoveColumnUp_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is ColumnListItem column)
				ViewModel.Columns?.MoveColumn(column, -1);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void MoveColumnDown_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is ColumnListItem column)
				ViewModel.Columns?.MoveColumn(column, 1);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void RemoveColumn_Click(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is ColumnListItem column)
				ViewModel.Columns?.RemoveColumn(column);
		}
	}
}
