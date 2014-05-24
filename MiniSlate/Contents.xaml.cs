using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WpfAnimatedGif;
using System.IO;

namespace MiniSlate
{
    /// <summary>
    /// Contents.xaml の相互作用ロジック
    /// </summary>
    public partial class Contents : UserControl
    {
        private static List<string> extensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".PNG", ".GIF", ".ICO" };
        private double scale = 1.0;
        private double x = 0.0;
        private double y = 0.0;

        public Contents()
        {
            InitializeComponent();
        }

        #region Func

        private bool IsValidExtension(string filename)
        {
            return !System.String.IsNullOrEmpty(extensions.Find(x => x.Equals(System.IO.Path.GetExtension(filename).ToUpperInvariant())));
        }

        private void ResizeImage(BitmapSource image)
        {
            if (image.PixelHeight * ImageCanvas.ActualWidth > ImageCanvas.ActualHeight * image.PixelWidth)
            {
                ImageBox.Height = Math.Min(image.PixelHeight, ImageCanvas.ActualHeight);
                ImageBox.Width = ImageBox.Height * image.PixelWidth / image.PixelHeight;
            }
            else
            {
                ImageBox.Width = Math.Min(image.PixelWidth, ImageCanvas.ActualWidth);
                ImageBox.Height = ImageBox.Width * image.PixelHeight / image.PixelWidth;
            }

            ImageBox.Width *= scale;
            ImageBox.Height *= scale;

            Canvas.SetLeft(ImageBox, (this.ImageCanvas.ActualWidth - ImageBox.Width) * 0.5 + x);
            Canvas.SetTop(ImageBox, (this.ImageCanvas.ActualHeight - ImageBox.Height) * 0.5 + y);
        }

        private void LoadImage(string filename)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(filename);
            image.EndInit();

            ResizeImage(image);

            try { ImageBehavior.SetAnimatedSource(this.ImageBox, image); }
            catch (FileFormatException) { this.ImageBox.Source = image; }
        }

        private void LoadThumbnails(string filename)
        {
            string directory = System.IO.Path.GetDirectoryName(filename);
            int count = 0;
            int current = 0;
            Func<string, Thumbnail> converter = x =>
            {
                if (x == filename)
                {
                    current = count;
                }
                count++;
                return new Thumbnail(x);
            };
            
            Task<List<Thumbnail>> task = Task.Factory.StartNew(() =>
            {
                return new List<Thumbnail>(
                    System.IO.Directory.GetFiles(directory)
                    .Where(IsValidExtension)
                    .Select(converter)
                    .ToList());
            });
            task.ContinueWith(x =>
            {
                ImageList.Dispatcher.BeginInvoke((Action)(() =>
                {
                    ImageList.ItemsSource = x.Result;
                    ImageList.SelectedIndex = current;
                }));
                ;
            });
        }

        private void LoadFiles(string filename)
        {
            //load thumbnails
            LoadThumbnails(filename);
            LoadImage(filename);
        }

        private void MoveLeft()
        {
            if (ImageList.SelectedIndex > 0)
            {
                ImageList.SelectedIndex--;
            }
            else if (ImageList.SelectedIndex == 0)
            {
                ImageList.SelectedIndex = ImageList.Items.Count - 1;
            }
        }

        private void MoveRight()
        {
            int max = ImageList.Items.Count - 1;
            if (ImageList.SelectedIndex < max)
            {
                ImageList.SelectedIndex++;
            }
            else if (ImageList.SelectedIndex >= 0)
            {
                ImageList.SelectedIndex = 0;
            }
        }

        #endregion

        #region Handler

        private void Image_Drop(object sender, DragEventArgs e)
        {
            string[] droppedFilenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (droppedFilenames != null || droppedFilenames.Count() > 0)
            {
                string filename = droppedFilenames[0];
                LoadFiles(filename);
            }
        }

        private void Image_DragOver(object sender, DragEventArgs e)
        {
            bool isEnabled = false;
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                string[] filenames =
                                 e.Data.GetData(DataFormats.FileDrop, true) as string[];
                isEnabled = IsValidExtension(filenames[0]);

            }
            if (!isEnabled)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private void ImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox imageList = sender as ListBox;
                Thumbnail thumbnail = imageList.SelectedItem as Thumbnail;
                if (thumbnail != null)
                {
                    imageList.Dispatcher.InvokeAsync(() => {
                        imageList.ScrollIntoView(thumbnail);
                        //load mainimage
                        LoadImage(thumbnail.Filename);                    
                    } );

                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
                Logger.Log(Logger.MessageType.ERROR, ex.ToString());
            }
        }

        private void LeftSide_Click(object sender, RoutedEventArgs e)
        {
            MoveLeft();
        }

        private void RightSide_Click(object sender, RoutedEventArgs e)
        {
            MoveRight();
        }

        private void OpenGroup_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton box = sender as RadioButton;
            if (ImageList != null && box.Tag != null)
            {
                ImageList.Items.SortDescriptions.Clear();
                ImageList.Items.SortDescriptions.Add(new SortDescription()
                {
                    PropertyName = "Name",
                    Direction = (ListSortDirection)Enum.Parse(typeof(ListSortDirection), box.Tag as string)
                });
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            foreach (string arg in args)
            { 
                if (arg[0] == '-')
                {
                    switch(arg.Substring(1))
                    {
                        case "l":
                            Logger.Enable(true);
                            break;
                    }
                }
            }

            if (args.Count() > 1)
            {
                LoadFiles(args[1]);
            }
        }

        private void ImageCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            ManOriginCircle.Visibility = Visibility.Visible;

            e.ManipulationContainer = sender as Canvas;
            e.Handled = true;
        }

        private void ImageCanvas_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            Canvas.SetLeft(ManOriginCircle, e.ManipulationOrigin.X);
            Canvas.SetTop(ManOriginCircle, e.ManipulationOrigin.Y);

            scale *= e.DeltaManipulation.Scale.X;
            if (scale < 1.0)
                scale = 1.0;

            double offsettedOriginX = e.ManipulationOrigin.X - this.ImageCanvas.ActualWidth * 0.5;
            double offsettedOriginY = e.ManipulationOrigin.Y - this.ImageCanvas.ActualHeight * 0.5;

            x = offsettedOriginX + e.DeltaManipulation.Scale.X * (x + e.DeltaManipulation.Translation.X - offsettedOriginX);
            y = offsettedOriginY + e.DeltaManipulation.Scale.Y * (y + e.DeltaManipulation.Translation.Y - offsettedOriginY);
            ResizeImage(ImageBox.Source as BitmapSource);

            e.Handled = true;
        }

        private void ImageCanvas_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            ManOriginCircle.Visibility = Visibility.Hidden;
        }

        private void ImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            BitmapSource source = this.ImageBox.Source as BitmapSource;
            if(source != null)
            {
                this.ResizeImage(this.ImageBox.Source as BitmapSource);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ImageBox.Source != null)
            {
                scale = 1.0;
                x = 0.0;
                y = 0.0;
                this.ResizeImage(this.ImageBox.Source as BitmapSource);
            }
        }

        #endregion


    }
    public class AngleToBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try 
            {
                int testangle = System.Convert.ToInt32(parameter);
                int angle = System.Convert.ToInt32(value);
                return testangle == angle;
            }
            catch (Exception e) {
                Logger.Log(Logger.MessageType.ERROR, e.ToString());
                return DependencyProperty.UnsetValue; 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return  System.Convert.ToInt32(parameter);
            }
            catch (Exception e)
            {
                Logger.Log(Logger.MessageType.ERROR, e.ToString());
                return DependencyProperty.UnsetValue;
            }
        }
        #endregion
    }
}
