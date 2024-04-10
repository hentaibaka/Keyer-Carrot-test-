using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Keyer__Carrot_test_
{
    public interface IDialogService
    {
        string OpenFilePath { get; set; }
        string SaveFilePath { get; set; }
        void ShowMessage(string message);
        bool OpenFileDialog();
        bool SaveFileDialog();
    }

    public class PngDialogService : IDialogService
    {
        private string filter = "Image files (*.png)|*.png";

        public string Filter { get { return filter; } }
        public string OpenFilePath { get; set; }
        public string SaveFilePath { get; set; }

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = Filter;

            if (openFileDialog.ShowDialog() == true)
            {
                OpenFilePath = openFileDialog.FileName;
                return true;
            }
            return false;
        }

        public bool SaveFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = Filter;

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveFilePath = saveFileDialog.FileName;
                return true;
            }
            return false;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
