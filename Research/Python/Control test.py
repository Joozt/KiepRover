#!/usr/bin/python
import time
import socket

UDP_IP = "192.168.1.1"
UDP_PORT = 5201
print "UDP target IP:", UDP_IP
print "UDP target port:", UDP_PORT

sock = socket.socket(socket.AF_INET, # Internet
                     socket.SOCK_DGRAM) # UDP



# LEFT RIGHT GO BACK INFRALEDOFF INFRALEDON CAMDOWN CAMUP SILENTOFF SILENTON
for i in range(1,20):

    for i in range(1,20):
        sock.sendto("LEFT", (UDP_IP, UDP_PORT))
        time.sleep(0.1)

    sock.sendto("STOP", (UDP_IP, UDP_PORT))
    time.sleep(1)

    for i in range(1,20):
        sock.sendto("GO", (UDP_IP, UDP_PORT))
        time.sleep(0.1)

    sock.sendto("STOP", (UDP_IP, UDP_PORT))
    time.sleep(1)

sock.close()
del sock
