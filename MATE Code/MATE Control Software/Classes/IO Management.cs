using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Windows.Forms;
using Firmata.NET;


namespace Linda
{
    /// <summary>
    /// A Complex MultiThreading and Process aware class for use with the 2012 MATE Mainframe program.
    /// </summary>
    class IO_Management
    {
        public Sensors Sensors = new Sensors();
        private System.Windows.Forms.Timer MotorSmoothingTimer = new System.Windows.Forms.Timer();

        Thread UpdateIOThread;

        public bool SetNeutral = false;

        private bool AutoDive = false;
        private bool AutoDiveDirection = true;

        private double motorFUD1 = 0;
        private double motorBUD1 = 0;
        private double motorFUD2 = 0;
        private double motorBUD2 = 0;

        public double joy1X;
        public double joy1Y;
        public double joy1Rz;
        public double joy1S;
        public double joy1POV0;

        public double joy1deadzoneX;
        public double joy1deadzoneYa;
        public double joy1deadzoneYb;
        public double joy1deadzoneRz;
        public double joy1deadzoneS;

        public double joy2instance = 0;
        public double joy2X;
        public double joy2Y;
        public double joy2Rz;
        public double joy2S;
        public double joy2POV0;

        public double joy2deadzoneX;
        public double joy2deadzoneY;

        public bool SyringeIsOn = false;

        public double motorVerticalRatio = 0;

        public bool Autonomouse = false;

        public double objectsize = 0;

        public int objectX = 0;
        public int objectY = 0;

        public double objectXcentered = 0;
        public double objectYcentered = 0;

        public int xspeed = 30;
        public int yspeed = 30;

        public int visualdeadzone = 30;
        public int Yvisualdeadzone = 20;

        public double s1;
        public double s2;

        /// <summary>
        /// The common speed of all the motors were applicable.
        /// </summary>
        public double motorSPEED = 0;
        /// <summary>
        /// The neutrally buoyant Front Vertical motor.
        /// </summary>
        public double motorNBF = 0;
        /// <summary>
        /// The neutrally buoyant Back Vertical Motor.
        /// </summary>
        public double motorNBB = 0;
        /// <summary>
        /// The Front Left Motor.
        /// </summary>
        public double motorFL = 0;
        /// <summary>
        /// The Front Right Motor.
        /// </summary>
        public double motorFR = 0;
        /// <summary>
        /// The Back Left Motor.
        /// </summary>
        public double motorBL = 0;
        /// <summary>
        /// The Back Right Motor.
        /// </summary>
        public double motorBR = 0;
        /// <summary>
        /// The RAW Front Vertical Motor.
        /// </summary>
        public double motorFUD = 0;
        /// <summary>
        /// The RAW Back Vertical Motor.
        /// </summary>
        public double motorBUD = 0;



        /// <summary>
        /// The Rotation of the Arm.
        /// </summary>
        public double motorAR = 0;
        /// <summary>
        /// The Swing position of the ARM.
        /// </summary>
        public double motorAS = 0;
        /// <summary>
        /// The Gripper State of the Arm.
        /// </summary>
        public double motorAG = 0;

        /// <summary>
        /// The Syr... State
        /// </summary>
        public double motorSGO = 0;

        public bool UpdatePrimaryMotorsThread = false;
        public bool UpdateSecondaryMotorsThread = false;
        public bool arduinoIsConnected = false;

        public bool joy1IsConnected = false;
        public bool joy2IsConnected = false;

        public bool joy1IsLost = true;
        public bool joy2IsLost = true;

        public string SerialPortName = "Unknown";
        public const int SerialPortSpeed = 57600;


        Arduino arduino = new Arduino(SerialDivert(), SerialPortSpeed, false, 8000);
        /// <summary>
        /// Lists all available serial ports on current system.
        /// </summary>
        /// <returns>An array of strings containing all available serial ports.</returns>
        private static string[] list()
        {
            return SerialPort.GetPortNames();
        }

        private static string SerialDivert()
        {
            try
            {
                string port = (list().ElementAt(list().Length - 1));
                return port;
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("No Serial Devices Detected, Defaulting to COM1");
                EventLog.WriteEntry("2012 Mate", "No Serial Devices Detected, Defaulting to COM1", EventLogEntryType.Information);
                return ("COM1");
            }
        }


        private Linda.Greeter rmGreeter;
        public string RControl;



        ///<summary>
        /// Starts the new thread and configures firmata for use.
        ///</summary>
        public void Start()
        {
            try
            {
                string port = (list().ElementAt(list().Length - 1));
                SerialPortName = port;
            }
            catch (ArgumentOutOfRangeException)
            {
                SerialPortName = "COM1";
            }

            if (arduinoIsConnected == false)
            {
                arduino.Open();
                InitFirmata();
            }
            else
            {
                StopMotors(true, true);
            }

            UpdateIOThread = new Thread(UpdateIO);
            UpdateIOThread.Name = "Update IO Thread";
            UpdateIOThread.IsBackground = true;

            UpdateIOThread.Start();

            UpdatePrimaryMotorsThread = true;
            UpdateSecondaryMotorsThread = true;

            SyringeIsOn = false;
        }

        public IO_Management()
        {
ChannelServices.RegisterChannel((new TcpChannel(2274)),false);
            rmGreeter = new Greeter();
            ObjRef refGreeter = RemotingServices.Marshal(rmGreeter, "Greeter");
            rmGreeter.RespondTime = 500;
            RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
            rmGreeter.HelloEvent += new Linda.Greeter.HelloEventHandler(Server_HelloEvent);
        }

        ///<summary>
        /// Pauses the thread and stops all motors.
        ///</summary>
        public void Pause()
        {
            UpdatePrimaryMotorsThread = false;
            UpdateSecondaryMotorsThread = false;
            StopMotors(true, true);
        }

        ///<summary>
        /// Resumes the thread and starts the motors at 0.
        ///</summary>
        public void Resume()
        {
            UpdatePrimaryMotorsThread = true;
            UpdateSecondaryMotorsThread = true;
        }

        ///<summary>
        /// Stops the thread.
        ///</summary>
        public void Stop()
        {
            StopMotors(true, true);
            UpdateIOThread.Abort();
            arduino.Close();
        }
        public bool RConnected = false;
        private void Server_HelloEvent(object sender, Linda.HelloEventArgs e)
        {
            try
            {
                if (e.Name == "S0")
                {
                    RControl = "Remote Pause";
                    Pause();

                }
                else if (e.Name == "S1")
                {
                    RControl = "Remote Start";
                    Resume();
                }
                else if (e.Name == "P0")
                {
                    RControl = "Remote Exit";
                    Environment.Exit(0);
                }
                else if (e.Name == "H1")
                {
                    RControl = "Remote Connect to Hardware";
                    lock(this)
                    {
                        RConnected = true;
                    }
                }
                else if (e.Name == "H0")
                {
                    RControl = "Remote Disconnect to Hardware";
                    lock(this)
                    {
                    RConnected = false;
                    }
                }
            }
            catch { }
        }
        //private int compasso;
        private void UpdateIO()
        {
            while (true)
            {
                try
                {
                    if (Autonomouse)
                    {
                        CalculateX();
                        CalculateY();
                    }
                    else
                    {
                        #region Joystick 1 Calculations
                        if (joy1IsConnected == true)
                        {
                            if (joy1IsLost == false)
                            {
                                if (UpdatePrimaryMotorsThread == true)
                                {
                                    CalculateSjoy1();
                                    CalculateYajoy1();
                                    CalculateYbjoy1();

                                    motorFUD = ((motorFUD1 / 2) + (motorFUD2 / 2));
                                    motorBUD = ((motorBUD1 / 2) + (motorBUD2 / 2));

                                    if (SetNeutral == true)
                                    {
                                        FSetNeutral();
                                    }
                                    if (joy1POV0 == -1)
                                    {
                                        if (joy1Rz > (joy1deadzoneRz) || joy1Rz < (-joy1deadzoneRz))
                                        {
                                            CalculateRzjoy1();
                                        }
                                        else
                                        {
                                            CalculateXjoy1();
                                        }
                                    }
                                    else
                                    {
                                        CalculatePOVjoy1();
                                    }
                                }
                                else
                                {
                                    if (AutoDive == true)
                                    {
                                        if (AutoDiveDirection == true) //Going Up
                                        {
                                            FAutomatedDiveSurface(true);
                                        }
                                        else if (AutoDiveDirection == false)  //Going Down
                                        {
                                            FAutomatedDiveSurface(false);
                                        }
                                    }
                                    else
                                    {
                                        StopMotors(true, false);
                                    }
                                }
                            }
                            else
                            {
                                StopMotors(true, false);
                            }
                        }

                        #endregion

                        #region Joystick 2 Calculations
                        if (joy2IsConnected == true)
                        {
                            if (joy2IsLost == false)
                            {
                                if (UpdateSecondaryMotorsThread == true)
                                {
                                    FClaw();
                                    CalculatePOVjoy2();
                                    CalculateXjoy2();
                                    CalculateRzjoy2();
                                }
                            }
                            else
                            {
                                StopMotors(false, true);
                            }
                        }
                        #endregion
                    }
                    Thread.BeginCriticalRegion();
                    #region Arduino Communications
                    if (arduinoIsConnected == true)
                    {
                        arduino.analogWrite(2, (MotorMap(motorFL)));
                        arduino.analogWrite(3, (MotorMap(motorFR)));
                        arduino.analogWrite(4, (MotorMap(motorBL)));
                        arduino.analogWrite(5, (MotorMap(motorBR)));

                        arduino.analogWrite(0, (MotorMap(motorFUD)));
                        arduino.analogWrite(1, (MotorMap(motorBUD)));

                        arduino.analogWrite(12, (MotorMap(motorAG)));  //Connect to Arduino Pin 12
                        arduino.analogWrite(13, (MotorMap(motorAR)));  //Connect to Transistor H Bridge (Pin 13)
                        arduino.analogWrite(14, (MotorMap(motorAS))); //Connect to Arduino Pin 14 
                        arduino.analogWrite(15, (MotorMap(motorSGO))); //Connect to Arduino Pin 14 
                        //MessageBox.Show("Wrote");
                        
                        Sensors.RawPressureInput = (arduino.analogRead(1));
                        
                        Sensors.SetCompass =(arduino.analogRead(0));
                        /*
                        if (compasso < 0)
                        {
                            Sensors.SetCompass = (360 + compasso);
                        }
                        else
                        {
                            Sensors.SetCompass = compasso;
                        }
                         */
                    }
                    #endregion
                    Thread.EndCriticalRegion();

                    Thread.Yield();//Yield to the operating system
                }
                catch (ThreadAbortException)
                {
                    StopMotors(true, true);
                }
            }
        }

        /// <summary>
        /// Stops the Motors by setting there values to 0 and then sending that value to the Arduino.
        /// </summary>
        /// <param name="PrimaryMotors">Stops motorFL, motorFR, motorBL, motorBR, motorFUD, and motorBUD if true.</param>
        /// <param name="SecondaryMotors">Stops motorAG, motorAR, motorAS, motorSGO if true.</param>
        public void StopMotors(bool PrimaryMotors, bool SecondaryMotors)
        {
            lock (this)
            {
                if (PrimaryMotors == true && SecondaryMotors == true)
                {
                    motorFL = 0;
                    motorFR = 0;
                    motorBL = 0;
                    motorBR = 0;
                    motorFUD = 0;
                    motorBUD = 0;

                    motorAG = 0;
                    motorAR = 0;
                    motorAR = 0;
                    motorSGO = 0;
                    if (arduinoIsConnected == true)
                    {
                        arduino.analogWrite(0, (MotorMap(0)));  //connect to Controller 1, Channel 1
                        arduino.analogWrite(1, (MotorMap(0)));  //connect to Controller 1, Channel 2
                        arduino.analogWrite(2, (MotorMap(0)));  //connect to Controller 2, Channel 1
                        arduino.analogWrite(3, (MotorMap(0)));  //connect to Controller 2, Channel 2
                        arduino.analogWrite(4, (MotorMap(0)));  //connect to Controller 3, Channel 1
                        arduino.analogWrite(5, (MotorMap(0)));  //connect to Controller 3, Channel 2

                        arduino.analogWrite(12, (MotorMap(0)));  //Connect to Arduino Pin 12
                        arduino.analogWrite(13, (MotorMap(0)));  //Connect to Transistor H Bridge (Pin 13)
                        arduino.analogWrite(14, (MotorMap(0))); //Connect to Arduino Pin 14
                        arduino.analogWrite(15, (MotorMap(0)));

                        arduino.analogWrite(18, (MotorMap(0)));
                        arduino.analogWrite(19, (MotorMap(0)));
                    }
                    return;
                }
                else if (PrimaryMotors == true)
                {
                    motorFL = 0;
                    motorFR = 0;
                    motorBL = 0;
                    motorBR = 0;
                    motorFUD = 0;
                    motorBUD = 0;

                    if (arduinoIsConnected == true)
                    {
                        arduino.analogWrite(0, (MotorMap(0)));  //connect to Controller 1, Channel 1
                        arduino.analogWrite(1, (MotorMap(0)));  //connect to Controller 1, Channel 2
                        arduino.analogWrite(2, (MotorMap(0)));  //connect to Controller 2, Channel 1
                        arduino.analogWrite(3, (MotorMap(0)));  //connect to Controller 2, Channel 2

                        arduino.analogWrite(4, (MotorMap(0)));  //connect to Controller 3, Channel 1
                        arduino.analogWrite(5, (MotorMap(0)));  //connect to Controller 3, Channel 2
                    }
                    return;
                }
                else if (SecondaryMotors == true)
                {
                    motorAG = 0;
                    motorAR = 0;
                    motorAR = 0;
                    motorSGO = 0;

                    if (arduinoIsConnected == true)
                    {
                        arduino.analogWrite(12, (MotorMap(0)));  //Connect to Arduino Pin 12
                        arduino.analogWrite(13, (MotorMap(0)));  //Connect to Transistor H Bridge (Pin 13)
                        arduino.analogWrite(14, (MotorMap(0))); //Connect to Arduino Pin 14
                        arduino.analogWrite(15, (MotorMap(0)));

                        arduino.analogWrite(18, (MotorMap(0)));
                        arduino.analogWrite(19, (MotorMap(0)));
                    }
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        ///Controls if the Sub will automatically surface/submerge.  Defaults to GoingUp automatically when called.  Diving true/false is required.
        /// </summary>
        /// <param name="Diving">When true, the sub wil activate automatically to the second variable.</param>
        /// <param name="GoingUp">Controls whether the sub is GoingUp (true), or GoingDown (false)</param>
        public void AutomatedDive(bool Diving, bool GoingUp = true)
        {
            AutoDive = Diving;
            AutoDiveDirection = GoingUp;
            return;
        }

        /// <summary>
        /// Remaps a variable to a new scale.
        /// </summary>
        /// <param name="value">The value to be remapped.</param>
        /// <param name="newStart">The new start of the scale.</param>
        /// <param name="newEnd">THe new end of the scale.</param>
        /// <param name="originalStart">The original start of the scale.</param>
        /// <param name="originalEnd">The original end of the scale.</param>
        /// <returns></returns>
        public double Map(double value, double newStart, double newEnd, double originalStart, double originalEnd)
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (int)(newStart + ((value - originalStart) * scale));
        }

        public double Average(double D1, double D2)
        {
            return ((D1 + D2) / 2);
        }

        ///<summary>
        /// Controls the Joystick Update States by setting a variable in the library.  The motor thread looks for this variable when it updates.
        ///</summary>
        /// <param name="Pilot">When true, the state will be passed to the Pilot(joystick1)</param>
        /// <param name="CoPilot">When true, the state will be passed to the CoPilot(joystick2)</param>
        /// <param name="State">Controls the variable 'state' that is passed to on of the 2 joysticks.  When 'State' is false, the joystick will not update.  When it is true it will.</param>
        public void StationManegement(bool Pilot, bool CoPilot, bool State)
        {
            if (Pilot == true)
            {
                if (State == true)
                {
                    UpdateSecondaryMotorsThread = true;
                    return;
                }
                else if (State == false)
                {
                    UpdatePrimaryMotorsThread = false;
                    StopMotors(true, false);
                    return;
                }
            }
            else if (CoPilot == true)
            {
                if (State == true)
                {
                    UpdateSecondaryMotorsThread = true;
                    return;
                }
                else if (State == false)
                {
                    UpdateSecondaryMotorsThread = false;
                    StopMotors(false, true);
                    return;
                }
            }
        }

        public void ControlVerticals(bool Acting, double Speed = 100, bool HalfSpeed = false)
        {
            if (Acting == true)
            {
                    if (HalfSpeed == false)
                    {
                        motorVerticalRatio = -Speed;
                    }
                    else
                    {
                        motorVerticalRatio = (-Speed / 2);
                    }
            }
            else
            {
                motorVerticalRatio = 0;
            }
        }



        private void FSetNeutral()
        {
            motorNBF = motorFUD;
            motorNBB = motorBUD;
            return;
        }

        private void FClaw()
        {
            //motorAFB = 0;
            SyringeIsOn = false;
            if ((joy2Y > (joy2deadzoneY)) || (joy2Y < (-joy2deadzoneY)))//if Y is a positive or negative
            {
                motorAG = (-joy2Y);
                return;
            }
            else //if Y is in the deadzone
            {
                motorAG = 0;
                return;
            }
        }

        private void FAutomatedDiveSurface(bool GoingUP = true)
        {
            if (GoingUP == true)
            {
                motorFUD = 100;
                motorBUD = 100;
                return;
            }
            else if (GoingUP == false)
            {
                motorFUD = -100;
                motorBUD = -100;
                return;
            }
        }



        private void CalculateXjoy1()
        {

            if ((joy1X > (joy1deadzoneX)) || (joy1X < (-joy1deadzoneX))) //if X is a positive or negative
            {
                //make it up abd down
                motorFL = ((-joy1X) / 2);
                motorFR = ((joy1X) / 2);
                motorBL = (motorSPEED);
                motorBR = (motorSPEED);
                return;
            }
            else //if X is in the deadzone
            {
                motorFL = (motorSPEED);
                motorFR = (motorSPEED);
                motorBL = (motorSPEED);
                motorBR = (motorSPEED);
                return;
            }
        }

        /// <summary>
        /// Used for doing angular adjustments based off of the Y Axis
        /// </summary>
        private void CalculateYajoy1()
        {
            if ((joy1Y > (joy1deadzoneYa)) || (joy1Y < (-joy1deadzoneYa))) //if Y is a positive or negative
            {
                //make it up
                motorFUD1 = (-joy1Y);
                motorBUD1 = (joy1Y);
                return;
            }
            else //if Y is in the deadzone
            {
                //stop
                motorFUD1 = 0;
                motorBUD1 = 0;
                return;
            }
        }

        /// <summary>
        /// Used for going straigt up and down based off of the Y Axis
        /// </summary>
        private void CalculateYbjoy1()
        {
            motorFUD2 = motorVerticalRatio;
            motorBUD2 = motorVerticalRatio;
        }

        private void CalculateRzjoy1()
        {
            if ((joy1Rz > (joy1deadzoneRz)) || (joy1Rz < (-joy1deadzoneRz))) //if Rz is a positive or negative
            {
                //make it rotate right
                motorFL = (-joy1Rz);
                motorBL = (-joy1Rz);
                motorFR = (joy1Rz);
                motorBR = (joy1Rz);
                return;
            }
            else //if Rz is in the deadzone
            {
                //stop
                motorFL = 0;
                motorFR = 0;
                motorBL = 0;
                motorBR = 0;
                return;
            }
        }

        private void CalculateRzjoy2()
        {
            if ((joy2Rz > (joy1deadzoneRz)) || (joy2X < (joy1deadzoneRz)))//if X is a positive
            {
                motorAS = (joy2Rz);
                return;
            }
            else //if X is in the deadzone
            {
                //stop
                motorAS = 0;
                return;
            }
        }

        private void CalculateSjoy1()
        {
            if ((joy1S > (joy1deadzoneS)) || (joy1S < (-joy1deadzoneS))) //if S is a positive or negative
            {
                //make it go faster
                motorSPEED = (-joy1S);
                return;
            }
            else //if S is in the deadzone
            {
                //stop
                motorSPEED = 0;
                return;
            }
        }

        private void CalculatePOVjoy1()
        {
            switch (joy1POV0.ToString())
            {
                case "0":  //Up
                    {
                        motorFL = (motorSPEED);
                        motorFR = (motorSPEED);
                        motorBL = (motorSPEED);
                        motorBR = (motorSPEED);
                        break;
                    }
                case "4500": //Up Right
                    {
                        motorFL = motorSPEED;
                        motorFR = (motorSPEED / 2);
                        motorBL = (motorSPEED / 2);
                        motorBR = motorSPEED;
                        break;
                    }
                case "9000":  //Right
                    {
                        motorFL = -motorSPEED;
                        motorFR = motorSPEED;
                        motorBL = motorSPEED;
                        motorBR = -motorSPEED;
                        break;
                    }
                case "18000":  //Down
                    {
                        motorFL = (-motorSPEED);
                        motorFR = (-motorSPEED);
                        motorBL = (-motorSPEED);
                        motorBR = (-motorSPEED);
                        break;
                    }
                case "27000":  //Left
                    {
                        motorFL = motorSPEED;
                        motorFR = -motorSPEED;
                        motorBL = -motorSPEED;
                        motorBR = motorSPEED;
                        break;
                    }
                case "31500":  //Up Left
                    {
                        motorFL = (motorSPEED / 2);
                        motorFR = motorSPEED;
                        motorBL = motorSPEED;
                        motorBR = (motorSPEED / 2);
                        break;
                    }
                default: //else
                    {
                        motorFL = 0;
                        motorFR = 0;
                        motorBL = 0;
                        motorBR = 0;
                        break;
                    }
            }
            return;
        }

        private void CalculatePOVjoy2()
        {
            switch (joy2POV0.ToString())
            {
                case "0":  //Up
                    {
                        motorSGO = ((Map(joy2S, 0, 100, 100, -100)));
                        break;
                    }
                case "4500": //Up Right
                    {
                        //NOTHING
                        break;
                    }
                case "9000":  //Right
                    {
                        //motorAR = 50;
                        break;
                    }
                case "18000":  //Down
                    {
                        motorSGO = (-(Map(joy2S, 0, 100, 100, -100)));
                        break;
                    }
                case "27000":  //Left
                    {
                       // motorAR = -50;
                        break;
                    }
                case "31500":  //Up Left
                    {
                        //NOTHING
                        break;
                    }
                default: //else
                    {
                        motorAR = 0;
                        motorSGO = 0;
                        break;
                    }
            }
        }

        private void CalculateXjoy2()
        {
            if ((joy2X > (joy2deadzoneX)) || (joy2X < (-joy2deadzoneX))) //if Rz is a positive or negative
            {
                //make it rotate right
                motorAR = joy2X;
                return;
            }
            else //if Rz is in the deadzone
            {
                //stop
                motorAR = 0;
                return;
            }
        }

        private void CalculateX()
        {
                    if ((objectXcentered < (-visualdeadzone)) ||(objectXcentered > (visualdeadzone))) //the object is on the right or left
                    {
                        motorFL = xspeed;
                        motorFR = -xspeed;
                        motorBL = -xspeed;
                        motorBR = xspeed;
                    }
                    else// the object is centered
                    {
                        motorFL = 0;
                        motorFR = 0;
                        motorBL = 0;
                        motorBR = 0;
                        Thread.Sleep(750);
                    }
        }

        private void CalculateY()
        {
                    if (objectYcentered < (-Yvisualdeadzone)) //the object is backwards
                    {
                        motorFUD = (+yspeed);
                        motorBUD = (+yspeed);
                    }
                    else if (objectYcentered > (Yvisualdeadzone)) //the object is forwrds
                    {
                        motorBUD = (-yspeed);
                        motorBUD = (-yspeed);
                    }

                    else// the object is centered
                    {
                        motorFUD = 0;
                        motorBUD = 0;
                        Thread.Sleep(750);
                    }
        }

        private int MotorMap(double motor)
        {
            //double scale = (double)(newEnd - newStart) / (originalEnd - originalStart); 
            //return (int)(newStart + ((value - originalStart) * scale));
            double scale = (double)((180) - (0)) / ((100) - (-100));
            return (int)(0 + ((motor - (-100)) * scale));
        }

        private void InitFirmata()
        {
            try
            {
                if (arduinoIsConnected == false)
                {
                    arduino.pinMode(0, Arduino.OUTPUT);
                    arduino.pinMode(1, Arduino.OUTPUT);
                    arduino.pinMode(2, Arduino.OUTPUT);
                    arduino.pinMode(3, Arduino.OUTPUT);
                    arduino.pinMode(4, Arduino.OUTPUT);
                    arduino.pinMode(5, Arduino.OUTPUT);

                    arduino.pinMode(12, Arduino.OUTPUT);
                    arduino.pinMode(13, Arduino.OUTPUT);
                    arduino.pinMode(14, Arduino.OUTPUT);
                    arduino.pinMode(15, Arduino.OUTPUT);

                    arduino.pinMode(18, Arduino.OUTPUT);
                    arduino.pinMode(19, Arduino.OUTPUT);


                    arduino.pinMode(1, Arduino.INPUT);
                    arduino.pinMode(0, Arduino.INPUT);
                   
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("2012 Mate", (e.ToString()), EventLogEntryType.Error, 3101, 3);
                arduinoIsConnected = false;
                return;

            }


            arduinoIsConnected = true;
        }
    }
}

