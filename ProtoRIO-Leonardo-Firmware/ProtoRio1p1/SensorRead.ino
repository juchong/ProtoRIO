/* Sensor A read; Sensor Types are:
 * 0) No Sensor
 * 1) ENCODER (A Only)
 * 2) HC-SR04 
 * 3) MAXBOTIX (1cm)
 * 4) MAXBOTIX (2CM)
 * 5) SHARP GP2Y0A60
 * 6) SHARP GP2Y0A02
 * 7) SHARP GP2Y0A21
 * 8) SHARP GP2Y0A41
 * 9) SHARP GP2Y0A51
 * 10) ANALOG READ
 * 11) VL530X TOF (B Only)
 * 12) LIDAR LITE (B Only)
 */

void SensorRead(int SenType, bool SenB) { 
  byte Pin1;
  byte Pin2;
  byte DistMod = 1; 
  byte SenCal = 0; 

  if (SenType==1) {          // ENCODER (A Only)
  }
  else if (SenType==2) {     // HC-SR04
    if (SenB) {              // Set pins to read if sensor B
      Pin1=DIOCPin;
      Pin2=DIODPin;    
    }
    else {                   // Set pins to read if sensor A
      Pin1=DIOAPin;
      Pin2=DIOBPin;
    }
    long duration;
    digitalWrite(Pin1, LOW);                       // Set TRIG pin low
    delayMicroseconds(2);                          // Wait 2us
    digitalWrite(Pin1, HIGH);                      // Set TRIG pin high
    delayMicroseconds(10);                         // Added this line
    digitalWrite(Pin1, LOW);                       // Set TRIG pin low - starts transmit
    duration = pulseIn(Pin2, HIGH);                // Measure how long it takes to get a signal back
    distance = (duration/2) / 29.1;
    if (distance <= 0) { distance=0; }             // Should not be any negative numbers
    else if (distance >= 400) { distance=400; }    // Set limits of distance to 4 meters
  }
  else if (SenType==3 || SenType==4 ) {     // MAXBOTIX
    if (SenB) { Pin1=AnalogInBPin; }        // Set pin to read if sensor B
    else {Pin1=AnalogInAPin; }              // Set pins to read if sensor A
    SenCal = SenType-2;
    distance = SenCal*analogRead(Pin1);
  }
  else if (SenType>4 && SenType<10) {       // SONY
    if (SenB) { Pin1=AnalogInBPin; }        // Set pin to read if sensor B
    else {Pin1=AnalogInAPin; }              // Set pins to read if sensor A
    if (SenType==5 || SenType==6) { SenCal = 130; }
    else if (SenType==7)  { SenCal = 80; }
    else if (SenType==8)  { SenCal = 30; }
    else if (SenType==9)  { SenCal = 15; }
    iAnalog = analogRead(Pin1);
    fAnalog = SenCal*((84.5/(iAnalog-9))-0.05);
    distance = fAnalog;
  }
  else if (SenType==10) {     // ANALOG READ
    if (SenB) { Pin1=AnalogInBPin; }        // Set pin to read if sensor B
    else {Pin1=AnalogInAPin; }              // Set pins to read if sensor A
    distance = analogRead(Pin1);
  }  
  else if (SenType==11) {     // VL530X TOF
    TOFsensor.init();
    TOFsensor.setTimeout(500);
    TOFsensor.setSignalRateLimit(0.1);
    // increase laser pulse periods (defaults are 14 and 10 PCLKs)
    TOFsensor.setVcselPulsePeriod(VL53L0X::VcselPeriodPreRange, 18);
    TOFsensor.setVcselPulsePeriod(VL53L0X::VcselPeriodFinalRange, 14);  
    // set timing budget (20ms to 200ms)
    TOFsensor.setMeasurementTimingBudget(40000);    
    distance = TOFsensor.readRangeSingleMillimeters()/10;
    if (distance > 200) { distance = 200; }  // Set upper limit of 2 meters
    if (TOFsensor.timeoutOccurred()) { 
      // Serial.println("TOF Timeout!");        // for debugging only
    }
  }
  else if (SenType==12) {     // LIDAR LITE
      distance = lidarGetRange();    
  }
}

// Function to get a measurement from the LIDAR Lite
int lidarGetRange(void) {
  int val = -1;

  Wire.beginTransmission((int)LIDARLite_ADDRESS); // transmit to LIDAR-Lite
  Wire.write((int)RegisterMeasure); // sets register pointer to  (0x00)  
  Wire.write((int)MeasureValue); // sets register pointer to  (0x00)  
  Wire.endTransmission(); // stop transmitting

  delay(20); // Wait 20ms for transmit

  Wire.beginTransmission((int)LIDARLite_ADDRESS); // transmit to LIDAR-Lite
  Wire.write((int)RegisterHighLowB); // sets register pointer to (0x8f)
  Wire.endTransmission(); // stop transmitting

  delay(20); // Wait 20ms for transmit
  
  Wire.requestFrom((int)LIDARLite_ADDRESS, 2); // request 2 bytes from LIDAR-Lite

  if(2 <= Wire.available()) // if two bytes were received
  {
    val = Wire.read(); // receive high byte (overwrites previous reading)
    val = val << 8; // shift high byte to be high 8 bits
    val |= Wire.read(); // receive low byte as lower 8 bits
  }
  // Calibration factor
  val=val-20;
  return val;
}

