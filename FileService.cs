using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Keyer__Carrot_test_
{
    public interface IFileService<T>
    {
        T Open(string filePath);
        bool Save(string filePath, T file);
    }
    public class PngService : IFileService<BitmapSource>
    {
        public BitmapSource Open(string filePath)
        {
            return new BitmapImage(new Uri(filePath, UriKind.Absolute)); ;
        }
        public bool Save(string filePath, BitmapSource file) 
        {
            FileStream stream = new FileStream(filePath, FileMode.Create);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(file));
            encoder.Save(stream);

            return true;
        }
    }
}
