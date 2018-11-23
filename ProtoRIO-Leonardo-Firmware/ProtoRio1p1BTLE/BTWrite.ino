void BTWrite(void) {
  if (Meter) {
    // Read and send battery voltage information from LTC2947
    fAnalog = LTC2947_VbRead();
    // Serial.print("  LTC2947 V = ");           // For debug only
    // Serial.println(fAnalog);                  // For debug only
    SData = "RBV";
    SData += fAnalog;
    fAnalog = LTC2947_IbRead();
    SData += "RBI";
    SData += fAnalog;
    SData += "X";              // 'X' denotes end of Battery Results       
  }
  else {
    // Read and send battery voltage information 
    fAnalog = analogRead(BatVPin)*0.013;       // Read the battery voltage
    SData = "RBV";
    SData += fAnalog;
    // Send NA for battery current information
    SData += "RBINAX";
  }

  // Read and send Sensor A information
  if ( SensorAType == 1) {                  // Encoder selected
    SData += "R1A";
    SData += encoderCnt;                    // Current encoder count
    SData += "R2A";
    fAnalog = (encoderCnt-encoderALast)*60000;
    fAnalog = fAnalog/(millis()-lastTime);
    fAnalog = fAnalog/encACntPerRev;
    iAnalog = int(fAnalog);
    SData += iAnalog;                        // Current Wheel RPM
    SData += "Y";                            // 'Y' denotes end of Sensor A Results    
  }
  else if ( SensorAType > 1) {
    SensorRead(SensorAType, 0);
    if (SensorAType == 10) {                 // Analog read so just send count
      SData += "R1A";
      SData += distance;
      SData += "R2A";
      fAnalog = distance;
      fAnalog=5*fAnalog/1023;
      SData += fAnalog;
    }
    else {                                   // Actual distance sensor, send distance
      SData += "R1A";
      SData += distance;                     // Distance in cm
      SData += "R2A";
      fAnalog = distance/2.54;
      iAnalog = fAnalog;
      SData += iAnalog;                      // Distance in in
    }
    SData += "Y";                            // 'Y' denotes end of Sensor A Results    
  }

  // Read and send Sensor B information
  if (LimC || LimD) {
      SData += "R1B";
      SData += digitalRead(DIOCPin);
      SData += "R2B";
      SData += digitalRead(DIODPin);  
  }
  else if ( SensorBType > 1) {
    SensorRead(SensorBType, 1);
    if (SensorBType == 10) {                 // Analog read so just send count
      SData += "R1B";
      SData += distance;
      SData += "R2B";
      fAnalog = distance;
      fAnalog=5*fAnalog/1023;
      SData += fAnalog;    
    }
    else {                                   // Actual distance sensor, send distance
      SData += "R1B";
      SData += distance;                     // Distance in cm
      SData += "R2B";
      fAnalog = distance/2.54;
      iAnalog = fAnalog;
      SData += iAnalog;                      // Distance in in
    }
  }
  SData += "Z";                            // 'Z' denotes end of Sensor B Results 
  if (Lim>0) {
    SData += "LIM";
    SData += Lim;
    Lim=0;
  }
  SData = String(SData + '\n');
  lastTime =  millis();                      // save current time
  encoderALast = encoderCnt;                 // save current encoder A count
  // Serial.println(SData);                  // For debug only
  Serial1.print(SData);                      // Write the sensor data to BT
  SData="";
}
