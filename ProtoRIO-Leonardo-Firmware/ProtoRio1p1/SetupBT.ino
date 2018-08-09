// Mode T selected: USB Motor Control
void SetupBT(void) {

  Serial1.end();         // Halt HC-05 communication
  digitalWrite(A0, LOW); // 
  digitalWrite(A1, LOW); // power off Bluetooth module     
  delay(1000);  
  digitalWrite(A0, HIGH); // HIGH to switch module to AT mode
  digitalWrite(A1, HIGH); // HIGH to power Bluetooth module  
  delay(1000);       
  Serial1.begin(38400);   // HC-05 default speed in AT command more
  Serial.println(F("BlueTooth Command Mode! Type '?' for commands"));
  int more=0;             // indicates more data to pass ("1"=name,"2"=password)
  while (Serial.available()){ 
    char c = Serial.read(); // Flush serial port
  }
  SData="";
  
  while (ComEN) {
    while (Serial.available()){  //Check if there is an available byte to read
      delay(10); //Delay added to make thing stable 
      char c = Serial.read(); //Conduct a serial read
      if (c != '\n') {
        SData += c;    //build the string
      }
      else {
        SData += '\n'; // Add build the string
        if (more==1) {
          Serial1.write("AT+NAME=");
          for (i=0; i<SData.length(); i++) {
            Serial1.write(SData.charAt(i));             
          }
          more=0;
        }
        else if (more==2) {
          Serial1.write("AT+PSWD=");
          for (i=0; i<SData.length(); i++) {
            Serial1.write(SData.charAt(i));             
          }
          more=0;          
        }
        else if (SData.length()>3) {
          Serial.println(F("Invalid input!"));
        }
        else if (SData.startsWith("V") || SData.startsWith("v")) { 
          Serial1.write("AT+VERSION?\r\n");
        }
        else if (SData.startsWith("B") || SData.startsWith("b")) { 
          Serial1.write("AT+UART=115200,1,0\r\n");
        }
        else if (SData.startsWith("N") || SData.startsWith("n")) {
          Serial.println(F("Enter Name for Bluetooth:"));
          more=1;
        }
        else if (SData.startsWith("P") || SData.startsWith("p")) {
          Serial.println(F("Enter Password for Bluetooth (0000-9999):"));
          more=2;
        }
        else if (SData.startsWith("?")) {
          help();
        }
        else if (SData.startsWith("Q") || SData.startsWith("q")) {
          Serial1.end();         // Halt HC-05 communication
          digitalWrite(A0, LOW); // 
          digitalWrite(A1, LOW); // power off Bluetooth module     
          delay(1000);  
          digitalWrite(A1, HIGH); // HIGH to power Bluetooth module  
          delay(1000);       
          Serial1.begin(115200);  // HC-05 default speed for ProtoRIO
          ComEN=0;
        }
        else {
          Serial.println(F("Invalid input!"));
        }
        SData="";
      }
    }
    // Keep reading from HC-05 and send to Arduino Serial Monitor
    if (Serial1.available()) {
      Serial.write(Serial1.read());    
    }
  }
}

void help(void) {
  Serial.println(F("Set Arduino Serial Monitor to 'Both NL & CR'"));
  Serial.println(F("Type 'V' to see BT Version"));
  Serial.println(F("Type 'N' to set BT name"));
  Serial.println(F("Type 'P' to set BT password"));
  Serial.println(F("Type 'B' to set BT baud rate to 115200"));
  Serial.println(F("Type 'Q' to exit BT Command mode"));
}
