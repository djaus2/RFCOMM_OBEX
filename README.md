# RFCOMM_OBEX
A Bluetooth File transfer through OBEXPush UWP app.

Implements code from [docs.Microsoft.com-Bluetooth RFCOMM](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/send-or-receive-files-with-rfcomm)
Note includes corrections to code.

# Usage
- Build the app and run on two Windows 10 desktop targets. (To be tested on IoT Core).
- Press Send on one and choose a text file to send.
- Press Recv on other target and choose file to save as. 

***Note** although filename gets transmitted, the file gets saved as the Save As filename.*
Also the services are initiated when the send and receive are actioned and disposed of when done.

