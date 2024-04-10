using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Ink;
using ColorPicker.Models;

namespace Keyer__Carrot_test_
{
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
    public class KeyerViewModel : INotifyPropertyChanged
    {
        protected BitmapSource origImg;
        protected BitmapSource procImg;

        protected byte[] pixels;
        protected int stride;

        protected Brush fillForRect;
        protected Color pickedColor;

        public Brush FillForRect { get { return fillForRect; } 
            set { fillForRect = value; 
                OnPropertyChanged("FillForRect"); } }
        public Color PickedColor { get { return pickedColor; } 
            set { pickedColor = value; 
                value.A = 255; 
                FillForRect = new SolidColorBrush(value); 
                OnPropertyChanged("PickedColor"); } }

        protected RelayCommand magicCommand;
        protected RelayCommand openCommand;
        protected RelayCommand saveCommand;

        protected IDialogService dialogService;
        protected IFileService<BitmapSource> fileService;

        public BitmapSource OrigImg { get { return origImg; } 
            set { origImg = value; 
                UpdatePixels();
                UpdateProcImg();
                OnPropertyChanged("OrigImg"); } }
        public BitmapSource ProcImg { get { return procImg; } 
            set { procImg = value; 
                OnPropertyChanged("ProcImg"); } }

        public RelayCommand OpenCommand { get { return openCommand ?? 
                    (openCommand = new RelayCommand(obj =>
                    {
                        if (dialogService == null || fileService == null) return;

                        try
                        {
                            if (dialogService.OpenFileDialog() == true)
                            {
                                OrigImg = fileService.Open(dialogService.OpenFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            dialogService.ShowMessage(ex.Message);
                        }
                    }
                    )); } }
        public RelayCommand SaveCommand { get { return saveCommand ?? 
                    (saveCommand = new RelayCommand(obj => 
                    {
                        if (dialogService == null || fileService == null) return;

                        try
                        {
                            if (dialogService.SaveFileDialog() == true)
                            {
                                fileService.Save(dialogService.SaveFilePath, ProcImg);
                            }
                        }
                        catch (Exception ex)
                        {
                            dialogService.ShowMessage(ex.Message);
                        }
                    }
                    )); } }
        public RelayCommand MagicCommand
        {
            get
            {
                return magicCommand ??
                    (magicCommand = new RelayCommand(obj =>
                    {
                        if (pixels == null) return;

                        for (int i = 3; i < pixels.Length; i += 4)
                        {
                            if (pixels[i-3] == PickedColor.B &&
                                pixels[i-2] == PickedColor.G &&
                                pixels[i-1] == PickedColor.R)
                            {
                                pixels[i] = 0;
                            }
                            else
                            {
                                pixels[i] = 255;
                            }
                        }
                        UpdateProcImg();
                    }
                    ));
            }
        }

        public KeyerViewModel(IDialogService dialogService, IFileService<BitmapSource> fileService)
        {
            this.dialogService = dialogService;
            this.fileService = fileService;
        }
        private void UpdatePixels()
        {
            stride = (origImg.PixelWidth * origImg.Format.BitsPerPixel) / 8;
            pixels = new byte[stride * origImg.PixelHeight];
            origImg.CopyPixels(pixels, stride, 0);
        }
        private void UpdateProcImg()
        {
            ProcImg = BitmapSource.Create(
                origImg.PixelWidth,
                origImg.PixelHeight,
                origImg.DpiX,
                origImg.DpiY,
                PixelFormats.Bgra32,
                //origImg.Format,
                origImg.Palette,
                pixels,
                stride);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
