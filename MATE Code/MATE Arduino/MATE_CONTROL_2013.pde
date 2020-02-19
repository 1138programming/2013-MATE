

/* This firmware supports as many servos as possible using the Servo" library 
 * included in Arduino 0012
 *  
 * 5-9-12  Papa: Added Compass code to RB original "2012 Firmata Mate Servo library" 
 */
 
#include <Firmata.h>
#include <Servo.h>

Servo FL;
Servo FR;
Servo BL;
Servo BR;

Servo FUD;
Servo BUD;

Servo AR;
Servo AS;
Servo AG;
Servo SGO;

Servo T1;
Servo T2;

// byte analogPin = 24;

/*==============================================================================
 * Compass Heading HMC6352 Related   Code from: http://wiring.org.co/learning/libraries/hmc6352sparkfun.html
 *============================================================================*/
#include <Wire.h>

int compassAddress = 0x42 >> 1; // From datasheet compass address is 0x42
// shift the address 1 bit right, the Wire library only needs the 7
// most significant bits for the address
int reading = 0; 

int compassSamplingInterval= 250;    // sample every 250 mili secs
byte compassAnalogSendPin = 0;       // "ANALOG" pin number that C# Client expects to receive the compass reading value from

int SDA_PIN =18;  // Analog Pin 4;        // Define the IC2 pin numbers on this Arduino board/device 
int SCL_PIN =19;  // Analog Pin 5;

/* timer variables */
unsigned long currentMillis;        // store the current value from millis()
unsigned long previousMillis;       // for comparison with currentMillis

/*============================================================================*/


void analogWriteCallback(byte pin, int value)
{
    if(pin == 0)
      FL.write(value);
    if(pin == 1)
      FR.write(value);
    if(pin == 2)
      BL.write(value);
    if(pin == 3)
      BR.write(value);
    if(pin == 4)
      FUD.write(value);
    if(pin == 5)
      BUD.write(value);
      
    if(pin == 12)
      AR.write(value);
    if(pin == 13)
      AS.write(value);
    if(pin == 14)
      AG.write(value);
    if(pin == 15)
      SGO.write(value);
    if(pin == 16)
      T1.write(value);
    if(pin == 17)
      T2.write(value);
}

void setup() 
{
    Firmata.setFirmwareVersion(0, 2);
    Firmata.attach(ANALOG_MESSAGE, analogWriteCallback);

    FL.attach(0);
    FR.attach(1);
    BL.attach(2);
    BR.attach(3);
    
    FUD.attach(4);
    BUD.attach(5);
    
    AR.attach(12);
    AS.attach(13);
    AG.attach(14);
    SGO.attach(15);
    
    T1.attach(16);
    T2.attach(17);
    
    Firmata.begin();
    
  // * Compass Heading HMC6352  SetuUp() code here:
    Wire.begin();       // join i2c bus (address optional for master) 
 // end Compass SetUp()  
    
}


/*==============================================================================
 * Compass Heading HMC6352 Related   
 *============================================================================*/
 
 void compassReadheading()
 {
    // step 1: instruct sensor to read echoes 
  Wire.beginTransmission(compassAddress);  // transmit to device
  // the address specified in the datasheet is 66 (0x42) 
  // but i2c adressing uses the high 7 bits so it's 33 
  
   
  // Note: New "Wire.write" =  Old "Wire.send",   New "Wire.read" =  Old  "Wire.receive" 
  Wire.send('A');        // command sensor to measure angle  
  Wire.endTransmission(); // stop transmitting 

  // step 2: wait for readings to happen 
  delay(10); // datasheet suggests at least 6000 microseconds 

  // step 3: request reading from sensor 
  Wire.requestFrom(compassAddress, 2); // request 2 bytes from slave device #33 

  // step 4: receive reading from sensor 
  if (2 <= Wire.available()) // if two bytes were received 
  { 
    reading = Wire.receive();  // receive high byte (overwrites previous reading) 
    reading = reading << 8; // shift high byte to be high 8 bits 
    reading += Wire.receive(); // receive low byte as lower 8 bits 
    reading /= 10;
    
    // Serial.println(reading); // print the reading
    
    // Send the reading to the Firmata C# client as an Analog reading on  Analog Pin #0 (default)
   Firmata.sendAnalog(compassAnalogSendPin, reading); 
  } 
   
 } // end compassReadheading()



void loop() 
{
    while(Firmata.available())
        Firmata.processInput();
        
   /* ANALOGREAD - do all analogReads() at the configured sampling interval */
  
  currentMillis = millis();
  if (currentMillis - previousMillis > compassSamplingInterval) {      // Default: Sends compass signal 4 times per second
    previousMillis += compassSamplingInterval;
      compassReadheading();      
    } 
        
/**  OLD RB  Analog Code commented out        
        Firmata.sendAnalog(analogPin, analogRead(31)); 
    analogPin = analogPin + 1;
    if (analogPin >= TOTAL_ANALOG_PINS) analogPin = 31;
**/    
    
}

