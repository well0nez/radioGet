#!/bin/bash
while [ true ]
do
      ping -c1 192.168.219.1 > /dev/null
      if [ $? -eq 0 ]
      then
	  current_time=$(date "+%Y.%m.%d-%H.%M.%S")
#         SCRIPT=REC ffmpeg -hide_banner -probesize 32 -acodec libfdk_aac -protocol_whitelist file,udp,rtp -fflags nobuffer -flags low_delay -reorder_queue_size 0  -fflags genpts -listen_timeout 0  -i /opt/scripts/speaker.sdp -f alsa hw:0
 	  SCRIPT=REC /opt/scripts/ffmpeg_ptt -acodec libfdk_aac -analyzeduration 0 -probesize 32 -i "tcp://192.168.219.5:1337/?listen_timeout=120000&listen=1&timeout=3000" -loglevel quiet -t 180 -abort_on empty_output_stream -y -f alsa hw:0 /opt/scripts/recordings/$current_time.aac && /opt/scripts/close_ptt || /opt/scripts/close_ptt
      else
          echo "fail"
    	  sleep 1
      fi
done
