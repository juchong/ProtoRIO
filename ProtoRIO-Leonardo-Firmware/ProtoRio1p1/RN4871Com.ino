/*

AUTHOR: Bill Walter 
For Leonardo communication with RN4871
DATE: 6/19/2018
*/

int i;
int start=1;    // send start message until first comm data
int ComEN=1;    // indicates first comms pass
int more=0;     // indicates more data to pass ("1"=name,"2"=password)
String SData;
char CData;
unsigned long lastTime=0;
void setup()
{
    pinMode(A0, OUTPUT);    // RN4871 (Reset pin) 
    digitalWrite(A0, LOW);  //  Set low to Reset BT

    delay(1000);
      
    digitalWrite(A0, HIGH);  //  Set low to Reset BT
    Serial.begin(115200);
    Serial1.begin(115200);  // RN4871 default speed in command mode
 
    delay(200);

    lastTime = millis();      // Read current time in ms
    SData="";
    while (start) {
      if (millis()-lastTime > 2000) {
        Serial.println(F("Type 'C' for Command Mode Setup; Type 'N' for Normal Op")); // Print to serial monitor for tethering
        lastTime = millis();      // Read current time in ms
      }
      while (Serial.available()>0) {
        CData = Serial.read();          // read all serial data entered
        if (CData == 'C' || CData == 'c' ) {  
          start = 0;
        }
        else if (CData == 'N' || CData == 'n' ) {  
          start = 0;
          ComEN = 0;
        }
      }
    }
  }

void loop()
{

  if (ComEN) {
    Serial1.write("$$$");
    delay(100);
    help();
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
  }
  // Check if there is serial data being sent from COM port
  while (Serial.available()>0) {
    Serial1.write(Serial.read());          // read all COM data sent
  }

  // Keep reading from RN4871 and send to Arduino Serial Monitor
  while (Serial1.available()) {
  Serial.write(Serial1.read()); //Conduct a serial read/write
  }

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
