#!/usr/bin/env python3
from flask import Flask
from flask import request
import time
import threading
import OPi.GPIO as GPIO
import subprocess
import psutil
import Adafruit_ADS1x15
from datetime import datetime

GPIO.setwarnings(False)
GPIO.setboard(GPIO.PCPCPLUS)
GPIO.setmode(GPIO.BOARD)
GPIO.setup(40, GPIO.OUT)
GPIO.setup(38, GPIO.OUT)
GPIO.setup(36, GPIO.OUT)
GPIO.setup(10, GPIO.IN)
adc = Adafruit_ADS1x15.ADS1115(address=0x48, busnum=0)
adc.start_adc(0, gain=1)
app = Flask(__name__)

latest_ping = None

def handle_timeout():
  print("started handle_timeout background-process")
  global latest_ping
  while True:
#    if latest_ping != None:
#      print((datetime.now() - latest_ping).total_seconds())
    if latest_ping != None and (datetime.now() - latest_ping).total_seconds() > 120:
#      print("OFF")
      latest_ping = None
      GPIO.output(40, 0)
    time.sleep(2)

def ffmpeg():
  global times
  global started
  global pid
  global process
  print("started ffmpeg background-process")
  while True:
    if started == True:
      times -= 0.05
      if(times <= 0):
        print("end ffmpeg transmit process")
        if pid != None:
          process.terminate()
          process.communicate()
          pid = None
        started = False
      print(times)
    time.sleep(0.05)

def callback(channel):
  global times
  global started
  global pid
  global process
  print("Sound detected")
  if latest_ping != None and (started == True or (datetime.now() - latest_ping).total_seconds() < 120):
    if started == False:
     times = 2
     if pid == None:
        started = True
        print("start ffmpeg transmit")
        command = "/usr/bin/ffmpeg -f alsa -ac 1 -ar 44100 -i hw:0,0  -xerror -analyzeduration 0 -probesize 32 -flags +global_header -c:a libfdk_aac -profile:a aac_eld -ac 2 -b:a 56k -loglevel quiet  -f rtp rtp://192.168.219.6:1337/out.aac";
        process = subprocess.Popen(["exec " + command], shell=True)
        pid = process.pid
    else:
      times = 2

process = None
pid = None
times = 0
started = False
GPIO.add_event_detect(10, GPIO.FALLING, callback=callback, bouncetime=50)
ffmpeg_thread = threading.Thread(target=ffmpeg, name="FFmpeg")   # starting ffmpeg bg process
ffmpeg_thread.start()
timeout_thread = threading.Thread(target=handle_timeout, name="handle_timeout") # starting handle_timeout bg process
timeout_thread.start()

@app.before_request 
def before_request_callback(): 
    global latest_ping
    latest_ping = datetime.now()

@app.route("/kill_rec")
def kill_rec():
  for proc in psutil.process_iter():
    if "ffmpeg" in proc.name():
       if "SCRIPT" in proc.environ():
        if "REC" in proc.environ()['SCRIPT']:
          #print("killing rec ffmpeg")
          proc.terminate()
          return "1"

@app.route("/voltage")
def voltage():
    if GPIO.input(40) == 0:
        GPIO.output(40, 1)
    value = adc.get_last_result()
    result = value *0.000626  # read ADC voltage value
    return str("{:.2f}".format(result))
#    return "test"

@app.route("/reboot")
def reboot():
    command = "/usr/sbin/shutdown -r now"
    process = subprocess.Popen([command], shell=True)
    return "1"

@app.route("/radio_poweroff")
def poweroff():
    GPIO.output(36, 0)   # disables ptt for safety
    GPIO.output(40, 0)   # poweroff
    return "1"


@app.route("/start")  #  ptt is managed by ffmpeg, not needed anymore
def start_():
    GPIO.output(36, 1)
    return "1"


@app.route("/end")
def end_():
    GPIO.output(36, 0)
    return "0"

@app.route("/up")    # gets the channel from txt and adds a digit if its under 8
def channel_up(): 
    GPIO.output(38, 1)
    time.sleep(0.1)
    GPIO.output(38, 0)
    channel = int(get_channel())
    new = set_channel(channel)
    return str(new)

@app.route("/get_channel")   # is called on start,  starts the radio
def chann():
    if GPIO.input(40) == 0:
        GPIO.output(40, 1)
    return str(get_channel())

@app.route('/set_channel')
def write_channel():
   string = request.args['channel']
   if string.isdigit():
      f = open("/opt/scripts/channel.txt", "w")
      f.write(string)
      f.close()
      return "OK"
   else:
      return "NO_DIGIT"


def get_channel():
   f = open("/opt/scripts/channel.txt", "r")
   channel = f.read(1)
   f.close()
   return channel

def set_channel(number):
    number += 1
    if number == 9:
        number = 1 
    f = open("/opt/scripts/channel.txt", "w")
    f.write(str(number))
    f.close()
    return number;

if __name__ == "__main__":
    try:
      app.run(host='0.0.0.0', port=80)
    except:
      GPIO.cleanup()
