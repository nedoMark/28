using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace _28
{
    public partial class MainWindow : Window
    {
        private string lastSortedColumn = null;
        private bool isSortAscending = true;
        private string currentPath;
        private string clipboardPath = null;
        private bool isCutOperation = false;
        private Stack<string> backHistory = new Stack<string>();
        private Stack<string> forwardHistory = new Stack<string>();
        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();



        public MainWindow()
        {
            InitializeComponent();
            currentPath = @"C:\"; 

            LoadDrives();
            fileItems = new ObservableCollection<FileItem>();
            FileListView.ItemsSource = fileItems;
            LoadFiles();


        }
        private void LoadFiles()
        {
            // Наприклад, заповнення fileItems
            fileItems.Add(new FileItem { Name = "File1.txt", Type = "File", Size = 1024 });
            fileItems.Add(new FileItem { Name = "Folder1", Type = "Folder", Size = 0 });

            // Додай інші файли чи папки
        }
        private void LoadDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                var item = new TreeViewItem { Header = drive.Name, Tag = drive.Name };
                item.Expanded += Folder_Expanded;
                item.Items.Add(null);
                DirectoryTree.Items.Add(item);
            }
        }
        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is string path && Directory.Exists(path))
            {
                currentPath = path;
                PathTextBox.Text = currentPath;
                NavigateTo(currentPath);
            }
        }
        private enum SortDirection { Ascending, Descending }

        private SortDirection _currentSortDirection = SortDirection.Ascending;
        private string _lastSortedColumn = string.Empty;

        private void GridViewColumnHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                string columnHeader = textBlock.Text;
                SortItems(columnHeader);
            }
        }

        private void SortItems(string columnHeader)
        {
            // Якщо це той самий стовпець, змінюємо напрямок сортування
            if (_lastSortedColumn == columnHeader)
            {
                _currentSortDirection = _currentSortDirection == SortDirection.Ascending
                    ? SortDirection.Descending
                    : SortDirection.Ascending;
            }
            else
            {
                // Якщо новий стовпець, сортуємо за зростанням
                _currentSortDirection = SortDirection.Ascending;
                _lastSortedColumn = columnHeader;
            }

            // Виконуємо сортування
            IOrderedEnumerable<FileItem> sortedItems = null;

            switch (columnHeader)
            {
                case "Name":
                    sortedItems = _currentSortDirection == SortDirection.Ascending
                        ? fileItems.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                        : fileItems.OrderByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase);
                    break;

                case "Type":
                    sortedItems = _currentSortDirection == SortDirection.Ascending
                        ? fileItems.OrderBy(f => f.Type)
                        : fileItems.OrderByDescending(f => f.Type);
                    break;

                case "Size":
                    sortedItems = _currentSortDirection == SortDirection.Ascending
                        ? fileItems.OrderBy(f => f.Size)
                        : fileItems.OrderByDescending(f => f.Size);
                    break;
            }
            if (sortedItems != null)
            {
                fileItems.Clear();
                foreach (var item in sortedItems)
                {
                    fileItems.Add(item);
                }
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Перехоплюємо подію для відкриття контекстного меню
            e.Handled = true;

            var item = VisualUpwardSearch<TreeViewItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                item.IsSelected = true;
                // Відображаємо контекстне меню
                item.ContextMenu = (ContextMenu)Resources["ItemContextMenu"];
                item.ContextMenu.IsOpen = true;
            }
        }

        // Обробка події для ListView
        private void FileListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Перехоплюємо подію для відкриття контекстного меню
            e.Handled = true;

            var item = VisualUpwardSearch<ListViewItem>((DependencyObject)e.OriginalSource);
            if (item != null)
            {
                item.IsSelected = true;
                // Відображаємо контекстне меню
                item.ContextMenu = (ContextMenu)Resources["ItemContextMenu"];
                item.ContextMenu.IsOpen = true;
            }
        }

        // Визначення допоміжного методу для пошуку елементів
        static T VisualUpwardSearch<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                source = VisualTreeHelper.GetParent(source);
            return source as T;
        }

        private void LoadFileList()
        {
            FileListView.Items.Clear();
            if (Directory.Exists(currentPath))
            {
                foreach (var file in Directory.GetFiles(currentPath))
                {
                    FileListView.Items.Add(new FileInfo(file));
                }
            }
        }



        private void NavigateTo(string path, bool addToHistory = true)
        {
            if (addToHistory && currentPath != null && Directory.Exists(currentPath))
                backHistory.Push(currentPath);

            forwardHistory.Clear(); 

            currentPath = path;
            PathTextBox.Text = path;

            fileItems.Clear();

            if (Directory.Exists(path))
            {
                var directories = Directory.GetDirectories(path).Select(d => new FileItem
                {
                    Name = Path.GetFileName(d),
                    Type = "Folder",
                    Size = 0
                });

                var files = Directory.GetFiles(path).Select(f =>
                {
                    var fileInfo = new FileInfo(f);
                    return new FileItem
                    {
                        Name = fileInfo.Name,
                        Type = "File",
                        Size = fileInfo.Length
                    };
                });

                foreach (var item in directories.Concat(files))
                {
                    fileItems.Add(item);
                }
            }
        }
        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedItem)
            {
                string selectedPath = Path.Combine(currentPath, selectedItem.Name);

                if (selectedItem.Type == "Folder" && Directory.Exists(selectedPath))
                {
                    NavigateTo(selectedPath);
                    currentPath = selectedPath;
                    PathTextBox.Text = currentPath;
                }
                else if (selectedItem.Type == "File" && File.Exists(selectedPath))
                {
                    OpenFile(selectedPath);
                }
            }
        }
        private void OpenFile(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();

            switch (extension)
            {
                case ".txt":
                case ".log":
                case ".csv":
                case ".json":
                case ".xml":
                case ".html":
                    OpenTextFile(filePath);
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    OpenImageFile(filePath);
                    break;
                default:
                    try { Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true }); }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не вдалося відкрити файл: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    break;
            }
        }
        private void OpenTextFile(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                new TextFileWindow(filePath, content).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка читання файлу: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OpenImageFile(string filePath)
        {
            try
            {
                new ImageWindow(filePath).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка відкриття зображення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (backHistory.Count > 0)
            {
                forwardHistory.Push(currentPath);
                string previous = backHistory.Pop();
                NavigateTo(previous, addToHistory: false);
            }
        }
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (forwardHistory.Count > 0)
            {
                backHistory.Push(currentPath);
                string next = forwardHistory.Pop();
                NavigateTo(next, addToHistory: false);
            }
        }
        private void Up_Click(object sender, RoutedEventArgs e)
        {
            var parentDirectory = Directory.GetParent(currentPath);
            if (parentDirectory != null)
            {
                currentPath = parentDirectory.FullName;
                PathTextBox.Text = currentPath;
                LoadFileList();
            }
        }
        private void FileList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = VisualUpwardSearch<ListViewItem>((DependencyObject)e.OriginalSource);
            if (item != null)
                item.IsSelected = true;
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selected)
            {
                clipboardPath = Path.Combine(PathTextBox.Text, selected.Name);
                isCutOperation = false;
            }
        }
        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(clipboardPath))
            {
                string dest = Path.Combine(PathTextBox.Text, Path.GetFileName(clipboardPath));
                if (File.Exists(clipboardPath)) File.Copy(clipboardPath, dest, true);
                else if (Directory.Exists(clipboardPath)) CopyDirectory(clipboardPath, dest);
                NavigateTo(PathTextBox.Text);
            }
        }
        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(sourceDir))
                CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }

        private void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selected)
            {
                string path = Path.Combine(PathTextBox.Text, selected.Name);
                new FilePropertiesWindow(path).ShowDialog();
            }
        }
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selected)
            {
                string path = Path.Combine(PathTextBox.Text, selected.Name);
                if (MessageBox.Show($"Видалити {selected.Name}?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (File.Exists(path)) File.Delete(path);
                        else if (Directory.Exists(path)) Directory.Delete(path, true);
                        NavigateTo(PathTextBox.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка видалення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string path = PathTextBox.Text;
                if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
                else
                {
                    MessageBox.Show("Невірний шлях.");
                }
            }
        }
        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                foreach (var dir in Directory.GetDirectories(item.Tag.ToString()))
                {
                    var sub = new TreeViewItem { Header = Path.GetFileName(dir), Tag = dir };
                    sub.Items.Add(null);
                    sub.Expanded += Folder_Expanded;
                    item.Items.Add(sub);
                }
            }
        }

        public class FileItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Size { get; set; }
        }
    }
    public class TextFileWindow : Window
    {
        public TextFileWindow(string filePath, string content)
        {
            Title = Path.GetFileName(filePath);
            Width = 800;
            Height = 600;
            Content = new TextBox
            {
                Text = content,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                TextWrapping = TextWrapping.NoWrap
            };
        }
    }

    public class ImageWindow : Window
    {
        private ScaleTransform scaleTransform = new ScaleTransform(1, 1);
        private Point start;
        private Image image;
        private ScrollViewer viewer;

        public ImageWindow(string filePath)
        {
            Title = Path.GetFileName(filePath);

            BitmapImage bitmap = new BitmapImage(new Uri(filePath));
            image = new Image
            {
                Source = bitmap,
                RenderTransformOrigin = new Point(0, 0),
                RenderTransform = scaleTransform
            };
            image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
            image.MouseMove += Image_MouseMove;
            image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
            image.Cursor = Cursors.Hand;

            viewer = new ScrollViewer
            {
                Content = image,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            Button zoomInButton = new Button { Content = "+", Width = 30, Margin = new Thickness(5) };
            zoomInButton.Click += (s, e) => Zoom(1.1);

            Button zoomOutButton = new Button { Content = "−", Width = 30, Margin = new Thickness(5) };
            zoomOutButton.Click += (s, e) => Zoom(1 / 1.1);

            Button resetButton = new Button { Content = "Reset", Width = 50, Margin = new Thickness(5) };
            resetButton.Click += (s, e) => ResetZoom();

            buttonPanel.Children.Add(zoomInButton);
            buttonPanel.Children.Add(zoomOutButton);
            buttonPanel.Children.Add(resetButton);

            DockPanel mainPanel = new DockPanel();
            DockPanel.SetDock(buttonPanel, Dock.Top);
            mainPanel.Children.Add(buttonPanel);
            mainPanel.Children.Add(viewer);

            Content = mainPanel;
            Width = Math.Min(bitmap.PixelWidth + 100, SystemParameters.PrimaryScreenWidth * 0.8);
            Height = Math.Min(bitmap.PixelHeight + 100, SystemParameters.PrimaryScreenHeight * 0.8);
        }

        private void Zoom(double factor)
        {
            scaleTransform.ScaleX *= factor;
            scaleTransform.ScaleY *= factor;
        }

        private void ResetZoom()
        {
            scaleTransform.ScaleX = 1;
            scaleTransform.ScaleY = 1;
            viewer.ScrollToHorizontalOffset(0);
            viewer.ScrollToVerticalOffset(0);
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                start = e.GetPosition(this);
                image.CaptureMouse();
            }
        }

        public class FileItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Size { get; set; }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            image.ReleaseMouseCapture();
        }
        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (image.IsMouseCaptured)
            {
                Point pos = e.GetPosition(this);
                double offsetX = pos.X - start.X;
                double offsetY = pos.Y - start.Y;

                viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset - offsetX);
                viewer.ScrollToVerticalOffset(viewer.VerticalOffset - offsetY);

                start = pos;
            }
        }
    }
}
