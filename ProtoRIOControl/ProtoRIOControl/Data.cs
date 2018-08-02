using System;
namespace ProtoRIOControl {
    public static class InData {
        public const string batteryVIn = "RBV";        // The battery voltage
        public const string batteryIIn = "RBI";        // The battery current draw
        public const string batteryEnd = "X";          // The end of battery data

        public const string sensorARes1 = "R1A";      // The first result from sensor A
        public const string sensorARes2 = "R2A";      // The second result from sensor A
        public const string sensorAEnd = "Y";         // The end of sensor A data

        public const string sensorBRes1 = "R1B";      // The first result from sensor A
        public const string sensorBRes2 = "R2B";      // The second result from sensor A
        public const string sensorBEnd = "Z";         // The end of sensor A data
    }
    public static class OutData{
        public const string requestRead = "BTRD";     // Request a read of sensors and battery
        public const string sendPWMA = "PA";          // Set PWM A speed
        public const string sendPWMB = "PB";          // Set PWM B speed
        public const string sendSolenoid = "SO";      // Set Solenoid state
        public const int solenoidAOn = 1;                // Solenoid A on B off
        public const int solenoidBOn = 2;                // Solenoid B on A off
        public const int bothSolenoidsOn = 3;            // Solenoid A on B on
        public const int bothSolenoidsOff = 0;           // Solenoid a off B off
    }
}
