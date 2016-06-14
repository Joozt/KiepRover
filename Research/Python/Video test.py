from threading import Timer
import socket
from Tkinter import Tk, Label
from PIL import ImageTk, Image  # install using: pip install Pillow
from io import BytesIO 

UDP_IP = "192.168.1.1"
UDP_PORT_CONTROL = 5201
UDP_PORT_VIDEO = 5207
MESSAGE_INIT_VIDEO = "VIEW\0x00\0x00\0x00\0x00Q-F"
MESSAGE_KEEPALIVE = "KEEPALIVE"

# Ask iCloudRover via UDP control socket to start sending video
controlsock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
controlsock.sendto(MESSAGE_INIT_VIDEO, (UDP_IP, UDP_PORT_CONTROL))
print(MESSAGE_INIT_VIDEO)

# Setup window with an image label
image_window_root = Tk()
image_window_label = Label(image_window_root)

# Setup receiving socket for JPG images over UDP
videosock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
videosock.bind(("0.0.0.0", UDP_PORT_VIDEO))

# Send keepalive every second
def keepalive():
    Timer(1.0, keepalive).start()
    controlsock.sendto(MESSAGE_KEEPALIVE, (UDP_IP, UDP_PORT_CONTROL))
    print(MESSAGE_KEEPALIVE)
keepalive()

buffer = bytes()
frame_count = 0
while True:

    # Receive 4kb video data in string
    data = videosock.recv(4096)
    
    # Append video data to buffer, skipping first 3 characters
    buffer += data[3:]
    
    # Search for JPG start and end
    jpg_start = buffer.find(b'\xff\xd8')
    jpg_end = buffer.find(b'\xff\xd9')
    
    # Process the JPG if start and end are found
    if jpg_start != -1 and jpg_end != -1:
        frame_count +=1
        
        # Copy the JPG characters from the buffer
        jpg = buffer[jpg_start:jpg_end+2]
        
        # Remove the JPG characters from the buffer
        buffer = buffer[jpg_end+2:]
        
        print('frame detected, length 2{:d}, number {:d}'.format(len(jpg), frame_count))

        try:
            # Open the JPG string as an Image
            img = Image.open(BytesIO(jpg))
            
            # Transform the Image to a PhotoImage that can be placed on Tkinter label
            tki = ImageTk.PhotoImage(img)
            
            # Update the label with the new image
            image_window_label.configure(image=tki)
            image_window_label.pack()
            image_window_root.update()
        except:
            print("FAIL")