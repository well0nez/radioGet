#include <wiringPi.h>
int main (void)
{
  wiringPiSetup () ;
  pinMode (24, OUTPUT) ;
  digitalWrite (24,  LOW);
}
