using System;
using System.Windows.Forms;
namespace Linda
{
    /// <summary>
    /// The Sensor Class for controlling sensors.
    /// </summary>
    class Sensors
    {
        
        public double RawPressureInput; 
       
        
        public double GetCurrentPressure()
        {
            return (double)(((RawPressureInput / 4) + 10) * 0.145038);//Returns in PSI
        }


        /// <summary>
        /// Sets the Recorded Pressure
        /// </summary>
        public void SetRecordedPressure()
        {
            SetPressure = GetCurrentPressure();
        }

        /// <summary>
        /// Retrieves the Recorded Pressure
        /// </summary>
        /// <returns>The Recorded Pressure</returns>
        public double GetRecordedPressure()
        {
            return SetPressure;
        }


        /// <summary>
        /// Calculates the Density of a Liquid
        /// </summary>
        /// <param name="EmptyContainerWieght">The Empty Wieght of the Container Used for the Liquid</param>
        /// <param name="FilledContainerWieght">The Full Wieght of the Container Used for the Liquid</param>
        /// <param name="VolumeofFluid">The Amount of Liquid in the Container</param>
        /// <returns>Returns the Water Density</returns>
        public void SetWaterDensity(double EmptyContainerWieght, double FilledContainerWieght, double VolumeofFluid)
        {
            double MassofFluid = (EmptyContainerWieght - FilledContainerWieght);
            double Density = MassofFluid/VolumeofFluid;
            SetDensity = Density;
        }

        /// <summary>
        /// Gets the Density of the Liquid
        /// </summary>
        /// <returns></returns>
        public double GetWaterDensity()
        {
            return SetDensity;
        }


        /// <summary>
        /// Calculate Depth
        /// </summary>
        /// <returns>Depth</returns>
        public double CalculateDepth()
        {
            double Depth = GetCurrentPressure() / (SetDensity * MetricGravity);
            return Depth;
        } 



        private double SetDensity = 0;
        //private double SetAtmosphericPressure = 0;
        private double SetPressure = 0;
        private static double MetricGravity = 9.81;//Meters per Secondivate

        public double SetCompass = 0;

        public double GetCompass()
        {
            try
            {
                return SetCompass;
            }
            catch
            {
                
                MessageBox.Show("Compass Get Failure: Will Self Fix");
                return 0;
            }
        }
    }
}
