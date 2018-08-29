// Mode T selected: USB Motor Control
void SetupBT(void) {

  Serial1.write("$$$");   // Put RN4871 into command mode
  delay(100);
  help();
  int more=0;             // indicates more data to pass ("1"=name,"2"=password)
  while (Serial.available()){ 
    char c = Serial.read(); // Flush serial port
  }
  SData="";
  
  while (ComEN) {
    while (Serial1.available()){  //Check if there is a BT byte to read
      Serial.write(Serial1.read()); //Conduct a serial read/write
    }
    SData="";          
    while (Serial.available()){  //Check if there is an available byte to read
      CData = Serial.read(); //Conduct a serial read
      if (CData != '\n') {
        SData += CData;    //build the string
      }
      else {
        if (more==1) {
          Serial1.write("S-,");
          for (i=0; i<SData.length(); i++) {
            Serial1.write(SData.charAt(i));             
          }
          Serial1.write('\n');
          more=0;
        }
        else if (more==2) {
          Serial1.write("SP,");
          for (i=0; i<SData.length(); i++) {
            Serial1.write(SData.charAt(i));             
          }
          Serial1.write("\n");
          more=0;          
        }
        else if (SData.length()>2) {
          Serial.println(F("Invalid input!"));
        }
        else if (SData.startsWith("N") || SData.startsWith("n")) {
          Serial.println(F("Enter Name for Bluetooth:"));
          more=1;
        }
        else if (SData.startsWith("P") || SData.startsWith("p")) {
          Serial.println(F("Enter Password for Bluetooth (0000-9999):"));
          more=2;
        }
        else if (SData.startsWith("V") || SData.startsWith("v")) {
          Serial1.write("V");
          Serial1.write("\n");
        }
        else if (SData.startsWith("S") || SData.startsWith("s")) {
          Serial1.write("SS,C0");
          Serial1.write("\n");
        }
        else if (SData.startsWith("R") || SData.startsWith("r")) {
          Serial1.write("SF,1");
          Serial1.write("\n");
        }
        else if (SData.startsWith("E") || SData.startsWith("e")) {
          Serial1.write("+");
          Serial1.write("\n");
        }
        else if (SData.startsWith("D") || SData.startsWith("d")) {
          Serial1.write("D");
          Serial1.write("\n");
        }
        else if (SData.startsWith("?")) {
          help();
        }
        else if (SData.startsWith("Q") || SData.startsWith("q")) {
          Serial1.end();         // Halt RN4871 communication
          digitalWrite(A0, LOW); // 
          delay(100);  
          digitalWrite(A0, HIGH); // HIGH to power Bluetooth module  
          delay(1000);       
          Serial1.begin(115200);  // RN4871 default speed for ProtoRIO
          ComEN=0;
          Serial.println(F("BT in Normal Operation"));
          delay(1000);       
        }
        else {
          Serial.println(F("Invalid input!"));
        }
        SData="";
      }
    }
  }
  
  digitalWrite(A0, LOW); // Reset BT module
  delay(1000);  
  digitalWrite(A0, HIGH); // HIGH Enable BT module
  delay(1000);       


}
//PS,4D F D524E 
//PC,BF3FBD80063F11E59E690002A5D5C501,02,02 
//PC,BF3FBD80063F11E59E690002A5D5C502,02,02 
//PC,BF3FBD80063F11E59E690002A5D5C503,18,04
void help(void) {
  Serial.println(F("Set Arduino Serial Monitor to 'Both NL & CR'"));
  Serial.println(F("Type 'E' to set Echo ON/OFF"));
  Serial.println(F("Type 'N' to set BT name"));
  Serial.println(F("Type 'P' to set BT password"));
  Serial.println(F("Type 'Q' to exit BT Command Mode"));
  Serial.println(F("Type 'R' Reset BT to Factory Reset"));
  Serial.println(F("Type 'S' Set BT Default Services"));
  Serial.println(F("Type 'D' Device Information"));
  Serial.println(F("Type 'V' Display BT version"));
  Serial.println(F("Type '?' to reprint this list"));
}
