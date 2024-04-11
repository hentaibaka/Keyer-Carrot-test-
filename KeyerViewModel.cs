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
        #region Fields
        protected BitmapSource origImg;
        protected BitmapSource procImg;
        public BitmapSource OrigImg
        {
            get { return origImg; }
            set
            {
                origImg = value;
                UpdatePixels();
                UpdateProcImg();
                OnPropertyChanged("OrigImg");
            }
        }
        public BitmapSource ProcImg
        {
            get { return procImg; }
            set
            {
                procImg = value;
                OnPropertyChanged("ProcImg");
            }
        }

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

        protected IDialogService dialogService;
        protected IFileService<BitmapSource> fileService;

        protected int rDelta = 0;
        protected int gDelta = 0;
        protected int bDelta = 0;
        public int RDelta { get { return rDelta; } set { rDelta = value; } }
        public int GDelta { get { return gDelta; } set { gDelta = value; } }
        public int BDelta { get { return bDelta; } set { bDelta = value; } }

        protected RelayCommand resetCommand;
        protected RelayCommand magicCommand;
        protected RelayCommand openCommand;
        protected RelayCommand saveCommand;
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

                        Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
                        {
                            if (pixels[n * 4    ] >= PickedColor.B - BDelta &&
                                pixels[n * 4    ] <= PickedColor.B + BDelta &&
                                pixels[n * 4 + 1] >= PickedColor.G - GDelta &&
                                pixels[n * 4 + 1] <= PickedColor.G + GDelta &&
                                pixels[n * 4 + 2] >= PickedColor.R - RDelta &&
                                pixels[n * 4 + 2] <= PickedColor.R + RDelta)
                            {
                                pixels[n * 4 + 3] = 0;
                            }
                            else
                            {
                                pixels[n * 4 + 3] = 255;
                            }
                        });
                        UpdateProcImg();
                    }
                    ));
            }
        }
        public RelayCommand ResetCommand
        {
            get
            {
                return resetCommand ??
                    (resetCommand = new RelayCommand(obj =>
                    {
                        if (pixels == null) return;

                        Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
                        {
                            pixels[n * 4 + 3] = 255;
                        });
                        UpdateProcImg();
                    }
                    ));
            }
        }
        #endregion

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
