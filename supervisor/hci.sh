#!/bin/bash

_term() {
  echo "SIGTERM"
	trap - SIGTERM
	kill -TERM $(jobs -p)
	pkill -g 0
	sleep 2
	exit
}
trap _term SIGINT SIGTERM

hcidevices=$(hciconfig | grep -Po '^hci[0-9](?=:)')

for hcidevice in $hcidevices ; do
	hcimac=$(hciconfig $hcidevice | grep -m1 -Po '(?<=Address: )[0-F:]+')
	topic="/opentrigger/rawhex/$hcimac"

	echo "$hcidevice -> $topic"
	hciconfig $hcidevice down
	hciconfig $hcidevice up
	hcitool -i $hcidevice lescan --duplicates > /dev/null &
	hcidump -i $hcidevice --raw | otraw2q | mosquitto_pub -t "$topic" -l &
done


trap "echo SIGCHLD $1" SIGCHLD

while :; do read; done
