using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
using Microsoft.DirectX.DirectInput;


namespace Linda
{
    public partial class MainForm : Form
    {
        //string StartOnConnect = @"C:\Program Files (x86)\Logitech\LWS\Webcam Software\CameraHelperShell.exe";

        private static SpeechSynthesizer speaker = new SpeechSynthesizer();

        //imaging variables
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        Help help;
        ChangeSetting settings;

        private int MaxMinutes = 15 - 1;
        private int elapsed_secs = 0;
        private int elapsed_mins = 0;

        public static JoystickState joy1state = new JoystickState();
        private Device joy1applicationDevice = null;
        public static int joy1numPOVs = 0;
        private int joy1SliderCount = 0;

        public static JoystickState joy2state = new JoystickState();
        private Device joy2applicationDevice = null;
        public static int joy2numPOVs = 0;
        private int joy2SliderCount = 0;

        private bool joy1buttonstick = true;
        private double joy2instance = 0;
        private bool joy2buttonstick = false;

        //private bool BlackBoxOn = false;

        private PerformanceCounter cpuCounter = new PerformanceCounter();
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        private IO_Management IO = new IO_Management();

        private bool SpeechRecognitionOn = false;

        //private bool authenticated = false;
        private string Authenticator = "System";

        //imaging variable
        //string d = "";
        EuclideanColorFiltering filter = new EuclideanColorFiltering();
        Color color = Color.Black;
        //GrayscaleBT709 grayscaleFilter = new GrayscaleBT709();
        GrayscaleBT709 grayscaleFilter = new GrayscaleBT709();
        BlobCounter blobCounter = new BlobCounter();
        int range = 120;

        public MainForm()
        {
            if (EventLog.SourceExists("2012 Mate")) { }
            else
            {
                try
                {
                    EventLog.CreateEventSource("2012 Mate", "2012 Mate Event Log");
                }
                catch
                {
                    MessageBox.Show("Event Log Creation Error");
                    Environment.Exit(0);
                }
                Thread.Sleep(10);
                EventLog.WriteEntry("2012 Mate", "New Log Created", EventLogEntryType.Information);
            }
            EventLog.WriteEntry("2012 Mate", "Application Started", EventLogEntryType.Information);

            speaker.Rate = 1;
            speaker.Volume = 0;
            speaker.Speak("Please Wait While I load the program.");
            EventLog.WriteEntry("2012 Mate", "Loading Application", EventLogEntryType.Information);

            EventLog.WriteEntry("2012 Mate", ("Authenticator: " + Authenticator), EventLogEntryType.Information);
            Authenticator = System.Environment.UserName;
            //authenticated = UserInSystemRole(WindowsBuiltInRole.Administrator);

            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);

            //InitSpeechRecognition();

            InitializeComponent();


            this.videoSourcePlayer1.NewFrame += new AForge.Controls.VideoSourcePlayer.NewFrameHandler(videoSourcePlayer1_NewFrame);
            this.videoSourcePlayer3.NewFrame += new AForge.Controls.VideoSourcePlayer.NewFrameHandler(videoSourcePlayer3_NewFrame);

            //begin image tracking
            blobCounter.MinWidth = 2;
            blobCounter.MinHeight = 2;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;

            Bitmap b = new Bitmap(320, 240);
            // Rectangle a = (Rectangle)r;
            Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
            Graphics g2 = Graphics.FromImage(b);
            pen1 = new Pen(Color.FromArgb(255, 0, 0), 3);
            g2.Clear(Color.White);
            g2.DrawLine(pen1, b.Width / 2, 0, b.Width / 2, b.Width);
            g2.DrawLine(pen1, b.Width, b.Height / 2, 0, b.Height / 2);
            lock (pictureBox1)
            {
                pictureBox1.Image = (System.Drawing.Image)b;
            }


            timerBlackBox.Start();


            EventLog.WriteEntry("2012 Mate", ("BlackBox Timer Started"), EventLogEntryType.Information);

            IO.Start();


        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            toolStripStatusLabelVersion.Text = this.ProductVersion.ToString();
            //ribbonLabelAuthenticator.Text = ("Authencticated: " + Authenticator);

            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";

            //ribbonLabelLogState.Text = ("Enable");

            ribbonButtonConnectHardware.Enabled = true;
            ribbonButtonBeginMatch.Enabled = true;
            ribbonButtonDisconnectHardware.Enabled = false;
            ribbonButtonEndMatch.Enabled = false;
            panelTimeRemaining.Visible = false;

            SpeechRecognitionOn = false;
            ribbonButtonSREnable.Enabled = true;
            ribbonButtonSRDisable.Enabled = false;

            ribbonButtonSDHigh.Enabled = false;
            ribbonButtonSDLow.Enabled = true;
            ribbonButtonSDMute.Enabled = true;

            GetCameras();

            IO.joy1deadzoneX = Properties.Settings.Default.joy1deadzoneX;
            IO.joy1deadzoneYa = Properties.Settings.Default.joy1deadzoneYa;
            IO.joy1deadzoneRz = Properties.Settings.Default.joy1deadzoneRz;
            IO.joy1deadzoneS = Properties.Settings.Default.joy1deadzoneS;
            IO.joy1deadzoneYb = Properties.Settings.Default.joy1deadzoneYb;

            IO.joy2deadzoneX = Properties.Settings.Default.joy2deadzoneX;
            IO.joy2deadzoneY = Properties.Settings.Default.joy2deadzoneY;
            toolStripProgressBar2.Visible = false;
            toolStripStatusLabel3.Visible = false;


            RefreshStatus();
            timerStatistics.Start();

            EventLog.WriteEntry("2012 Mate", "Loading Complete", EventLogEntryType.Information);
            speaker.SpeakAsync("Loading Complete.");
        }

        private void MainForm_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            EventLog.WriteEntry("2012 Mate", "Application Closing", EventLogEntryType.Information);
            if (ribbonButtonEndMatch.Enabled == true || ribbonButtonDisconnectHardware.Enabled == true)
            {
                // Display a MsgBox asking the user to save changes or abort.
                speaker.SpeakAsync("Are you sure you want to exit while the program is still active?");
                if (MessageBox.Show("Are you sure you want to exit while the program is still active?", "2012 MATE",
                   MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    // Cancel the Closing event from closing the form.
                    speaker.SpeakAsync("Closing Canceled");
                    EventLog.WriteEntry("2012 Mate", "Closing Canceled", EventLogEntryType.Information);

                    e.Cancel = true;
                }
                else
                {
                    try
                    {
                        EndMatch();
                        DisconnectHardware();
                        IO.Stop();
                        speaker.Speak("Closing Program");
                        timerBlackBox.Stop();
                        EventLog.WriteEntry("2012 Mate", ("BlackBox Timer Stoped"), EventLogEntryType.Information);
                        EventLog.WriteEntry("2012 Mate", "Closing Successful", EventLogEntryType.Information);
                    }
                    catch (Exception ce)
                    {
                        Debug.WriteLine(ce.ToString());
                        EventLog.WriteEntry("2012 Mate", "Closing Failure", EventLogEntryType.Error);
                    }
                }
            }
            else
            {
                try
                {
                    IO.Stop();
                    speaker.Speak("Closing Program");
                    timerBlackBox.Stop();
                    EventLog.WriteEntry("2012 Mate", ("BlackBox Timer Stoped"), EventLogEntryType.Information);
                    EventLog.WriteEntry("2012 Mate", "Closing Successful", EventLogEntryType.Information);
                }
                catch (Exception ce)
                {
                    Debug.WriteLine(ce.ToString());
                    EventLog.WriteEntry("2012 Mate", "Closing Failure", EventLogEntryType.Error); ;
                }
            }
        }

        #region Initializing Code

        private void GetCameras()
        {
            EventLog.WriteEntry("2012 Mate", "Getting Cameras on System", EventLogEntryType.Information);
            try
            {
                camerasCombo0.Items.Clear();
                camerasCombo1.Items.Clear();
                // enumerate video devices
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    camerasCombo0.Items.Add(device.Name);
                    camerasCombo1.Items.Add(device.Name);
                }

                camerasCombo0.SelectedIndex = 0;
                camerasCombo1.SelectedIndex = 1;
                EventLog.WriteEntry("2012 Mate", "Cameras Added to List", EventLogEntryType.Information);
            }
            catch (ApplicationException ce)
            {
                EventLog.WriteEntry("2012 Mate", ("No local capture devices found on System." + ce.ToString()), EventLogEntryType.Warning, 1100, 1);
                camerasCombo0.Items.Add("No local capture devices");
                camerasCombo1.Items.Add("No local capture devices");
                videoDevices = null;
            }
        }

        private void InitCamera(int WhichCamera)
        {
            if (WhichCamera == 0)
            {
                try
                {
                    EventLog.WriteEntry("2012 Mate", "Initializing Camera", EventLogEntryType.Information);
                    //connect the webcam
                    videoSourcePlayer1.SignalToStop();
                    videoSourcePlayer1.WaitForStop();
                    videoSourcePlayer2.SignalToStop();
                    videoSourcePlayer2.WaitForStop();
                    videoSourcePlayer3.SignalToStop();
                    videoSourcePlayer3.WaitForStop();
                    videoSource = new VideoCaptureDevice(videoDevices[camerasCombo0.SelectedIndex].MonikerString);
                    videoSource.DesiredFrameSize = new Size(320, 240);
                    videoSource.DesiredFrameRate = 4;

                    videoSourcePlayer1.VideoSource = videoSource;
                    videoSourcePlayer1.Start();
                    videoSourcePlayer2.VideoSource = videoSource;
                    videoSourcePlayer2.Start();
                    videoSourcePlayer3.VideoSource = videoSource;
                    videoSourcePlayer3.Start();
                    EventLog.WriteEntry("2012 Mate", "Camera Initialized Successful", EventLogEntryType.Information);
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 1101, 1);
                }
            }
            else if (WhichCamera == 1)
            {
                try
                {
                    EventLog.WriteEntry("2012 Mate", "Initializing Camera", EventLogEntryType.Information);
                    //connect the webcam

                    //videoSourcePlayer4.SignalToStop();
                    // videoSourcePlayer4.WaitForStop();

                    videoSource = new VideoCaptureDevice(videoDevices[camerasCombo1.SelectedIndex].MonikerString);
                    videoSource.DesiredFrameSize = new Size(320, 240);
                    videoSource.DesiredFrameRate = 12;

                    videoSourcePlayer4.VideoSource = videoSource;
                    videoSourcePlayer4.Start();

                    EventLog.WriteEntry("2012 Mate", "Camera Initialized Successful", EventLogEntryType.Information);
                }
                catch (Exception e)
                {
                    EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 1101, 1);
                }
            }
        }

        private bool InitDirectInputjoy1()
        {
            try
            {
                EventLog.WriteEntry("2012 Mate", "Initializing DirectInput for Joy 1", EventLogEntryType.Information);
                // Enumerate joysticks in the system.
                foreach (DeviceInstance instance in Microsoft.DirectX.DirectInput.Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
                {
                    // Create the device.  Just pick the first one
                    joy1applicationDevice = new Device(instance.InstanceGuid);
                    break;
                }

                if (null == joy1applicationDevice)
                {
                    EventLog.WriteEntry("2012 Mate", "Joy 1 Not Found", EventLogEntryType.Warning, 2100, 2);
                    IO.joy1IsConnected = false;
                    return false;
                }

                // Set the data format to the c_dfDIJoystick pre-defined format.
                joy1applicationDevice.SetDataFormat(DeviceDataFormat.Joystick);
                // Set the cooperative level for the device.
                joy1applicationDevice.SetCooperativeLevel(this, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
                // Enumerate all the objects on the device.
                foreach (DeviceObjectInstance d in joy1applicationDevice.Objects)
                {
                    // For axes that are returned, set the DIPROP_RANGE property for the
                    // enumerated axis in order to scale min/max values.

                    if ((0 != (d.ObjectId & (int)DeviceObjectTypeFlags.Axis)))
                    {
                        // Set the range for the axis.
                        joy1applicationDevice.Properties.SetRange(ParameterHow.ById, d.ObjectId, new InputRange(-1000, +1000));
                    }
                    // Update the controls to reflect what
                    // objects the device supports.
                    InitControlsjoy1(d);
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2101, 2);
                IO.joy1IsConnected = false;
                return false;
            }
            IO.joy1IsConnected = true;
            EventLog.WriteEntry("2012 Mate", "Joy 1 Connected", EventLogEntryType.Information);
            return true;
        }

        private bool InitDirectInputjoy2()
        {
            try
            {
                EventLog.WriteEntry("2012 Mate", "Initializing DirectInput for Joy 2", EventLogEntryType.Information);
                // Enumerate joysticks in the system.
                foreach (DeviceInstance instance in Microsoft.DirectX.DirectInput.Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
                {
                    if (joy2instance == 1)
                    {
                        // Create the device.  Just pick the first one
                        joy2applicationDevice = new Device(instance.InstanceGuid);
                        break;
                    }
                    joy2instance = 1;
                }

                joy2instance = 0;
                if (null == joy2applicationDevice)
                {
                    EventLog.WriteEntry("2012 Mate", "Joy 2 Not Found", EventLogEntryType.Warning, 2200, 2);
                    IO.joy2IsConnected = false;
                    return false;
                }
                // Set the data format to the c_dfDIJoystick pre-defined format.
                joy2applicationDevice.SetDataFormat(DeviceDataFormat.Joystick);
                // Set the cooperative level for the device.
                joy2applicationDevice.SetCooperativeLevel(this, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
                // Enumerate all the objects on the device.
                foreach (DeviceObjectInstance f in joy2applicationDevice.Objects)
                {
                    // For axes that are returned, set the DIPROP_RANGE property for the
                    // enumerated axis in order to scale min/max values.

                    if ((0 != (f.ObjectId & (int)DeviceObjectTypeFlags.Axis)))
                    {
                        // Set the range for the axis.
                        joy2applicationDevice.Properties.SetRange(ParameterHow.ById, f.ObjectId, new InputRange(-1000, +1000));
                    }
                    // Update the controls to reflect what
                    // objects the device supports.
                    InitControlsjoy2(f);
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2201, 2);
                IO.joy2IsConnected = false;
                return false;
            }
            IO.joy2IsConnected = true;
            EventLog.WriteEntry("2012 Mate", "Joy 2 Connected", EventLogEntryType.Information);
            return true;
        }

        private void InitControlsjoy1(DeviceObjectInstance d)
        {
            try
            {
                //EventLog.WriteEntry("2012 Mate", "Initializing Controls for Joy 1", EventLogEntryType.Information);
                // Set the UI to reflect what objects the joystick supports.
                if (ObjectTypeGuid.XAxis == d.ObjectType)
                {
                    labelXAxis.Enabled = true;
                    labelXAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.YAxis == d.ObjectType)
                {
                    labelYAxis.Enabled = true;
                    labelYAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.ZAxis == d.ObjectType)
                {
                    labelZAxis.Enabled = true;
                    labelZAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.RxAxis == d.ObjectType)
                {
                    labelXRotation.Enabled = true;
                    labelXRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.RyAxis == d.ObjectType)
                {
                    labelYRotation.Enabled = true;
                    labelYRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.RzAxis == d.ObjectType)
                {
                    labelZRotation.Enabled = true;
                    labelZRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.Slider == d.ObjectType)
                {
                    switch (joy1SliderCount++)
                    {
                        case 0:
                            labelSlider0.Enabled = true;
                            labelSlider0Text.Enabled = true;
                            break;

                        case 1:
                            labelSlider1.Enabled = true;
                            labelSlider1Text.Enabled = true;
                            break;
                    }
                }
                if (ObjectTypeGuid.PointOfView == d.ObjectType)
                {
                    switch (joy1numPOVs++)
                    {
                        case 0:
                            labelPOV0.Enabled = true;
                            labelPOV0Text.Enabled = true;
                            break;

                        case 1:
                            labelPOV1.Enabled = true;
                            labelPOV1Text.Enabled = true;
                            break;

                        case 2:
                            labelPOV2.Enabled = true;
                            labelPOV2Text.Enabled = true;
                            break;

                        case 3:
                            labelPOV3.Enabled = true;
                            labelPOV3Text.Enabled = true;
                            break;
                    }
                }
                //EventLog.WriteEntry("2012 Mate", "Joy 1 Control Initialization Successful", EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2102, 2);
            }
        }

        private void InitControlsjoy2(DeviceObjectInstance f)
        {
            try
            {
                //EventLog.WriteEntry("2012 Mate", "Initializing Controls for Joy 2", EventLogEntryType.Information);
                // Set the UI to reflect what objects the joystick supports.
                if (ObjectTypeGuid.XAxis == f.ObjectType)
                {
                    labeljoy2XAxis.Enabled = true;
                    labeljoy2XAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.YAxis == f.ObjectType)
                {
                    labeljoy2YAxis.Enabled = true;
                    labeljoy2YAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.ZAxis == f.ObjectType)
                {
                    labeljoy2ZAxis.Enabled = true;
                    labeljoy2ZAxisText.Enabled = true;
                }
                if (ObjectTypeGuid.RxAxis == f.ObjectType)
                {
                    labeljoy2XRotation.Enabled = true;
                    labeljoy2XRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.RyAxis == f.ObjectType)
                {
                    labeljoy2YRotation.Enabled = true;
                    labeljoy2YRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.RzAxis == f.ObjectType)
                {
                    labeljoy2ZRotation.Enabled = true;
                    labeljoy2ZRotationText.Enabled = true;
                }
                if (ObjectTypeGuid.Slider == f.ObjectType)
                {
                    switch (joy2SliderCount++)
                    {
                        case 0:
                            labeljoy2Slider0.Enabled = true;
                            labeljoy2Slider0Text.Enabled = true;
                            break;

                        case 1:
                            labeljoy2Slider1.Enabled = true;
                            labeljoy2Slider1Text.Enabled = true;
                            break;
                    }
                }
                if (ObjectTypeGuid.PointOfView == f.ObjectType)
                {
                    switch (joy2numPOVs++)
                    {
                        case 0:
                            labeljoy2POV0.Enabled = true;
                            labeljoy2POV0Text.Enabled = true;
                            break;

                        case 1:
                            labeljoy2POV1.Enabled = true;
                            labeljoy2POV1Text.Enabled = true;
                            break;

                        case 2:
                            labeljoy2POV2.Enabled = true;
                            labeljoy2POV2Text.Enabled = true;
                            break;

                        case 3:
                            labeljoy2POV3.Enabled = true;
                            labeljoy2POV3Text.Enabled = true;
                            break;
                    }
                }
                //EventLog.WriteEntry("2012 Mate", "Joy 2 Control Initialization Successful", EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2202, 2);
            }
        }

        //private void InitSpeechRecognition()
        //{
        //    PoliteRobot.Add("Hi");

        //    PoliteRobotGrammar = new Grammar(new GrammarBuilder(PoliteRobot));

        //    _recognizer.LoadGrammar(PoliteRobotGrammar);
        //}

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            elapsed_secs = elapsed_secs + 1;

            if (elapsed_secs > 59)
            {
                elapsed_mins = elapsed_mins + 1;
                elapsed_secs = 0;
            }

            labelminsleft.Text = ((MaxMinutes - elapsed_mins).ToString());
            labelsecsleft.Text = ((60 - elapsed_secs).ToString());
            toolStripProgressBar2.Value = ((MaxMinutes - elapsed_mins) * 60) + ((60 - elapsed_secs));

        }

        #region Starting Functions

        private void ConnectHardware()
        {


            speaker.SpeakAsync("Now Connecting to Hardware");
            EventLog.WriteEntry("2012 Mate", "Connecting to Hardware", EventLogEntryType.Information);
            IO.Resume();
            toolStripProgressBar1.Value = (40);

            InitCamera(0);
            InitCamera(1);
            toolStripProgressBar1.Value = (50);

            //joystick code
            InitDirectInputjoy1();
            toolStripProgressBar1.Value = (60);


            InitDirectInputjoy2();
            toolStripProgressBar1.Value = (70);

            joy1buttonstick = true;

            IO.StationManegement(true, false, false);
            tableLayoutPanel1.Enabled = false;

            joy2buttonstick = true;

            IO.StationManegement(false, true, false);
            tableLayoutPanel2.Enabled = false;

            timerUpdateJoystick.Start();
            toolStripProgressBar1.Value = (100);
            EventLog.WriteEntry("2012 Mate", "Connection Successful", EventLogEntryType.Information);
            speaker.SpeakAsync("Connection successful");
        }

        private void DisconnectHardware()
        {
            speaker.SpeakAsync("Now Disconnecting Hardware");
            EventLog.WriteEntry("2012 Mate", "Disconnecting Hardware", EventLogEntryType.Information);
            IO.Pause();

            toolStripProgressBar1.Value = 75;

            try
            {
                //discounnect the webcam
                videoSourcePlayer2.SignalToStop();
                videoSourcePlayer2.WaitForStop();

                videoSourcePlayer4.SignalToStop();
                videoSourcePlayer4.WaitForStop();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 1102, 1);
            }

            //disconnect the joystick
            timerUpdateJoystick.Stop();

            // Unacquire all DirectInput objects.
            try
            {
                if (IO.joy1IsConnected == true)
                {
                    if (null != joy1applicationDevice)
                        joy1applicationDevice.Unacquire();
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2105, 2);
            }

            try
            {
                if (IO.joy2IsConnected == true)
                {
                    if (null != joy2applicationDevice)
                        joy2applicationDevice.Unacquire();
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2205, 2);
            }

            toolStripProgressBar1.Value = (50);

            //reset the labels
            labelXAxis.Enabled = false;
            labelXAxisText.Enabled = false;
            labelYAxis.Enabled = false;
            labelYAxisText.Enabled = false;
            labelZAxis.Enabled = false;
            labelZAxisText.Enabled = false;
            labelXRotation.Enabled = false;
            labelXRotationText.Enabled = false;
            labelYRotation.Enabled = false;
            labelYRotationText.Enabled = false;
            labelZRotation.Enabled = false;
            labelZRotationText.Enabled = false;
            labelSlider0.Enabled = false;
            labelSlider0Text.Enabled = false;
            labelSlider1.Enabled = false;
            labelSlider1Text.Enabled = false;
            labelPOV0.Enabled = false;
            labelPOV0Text.Enabled = false;
            labelPOV1.Enabled = false;
            labelPOV1Text.Enabled = false;
            labelPOV2.Enabled = false;
            labelPOV2Text.Enabled = false;
            labelPOV3.Enabled = false;
            labelPOV3Text.Enabled = false;


            //reset the labels
            labeljoy2XAxis.Enabled = false;
            labeljoy2XAxisText.Enabled = false;
            labeljoy2YAxis.Enabled = false;
            labeljoy2YAxisText.Enabled = false;
            labeljoy2ZAxis.Enabled = false;
            labeljoy2ZAxisText.Enabled = false;
            labeljoy2XRotation.Enabled = false;
            labeljoy2XRotationText.Enabled = false;
            labeljoy2YRotation.Enabled = false;
            labeljoy2YRotationText.Enabled = false;
            labeljoy2ZRotation.Enabled = false;
            labeljoy2ZRotationText.Enabled = false;
            labeljoy2Slider0.Enabled = false;
            labeljoy2Slider0Text.Enabled = false;
            labeljoy2Slider1.Enabled = false;
            labeljoy2Slider1Text.Enabled = false;
            labeljoy2POV0.Enabled = false;
            labeljoy2POV0Text.Enabled = false;
            labeljoy2POV1.Enabled = false;
            labeljoy2POV1Text.Enabled = false;
            labeljoy2POV2.Enabled = false;
            labeljoy2POV2Text.Enabled = false;
            labeljoy2POV3.Enabled = false;
            labeljoy2POV3Text.Enabled = false;


            toolStripProgressBar1.Value = (10);

            IO.StopMotors(true, true);

            labelFL.Text = ("Front Left");
            labelFR.Text = ("Front Right");
            labelBL.Text = ("Back Left");
            labelBR.Text = ("Back Right");
            labelFUD.Text = ("Front Up/Down");
            labelBUD.Text = ("Back Up/Down");
            labelmotorSPEED.Text = ("Motor Speed");
            aGauge1.Value = 0;

            labelAG.Text = ("Arm Gripper");
            labelAR.Text = ("Arm Rotation");
            labelAS.Text = ("Arm Swing");
            labelSGO.Text = ("Syringe Move");

            labelSucker.Text = ("Syringe");

            labelPressureAtTaken.Text = ("Pressure @ T");
            labelPressure.Text = ("Pressure @ C");

            labelCompass.Text = ("Compass");

            toolStripProgressBar1.Value = (0);

            EventLog.WriteEntry("2012 Mate", "Disconnection Successful", EventLogEntryType.Information);
            speaker.SpeakAsync("Disconnection successful");
        }

        private void BeginMatch()
        {
            speaker.SpeakAsync("Begining Match");
            timerMatchTime.Start();
            panelTimeRemaining.Visible = true;
            toolStripProgressBar2.Visible = true;
            toolStripStatusLabel3.Visible = true;
            EventLog.WriteEntry("2012 Mate", "Beginning Match", EventLogEntryType.Information);
        }

        private void EndMatch()
        {
            speaker.SpeakAsync("Ending Match");
            timerMatchTime.Stop();
            panelTimeRemaining.Visible = false;
            toolStripProgressBar2.Visible = false;
            toolStripStatusLabel3.Visible = false;
            MaxMinutes = 15 - 1;
            elapsed_secs = 0;
            elapsed_mins = 0;
            EventLog.WriteEntry("2012 Mate", "Ending Match", EventLogEntryType.Information);
        }

        #endregion

        #region Polling Code

        private void timerUpdateJoystick_Tick(object sender, EventArgs e)
        {
            try
            {
                if (IO.joy1IsConnected)
                {
                    GetDatajoy1();// polls the joystick for new data
                }
                if (IO.joy2IsConnected)
                {
                    GetDatajoy2();
                }
                UpdateControlLabels();
            }
            catch (Exception f)
            {
                MessageBox.Show("Exception Caught on Joystick Update: {0}", f.ToString());
            }
        }

        public void GetDatajoy1()
        {
            // Make sure there is a valid device.
            if (null == joy1applicationDevice)
                return;

            try
            {
                // Poll the device for info.
                joy1applicationDevice.Poll();
                IO.joy1IsLost = false;
            }
            catch (InputException inputex)
            {
                if ((inputex is NotAcquiredException) || (inputex is InputLostException))
                {
                    // Check to see if either the app
                    // needs to acquire the device, or
                    // if the app lost the device to another
                    // process.
                    try
                    {
                        // Acquire the device.
                        joy1applicationDevice.Acquire();
                        IO.joy1IsLost = false;
                    }
                    catch (InputException e)
                    {
                        // Failed to acquire the device.
                        // This could be because the app
                        // doesn't have focus.
                        EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Warning, 2103, 2);
                        IO.joy1IsLost = true;
                        return;
                    }

                }
                else
                {
                    IO.joy1IsLost = true;
                }

            } //catch(InputException inputex)

            // Get the state of the device.
            try { joy1state = joy1applicationDevice.CurrentJoystickState; }
            // Catch any exceptions. None will be handled here, 
            // any device re-aquisition will be handled above.  
            catch (InputException)
            {
                return;
            }
            UpdateUIjoy1();
        }

        public void GetDatajoy2()
        {
            // Make sure there is a valid device.
            if (null == joy2applicationDevice)
                return;

            try
            {
                // Poll the device for info.
                joy2applicationDevice.Poll();
                IO.joy2IsLost = false;
            }
            catch (InputException inputex)
            {
                if ((inputex is NotAcquiredException) || (inputex is InputLostException))
                {
                    // Check to see if either the app
                    // needs to acquire the device, or
                    // if the app lost the device to another
                    // process.
                    try
                    {
                        // Acquire the device.
                        joy2applicationDevice.Acquire();
                        IO.joy2IsLost = false;
                    }
                    catch (InputException e)
                    {
                        // doesn't have focus.
                        EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Warning, 1203, 1);
                        IO.joy2IsLost = true;
                        return;
                    }
                }
                else
                {
                    IO.joy2IsLost = true;
                }

            } //catch(InputException inputex)

            // Get the state of the device.
            try { joy2state = joy2applicationDevice.CurrentJoystickState; }
            // Catch any exceptions. None will be handled here, 
            // any device re-aquisition will be handled above.  
            catch (InputException)
            {
                return;
            }
            UpdateUIjoy2();
        }

        private void UpdateUIjoy1()
        {
            try
            {
                // This function updated the UI with joystick state information.
                string joy1strText = null;

                // ** Read/Get the joystick values 
                // ** Divide by 100 gives values from -10 to +10
                IO.joy1Y = joy1state.Y / -10;
                IO.joy1X = joy1state.X / 10;
                IO.joy1Rz = joy1state.Rz / 10;


                //Update GUI
                labelXAxis.Text = joy1state.X.ToString();
                labelYAxis.Text = joy1state.Y.ToString();
                labelZAxis.Text = joy1state.Z.ToString();

                labelXRotation.Text = joy1state.Rx.ToString();
                labelYRotation.Text = joy1state.Ry.ToString();
                labelZRotation.Text = joy1state.Rz.ToString();


                int[] joy1slider = joy1state.GetSlider();

                //my code
                IO.joy1S = ((joy1slider[0]) / 10);

                labelSlider0.Text = joy1slider[0].ToString();
                labelSlider1.Text = joy1slider[1].ToString();

                int[] joy1pov = joy1state.GetPointOfView();

                labelPOV0.Text = joy1pov[0].ToString();
                //mycode
                IO.joy1POV0 = (joy1pov[0]);

                labelPOV1.Text = joy1pov[1].ToString();
                labelPOV2.Text = joy1pov[2].ToString();
                labelPOV3.Text = joy1pov[3].ToString();

                // Fill up text with which buttons are pressed
                byte[] joy1buttons = joy1state.GetButtons();

                int joy1button = 0;
                foreach (byte b in joy1buttons)
                {
                    if (0 != (b & 0x80))
                        joy1strText += joy1button.ToString("00 ");
                    joy1button++;
                }
                labeljoy1Buttons.Text = joy1strText;

                CheckButtonsjoy1();

            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2104, 2);
            }
        }

        private void UpdateUIjoy2()
        {
            try
            {
                // This function updated the UI with joystick state information.
                string joy2strText = null;

                // ** Read/Get the joystick values 
                // ** Divide by 100 gives values from -10 to +10
                IO.joy2Y = joy2state.Y / -10;
                IO.joy2X = joy2state.X / 10;
                IO.joy2Rz = joy2state.Rz / 10;


                //Update GUI
                labeljoy2XAxis.Text = joy2state.X.ToString();
                labeljoy2YAxis.Text = joy2state.Y.ToString();
                labeljoy2ZAxis.Text = joy2state.Z.ToString();

                labeljoy2XRotation.Text = joy2state.Rx.ToString();
                labeljoy2YRotation.Text = joy2state.Ry.ToString();
                labeljoy2ZRotation.Text = joy2state.Rz.ToString();


                int[] joy2slider = joy2state.GetSlider();

                //my code
                IO.joy2S = ((joy2slider[0]) / 10);

                labeljoy2Slider0.Text = joy2slider[0].ToString();
                labeljoy2Slider1.Text = joy2slider[1].ToString();

                int[] joy2pov = joy2state.GetPointOfView();

                labeljoy2POV0.Text = joy2pov[0].ToString();
                //mycode
                IO.joy2POV0 = (joy2pov[0]);

                labeljoy2POV1.Text = joy2pov[1].ToString();
                labeljoy2POV2.Text = joy2pov[2].ToString();
                labeljoy2POV3.Text = joy2pov[3].ToString();

                // Fill up text with which buttons are pressed
                byte[] joy2buttons = joy2state.GetButtons();

                int joy2button = 0;
                foreach (byte b in joy2buttons)
                {
                    if (0 != (b & 0x80))
                        joy2strText += joy2button.ToString("00 ");
                    joy2button++;
                }
                labeljoy2Buttons.Text = joy2strText;

                CheckButtonsjoy2();
                //CheckButtons();

            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 2204, 2);
            }
        }

        private void CheckButtonsjoy1()
        {
            switch (labeljoy1Buttons.Text)
            {
                case "00 ":
                    {
                        //joy1buttonstick = false;
                        //IO.SetNeutral = true;
                        IO.ControlVerticals(true, -100);
                        return;
                    }
                case "01 ":  //Button 2- MicroMoving Left and Right
                    {
                        //if (IO.joy2X > (IO.joy2deadzoneX)) //if X is a positive
                        //{
                        //    IO.motorFL = ((IO.joy2X) / 2);
                        //    IO.motorFR = ((-IO.joy2X) / 2);
                        //    IO.motorBL = ((-IO.joy2X) / 2);
                        //    IO.motorBR = ((IO.joy2X) / 2);
                        //    break;
                        //}
                        //else if (IO.joy2X < (IO.joy2deadzoneX)) //if X is a negative
                        //{
                        //    IO.motorFL = ((IO.joy2X) / 2);
                        //    IO.motorFR = ((-IO.joy2X) / 2);
                        //    IO.motorBL = ((-IO.joy2X) / 2);
                        //    IO.motorBR = ((IO.joy2X) / 2);
                        //    break;
                        //}
                        //else //if X is in the deadzone
                        //{
                        //    //stop
                        //    IO.motorFL = 0;
                        //    IO.motorFR = 0;
                        //    IO.motorBL = 0;
                        //    IO.motorBR = 0;
                        //    break;
                        //}
                        IO.ControlVerticals(true, 100);
                        return;
                    }
                case "02 ":  //Button 3- Pause Program
                    {
                        joy1buttonstick = true;

                        IO.StationManegement(true, false, false);
                        tableLayoutPanel1.Enabled = false;
                        /*
                        labelFL.Enabled = false;
                        labelFR.Enabled = false;
                        labelBL.Enabled = false;
                        labelBR.Enabled = false;
                        labelFUD.Enabled = false;
                        labelBUD.Enabled = false;
                        labelmotorSPEED.Enabled = false;
                        */
                        break;
                    }
                case "03 ":  //Button 4- Resume Program
                    {

                        joy1buttonstick = false;
                        /*
                        labelFL.Enabled = true;
                        labelFR.Enabled = true;
                        labelBL.Enabled = true;
                        labelBR.Enabled = true;
                        labelFUD.Enabled = true;
                        labelBUD.Enabled = true;

                        labelmotorSPEED.Enabled = true;
                        */
                        tableLayoutPanel1.Enabled = true;
                        IO.StationManegement(true, false, true);

                        break;
                    }
                case "04 ":
                    {
                        checkBoxAutonomouseMode.Checked = false;
                        break;
                    }
                case "05 ":
                    {
                        checkBoxAutonomouseMode.Checked = true;
                        break;
                    }
                case "06 ":
                    {
                        IO.ControlVerticals(true, -100);
                        return;
                    }
                case "10 ":
                    {
                        IO.ControlVerticals(true, 100);
                        return;
                    }
                case "06 08 ":
                    {
                        IO.ControlVerticals(true, 100, true);
                        return;
                    }
                case "08 10 ":
                    {
                        IO.ControlVerticals(true, -100, true);
                        return;
                    }
                case "07 ":  //Button 8- Automated Dive (Only Works When Program Disabled)
                    {
                        joy1buttonstick = true;
                        if (IO.UpdatePrimaryMotorsThread == false)
                        {
                            lock (this)
                            {
                                labelFUD.Enabled = true;
                                labelBUD.Enabled = true;
                                IO.AutomatedDive(true, false);
                            }
                        }
                        break;
                    }
                case "09 ":  //Button 10- Automated Controls Stop (Only Works When Program Diabled)
                    {
                        lock (this)
                        {
                            labelFUD.Enabled = true;
                            labelBUD.Enabled = true;
                            IO.AutomatedDive(false);
                        }
                        break;
                    }
                case "11 ":  //Button 12- Automated Surface (Only Works When Program Diabled)
                    {
                        joy1buttonstick = true;
                        if (IO.UpdatePrimaryMotorsThread == false)
                        {
                            lock (this)
                            {
                                labelFUD.Enabled = true;
                                labelBUD.Enabled = true;
                                IO.AutomatedDive(true, true);
                            }
                        }
                        break;
                    }
                default:
                    {
                        if (joy1buttonstick == false)
                        {
                            IO.AutomatedDive(false, true);

                            joy1buttonstick = true;
                            labelFL.Enabled = true;
                            labelFR.Enabled = true;
                            labelBL.Enabled = true;
                            labelBR.Enabled = true;
                            labelFUD.Enabled = true;
                            labelBUD.Enabled = true;
                            labelmotorSPEED.Enabled = true;

                            if (IO.UpdatePrimaryMotorsThread == false)
                            {
                                IO.UpdatePrimaryMotorsThread = true;
                            }

                            IO.SetNeutral = false;
                        }
                        IO.ControlVerticals(false);
                        break;
                    }
            }
        }

        private void CheckButtonsjoy2()
        {
            switch (labeljoy2Buttons.Text)
            {
                case "02 ":
                    {
                        joy2buttonstick = true;

                        IO.StationManegement(false, true, false);
                        tableLayoutPanel2.Enabled = false;
                        /*
                        labelAG.Enabled = false;
                        labelAR.Enabled = false;
                        labelAS.Enabled = false;
                        labelSGO.Enabled = false;
                        labelSucker.Enabled = false;
                        */
                        break;
                    }
                case "03 ":
                    {
                        joy2buttonstick = false;
                        /*
                        labelAG.Enabled = true;
                        labelAR.Enabled = true;//
                        labelAS.Enabled = true;//
                        labelSGO.Enabled = true;//
                        labelSucker.Enabled = true;//
                        */
                        tableLayoutPanel2.Enabled = true;
                        IO.StationManegement(false, true, true);
                        break;
                    }
                default:
                    {
                        if (joy2buttonstick == false)
                        {
                            labelAS.Enabled = true;
                            labelAG.Enabled = true; //changed to true?
                            //IO.motorAG = 0;
                            IO.UpdateSecondaryMotorsThread = true;
                        }
                        break;
                    }
            }

            if (joy2buttonstick == false)
            {
                if (labeljoy2Buttons.Text == "00 " || labeljoy2Buttons.Text == "00 01 ")
                {
                    IO.SyringeIsOn = true;
                    IO.Sensors.SetRecordedPressure();
                }
                else
                {
                    IO.SyringeIsOn = false;
                }
            }
        }

        private void UpdateControlLabels()
        {
            //update the motors
            labelFL.Text = (IO.motorFL.ToString());
            labelFR.Text = (IO.motorFR.ToString());
            labelBL.Text = (IO.motorBL.ToString());
            labelBR.Text = (IO.motorBR.ToString());
            labelFUD.Text = (IO.motorFUD.ToString());
            labelBUD.Text = (IO.motorBUD.ToString());
            labelmotorSPEED.Text = (IO.motorSPEED.ToString());
            aGauge1.Value = Convert.ToSingle(IO.motorSPEED);

            labelAG.Text = (IO.motorAG.ToString());
            labelAR.Text = (IO.motorAR.ToString());
            labelAS.Text = (IO.motorAS.ToString());
            labelSGO.Text = (IO.motorSGO.ToString());

            aGauge3.Value = Convert.ToSingle(IO.joy1Rz);

            labelPressure.Text = ((IO.Sensors.GetCurrentPressure()).ToString());
            labelPressureAtTaken.Text = ((IO.Sensors.GetRecordedPressure()).ToString());

            if (IO.SyringeIsOn == true)
            {
                labelSucker.Text = ("Syringe is On");
            }
            else
            {
                labelSucker.Text = ("Syringe is Off");
            }
            if (IO.arduinoIsConnected == true)
            {
                labelArduinoConnected.Text = ("Connected: " + (IO.SerialPortName));
            }
            else
            {
                labelArduinoConnected.Text = ("Not Connected");
            }
            try
            {
                toolStripStatusLabel1.Text = IO.RControl;
            }
            catch { }
            labelCompass.Text = IO.Sensors.GetCompass().ToString();
            aGauge2.Value = Convert.ToSingle(IO.Sensors.GetCompass());
        }

        #endregion

        #region Logging

        private void LogAll()
        {
            LogRobot();
            LogCaptains();
            LogSystem();
        }

        private void LogRobot()
        {
            EventLog.WriteEntry("2012 Mate", "logging All Information: " + "FL: " + IO.motorFL.ToString() + " FR: " + IO.motorFR.ToString() + " BL: "
          + IO.motorBL.ToString() + " BR: " + IO.motorBR.ToString() + " FUD: " + IO.motorFUD.ToString() + " BUD: " + IO.motorBUD.ToString() + " motorSPEED: "
          + IO.motorSPEED.ToString() + " AG: " + IO.motorAG.ToString() + " AR " + IO.motorAR.ToString() + " AS: " + IO.motorAS.ToString() + " Current Pressure: "
          + (IO.Sensors.GetCurrentPressure().ToString()) + " Pressure at Taken: " + ((IO.Sensors.GetRecordedPressure()).ToString()) + " Syringe Is On: "
          + IO.SyringeIsOn.ToString() + " ArduinoIsConneced: " + IO.arduinoIsConnected.ToString() + " Joy1X: " + IO.joy1X.ToString() + " Joy1Y: " + IO.joy1Y.ToString() + " Joy1Rz: "
          + IO.joy1Rz.ToString() + " Joy1Slider: " + IO.joy1S.ToString() + " Joy2X: " + IO.joy2X.ToString() + " Joy2Y: " + IO.joy2Y.ToString() + " Joy1Buttons: "
          + labeljoy1Buttons.Text.ToString() + " Joy2Buttons: " + labeljoy1Buttons.Text.ToString() + " Joy1POV: " + labelPOV0.Text.ToString() + " Joy2POV: "
          + labeljoy2POV0.Text.ToString(), EventLogEntryType.Information);
        }

        private void LogCaptains()
        {
            EventLog.WriteEntry("2012 Mate", ("Captain's Log: " + richTextBox1.Text), EventLogEntryType.Information);
        }

        private void LogSystem()
        {
            EventLog.WriteEntry("2012 Mate", ("Charge Remaining: " + labelChargeRemaining.Text + " Battery Status: " + labelBatteryStatus.Text + " CPU %: "
                + labelCPU.Text + " RAM: " + labelRAM.Text), EventLogEntryType.Information);
        }

        private void timerBlackBox_Tick(object sender, EventArgs e)
        {
            LogAll();
        }

        #endregion

        #region System Statistics

        private void timerStatistics_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
            if (IO.RConnected)
            {
                if (IO.RConnected == ribbonButtonConnectHardware.Enabled)
                {
                    ribbonButtonConnectHardware.Enabled = false;
                    ribbonButtonDisconnectHardware.Enabled = true;
                    buttonESTOP.Enabled = true;
                    ConnectHardware();
                    this.BackgroundImage = Linda.Properties.Resources.blue_circuit_board;
                }
            }
            else
            {
                if (IO.RConnected == ribbonButtonConnectHardware.Enabled)
                {
                    ribbonButtonConnectHardware.Enabled = true;
                    ribbonButtonDisconnectHardware.Enabled = false;
                    buttonESTOP.Enabled = false;
                    DisconnectHardware();
                    this.BackgroundImage = Linda.Properties.Resources.blue_circuit_board;
                }
            }
        }

        private void RefreshStatus()
        {
            PowerStatus power = SystemInformation.PowerStatus;

            switch (power.PowerLineStatus)
            {
                case PowerLineStatus.Online:
                    checkBoxMainsPower.Checked = true;
                    break;

                case PowerLineStatus.Offline:
                    checkBoxMainsPower.Checked = false;
                    break;

                case PowerLineStatus.Unknown:
                    checkBoxMainsPower.CheckState = CheckState.Indeterminate;
                    break;
            }

            int powerPercent = (int)(power.BatteryLifePercent * 100);
            if (powerPercent <= 100)
                progressBarBatteryIndicator.Value = powerPercent;
            else
                progressBarBatteryIndicator.Value = 0;

            int secondsRemaining = power.BatteryLifeRemaining;
            if (secondsRemaining >= 0)
                labelChargeRemaining.Text = string.Format("{0} min", secondsRemaining / 60);
            else
                labelChargeRemaining.Text = string.Empty;

            labelBatteryStatus.Text = power.BatteryChargeStatus.ToString();

            labelCPU.Text = getCurrentCpuUsage();
            labelRAM.Text = getAvailableRAM();
        }

        private string getCurrentCpuUsage()
        {
            return cpuCounter.NextValue() + " %";
        }

        private string getAvailableRAM()
        {
            return ramCounter.NextValue() + " Mb";
        }

        #endregion

        #region Saftey Function
        bool IsNotStopped = true;

        private void buttonESTOP_Click(object sender, System.EventArgs e)
        {
            if (IsNotStopped)
            {
                IO.Pause();
                buttonESTOP.ForeColor = System.Drawing.Color.White;
                buttonESTOP.BackColor = System.Drawing.Color.Crimson;
                this.BackColor = System.Drawing.Color.LightPink;
                IsNotStopped = false;
            }
            else
            {
                IO.Resume();
                buttonESTOP.ForeColor = System.Drawing.Color.Crimson;
                buttonESTOP.BackColor = System.Drawing.SystemColors.Control;
                this.BackColor = System.Drawing.SystemColors.Control;
                IsNotStopped = true;
            }
        }

        #endregion

        #region Ribbon

        private void ribbonButtonSettings_Click(object sender, EventArgs e)
        {
            settings = new ChangeSetting();
            settings.Show();
        }

        private void ribbonButtonLookforSettings_Click(object sender, EventArgs e)
        {
            timerUpdateJoystick.Stop();
            IO.joy1deadzoneX = Properties.Settings.Default.joy1deadzoneX;
            IO.joy1deadzoneYa = Properties.Settings.Default.joy1deadzoneYa;
            IO.joy1deadzoneRz = Properties.Settings.Default.joy1deadzoneRz;
            IO.joy1deadzoneS = Properties.Settings.Default.joy1deadzoneS;
            IO.joy1deadzoneYb = Properties.Settings.Default.joy1deadzoneYb;

            IO.joy2deadzoneX = Properties.Settings.Default.joy2deadzoneX;
            IO.joy2deadzoneY = Properties.Settings.Default.joy2deadzoneY;
            timerUpdateJoystick.Start();
        }

        private void ribbonButtonJoystck_Click(object sender, EventArgs e)
        {
            if (IO.joy1IsConnected == true)
            {
                joy1applicationDevice.RunControlPanel();
            }
            else
            {
                speaker.SpeakAsync("No Joystick to Configure, Cannot Run");
            }
        }

        private void ribbonButtonWebcamSettings_Click(object sender, EventArgs e)
        {
            try
            {
                videoSource.DisplayPropertyPage(IntPtr.Zero);
                EventLog.WriteEntry("2012 Mate", "Dispalying Camera Properties Page", EventLogEntryType.Information);
            }
            catch
            {
                speaker.SpeakAsync("No Webcam to Configure, Cannot Run");
            }
        }

        private void ribbonButtonConnectHardware_Click(object sender, EventArgs e)
        {
            ribbonButtonConnectHardware.Enabled = false;
            ribbonButtonDisconnectHardware.Enabled = true;
            IO.RConnected = true;
            buttonESTOP.Enabled = true;
            ConnectHardware();
        }

        private void ribbonButtonDisconnectHardware_Click(object sender, EventArgs e)
        {
            ribbonButtonConnectHardware.Enabled = true;
            ribbonButtonDisconnectHardware.Enabled = false;
            IO.RConnected = false;
            buttonESTOP.Enabled = false;
            DisconnectHardware();
        }

        private void ribbonButtonBeginMatch_Click(object sender, EventArgs e)
        {
            ribbonButtonBeginMatch.Enabled = false;
            ribbonButtonEndMatch.Enabled = true;
            if (ribbonButtonDisconnectHardware.Enabled == false)
            {
                ribbonButtonConnectHardware.Enabled = false;
                ribbonButtonDisconnectHardware.Enabled = true;
                ConnectHardware();
            }

            BeginMatch();
        }

        private void ribbonButtonEndMatch_Click(object sender, EventArgs e)
        {
            ribbonButtonBeginMatch.Enabled = true;
            ribbonButtonEndMatch.Enabled = false;
            EndMatch();
        }

        private void ribbonButtonHelp_Click(object sender, EventArgs e)
        {
            help = new Help();
            help.Show();
        }

        private void ribbonButtonSearchforHardware_Click(object sender, EventArgs e)
        {
            speaker.SpeakAsync("Now Searching for Hardware");
            GetCameras();
            timerUpdateJoystick.Stop();
            if (IO.joy1IsConnected == false)
            {
                InitDirectInputjoy1();
            }
            if (IO.joy2IsConnected == false)
            {
                InitDirectInputjoy2();
            }
            timerUpdateJoystick.Start();
        }

        private void ribbonButtonLogAll_Click(object sender, EventArgs e)
        {
            LogAll();
        }

        private void ribbonButtonLogRobot_Click(object sender, EventArgs e)
        {
            LogRobot();
        }

        private void ribbonButtonLogCaptainsLog_Click(object sender, EventArgs e)
        {
            LogCaptains();
        }

        private void ribbonButtonLogSystemStatistics_Click(object sender, EventArgs e)
        {
            LogSystem();
        }

        private void ribbonButtonOpenLog_Click(object sender, EventArgs e)
        {
            Process LogOpen = new Process();
            LogOpen.StartInfo.FileName = "eventvwr.msc";
            LogOpen.Start();
        }

        private void ribbonLabelLogState_Click(object sender, EventArgs e)
        {
            //if (ribbonLabelLogState.Text == ("Enable"))
            //{
            //    BlackBoxOn = true;
            //    EventLog.WriteEntry("2012 Mate", ("BlackBox Logging Started"), EventLogEntryType.Information);
            //    ribbonLabelLogState.Text = ("Disable");
            //}
            //else
            //{
            //    BlackBoxOn = false;
            //    EventLog.WriteEntry("2012 Mate", ("BlackBox Logging Stoped"), EventLogEntryType.Information);
            //    ribbonLabelLogState.Text = ("Enable");
            //}
        }

        private void ribbonButtonSDHigh_Click(object sender, EventArgs e)
        {
            speaker.Volume = 75;
            ribbonButtonSDHigh.Enabled = false;
            ribbonButtonSDLow.Enabled = true;
            ribbonButtonSDMute.Enabled = true;
        }

        private void ribbonButtonSDLow_Click(object sender, EventArgs e)
        {
            speaker.Volume = 50;
            ribbonButtonSDHigh.Enabled = true;
            ribbonButtonSDLow.Enabled = false;
            ribbonButtonSDMute.Enabled = true;
        }

        private void ribbonButtonSDMute_Click(object sender, EventArgs e)
        {
            speaker.Volume = 0;
            ribbonButtonSDHigh.Enabled = true;
            ribbonButtonSDLow.Enabled = true;
            ribbonButtonSDMute.Enabled = false;
        }

        private void ribbonButtonSREnable_Click(object sender, EventArgs e)
        {
            if (SpeechRecognitionOn == false)
            {
                SpeechRecognitionOn = true;
                //PoliteRobotGrammar.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(PoliteRobotGrammar_SpeechRecognized);
                ribbonButtonSREnable.Enabled = false;
                ribbonButtonSRDisable.Enabled = true;
            }
        }

        private void ribbonButtonSRDisable_Click(object sender, EventArgs e)
        {
            if (SpeechRecognitionOn == true)
            {
                SpeechRecognitionOn = false;
                //PoliteRobotGrammar.SpeechRecognized -= new EventHandler<SpeechRecognizedEventArgs>(PoliteRobotGrammar_SpeechRecognized);
                ribbonButtonSREnable.Enabled = true;
                ribbonButtonSRDisable.Enabled = false;
            }
        }

        #endregion

        private void searchForToolStripMenuItem_Click(object sender, EventArgs e)
        {
            speaker.SpeakAsync("Now Searching for Webcam Hardware");
            GetCameras();
        }

        #region Autonomous

        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap image)
        {
            if (IO.Autonomouse)
            {
                Bitmap objectsImage = null;
                Bitmap mImage = null;
                mImage = (Bitmap)image.Clone();
                filter.CenterColor = new RGB(Color.FromArgb(color.ToArgb()));
                filter.Radius = (short)range;

                objectsImage = image;
                filter.ApplyInPlace(objectsImage);

                BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, image.PixelFormat);
                UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));
                objectsImage.UnlockBits(objectsData);


                blobCounter.ProcessImage(grayImage);
                // Rectangle[] rects = blobCounter;
                // Rectangle[] rects = blobCounter.GetObjectRectangles();
                Rectangle[] rects = blobCounter.GetObjectsRectangles();
                if (rects.Length > 0)
                {

                    foreach (Rectangle objectRect in rects)
                    {
                        Graphics g = Graphics.FromImage(mImage);
                        using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 5))
                        {
                            g.DrawRectangle(pen, objectRect);
                        }

                        g.Dispose();
                    }
                }

                image = mImage;
                Thread.Yield();
            }
        }

        private void videoSourcePlayer3_NewFrame(object sender, ref Bitmap image)
        {
            if (IO.Autonomouse)
            {
                Bitmap objectsImage = null;


                // set center colol and radius
                filter.CenterColor = new RGB(Color.FromArgb(color.ToArgb()));
                filter.Radius = (short)range;
                // apply the filter
                objectsImage = image;
                filter.ApplyInPlace(image);

                // lock image for further processing
                BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, image.PixelFormat);

                // grayscaling
                UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));

                // unlock image
                objectsImage.UnlockBits(objectsData);

                // locate blobs 
                blobCounter.ProcessImage(grayImage);
                //Rectangle[] rects = blobCounter.GetObjectRectangles();
                Rectangle[] rects = blobCounter.GetObjectsRectangles();

                if (rects.Length > 0)
                {
                    Rectangle objectRect = rects[0];

                    // draw rectangle around derected object
                    Graphics g = Graphics.FromImage(image);

                    using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 5))
                    {
                        g.DrawRectangle(pen, objectRect);
                    }
                    g.Dispose();

                    IO.objectX = objectRect.X + objectRect.Width / 2 - image.Width / 2;
                    IO.objectY = image.Height / 2 - (objectRect.Y + objectRect.Height / 2);

                    if (IO.objectX >= 160)
                    {
                        IO.objectXcentered = (IO.objectX - 160);
                    }
                    else if (IO.objectX <= 160)
                    {
                        IO.objectXcentered = (0 - IO.objectX);
                    }
                    else
                    {
                        IO.objectXcentered = 0;
                    }



                    if (IO.objectsize >= 120)
                    {
                        IO.objectYcentered = (IO.objectsize - 120);
                    }
                    else if (IO.objectsize <= 120)
                    {
                        IO.objectYcentered = (0 - IO.objectsize);
                    }
                    else
                    {
                        IO.objectYcentered = 0;
                    }


                    IO.s1 = objectRect.Width;
                    IO.s2 = objectRect.Height;

                    ParameterizedThreadStart t = new ParameterizedThreadStart(p);
                    Thread aa = new Thread(t);
                    aa.Start(rects[0]);
                }
                Graphics g1 = Graphics.FromImage(image);
                Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
                g1.DrawLine(pen1, image.Width / 2, 0, image.Width / 2, image.Width);
                g1.DrawLine(pen1, image.Width, image.Height / 2, 0, image.Height / 2);
                g1.Dispose();

                Thread.Yield();
            }
        }

        void p(object r)
        {
            if (IO.Autonomouse)
            {
                try
                {
                    lock (pictureBox1)
                    {
                        Bitmap b = new Bitmap(pictureBox1.Image);

                        Rectangle a = (Rectangle)r;
                        Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
                        Graphics g2 = Graphics.FromImage(b);
                        pen1 = new Pen(color, 3);
                        // Brush b5 = null;
                        SolidBrush b5 = new SolidBrush(color);
                        //   g2.Clear(Color.Black);

                        Font f = new Font(Font, FontStyle.Bold);

                        g2.DrawString("o", f, b5, a.Location);
                        g2.Dispose();

                        pictureBox1.Image = (System.Drawing.Image)b;

                        this.Invoke((MethodInvoker)delegate
                        {
                            richTextBoxTrackingHistory.Text = a.Location.ToString() + "\n" + richTextBoxTrackingHistory.Text + "\n"; ;
                        });
                    }
                }
                catch (Exception)
                {
                    Thread.CurrentThread.Abort();
                }
                Thread.CurrentThread.Abort();
            }
        }

        private void checkBoxAutonomouseMode_CheckedChanged(object sender, EventArgs e)
        {
            IO.Autonomouse = checkBoxAutonomouseMode.Checked;
        }

        private void buttonTrackingColor_Click(object sender, EventArgs e)
        {
            colorDialog.ShowDialog();
            color = colorDialog.Color;
        }

        private void numericUpDownRange_ValueChanged(object sender, EventArgs e)
        {
            range = Convert.ToInt32(numericUpDownRange.Value);
        }

        private void numericUpDownMinHieght_ValueChanged(object sender, EventArgs e)
        {
            blobCounter.MaxWidth = Convert.ToInt32(numericUpDownMinHieght.Value);
        }

        private void numericUpDownMinWidth_ValueChanged(object sender, EventArgs e)
        {
            blobCounter.MinWidth = Convert.ToInt32(numericUpDownMinWidth.Value);
        }
        #endregion

        private void camerasCombo0_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (camerasCombo0.SelectedItem == camerasCombo1.SelectedItem)
            {
                try
                {
                    camerasCombo1.SelectedIndex = (camerasCombo1.SelectedIndex + 1);
                }
                catch
                {
                    camerasCombo1.SelectedIndex = (camerasCombo1.SelectedIndex - 1);
                }
            }
        }

        private void ExitButton(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void camerasCombo1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (camerasCombo1.SelectedItem == camerasCombo0.SelectedItem)
            {
                try
                {
                    camerasCombo0.SelectedIndex = (camerasCombo0.SelectedIndex + 1);
                }
                catch
                {
                    camerasCombo0.SelectedIndex = (camerasCombo0.SelectedIndex - 1);
                }
            }
        }

    }
}