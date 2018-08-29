void BTRead(void) { 
  while (Serial1.available()){  //Check if there is an available byte to read
    delay(10); //Delay added to make thing stable 
    char c = Serial1.read(); //Conduct a serial read
    if (c != '\n') {
      if (int(c) != 0) {     // remove null characters
        SData += c; //build the string
      }
    }
    else {
      //Serial.println(SData);                     // For debugging only
      if (SData.startsWith("BTRD")) { BTRD=1; }     // Bluetooth call to read sensor data
      if (SData.startsWith("PA")) { 
        SData.remove(0,2);
        iAnalog = SData.toInt();
        if (iAnalog < 0 || iAnalog > 200){
          myservoA.detach();                // Disable the PWM pin if out of range
        }
        else if (iAnalog == 100){
          myservoA.attach(PWMAPin);                        // Setup the PWM pin to work with servo lib
          myservoA.writeMicroseconds((PWMAMin+PWMAMax)/2); // Set PWM to Off state         
        }
        else {
          //Serial.println(iAnalog);                // for debugging only
          fAnalog=(PWMAMax-PWMAMin)/200;
          //Serial.println(fAnalog);                // for debugging only
          fAnalog=fAnalog*iAnalog;
          //Serial.println(fAnalog);                // for debugging only
          fAnalog=fAnalog+PWMAMin;
          //Serial.println(int(fAnalog));           // for debugging only
          myservoA.writeMicroseconds(int(fAnalog)); 
        }
      }
      else if (SData.startsWith("PB")) { 
        SData.remove(0,2);
        iAnalog = SData.toInt();
        if (iAnalog < 0 || iAnalog > 200){
          myservoB.detach();            // Disable the PWM pin if out of range
        }
        else if (iAnalog == 100){
          myservoB.attach(PWMBPin);                        // Setup the PWM pin to work with servo lib
          myservoB.writeMicroseconds((PWMBMin+PWMBMax)/2); // Set PWM to Off state         
        }
        else {
          //Serial.println(iAnalog);                // for debugging only
          fAnalog=(PWMBMax-PWMBMin)/200;
          //Serial.println(fAnalog);                // for debugging only
          fAnalog=fAnalog*iAnalog;
          //Serial.println(fAnalog);                // for debugging only
          fAnalog=fAnalog+PWMBMin;
          //Serial.println(int(fAnalog));           // for debugging only
          myservoB.writeMicroseconds(int(fAnalog)); 
        }
      }
      else if (SData.startsWith("SA")) { 
        SData.remove(0,2);
        SensorAType = SData.toInt();
        if (SensorAType < 1 || SensorAType > 10){  // sensor out of range set to none!
          SensorAType=0;
        }
        if (SensorAType == 1) { 
          attachInterrupt(4, doEncoder, RISING);   // Interrupt 4 is DIO Pin 7 
          encoderCnt=0; 
          encoderALast=0;
        }
        else if (SensorAType == 2) { 
          pinMode(DIOAPin, OUTPUT);                // Set TRIG pin Output
          digitalWrite(DIOAPin, LOW);              // Set TRIG pin low
          detachInterrupt(4); 
        }
        else { 
          pinMode(DIOAPin, INPUT);                 // Set DIOAPin to input
          detachInterrupt(4); 
        } 
      }
      else if (SData.startsWith("SB")) { 
        SData.remove(0,2);
        SensorBType = SData.toInt();
        if (SensorBType < 2 || SensorBType > 12){  // sensor out of range set to none!
          SensorBType=0;
        }
        if (SensorBType == 2) { 
          pinMode(DIOCPin, OUTPUT);                // Set TRIG pin Output
          digitalWrite(DIOCPin, LOW);              // Set TRIG pin low
        }
        else { 
          pinMode(DIOCPin, INPUT);                 // Set DIOAPin to input
        } 
        // Serial.println(int(iSensorAType));      // for debugging only
      }
      else if (SData.startsWith("EA")) { 
        SData.remove(0,2);
        encACntPerRev = SData.toInt();
        // Serial.println(int(encACntPerRev));   // for debugging only
      }
      else if (SData.startsWith("SO")) { 
        SData.remove(0,2);
        i = SData.toInt();
        if (i==1){                              // set Solenoid A ON
          digitalWrite(SolenoidA, HIGH); 
          digitalWrite(SolenoidB, LOW); 
        }
        else if (i==2){                         // set Solenoid B ON
          digitalWrite(SolenoidA, LOW); 
          digitalWrite(SolenoidB, HIGH); 
        }
        else if (i==3){                         // set both Solenoids ON
          digitalWrite(SolenoidA, HIGH); 
          digitalWrite(SolenoidB, HIGH); 
        }
        else {                                  // set both Solenoids OFF
          digitalWrite(SolenoidA, LOW); 
          digitalWrite(SolenoidB, LOW); 
        }
      }
      SData = "";
    }
  } 
}
