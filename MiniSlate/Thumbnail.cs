using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.ComponentModel;

namespace MiniSlate
{
    class Thumbnail : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        private ImageSource source;
        public ImageSource Source
        {
            get
            {
                return source;
            }
        }

        private string filename;
        public string Filename
        {
            get
            {
                return filename;
            }
        }
        public Thumbnail(string filename)
        {
            this.filename = filename;
            name = Path.GetFileName(filename);
            source = GetThumbnailImage(filename, 64, 64);
            NotifyPropertyChanged("Source");
        }

        private ImageSource GetThumbnailImage(string fileName, int width, int height)
        {
            try
            {
                byte[] buffer = System.IO.File.ReadAllBytes(fileName);
                MemoryStream stream = new MemoryStream(buffer);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.DecodePixelWidth = width;
                bitmap.DecodePixelHeight = height;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (NotSupportedException e)
            {
                Logger.Log(Logger.MessageType.WARNING,e.ToString());
                return null;
            }

            catch (ArgumentException e)
            {
                System.Windows.MessageBox.Show(e.ToString());
                Logger.Log(Logger.MessageType.ERROR,e.ToString());
                return null;
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
