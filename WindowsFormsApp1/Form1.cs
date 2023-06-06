using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using WMPLib;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private WindowsMediaPlayer player;

        public Form1()
        {
            InitializeComponent();
            player = new WindowsMediaPlayer();
            axWindowsMediaPlayer1.Dock = DockStyle.Fill;
            axWindowsMediaPlayer1.uiMode = "none";
            player.uiMode = "none";
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.pause();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.URL = this.textBox_path.Text;
            axWindowsMediaPlayer1.Ctlcontrols.play();
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.stop();
        }

        private void выбратьФайлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                player.URL = openFileDialog.FileName;
                this.textBox_path.Text = openFileDialog.FileName;
                player.controls.play();
            }
        }

        private void ProcessVideo()
        {
            using (Bitmap videoFrame = new Bitmap(axWindowsMediaPlayer1.Width, axWindowsMediaPlayer1.Height))
            {
                axWindowsMediaPlayer1.DrawToBitmap(videoFrame, new Rectangle(0, 0, videoFrame.Width, videoFrame.Height));

                Bitmap grayFrame = ConvertToGrayscale(videoFrame);

                // Применение фильтра Гаусса для удаления шума
                int filterRadius = 3; // Регулирование
                double filterSigma = 1.0; // Тоже регулирование
                grayFrame = ApplyGaussianFilter(grayFrame, filterRadius, filterSigma);

                // обработка оттенков серого или их отображение

                grayFrame.Dispose();
            }
        }

        
        private Bitmap ConvertToGrayscale(Bitmap image)
        {
            Bitmap grayImage = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);

                    int grayValue = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    Color grayPixel = Color.FromArgb(grayValue, grayValue, grayValue);

                    grayImage.SetPixel(x, y, grayPixel);
                }
            }

            return grayImage;
        }

        private void TimerTick(object sender, EventArgs e)
        {
            ProcessVideo();
        }

        private Bitmap ApplyGaussianFilter(Bitmap image, int radius, double sigma)
        {
            Bitmap filteredImage = new Bitmap(image.Width, image.Height);

            int size = radius * 2 + 1;
            double[,] kernel = new double[size, size];
            double kernelSum = 0;

            // Генерация гауссовой формулы
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    double exponent = -(x * x + y * y) / (2 * sigma * sigma);
                    double weight = Math.Exp(exponent) / (2 * Math.PI * sigma * sigma);

                    kernel[x + radius, y + radius] = weight;
                    kernelSum += weight;
                }
            }

            // Нормализация 
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    kernel[x, y] /= kernelSum;
                }
            }

            // Примениние фильтра
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double grayValue = 0;

                    for (int i = -radius; i <= radius; i++)
                    {
                        for (int j = -radius; j <= radius; j++)
                        {
                            int pixelX = x + i;
                            int pixelY = y + j;

                            if (pixelX >= 0 && pixelX < image.Width && pixelY >= 0 && pixelY < image.Height)
                            {
                                Color pixel = image.GetPixel(pixelX, pixelY);
                                double luminance = (pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                                grayValue += luminance * kernel[i + radius, j + radius];
                            }
                        }
                    }

                    Color filteredPixel = Color.FromArgb((int)grayValue, (int)grayValue, (int)grayValue);
                    filteredImage.SetPixel(x, y, filteredPixel);
                }
            }

            return filteredImage;
        }

    }
}
