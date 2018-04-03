:# RFCOMM_OBEX
A Bluetooth File transfer through OBEXPush UWP app.

Implements code from [docs.Microsoft.com-Bluetooth RFCOMM](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/send-or-receive-files-with-rfcomm)
Note includes corrections to code.

# Usage
- Build the app and run on two Windows 10 desktop targets. (To be tested on IoT Core).
- Press Recv on one target and choose file to save as **FIRST**.
- Press Send on the other instance of the app and choose a text file to send.

***Note** The filename gets transmitted, but initially get saved as the gets saved as the Save As filename. It then gets renamed as teh transmitted name. But if the Save As name exists as a file that will be deleted.*
Also the services are initiated when the send and receive are actioned and disposed of when done.

