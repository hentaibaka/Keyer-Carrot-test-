using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        /// <summary>
        /// Оригинальное изображение.
        /// </summary>
        public BitmapSource OrigImg
        {
            get { return origImg; }
            set
            {
                origImg = value;

                // обновляем ProcImg
                UpdatePixels();
                if (ShowAlpha) ProcImg = UpdateProcImgAlpha();
                else ProcImg = UpdateProcImg();

                OnPropertyChanged("OrigImg");
            }
        }
        /// <summary>
        /// Обработанное изображение.
        /// </summary>
        public BitmapSource ProcImg
        {
            get { return procImg; }
            set
            {
                procImg = value;
                OnPropertyChanged("ProcImg");
            }
        }

        /// <summary>
        /// Массив пикселей.
        /// </summary>
        protected byte[] pixels;
        /// <summary>
        /// pixels * channels в одной строке оригинального изображения.
        /// </summary>
        protected int stride;

        protected bool isColorPicker;
        protected Brush fillForRect;
        protected Color pickedColor;
        /// <summary>
        /// Brush того же цвета, что и PickedColor.
        /// </summary>
        public Brush FillForRect 
        {
            get { return fillForRect; }
            set 
            { 
                fillForRect = value;
                OnPropertyChanged("FillForRect"); 
            } 
        }
        /// <summary>
        /// Выбранный цвет.
        /// </summary>
        public Color PickedColor 
        {
            get { return pickedColor; }
            set 
            { 
                pickedColor = value;

                // обновляем FillForRect
                value.A = 255;
                FillForRect = new SolidColorBrush(value);

                OnPropertyChanged("PickedColor"); 
            } 
        }
        /// <summary>
        /// Включена ли пипетка.
        /// </summary>
        public bool IsColorPicker 
        {
            get { return isColorPicker; }
            set 
            { 
                isColorPicker = value;
                OnPropertyChanged("IsColorPicker"); 
            } 
        }

        protected IDialogService dialogService;
        protected IFileService<BitmapSource> fileService;

        protected int rDelta;
        protected int gDelta;
        protected int bDelta;
        /// <summary>
        /// Разброс красного канала, для сопоставления цвета.
        /// </summary>
        public int RDelta { get { return rDelta; } set { rDelta = value; } }
        /// <summary>
        /// Разброс зелёного канала, для сопоставления цвета.
        /// </summary>
        public int GDelta { get { return gDelta; } set { gDelta = value; } }
        /// <summary>
        /// Разброс синего канала, для сопоставления цвета.
        /// </summary>
        public int BDelta { get { return bDelta; } set { bDelta = value; } }

        protected bool showAlpha;
        /// <summary>
        /// Показывать ли прозрачность пикселей.
        /// </summary>
        public bool ShowAlpha 
        { 
            get { return showAlpha; } 
            set 
            { 
                showAlpha = value;
                if (ShowAlpha) ProcImg = UpdateProcImgAlpha();
                else ProcImg = UpdateProcImg();
                OnPropertyChanged("ShowAlpha"); 
            } 
        }

        protected int smoothRadius;
        protected double smoothStrength;
        /// <summary>
        /// Радиус размытия краёв.
        /// </summary>
        public int SmoothRadius 
        {
            get { return smoothRadius; } 
            set 
            { 
                smoothRadius = value;
                OnPropertyChanged("SmoothRadius");
            }
        }
        /// <summary>
        /// Сила размытия краёв.
        /// </summary>
        public double SmoothStrength
        {
            get { return smoothStrength; }
            set
            {
                smoothStrength = value;
                OnPropertyChanged("SmoothStrength");
            }
        }

        protected RelayCommand resetCommand;
        protected RelayCommand magicCommand;
        protected RelayCommand openCommand;
        protected RelayCommand saveCommand;
        protected RelayCommand pickColorCommand;
        /// <summary>
        /// Команда открытия файла.
        /// </summary>
        public RelayCommand OpenCommand 
        { 
            get 
            { 
                return openCommand ?? 
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
                    )); 
            } 
        }
        /// <summary>
        /// Команда сохранения изменённого файла.
        /// </summary>
        public RelayCommand SaveCommand 
        { 
            get 
            { 
                return saveCommand ?? 
                    (saveCommand = new RelayCommand(obj => 
                    {
                        if (dialogService == null || fileService == null) return;

                        try
                        {
                            if (dialogService.SaveFileDialog() == true)
                            {
                                fileService.Save(dialogService.SaveFilePath, UpdateProcImg());
                            }
                        }
                        catch (Exception ex)
                        {
                            dialogService.ShowMessage(ex.Message);
                        }
                    }
                    )); 
            } 
        }
        /// <summary>
        /// Команда удаления пикселей выделенного цвета с изображения.
        /// </summary>
        public RelayCommand MagicCommand
        {
            get
            {
                return magicCommand ??
                    (magicCommand = new RelayCommand(obj =>
                    {
                        if (pixels == null) return;

                        // Проходимся по всем пикселям изобрежения
                        Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
                        {
                            // Если цвет пикселя в диапазоне
                            // PickedColor - Delta <= PixelColor <= PickedColor + Delta
                            // делаем его прозрачным
                            // иначе делаем видимым
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

                        // если включено скругление краёв
                        if (SmoothRadius > 0 && SmoothStrength > 0) 
                        {
                            double strength = SmoothStrength, newAlpha;
                            int width = OrigImg.PixelWidth,
                                height = OrigImg.PixelHeight,
                                radius = SmoothRadius,
                                alphaN = radius * radius * 2 + radius * 2;

                            if (radius > 1) alphaN -= 4;

                            byte[] smoothPixels = new byte[pixels.Length];
                            pixels.CopyTo(smoothPixels, 0);

                            Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
                            {
                                if (pixels[n * 4 + 3] == 0) return;

                                int alphaSum = 0, currentIndex,
                                    x, y, absi, absj;

                                // для каждого непрозрачного пикселя перебираем соседние пиксели
                                // в радиусе SmoothRadius, смотрим сколько из них прозрачные.
                                // Чем больше прозрачных пикселей в округе, тем более прозрачным
                                // будет текущий пиксель
                                for (int i = -radius; i < radius + 1; i++)
                                {
                                    for (int j = -radius; j < radius + 1; j++)
                                    {
                                        absi = Math.Abs(i);
                                        absj = Math.Abs(j);
                                        // пытаемся сделать окно свёртки более округлым
                                        // срезаем углы квадрата, в теории должен быть круг,
                                        // но получился ромб со скруглёнными углами
                                        if (absi + absj > radius ||
                                            (absi == 0 && absj == 0) ||
                                            (((absi == 0 && absj == radius) ||
                                              (absi == radius && absj == 0)) &&
                                              radius > 1)) continue;

                                        // преобразуем индекс пикселя n в двумерные координаты
                                        y = n / width;
                                        x = n - y * width;
                                        // получаем абсолютные двумерные координаты соседнего пикселя
                                        y += i;
                                        x += j;
                                        // смотрим, чтобы координаты не выходили за границы
                                        if (y < 0 || y > height - 1 || 
                                            x < 0 || x > width - 1) continue;
                                        // получаем индекс соседнего пикселя
                                        currentIndex = y * width + x;
                                        // если этот пиксель прозрачный, считаем его
                                        if (pixels[currentIndex * 4 + 3] == 0) alphaSum++;
                                    }
                                }
                                // если рядом с текущим пикселем бли прозрачные пиксели
                                if (alphaSum > 0)
                                {
                                    // считаем новую прозрачность
                                    newAlpha = 255 - (byte)((double)alphaSum / alphaN * 255);
                                    if (newAlpha < 255) newAlpha *= 1.0 - (strength / 100);
                                    smoothPixels[n * 4 + 3] = (byte)(newAlpha);
                                }                             
                            });
                            smoothPixels.CopyTo(pixels, 0);
                        }

                        if (ShowAlpha) ProcImg = UpdateProcImgAlpha();
                        else ProcImg = UpdateProcImg();
                    }
                    ));
            }
        }
        /// <summary>
        /// Команда сброса всех изменений.
        /// </summary>
        public RelayCommand ResetCommand
        {
            get
            {
                return resetCommand ??
                    (resetCommand = new RelayCommand(obj =>
                    {
                        if (pixels == null) return;
                        // проходимся по всем пикселям и делаем их видимыми
                        Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
                        {
                            pixels[n * 4 + 3] = 255;
                        });
                        if (ShowAlpha) ProcImg = UpdateProcImgAlpha();
                        else ProcImg = UpdateProcImg();
                    }
                    ));
            }
        }
        /// <summary>
        /// Команда выбора цвета, как у пикселя на который кликнули.
        /// </summary>
        public RelayCommand PickColorCommand
        {
            get 
            {
                return pickColorCommand ??
                    (pickColorCommand = new RelayCommand(obj =>
                    {
                        if (OrigImg == null || !IsColorPicker) return;
                        
                        MouseButtonEventArgs mouse = (MouseButtonEventArgs)obj;
                        Image image = (Image)mouse.Source;

                        // получаем размер изображения на экране и в пикселях
                        Point size = new Point(image.ActualWidth, image.ActualHeight);
                        Point realSize = new Point(((BitmapSource)image.Source).PixelWidth, ((BitmapSource)image.Source).PixelHeight);

                        // получаем точку куда нажал пользователь на экране
                        Point position = mouse.GetPosition(image);
                        // преобразуем координаты экрана в координаты пикселей
                        Point realPosition = new Point(realSize.X / size.X * position.X, realSize.Y / size.Y * position.Y);


                        int pixelNum = (int)realPosition.Y * (int)realSize.X + (int)realPosition.X;
                        PickedColor = Color.FromRgb(pixels[pixelNum * 4 + 2], pixels[pixelNum * 4 + 1], pixels[pixelNum * 4]);

                        IsColorPicker = false; 
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

        /// <summary>
        /// Обновляет byte[] pixels. Преобразует OrigImg.Source в массив пикселей byte[] pixels. 
        /// </summary>
        protected void UpdatePixels()
        {
            stride = (origImg.PixelWidth * origImg.Format.BitsPerPixel) / 8;
            pixels = new byte[stride * origImg.PixelHeight];
            origImg.CopyPixels(pixels, stride, 0);
        }
        /// <summary>
        /// Преобразует byte[] pixels в BitmapSource. 
        /// </summary>
        /// <returns>BitmapSource для Image.Source</returns>
        protected BitmapSource UpdateProcImg()
        {
            if (pixels== null) return null;

            return BitmapSource.Create(
                origImg.PixelWidth,
                origImg.PixelHeight,
                origImg.DpiX,
                origImg.DpiY,
                PixelFormats.Bgra32,
                origImg.Palette,
                pixels,
                stride);
        }
        /// <summary>
        /// Преобразует byte[] pixels в BitmapSource, где Alpha-канал это значение RGB-каналов. 
        /// </summary>
        /// <returns>BitmapSource для Image.Source</returns>
        protected BitmapSource UpdateProcImgAlpha()
        {
            if (pixels == null)
            {
                return null;
            }

            byte[] alphaPixels = new byte[pixels.Length];
            pixels.CopyTo(alphaPixels, 0);
            Parallel.For(0, OrigImg.PixelHeight * origImg.PixelWidth, n =>
            {
                alphaPixels[n * 4 + 2] = pixels[n * 4 + 3];
                alphaPixels[n * 4 + 1] = pixels[n * 4 + 3];
                alphaPixels[n * 4    ] = pixels[n * 4 + 3]; 
                alphaPixels[n * 4 + 3] = 255;

            });
            return BitmapSource.Create(
                origImg.PixelWidth,
                origImg.PixelHeight,
                origImg.DpiX,
                origImg.DpiY,
                PixelFormats.Bgra32,
                origImg.Palette,
                alphaPixels,
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
