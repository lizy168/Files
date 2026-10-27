// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Lays out the cells of a <see cref="DetailsLayoutPage"/> row horizontally in column order.
	/// </summary>
	public sealed partial class DetailsLayoutRowPanel : Panel
	{
		private readonly Dictionary<string, TextBlock> _propertyCells = [];
		private double[] _childWidths = [];
		private int[] _orderedIndices = [];
		private int[] _orderKeys = [];

		public static readonly DependencyProperty ColumnOrderProperty =
			DependencyProperty.Register(
				nameof(ColumnOrder),
				typeof(object),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null, (d, _) => ((DetailsLayoutRowPanel)d).OnColumnOrderChanged()));

		public object? ColumnOrder
		{
			get => GetValue(ColumnOrderProperty);
			set => SetValue(ColumnOrderProperty, value);
		}

		public static readonly DependencyProperty PropertyCellStyleProperty =
			DependencyProperty.Register(
				nameof(PropertyCellStyle),
				typeof(Style),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(null));

		public Style? PropertyCellStyle
		{
			get => (Style?)GetValue(PropertyCellStyleProperty);
			set => SetValue(PropertyCellStyleProperty, value);
		}

		public static readonly DependencyProperty LayoutVersionProperty =
			DependencyProperty.Register(
				nameof(LayoutVersion),
				typeof(int),
				typeof(DetailsLayoutRowPanel),
				new PropertyMetadata(0, (d, _) => ((DetailsLayoutRowPanel)d).InvalidateMeasure()));

		public int LayoutVersion
		{
			get => (int)GetValue(LayoutVersionProperty);
			set => SetValue(LayoutVersionProperty, value);
		}

		public DetailsLayoutRowPanel()
		{
			DataContextChanged += (_, _) => RefreshPropertyCells();
		}

		public void RefreshPropertyCells()
		{
			if (_propertyCells.Count == 0)
				return;

			var values = (DataContext as ListedItem)?.ShellPropertyValues;
			foreach (var (id, cell) in _propertyCells)
				cell.Text = values is not null && values.TryGetValue(id, out var value) ? value.Text ?? string.Empty : string.Empty;
		}

		private void OnColumnOrderChanged()
		{
			var columns = (ColumnOrder as DetailsLayoutColumnOrder)?.Columns;
			var propertyColumnIds = columns?.PropertyColumnIds ?? [];

			foreach (var id in _propertyCells.Keys.Where(id => !propertyColumnIds.Contains(id)).ToArray())
			{
				Children.Remove(_propertyCells[id]);
				_propertyCells.Remove(id);
			}

			if (columns is not null)
			{
				foreach (var id in propertyColumnIds)
				{
					if (!_propertyCells.TryGetValue(id, out var cell))
					{
						cell = new() { Padding = new(10, 0, 0, 0), Style = PropertyCellStyle };
						DetailsLayoutColumnExtensions.SetId(cell, id);
						_propertyCells[id] = cell;
						Children.Add(cell);
					}
				}

				RefreshPropertyCells();
			}

			InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var columns = (ColumnOrder as DetailsLayoutColumnOrder)?.Columns;
			var children = Children;
			var count = children.Count;

			if (_childWidths.Length < count)
			{
				_childWidths = new double[count];
				_orderedIndices = new int[count];
				_orderKeys = new int[count];
			}

			double width = 0, height = 0;
			for (int i = 0; i < count; i++)
			{
				var child = children[i];
				var columnWidth = GetColumnWidth(columns, DetailsLayoutColumnExtensions.GetId(child));

				child.Measure(new Size(double.IsNaN(columnWidth) ? double.PositiveInfinity : columnWidth, availableSize.Height));
				_childWidths[i] = double.IsNaN(columnWidth) ? child.DesiredSize.Width : columnWidth;
				width += _childWidths[i];
				height = Math.Max(height, child.DesiredSize.Height);
			}

			return new(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var order = ColumnOrder as DetailsLayoutColumnOrder ?? DetailsLayoutColumnOrder.Default;
			var children = Children;
			var count = Math.Min(children.Count, _childWidths.Length);

			for (int i = 0; i < count; i++)
			{
				var key = order.IndexOf(DetailsLayoutColumnExtensions.GetId(children[i]));

				int j = i - 1;
				while (j >= 0 && _orderKeys[j] > key)
				{
					_orderKeys[j + 1] = _orderKeys[j];
					_orderedIndices[j + 1] = _orderedIndices[j];
					j--;
				}

				_orderKeys[j + 1] = key;
				_orderedIndices[j + 1] = i;
			}

			double x = 0;
			for (int i = 0; i < count; i++)
			{
				var index = _orderedIndices[i];
				var child = children[index];
				var width = _childWidths[index];
				child.Arrange(new Rect(x, 0, width, Math.Max(finalSize.Height, child.DesiredSize.Height)));
				x += width;
			}

			return finalSize;
		}

		private static double GetColumnWidth(ColumnsViewModel? columns, string? id)
		{
			if (columns is null || id is null || columns.FindColumn(id) is not { } column)
				return double.NaN;

			return id == DetailsLayoutColumnIds.Icon ? column.LengthPixels : column.LengthIncludingGridSplitterPixels;
		}
	}
}
