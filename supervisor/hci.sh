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

command_missing () {
    ! type "$1" &> /dev/null ;
}

required="grep otraw2q mosquitto_pub hciconfig hcitool hcidump pkill"
missing=""
for c in $required ; do
	if command_missing $c ; then
		echo "missing: $c"
		missing="$missing $c"
	fi
done

if [[ ! -z "$missing" ]]; then
	echo "dependencies missing, setup is probably not complete..."
	exit 1
fi

hcidevices=$(hciconfig | grep -Po '^hci[0-9](?=:)')

if [[ -z "$hcidevices" ]]; then
	echo "no devices found"
	exit 1
fi

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
