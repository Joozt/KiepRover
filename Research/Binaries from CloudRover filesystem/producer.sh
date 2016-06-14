#!/bin/sh
#mdev -s
/root/producer &
insmod /root/g_file_storage.ko file=/dev/ram0 removable=1 stall=0
