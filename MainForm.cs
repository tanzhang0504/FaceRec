
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace MultiFaceRec
{
    public partial class FrmPrincipal : Form
    {
        //Declararation of all variables, vectors and haarcascades
        Image<Bgr, Byte> currentFrame;
        Capture grabber;
        HaarCascade face;
        HaarCascade eye;
        VideoWriter writer = null;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> result, TrainedFace = null;
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels= new List<string>();
        List<string> NamePersons = new List<string>();
        int ContTrain, NumLabels, t;
        string name, names = null;

        // For video streaming
        TcpClient tcpClient = null;
        NetworkStream stream = null;

        // Log frame rate
        Timer timer = new Timer();
        int frame_cnt = 0;

        public FrmPrincipal()
        {
            InitializeComponent();
            this.Text = "Face Recognizer";
            //Load haarcascades for face detection
            face = new HaarCascade("haarcascade_frontalface_default.xml");
            //eye = new HaarCascade("haarcascade_eye.xml");
            try
            {
                //Load of previus trainned faces and labels for each image
                string Labelsinfo = File.ReadAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt");
                string[] Labels = Labelsinfo.Split('%');
                NumLabels = Convert.ToInt16(Labels[0]);
                ContTrain = NumLabels;
                string LoadFaces;

                for (int tf = 1; tf < NumLabels+1; tf++)
                {
                    LoadFaces = "face" + tf + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/TrainedFaces/" + LoadFaces));
                    labels.Add(Labels[tf]);
                }
            
            }
            catch(Exception e)
            {
                //MessageBox.Show(e.ToString());
                MessageBox.Show("Nothing in binary database, please add at least a face", "Triained faces load", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            // Timer to track frame rate
            timer.Interval = 1000;
            label_frame_cnt.Text = frame_cnt.ToString();
            label_capture_resolution.Text = string.Empty;
            label_stream_resolution.Text = string.Empty;
            timer.Tick += new EventHandler(timer_Tick);
 
        }

        void timer_Tick(object sender, EventArgs e) 
        {
            label_frame_cnt.Text = frame_cnt.ToString();
            //Debug.WriteLine("cnt: " + frame_cnt.ToString());
            frame_cnt = 0;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //Initialize the capture device            
            grabber = new Capture(0);
            double fps = grabber.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS);
            grabber.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH, 320);
            grabber.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT, 240);
            double width = grabber.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH);
            double height = grabber.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT);
            Debug.WriteLine("Capture fps: " + fps.ToString() + " width: " + width.ToString() + " height: " + height.ToString());
            timer.Start();
             //Initialize the FrameGraber event
                Application.Idle += new EventHandler(FrameGrabber);
                button1.Enabled = false;   
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            try
            {
                //Trained face counter
                ContTrain = ContTrain + 1;

                //Get a gray frame from capture device
                gray = grabber.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);

                //Face Detector
                MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                face,
                1.2,
                10,
                Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                new Size(20, 20));

                //Action for each element detected
                foreach (MCvAvgComp f in facesDetected[0])
                {
                    TrainedFace = currentFrame.Copy(f.rect).Convert<Gray, byte>();
                    break;
                }

                //resize face detected image for force to compare the same size with the 
                //test image with cubic interpolation type method
                TrainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                trainingImages.Add(TrainedFace);
                labels.Add(textBox1.Text);

                //Show face added in gray scale
                imageBox1.Image = TrainedFace;

                //Write the number of triained faces in a file text for further load
                File.WriteAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", trainingImages.ToArray().Length.ToString() + "%");

                //Write the labels of triained faces in a file text for further load
                for (int i = 1; i < trainingImages.ToArray().Length + 1; i++)
                {
                    trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/TrainedFaces/face" + i + ".bmp");
                    File.AppendAllText(Application.StartupPath + "/TrainedFaces/TrainedLabels.txt", labels.ToArray()[i - 1] + "%");
                }

                MessageBox.Show(textBox1.Text + "´s face detected and added!", "Training OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Enable the face detection first", "Training Fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        void FrameGrabber(object sender, EventArgs e)
        {
            label3.Text = "0";
            //label4.Text = "";
            NamePersons.Add("");

            //Get the current frame form capture device
            Image<Bgr, Byte> originalFrame = grabber.QueryFrame();
            //Debug.WriteLine("Original frame width: " + originalFrame.Width.ToString() + " height: " + originalFrame.Height.ToString());
            label_capture_resolution.Text = originalFrame.Width.ToString() + "X" + originalFrame.Height.ToString();
            currentFrame = originalFrame.Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                    //Convert it to Grayscale
                    gray = currentFrame.Convert<Gray, Byte>();

                    //Face Detector
                    MCvAvgComp[][] facesDetected = gray.DetectHaarCascade(
                  face,
                  1.2,
                  10,
                  Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                  new Size(20, 20));

                    //Action for each element detected
                    foreach (MCvAvgComp f in facesDetected[0])
                    {
                        t = t + 1;
                        result = currentFrame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                        //draw the face detected in the 0th (gray) channel with blue color
                        currentFrame.Draw(f.rect, new Bgr(Color.Red), 2);


                        if (trainingImages.ToArray().Length != 0)
                        {
                            //TermCriteria for face recognition with numbers of trained images like maxIteration
                        MCvTermCriteria termCrit = new MCvTermCriteria(ContTrain, 0.001);

                        //Eigen face recognizer
                        EigenObjectRecognizer recognizer = new EigenObjectRecognizer(
                           trainingImages.ToArray(),
                           labels.ToArray(),
                           3000,
                           ref termCrit);

                        name = recognizer.Recognize(result);

                        //Draw the label for each face detected and recognized
                        currentFrame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.LightGreen));

                        }

                        NamePersons[t-1] = name;
                        NamePersons.Add("");

                        //Set the number of faces detected on the scene
                        label3.Text = facesDetected[0].Length.ToString();
                       
                        /*
                        //Set the region of interest on the faces
                        
                        gray.ROI = f.rect;
                        MCvAvgComp[][] eyesDetected = gray.DetectHaarCascade(
                           eye,
                           1.1,
                           10,
                           Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                           new Size(20, 20));
                        gray.ROI = Rectangle.Empty;

                        foreach (MCvAvgComp ey in eyesDetected[0])
                        {
                            Rectangle eyeRect = ey.rect;
                            eyeRect.Offset(f.rect.X, f.rect.Y);
                            currentFrame.Draw(eyeRect, new Bgr(Color.Blue), 2);
                        }
                         */
                    }
                        t = 0;

                        //Names concatenation of persons recognized
                    for (int nnn = 0; nnn < facesDetected[0].Length; nnn++)
                    {
                        names = names + NamePersons[nnn] + ", ";
                    }
                    //Show the faces procesed and recognized
                    imageBoxFrameGrabber.Image = currentFrame;
                    label4.Text = names;
                    names = "";
                    if (writer != null)
                    {
                        writer.WriteFrame(currentFrame);
                    }
                    
                    // Stream the image
                    if (tcpClient != null)
                    {
                        Byte[] sendBytes = ConvertImageToByte(currentFrame);
                        try
                        {
                            //Debug.WriteLine("Size: [" + sendBytes.Length.ToString() + "]");
                            SendData(stream, sendBytes);
                            label_stream_resolution.Text = currentFrame.Width.ToString() + "X" + currentFrame.Height.ToString();
                        }
                        catch (Exception ex)
                        {
                            tcpClient.Close();
                            tcpClient = null;
                            MessageBox.Show("Server closes connection...");
                            Application.Idle -= new EventHandler(FrameGrabber);
                            button1.Enabled = true;  
                        }
                    }

                    //Clear the list(vector) of names
                    NamePersons.Clear();
                    frame_cnt++;
                }

        private void FrmPrincipal_Load(object sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //Set the filename to store video
            saveFileDialog1.Filter = "Video files (*.avi)|*.*";
            saveFileDialog1.DefaultExt = "avi";
            saveFileDialog1.InitialDirectory = @"..\..\";
            saveFileDialog1.AddExtension = true;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = saveFileDialog1.FileName;
                //MessageBox.Show("Save video to " + fileName);
                //Initialize video writer
                writer = new VideoWriter(fileName, -1, 10, 320, 240, true);
            }
            else 
            {
                MessageBox.Show("Save video error!");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string IP = Interaction.InputBox("Receiver IP: ", "IP Form");
            //MessageBox.Show("Receiver IP: " + IP);
            tcpClient = new TcpClient(IP, 12345);
            stream = tcpClient.GetStream();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (tcpClient != null) tcpClient.Close();
            this.Close();
        }

        private void saveFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private static byte[] ConvertImageToByte(Image<Bgr, Byte> image)
        {
            MemoryStream ms = new MemoryStream();
            Bitmap bmp = image.ToBitmap();           
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            Byte[] arr = ms.ToArray();
            ms.Dispose();
            bmp.Dispose();
            return arr;
        }
        private static void SendData(NetworkStream stream, Byte[] data)
        {
            byte[] datasize = new byte[4];
            datasize = BitConverter.GetBytes(data.Length);
            stream.Write(datasize, 0, datasize.Length);
            stream.Write(data, 0, data.Length);
        }

        private void label_frame_cnt_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

    }
}
