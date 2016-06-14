# CloudRover RC car research & reverse engineering

| Connection details | |
| --- | --- |
| Wifi SSID | iCloudRover_879 |
| Wifi password | 12345 |
| IP address | 192.168.1.1 |
| Port 21 | FTP (username: root, no password) |
| Port 23 | Telnet (username: root, no password) |
| Port 5200 (TCP) | Unknown |
| Port 5201 (UDP) | Control port |

## Tools
 - [Android app](Android app/), decompiled (JAR can be inspected with [JD-GUI](https://github.com/java-decompiler/jd-gui))
 - [Binaries from CloudRover filesystem](Binaries from CloudRover filesystem/), retrieved from the RC car via FTP
 - [Windows application](Windows application/), traffic inspected with [Wireshark](https://www.wireshark.org/) (see the capture files)
 - [Python scripts](Python/), created by inspecting the traffic of the Windows application and the Android sources

## Python scripts
The [`Control test.py`](Python/Control test.py) script sends control commands to UDP port 5201 of the CloudRover. It can make it drive, turn the LEDs on/off and control the camera.

The [`Video test.py`](Python.Video test.py) script sends the video start and keepalive commands to UDP port 5201 of the CloudRover. Then, the CloudRover starts broadcasting back JPGs to our UDP port 5207. The JPGs are put on the screen.

## CloudRover telnet details
```
[root@anyka ~]$ ls -l
total 1040
-rwxr--r--    1 1006     1000        686489 Mar  6  2013 8188eu.ko
-rw-r--r--    1 1006     1000         33879 Apr  9  2012 g_file_storage.ko
-rw-r--r--    1 root     root          5645 Jun 22  2013 gpio.ko
-rwxr-xr-x    1 root     root         52259 Aug  6  2013 led_io_ctrl
-rwxr-xr-x    1 1006     1000         64060 Sep  7  2011 producer
-rwxr-xr-x    1 1006     1000           102 Sep  7  2011 producer.sh
-rwxr-xr-x    1 root     root        206440 Aug  6  2013 test
drwxr-xr-x    2 root     root             0 Jan  1 00:00 video


[root@anyka ~]$ netstat -tulpn
Active Internet connections (only servers)
Proto Recv-Q Send-Q Local Address           Foreign Address         State       PID/Program name
tcp        0      0 0.0.0.0:5200            0.0.0.0:*               LISTEN      53/test
tcp        0      0 0.0.0.0:21              0.0.0.0:*               LISTEN      45/tcpsvd
tcp        0      0 0.0.0.0:23              0.0.0.0:*               LISTEN      47/telnetd
netstat: /proc/net/tcp6: No such file or directory
udp        0      0 0.0.0.0:67              0.0.0.0:*                           88/dhcpd
udp        0      0 0.0.0.0:5201            0.0.0.0:*                           53/test
netstat: /proc/net/udp6: No such file or directory


[root@anyka ~]$ ps
PID   USER     TIME   COMMAND
    1 root       0:01 init
    2 root       0:00 [kthreadd]
    3 root       0:00 [ksoftirqd/0]
    4 root       0:00 [events/0]
    5 root       0:00 [cpuset]
    6 root       0:00 [khelper]
    7 root       0:00 [async/mgr]
    8 root       0:00 [sync_supers]
    9 root       0:00 [bdi-default]
   10 root       0:00 [kblockd/0]
   11 root       0:00 [ksuspend_usbd]
   12 root       0:00 [khubd]
   13 root       0:00 [kseriod]
   14 root       0:00 [irq/132-aw9523_]
   15 root       0:00 [kmmcd]
   16 root       0:00 [kswapd0]
   17 root       0:00 [ksmd]
   18 root       0:00 [aio/0]
   19 root       0:00 [crypto/0]
   23 root       0:00 [ak37-spi]
   24 root       0:00 [mtdblockd]
   25 root       0:00 [usb_otg_wq]
   37 root       0:00 [jffs2_gcd_mtd0]
   41 root       5:27 /root/led_io_ctrl
   45 root       0:00 tcpsvd 0 21 ftpd -w /
   47 root       0:00 telnetd
   50 root       0:22 /bin/sh /etc/jffs2/rcS2
   51 root       0:00 /sbin/getty -L ttySAK1 115200 vt100
   53 root       9:22 /root/test -r /root/video/ -p udp -i 172.19.10.125 -d 5220 -w 640 -h 480 -S
   84 root       0:15 [RTW_CMD_THREAD]
   85 root       0:00 hostapd /etc/hostapd.conf -B
   88 root       0:00 dhcpd
 3679 root       0:00 -sh
 4040 root       0:00 sleep 5
 4041 root       0:00 ps

```
