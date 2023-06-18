using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private Bitmap previousFrame;
        private int frameWidth = 848; // Ширина кадра
        private int frameHeight = 464; // Высота кадра
        private int frameRate = 25; // Частота кадров в секунду
        //private Timer timer1;

        public Form1()
        {
            InitializeComponent();
            axWindowsMediaPlayer1.uiMode = "none";
            axWindowsMediaPlayer1.Dock = DockStyle.Fill;
            timer1.Interval = 1000 / frameRate; // Интервал в миллисекундах
            timer1.Tick += TimerTick; // Установка обработчика события таймера

        }



        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.pause();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox_path.Text))
            {
                axWindowsMediaPlayer1.URL = textBox_path.Text;
                axWindowsMediaPlayer1.Ctlcontrols.play();
            }
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
                axWindowsMediaPlayer1.URL = openFileDialog.FileName;
                textBox_path.Text = openFileDialog.FileName;
                axWindowsMediaPlayer1.Ctlcontrols.play();
                timer1.Start();
            }
        }

        private void ProcessVideo()
        {
            using (Bitmap videoFrame = new Bitmap(frameWidth, frameHeight))
            {
                axWindowsMediaPlayer1.DrawToBitmap(videoFrame, new Rectangle(0, 0, videoFrame.Width, videoFrame.Height));

                Bitmap grayFrame = ConvertToGrayscale(videoFrame);

                // Apply Gaussian filter to remove noise
                int filterRadius = 3; // Adjust as needed
                double filterSigma = 1.0; // Adjust as needed
                grayFrame = ApplyGaussianFilter(grayFrame, filterRadius, filterSigma);

                // Compute the difference between the current and previous frames
                if (previousFrame != null)
                {
                    Bitmap differenceFrame = ComputeFrameDifference(grayFrame, previousFrame);

                    // Binarization of the image
                    Bitmap binaryFrame = ApplyThresholding(differenceFrame, 15); // Set threshold as desired

                    // Apply morphological operations (e.g., closing, opening, or erosion) for noise removal and contour enhancement
                    Bitmap processedFrame = ApplyMorphologicalOperations(binaryFrame);

                    // Выделение контуров движущихся объектов
                    List<Rectangle> motionRectangles = FindMotion(processedFrame);

                    // Рисование прямоугольников вокруг контуров объектов
                    using (Graphics graphics = Graphics.FromImage(videoFrame))
                    {
                        foreach (Rectangle rectangle in motionRectangles)
                        {
                            graphics.DrawRectangle(Pens.Red, rectangle);
                        }
                    }

                    // Find motion on the contours


                    // Dispose intermediate frames
                    differenceFrame.Dispose();
                    binaryFrame.Dispose();
                    processedFrame.Dispose();
                }
                previousFrame?.Dispose();

                // Store the current frame as the previous frame for the next iteration
                previousFrame = grayFrame;
                axWindowsMediaPlayer1.Invalidate();
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

        private Bitmap ApplyGaussianFilter(Bitmap image, int radius, double sigma)
        {
            Bitmap filteredImage = new Bitmap(image.Width, image.Height);

            // Convert the image to a 2D matrix of grayscale values
            int[,] matrix = ConvertToMatrix(image);

            // Apply Gaussian filter
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    double sum = 0;
                    double weightSum = 0;

                    // Apply the Gaussian filter to the neighborhood of the current pixel
                    for (int j = -radius; j <= radius; j++)
                    {
                        for (int i = -radius; i <= radius; i++)
                        {
                            if (x + i >= 0 && x + i < image.Width && y + j >= 0 && y + j < image.Height)
                            {
                                double weight = Gaussian(i, j, sigma);
                                sum += matrix[y + j, x + i] * weight;
                                weightSum += weight;
                            }
                        }
                    }

                    // Normalize the filtered value and assign it to the corresponding pixel
                    int filteredValue = (int)Math.Round(sum / weightSum);
                    filteredImage.SetPixel(x, y, Color.FromArgb(filteredValue, filteredValue, filteredValue));
                }
            }

            return filteredImage;
        }

        private double Gaussian(int x, int y, double sigma)
        {
            return Math.Exp(-(x * x + y * y) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma);
        }

        private int[,] ConvertToMatrix(Bitmap image)
        {
            int[,] matrix = new int[image.Height, image.Width];

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int grayValue = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);
                    matrix[y, x] = grayValue;
                }
            }

            return matrix;
        }

        private Bitmap ComputeFrameDifference(Bitmap currentFrame, Bitmap previousFrame)
        {
            Bitmap differenceFrame = new Bitmap(currentFrame.Width, currentFrame.Height);

            for (int y = 0; y < currentFrame.Height; y++)
            {
                for (int x = 0; x < currentFrame.Width; x++)
                {
                    Color currentPixel = currentFrame.GetPixel(x, y);
                    Color previousPixel = previousFrame.GetPixel(x, y);

                    int diffR = Math.Abs(currentPixel.R - previousPixel.R);
                    int diffG = Math.Abs(currentPixel.G - previousPixel.G);
                    int diffB = Math.Abs(currentPixel.B - previousPixel.B);

                    int diffValue = (diffR + diffG + diffB) / 3;

                    differenceFrame.SetPixel(x, y, Color.FromArgb(diffValue, diffValue, diffValue));
                }
            }

            return differenceFrame;
        }

        private Bitmap ApplyThresholding(Bitmap image, int threshold)
        {
            Bitmap binaryImage = new Bitmap(image.Width, image.Height);

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    int grayValue = (int)(pixel.R * 0.299 + pixel.G * 0.587 + pixel.B * 0.114);

                    if (grayValue >= threshold)
                        binaryImage.SetPixel(x, y, Color.White);
                    else
                        binaryImage.SetPixel(x, y, Color.Black);
                }
            }

            return binaryImage;
        }

        private Bitmap ApplyMorphologicalOperations(Bitmap image)
        {
            Bitmap processedImage = new Bitmap(image.Width, image.Height);

            // Apply morphological operations (erosion and dilation) using a structuring element
            int[,] structuringElement = {
        { 1, 1, 1 },
        { 1, 1, 1 },
        { 1, 1, 1 }
    };

            // Erosion
            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    bool shouldErode = true;

                    // Check if all pixels in the neighborhood are white
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            if (structuringElement[j + 1, i + 1] == 1 && image.GetPixel(x + i, y + j).ToArgb() != Color.White.ToArgb())
                            {
                                shouldErode = false;
                                break;
                            }
                        }

                        if (!shouldErode)
                            break;
                    }

                    if (shouldErode)
                        processedImage.SetPixel(x, y, Color.Black);
                    else
                        processedImage.SetPixel(x, y, Color.White);
                }
            }

            // Dilation
            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    bool shouldDilate = false;

                    // Check if at least one pixel in the neighborhood is white
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            if (structuringElement[j + 1, i + 1] == 1 && image.GetPixel(x + i, y + j).ToArgb() == Color.White.ToArgb())
                            {
                                shouldDilate = true;
                                break;
                            }
                        }

                        if (shouldDilate)
                            break;
                    }

                    if (shouldDilate)
                        processedImage.SetPixel(x, y, Color.White);
                }
            }

            return processedImage;
        }

        private List<Rectangle> FindMotion(Bitmap binaryImage)
        {
            List<Rectangle> motionRectangles = new List<Rectangle>();

            int width = binaryImage.Width;
            int height = binaryImage.Height;

            bool[,] visited = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[x, y] && binaryImage.GetPixel(x, y) == Color.Black)
                    {
                        // Start a new motion region
                        Rectangle motionRegion = ExpandMotionRegion(binaryImage, visited, x, y);
                        motionRectangles.Add(motionRegion);
                    }
                }
            }

            return motionRectangles;
        }

        private Rectangle ExpandMotionRegion(Bitmap binaryImage, bool[,] visited, int startX, int startY)
        {
            int minX = startX;
            int maxX = startX;
            int minY = startY;
            int maxY = startY;

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                Point point = queue.Dequeue();
                int x = point.X;
                int y = point.Y;

                // Update min/max coordinates
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);

                // Check neighboring pixels
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;

                        if (IsValidPixel(binaryImage, visited, nx, ny) && binaryImage.GetPixel(nx, ny) == Color.Black)
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue(new Point(nx, ny));
                        }
                    }
                }
            }

            // Calculate the width and height of the motion region
            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            return new Rectangle(minX, minY, width, height);
        }

        private bool IsValidPixel(Bitmap image, bool[,] visited, int x, int y)
        {
            int width = image.Width;
            int height = image.Height;

            return x >= 0 && x < width && y >= 0 && y < height && !visited[x, y];
        }


        private void TimerTick(object sender, EventArgs e)
        {
            ProcessVideo();
        }
    }
}