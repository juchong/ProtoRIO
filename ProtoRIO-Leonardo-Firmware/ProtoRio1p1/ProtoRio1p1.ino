/*
 * Code for ProtoRio with Bluetooth module 
 * ProtoRIO Rev 1.1: Leonardo using LTC2947 
 * Written by Bill Walter
 * Working 3/5/2018
 */
 
#include <stdint.h>
#include "LT_I2C.h"
#include "UserInterface.h"
#include "LTC2947.h"
#include <VL53L0X.h>
#include <Servo.h>
#include <Wire.h>
#include <math.h> 
#include <avr/pgmspace.h>

String SData="";                     // string to store incoming bluetooth data
char CData;                          // For reading serial data 1 byte at a time

VL53L0X TOFsensor;                   // create VL53L0X time of flight object 
Servo myservoA;                      // create servo object to control a PWM A
#define PWMAMax 2000                 // Maximum PWM A Pulse width value  (max 2400)
#define PWMAMin 960                  // Minimum PWM A Pulse width value (min 550)
Servo myservoB;                      // create servo object to control a PWM B
#define PWMBMax 2000                 // Maximum PWM B Pulse width value (max 2400)
#define PWMBMin 960                  // Minimum PWM B Pulse width value (min 550)

#define LTC2947_ADDRESS     0x5C     // Default I2C Address of LTC2947.
#define LIDARLite_ADDRESS   0x62     // Default I2C Address of LIDAR-Lite.
#define RegisterMeasure     0x00     // Register to write to initiate ranging.
#define MeasureValue        0x04     // Value to initiate ranging.
#define RegisterHighLowB    0x8f     // Register to get both High and Low bytes in 1 call.
#define AlertPin 4                   // Alert Pin from LTC2947 
#define PWMAPin 5                    // PWM A Pin 
#define PWMBPin 6                    // PWM B Pin 
#define DIOAPin 7                    // Digital I/O Pin A 
#define DIOBPin 8                    // Digital I/O Pin B  
#define DIOCPin 9                    // Digital I/O Pin C  
#define DIODPin 10                   // Digital I/O Pin D  
#define SolenoidB 11                 // Solenoid Pin B SW2
#define SolenoidA 13                 // Solenoid Pin A SW1
#define AnalogInAPin 2               // Anaolg sensor A input Pin 
#define AnalogInBPin 3               // Anaolg sensor B input Pin 
#define BatVPin 5                    // Analog Battery voltage Pin 

// Define variables used for encoder position and speed
long encoderCnt=0;
long encoderALast=0;
unsigned int encACntPerRev=1;
unsigned long lastTime=0;
unsigned long lastTime2=0;
long distance=0;

float fAnalog=0;     // Floationg point variable for analog calculations
int  iAnalog=0;      // Integer variable for analog reading and other calculations
int  i;              // counter variable
int  SensorAType=0;  // place holder defines sensor A type (0=none)
int  SensorBType=0;  // place holder defines sensor B type (0=none)
bool BTRD=0;         // Bluetooth call to read sensors
bool ComEN=0;        // indicates that USB Input is connected and talking
bool Meter=1;        // set to 0 if LTC2947 is missing

void setup() {
  pinMode(AlertPin, INPUT_PULLUP);   // Alert pin for LTC2947 (active low)
  pinMode(A0, OUTPUT);               // Command pin for BT
  pinMode(A1, OUTPUT);               // Power pin for BT
  digitalWrite(A0, LOW);             // Drive CMD low for normal operation 
  digitalWrite(A1, HIGH);            // Enable power for BT operation 
  delay(100);
  Serial1.begin(115200); // bluetooth serial communication at 115200 Baud
  Serial.begin(9600);    // USB Serial communication at 9600 Baud
  Wire.begin();          // used for I2C communication

  // set servos to drive for off/midpoint position then shut off
  myservoB.attach(PWMBPin);
  myservoB.writeMicroseconds((PWMBMin+PWMBMax)/2);
  myservoA.attach(PWMAPin);
  myservoA.writeMicroseconds((PWMAMin+PWMAMax)/2);

  delay(500);               // Wait to make sure everything is setup 
  myservoB.detach();
  myservoA.detach();
  
  lastTime = millis();      // Read current time in ms
  lastTime2 = millis();     // Read current time in ms

  Wire.beginTransmission (LTC2947_ADDRESS);
  if (Wire.endTransmission () == 0) {
    delay(1);               // Wait 
    LTC2947_init();         // initialize LTC2947
  }
  else {
    Meter=0;                // No response from LTC2974 so just monitor battery voltage
  }

  pinMode(SolenoidA, OUTPUT);
  digitalWrite(SolenoidA, LOW); 
  pinMode(SolenoidB, OUTPUT);
  digitalWrite(SolenoidB, LOW); 
  pinMode(DIOAPin, INPUT);
  pinMode(DIOBPin, INPUT);
  pinMode(DIOCPin, INPUT);
  pinMode(DIODPin, INPUT);
}

void loop() {
  // Check if there is serial data being sent from COM port
  while (Serial.available()>0) {
    CData = Serial.read();          // read all serial data entered
    if (char(CData) == 'S' || char(CData) == 's' ) {  
      ComEN = 1;
      SetupBT();
    }
  }

  if (millis()-lastTime2 > 2000 && ComEN==0) {
    Serial.println(F("Type 'S' to Setup Bluetooth")); // Print to serial monitor for tethering
    lastTime2 = millis();      // Read current time in ms
  }

  // Check if there is an available byte to read from Bluetooth
  if (Serial1.available()){  
    BTRead();
  } 

  if (BTRD) {                 // Write sensor data if called by BT
    BTWrite();
    BTRD=0;
  }
    
  // Exit mode program, start loop over
}

// Encoder interrupt routine
void doEncoder() {
  if (digitalRead(DIOAPin) == digitalRead(DIOBPin)) {
    encoderCnt--;  // ENC B high on rising edge of ENC A for count up
  } else {
    encoderCnt++;  // ENC B low on rising edge of ENC A for count down
  }
}


