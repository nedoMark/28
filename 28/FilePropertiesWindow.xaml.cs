using System;
using System.IO;
using System.Windows;

namespace _28
{
    public partial class FilePropertiesWindow : Window
    {
        private string itemPath;

        public FilePropertiesWindow(string path)
        {
            InitializeComponent();
            itemPath = path;

            ItemPathTextBlock.Text = "Path: " + itemPath;

            if (Directory.Exists(itemPath))
            {
                // Папка
                DirectoryInfo dirInfo = new DirectoryInfo(itemPath);
                ItemNameTextBlock.Text = "Folder: " + dirInfo.Name;
                ItemTypeTextBlock.Text = "Type: Folder";
                ItemSizeTextBlock.Text = "Size: " + GetFolderSize(itemPath) + " bytes";

                CreationDateTextBlock.Text = "Created: " + dirInfo.CreationTime;
                LastModifiedDateTextBlock.Text = "Modified: " + dirInfo.LastWriteTime;

                int fileCount = Directory.GetFiles(itemPath, "*", SearchOption.AllDirectories).Length;
                int folderCount = Directory.GetDirectories(itemPath, "*", SearchOption.AllDirectories).Length;
                ItemCountTextBlock.Text = $"Contains: {fileCount} files, {folderCount} folders";

                NewItemExtensionTextBox.Visibility = Visibility.Collapsed;
                NewItemNameTextBox.Text = dirInfo.Name;
            }
            else if (File.Exists(itemPath))
            {
                // Файл
                FileInfo fileInfo = new FileInfo(itemPath);
                ItemNameTextBlock.Text = "File: " + fileInfo.Name;
                ItemTypeTextBlock.Text = "Type: " + fileInfo.Extension;
                ItemSizeTextBlock.Text = "Size: " + fileInfo.Length + " bytes";

                CreationDateTextBlock.Text = "Created: " + fileInfo.CreationTime;
                LastModifiedDateTextBlock.Text = "Modified: " + fileInfo.LastWriteTime;

                ItemCountTextBlock.Text = "File";

                NewItemExtensionTextBox.Visibility = Visibility.Visible;
                NewItemNameTextBox.Text = Path.GetFileNameWithoutExtension(fileInfo.Name);
                NewItemExtensionTextBox.Text = fileInfo.Extension;
            }
        }

        private long GetFolderSize(string folderPath)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        size += fi.Length;
                    }
                    catch { /* Пропустити проблемні файли */ }
                }
            }
            catch { /* Пропустити, якщо немає доступу */ }
            return size;
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            string newItemName = NewItemNameTextBox.Text;
            string newItemExtension = NewItemExtensionTextBox.Text;

            if (string.IsNullOrWhiteSpace(newItemName))
            {
                MessageBox.Show("Назва не може бути порожньою.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (File.Exists(itemPath))
                {
                    if (!string.IsNullOrWhiteSpace(newItemExtension) && !newItemExtension.StartsWith("."))
                    {
                        MessageBox.Show("Розширення має починатися з крапки.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string newPath = Path.Combine(Path.GetDirectoryName(itemPath), newItemName + newItemExtension);
                    if (File.Exists(newPath))
                    {
                        MessageBox.Show("Файл з такою назвою вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    File.Move(itemPath, newPath);
                    MessageBox.Show("Файл перейменовано успішно.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else if (Directory.Exists(itemPath))
                {
                    string newPath = Path.Combine(Path.GetDirectoryName(itemPath), newItemName);
                    if (Directory.Exists(newPath))
                    {
                        MessageBox.Show("Тека з такою назвою вже існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    Directory.Move(itemPath, newPath);
                    MessageBox.Show("Тека перейменована успішно.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка перейменування: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
