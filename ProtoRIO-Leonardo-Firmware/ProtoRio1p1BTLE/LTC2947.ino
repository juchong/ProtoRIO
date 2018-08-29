// LTC2947 I2C commands
void LTC2947_init(void) {
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0xFF);                        // write PAGE register
  Wire.write(0x01);                        // Write for PAGE 1
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0x80);                        // write I Bat Hi register
  Wire.write(0x09);                        // Set Alert value
  Wire.write(0xC4);                        // I Bat Hi = 30A (0.012A/LSB)
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0x92);                        // write V Bat Lo register
  Wire.write(0x0D);                        // Set Alert value
  Wire.write(0xAC);                        // V Bat Lo = 7V (0.002V/LSB)
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0x94);                        // write Temp Hi register
  Wire.write(0x01);                        // Set Alert value
  Wire.write(0x88);                        // Temp Hi = 85C (5+0.204C/LSB)
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0xFF);                        // write PAGE register
  Wire.write(0x00);                        // Write for PAGE 0
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0xF0);                        // write Control register
  Wire.write(0x08);                        // Write for Continuous Sampling
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0x89);                        // write STAT MASK register
  Wire.write(0x39);                        // Alert on V Bat Lo & Temp Hi
  Wire.write(0x0E);                        // Alert on IV Bat Hi
  Wire.endTransmission();                  // stop transmitting   
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0xE8);                        // write Mask register
  Wire.write(0x01);                        // Alerts are forwarded ALERT pin
  Wire.endTransmission();                  // stop transmitting   
}

float LTC2947_IbRead(void) {
  return LTC2947_I2CR3(0x90)*0.003;        // Return I Bat value (scaled by 0.003A/bit)
}

float LTC2947_IbMXRead(void) {
  return LTC2947_I2CR2(0x40)*0.012;        // Return I Bat MAX value (scaled by 0.012A/bit)
}

float LTC2947_IbMNRead(void) {
  return LTC2947_I2CR2(0x42)*0.012;        // Return I Bat MIN value (scaled by 0.012A/bit)
}

float LTC2947_WbRead(void) {
  return LTC2947_I2CR3(0x93)*0.05;         // Return W Bat value (scaled by 0.05W/bit)
}

float LTC2947_WbMXRead(void) { 
  return LTC2947_I2CR2(0x44)*0.2;          // Return W Bat MAX value (scaled by 0.2W/bit)
}

float LTC2947_WbMNRead(void) {
  return LTC2947_I2CR2(0x46)*0.2;          // Return W Bat MIN value (scaled by 0.2W/bit)
}

float LTC2947_VbRead(void) {
  return LTC2947_I2CR2(0xA0)*0.002;        // Return V Bat value (scaled by 0.002V/bit)
}

float LTC2947_VbMXRead(void) {
  return LTC2947_I2CR2(0xA0)*0.002;        // Return V Bat MAX value (scaled by 0.002V/bit)
}

float LTC2947_VbMNRead(void) {
  return LTC2947_I2CR2(0xA0)*0.002;        // Return V Bat MIN value (scaled by 0.002V/bit)
}

float LTC2947_TcRead(void) {
  return LTC2947_I2CR2(0xA2)*0.204+5.5;   // Return Temperature value (scaled by 0.204C/bit+5.5C)
}

float LTC2947_VddRead(void) {
  return LTC2947_I2CR2(0xA4)*0.145;       // Return Vdd value (scaled by 0.145V/bit)
}

float LTC2947_CHRG(void) {
  long value4;
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(0x00);                        // write Charge Acc register
  Wire.endTransmission();                  // stop transmitting   
  Wire.requestFrom(LTC2947_ADDRESS, 4);    // request 4 bytes from LTC2947
  value4=Wire.read();                      // read Charge MSB register
  value4=value4<<8;                        // shift value
  value4=value4+Wire.read();               // read next register & add to value
  value4=value4<<8;                        // shift value
  value4=value4+Wire.read();               // read next register & add to value
  value4=value4<<8;                        // shift value
  value4=value4+Wire.read();               // read LSB register and add to value
  Wire.endTransmission();                  // stop transmitting   
  return value4*0.0000216;                 // Scale by 21.6e-6 AHr / LSB
}

int LTC2947_I2CR2(int I2CReg) {
  int value2;
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(I2CReg);                      // write control register
  Wire.endTransmission();                  // stop transmitting   
  Wire.requestFrom(LTC2947_ADDRESS, 2);    // request 2 bytes from LTC2947
  value2=Wire.read();                      // read register MSB
  value2=value2<<8;                        // shift MSB
  value2=value2+Wire.read();               // read register LSB and add to MSB
  Wire.endTransmission();                  // stop transmitting   
  return value2;
}

long LTC2947_I2CR3(int I2CReg) {
  long value3;
  Wire.beginTransmission(LTC2947_ADDRESS); // transmit to LTC2947
  Wire.write(I2CReg);                      // write control register
  Wire.endTransmission();                  // stop transmitting   
  Wire.requestFrom(LTC2947_ADDRESS, 3);    // request 3 bytes from LTC2947
  value3=Wire.read();                      // read register MSB
  value3=value3<<8;                        // shift MSB
  value3=value3+Wire.read();               // read next register & add to value
  value3=value3<<8;                        // shift value
  value3=value3+Wire.read();               // read register LSB and add to value
  Wire.endTransmission();                  // stop transmitting   
  return value3;
}
