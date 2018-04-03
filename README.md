s: :# RFCOMM_OBEX
A Bluetooth File transfer through OBEXPush UWP app.

Implements code from [docs.Microsoft.com-Bluetooth RFCOMM](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/send-or-receive-files-with-rfcomm)
Note includes corrections to code.

# Usage
- Build the app and run on two Windows 10 desktop targets. (To be tested on IoT Core).
- **FIRST** Press Recv on one target and choose file to Save As (This is a dummy, see Notes).
- Press Send on the other instance of the app and choose a text file to send.

***Notes** You must initiate the Receiver first. Also, the filename gets transmitted, but initially the file is saved as the Save As filename. It then gets renamed as the transmitted name. But if the Save As named file exists as a file that will be deleted.
Finally, the services are initiated when the send and receive are actioned and disposed of when done.

Could improve code by the Receiver always being ready and user prompted when a file is received.

