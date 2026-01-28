using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using ToonTokenizer.Ast;

namespace ToonVS
{
    /// <summary>
    /// WPF UserControl for displaying the document outline in the Document Outline tool window.
    /// Shows a hierarchical tree of Toon properties for navigation.
    /// </summary>
    public partial class DocumentOutlineControl : UserControl
    {
        private Document _document;
        private IWpfTextView _textView;
        private IVsTextView _vsTextView;
        private bool _isNavigating;

        public ObservableCollection<OutlineItem> Items { get; } = new ObservableCollection<OutlineItem>();

        public DocumentOutlineControl()
        {
            InitializeComponent();
            OutlineTreeView.ItemsSource = Items;
        }

        /// <summary>
        /// Initializes the control with the document and text view for the Toon file.
        /// </summary>
        public void Initialize(Document document, IWpfTextView textView, IVsTextView vsTextView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Unsubscribe from previous document if any
            if (_document != null)
            {
                _document.Parsed -= OnDocumentParsed;
            }

            _document = document;
            _textView = textView;
            _vsTextView = vsTextView;

            if (_document != null)
            {
                _document.Parsed += OnDocumentParsed;

                // Subscribe to caret position changes for sync
                if (_textView != null)
                {
                    _textView.Caret.PositionChanged += OnCaretPositionChanged;
                }

                // Initial population
                RefreshItems();
            }
        }

        /// <summary>
        /// Cleans up event subscriptions when the control is disposed.
        /// </summary>
        public void Cleanup()
        {
            if (_document != null)
            {
                _document.Parsed -= OnDocumentParsed;
            }

            if (_textView != null)
            {
                _textView.Caret.PositionChanged -= OnCaretPositionChanged;
            }

            _document = null;
            _textView = null;
            _vsTextView = null;
            Items.Clear();
        }

        private void OnDocumentParsed(Document document)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                RefreshItems();
            });
#pragma warning restore VSTHRD110
        }

        private void OnCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            if (_isNavigating || _document?.Result?.Document == null)
            {
                return;
            }

            // Find and select the item that contains the current caret position
            int caretPosition = e.NewPosition.BufferPosition.Position;
            SelectItemAtPosition(caretPosition);
        }

        private void SelectItemAtPosition(int position)
        {
            // Find the closest item at or before the caret position
            OutlineItem bestMatch = FindItemAtPosition(Items, position);

            if (bestMatch != null && OutlineTreeView.SelectedItem != bestMatch)
            {
                _isNavigating = true;
                try
                {
                    SelectTreeViewItem(OutlineTreeView, bestMatch);
                }
                finally
                {
                    _isNavigating = false;
                }
            }
        }

        private OutlineItem FindItemAtPosition(IEnumerable<OutlineItem> items, int position)
        {
            OutlineItem result = null;

            foreach (OutlineItem item in items)
            {
                if (item.StartPosition <= position && position <= item.EndPosition)
                {
                    result = item;

                    // Check children for a more specific match
                    OutlineItem childMatch = FindItemAtPosition(item.Children, position);
                    if (childMatch != null)
                    {
                        result = childMatch;
                    }
                }
            }

            return result;
        }

        private void RefreshItems()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Items.Clear();

            if (_document?.Result?.Document?.Properties == null || !_document.Result.Document.Properties.Any())
            {
                EmptyMessage.Visibility = Visibility.Visible;
                return;
            }

            EmptyMessage.Visibility = Visibility.Collapsed;

            // Build hierarchical structure from properties
            foreach (PropertyNode property in _document.Result.Document.Properties)
            {
                OutlineItem item = CreateOutlineItem(property, 0);
                Items.Add(item);
            }

            // Expand all items
            ExpandAllTreeViewItems();
        }

        private OutlineItem CreateOutlineItem(PropertyNode property, int depth)
        {
            bool hasChildren = property.Value is ObjectNode objectNode && objectNode.Properties != null && objectNode.Properties.Count > 0;

            var item = new OutlineItem
            {
                Text = property.Key,
                Depth = depth,
                StartPosition = property.StartPosition,
                EndPosition = property.EndPosition,
                FontWeight = depth == 0 ? FontWeights.Bold : FontWeights.Normal,
                IconMoniker = hasChildren ? KnownMonikers.Namespace : KnownMonikers.Property
            };

            // Add children if the property value is an ObjectNode
            if (hasChildren)
            {
                foreach (PropertyNode childProperty in ((ObjectNode)property.Value).Properties)
                {
                    OutlineItem childItem = CreateOutlineItem(childProperty, depth + 1);
                    item.Children.Add(childItem);
                }
            }

            return item;
        }

        private void NavigateToItem(OutlineItem item)
        {
            if (item == null || _vsTextView == null)
            {
                return;
            }

            ThreadHelper.ThrowIfNotOnUIThread();

            _isNavigating = true;
            try
            {
                // Get line and column from position
                _vsTextView.GetLineAndColumn(item.StartPosition, out int line, out int column);

                // Navigate to the item line
                _vsTextView.SetCaretPos(line, column);
                _vsTextView.CenterLines(line, 1);

                // Ensure the editor has focus
                _vsTextView.SendExplicitFocus();
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void OutlineTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (OutlineTreeView.SelectedItem is OutlineItem item)
            {
                NavigateToItem(item);
            }
        }

        private void OutlineTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (e.Key == Key.Enter && OutlineTreeView.SelectedItem is OutlineItem item)
            {
                NavigateToItem(item);
                e.Handled = true;
            }
        }

        private void OutlineTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Single-click navigation (optional - can be removed if double-click only is preferred)
            // Uncomment the following to enable single-click navigation:
            // if (!_isNavigating && e.NewValue is OutlineItem item)
            // {
            //     NavigateToItem(item);
            // }
        }

        private void ExpandAllTreeViewItems()
        {
            foreach (OutlineItem item in Items)
            {
                ExpandTreeViewItem(OutlineTreeView, item);
            }
        }

        private void ExpandTreeViewItem(ItemsControl container, OutlineItem item)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = true;

                foreach (OutlineItem child in item.Children)
                {
                    ExpandTreeViewItem(treeViewItem, child);
                }
            }
        }

        private void SelectTreeViewItem(ItemsControl container, OutlineItem item)
        {
            // First, try to find the item directly
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsSelected = true;
                treeViewItem.BringIntoView();
                return;
            }

            // Search through all items recursively
            foreach (object containerItem in container.Items)
            {
                if (container.ItemContainerGenerator.ContainerFromItem(containerItem) is TreeViewItem childContainer)
                {
                    if (containerItem == item)
                    {
                        childContainer.IsSelected = true;
                        childContainer.BringIntoView();
                        return;
                    }

                    SelectTreeViewItem(childContainer, item);
                }
            }
        }
    }

    /// <summary>
    /// Represents an item in the document outline tree.
    /// </summary>
    public class OutlineItem
    {
        public string Text { get; set; }
        public int Depth { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public FontWeight FontWeight { get; set; }
        public ImageMoniker IconMoniker { get; set; }
        public ObservableCollection<OutlineItem> Children { get; } = new ObservableCollection<OutlineItem>();
    }
}
