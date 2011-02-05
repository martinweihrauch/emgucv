using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.GPU;

namespace FaceDetection
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         if (!IsPlaformCompatable()) return;
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Run();
      }

      static void Run()
      {
         Image<Bgr, Byte> image = new Image<Bgr, byte>("lena.jpg"); //Read the files as an 8-bit Bgr image  

         Stopwatch watch; 
         String faceFileName = "haarcascade_frontalface_alt.xml";
         String eyeFileName = "haarcascade_eye.xml";

         if (GpuInvoke.HasCuda) 
         {
            using (GpuCascadeClassifier face = new GpuCascadeClassifier(faceFileName))
            using (GpuCascadeClassifier eye = new GpuCascadeClassifier(eyeFileName))
            using (GpuImage<Bgr, Byte> gpuImage = new GpuImage<Bgr,byte>(image))
            using (GpuImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
            {
               watch = Stopwatch.StartNew();
               Rectangle[] faceRegion = face.DetectMultiScale(gpuGray, 1.1, 10, Size.Empty);
               foreach(Rectangle region in faceRegion)
               {
                  //draw the face detected in the 0th (gray) channel with blue color
                  image.Draw(region, new Bgr(Color.Blue), 2);
               }
               watch.Stop();
            }
         }
         else
         {
            //Read the HaarCascade objects
            using(HaarCascade face = new HaarCascade(faceFileName))
            using(HaarCascade eye = new HaarCascade(eyeFileName))
            using (Image<Gray, Byte> gray = image.Convert<Gray, Byte>()) //Convert it to Grayscale
            {
               watch = Stopwatch.StartNew();

               //normalizes brightness and increases contrast of the image
               gray._EqualizeHist();          

               //Detect the faces  from the gray scale image and store the locations as rectangle
               //The first dimensional is the channel
               //The second dimension is the index of the rectangle in the specific channel
               MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                  face,
                  1.1,
                  10,
                  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                  new Size(20, 20));

               foreach (MCvAvgComp f in facesDetected[0])
               {
                  //draw the face detected in the 0th (gray) channel with blue color
                  image.Draw(f.rect, new Bgr(Color.Blue), 2);

                  //Set the region of interest on the faces
                  gray.ROI = f.rect;
                  MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                     eye,
                     1.1,
                     10,
                     Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                     new Size(20, 20));
                  gray.ROI = Rectangle.Empty;

                  foreach (MCvAvgComp e in eyesDetected[0])
                  {
                     Rectangle eyeRect = e.rect;
                     eyeRect.Offset(f.rect.X, f.rect.Y);
                     image.Draw(eyeRect, new Bgr(Color.Red), 2);
                  }
               }
               watch.Stop();
            }
         }

         //display the image 
         ImageViewer.Show(image, String.Format(
            "Completed face and eye detection using {0} in {1} milliseconds", 
            GpuInvoke.HasCuda ? "GPU": "CPU", 
            watch.ElapsedMilliseconds));
      }

      /// <summary>
      /// Check if both the managed and unmanaged code are compiled for the same architecture
      /// </summary>
      /// <returns>Returns true if both the managed and unmanaged code are compiled for the same architecture</returns>
      static bool IsPlaformCompatable()
      {
         int clrBitness = Marshal.SizeOf(typeof(IntPtr)) * 8;
         if (clrBitness != CvInvoke.UnmanagedCodeBitness)
         {
            MessageBox.Show(String.Format("Platform mismatched: CLR is {0} bit, C++ code is {1} bit."
               + " Please consider recompiling the executable with the same platform target as C++ code.",
               clrBitness, CvInvoke.UnmanagedCodeBitness));
            return false;
         }
         return true;
      }
   }
}