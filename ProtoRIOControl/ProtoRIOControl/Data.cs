﻿using ProtoRIOControl.Localization;
using System;
namespace ProtoRIOControl {
    public static class InData {
        public const string batteryVIn = "RBV";           // The battery voltage
        public const string batteryIIn = "RBI";           // The battery current draw
        public const string batteryEnd = "X";             // The end of battery data

        public const string sensorARes1 = "R1A";         // The first result from sensor A
        public const string sensorARes2 = "R2A";         // The second result from sensor A
        public const string sensorAEnd = "Y";            // The end of sensor A data

        public const string sensorBRes1 = "R1B";         // The first result from sensor B
        public const string sensorBRes2 = "R2B";         // The second result from sensor B
        public const string sensorBEnd = "Z";            // The end of sensor B data
    }
    public static class OutData{
        public const string requestRead = "BTRD";        // Request a read of sensors and battery
        public const string sendPWMA = "PA";             // Set PWM A speed
        public const string sendPWMB = "PB";             // Set PWM B speed
        public const string sendSolenoid = "SO";         // Set Solenoid state:
        public const int solenoidAOn = 1;                   // A on B off
        public const int solenoidBOn = 2;                   // B on A off
        public const int bothSolenoidsOn = 3;               // A on B on
        public const int bothSolenoidsOff = 0;              // a off B off
        public const string sendSensorA = "SA";          // Configure the selected sensor for sensor A
        public const string sendSensorB = "SB";          // Configure the selected sensor for sensor B
        public const string sendSensorASetting = "EA";   // Configure the setting for sensor A
        public const string sendSensorBSetting = "EB";   // Configure the setting for sensor B
    }

    /// <summary>
    /// A type to hold all needed info for each sensor
    /// </summary>
    public class Sensor {
        public Sensor(int sensorType, string name, string firstUnit, string secondUnit, int? setting, string settingUnit, bool userSetting, string connection) {
            this.sensorType = sensorType;
            this.name = name;
            this.firstUnit = firstUnit;
            this.secondUnit = secondUnit;
            this.setting = setting;
            this.settingUnit = settingUnit;
            this.userSetting = userSetting;
            this.connection = connection;
        }
        public int sensorType { get; private set; }
        public string name { get; private set; }
        public string firstUnit { get; private set; }
        public string secondUnit { get; private set; }
        public int? setting { get; private set; }
        public string settingUnit { get; private set; }
        public bool userSetting { get; private set; }
        public string connection { get; private set; }
    }

    /* 
     * In the sensor connection descriptions _ and * and + are placeholders (see AppResources.Connect strings)
     * _ is the first DIO channel, * is the second DIO channel coresponding to the selected sensor
     * For sensor A chanel 1 is A and Channel 2 is B (DIOA and DIOB)
     * For sensor B chanel 1 is C and Chennel 2 is C (DIOC and DIOD)
     * + is a placeholder for the analog channel (A (ANA) for sensor A and B (ANAB) for sensor B) 
     * 
     * Example:
     * for the HC-SR04 the connection string is: "Trig:DIO_ Echo:DIO*"
     * if sensor A that would become "Trig:DIOA Echo:DIOB"
     * if sensor B that would become "Trig:DIOC Echo:DIOD"
     */
    public static class Sensors {

        public static string getConnectionText(string source, bool isA) {
            source.Replace('_', isA ? 'A' : 'C');
            source.Replace('*', isA ? 'B' : 'D');
            source.Replace('+', isA ? 'A' : 'B');
            return source;
        }

        public static Sensor none = new Sensor(0, AppResources.SensorNone, "", "", null, "", false, "");
        public static Sensor encoder = new Sensor(1, AppResources.SensorEncoder, AppResources.UnitTicks, AppResources.UnitRPM, null, AppResources.SettingTickPerRev, true, AppResources.ConnectEncoder);
        public readonly static Sensor hcsr04 = new Sensor(2, AppResources.SensorHCSR04, AppResources.UnitCentimeters, AppResources.UnitInches, null, "", false, AppResources.ConnectHCSR04);
        public readonly static Sensor mb1cm = new Sensor(3, AppResources.SensorMB1cm, AppResources.UnitCentimeters, AppResources.UnitInches, 1, AppResources.SettingCmPerCount, false, AppResources.ConnectAnalog);
        public readonly static Sensor mb2cm = new Sensor(4, AppResources.SensorMB2cm, AppResources.UnitCentimeters, AppResources.UnitInches, 2, AppResources.SettingCmPerCount, false, AppResources.ConnectAnalog);
        public readonly static Sensor sa60 = new Sensor(5, AppResources.SensorSA60, AppResources.UnitCentimeters, AppResources.UnitInches, 150, AppResources.SettingCmMax, false, AppResources.ConnectAnalog);
        public readonly static Sensor sa02 = new Sensor(6, AppResources.SensorSA02, AppResources.UnitCentimeters, AppResources.UnitInches, 150, AppResources.SettingCmMax, false, AppResources.ConnectAnalog);
        public readonly static Sensor sa21 = new Sensor(7, AppResources.SensorSA21, AppResources.UnitCentimeters, AppResources.UnitInches, 80, AppResources.SettingCmMax, false, AppResources.ConnectAnalog);
        public readonly static Sensor sa41 = new Sensor(8, AppResources.SensorSA41, AppResources.UnitCentimeters, AppResources.UnitInches, 30, AppResources.SettingCmMax, false, AppResources.ConnectAnalog);
        public readonly static Sensor sa51 = new Sensor(9, AppResources.SensorSA51, AppResources.UnitCentimeters, AppResources.UnitInches, 15, AppResources.SettingCmMax, false, AppResources.ConnectAnalog);
        public readonly static Sensor analog = new Sensor(10, AppResources.SensorAnalog, AppResources.UnitRaw, AppResources.UnitVolts, null, "", false, AppResources.ConnectAnalog);
        public readonly static Sensor vl53l0xtof = new Sensor(11, AppResources.SensorVL53L0XTOF, AppResources.UnitCentimeters, AppResources.UnitInches, null, "", false, AppResources.Connect5VI2C);
        public readonly static Sensor lidarlite = new Sensor(12, AppResources.SensorLidarLite, AppResources.UnitCentimeters, AppResources.UnitInches, null, "", false, AppResources.Connect5VI2C);
    }
}
