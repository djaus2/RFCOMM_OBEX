using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace RFCOMM_OBEX
{
    class OBEX_Sender
    {
        Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
        Windows.Networking.Sockets.StreamSocket _socket;

        private void PostMessage(string method, string msg)
        {
            MainPage.root.PostMessage(method, msg);
        }

        public async Task Initialize()
        {
            try
            {
                // Enumerate devices with the object push service
                var services =
                    await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                        RfcommDeviceService.GetDeviceSelector(
                            RfcommServiceId.ObexObjectPush));

                //Windows.Devices.Enumeration.DeviceInformationCollection DeviceInfoCollection = 
                //    await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                //        RfcommDeviceService.GetDeviceSelector(
                //            RfcommServiceId.SerialPort));

                //var rfcommProvider = 
                //    await RfcommServiceProvider.CreateAsync(
                //        RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid));



                if (services.Count > 0)
                {
                    // Initialize the target Bluetooth BR device
                    var service = await RfcommDeviceService.FromIdAsync(services[0].Id);

                    // Check that the service meets this App's minimum requirement
                    bool isCompatible = await IsCompatibleVersion(service);
                    if (SupportsProtection(service) && isCompatible)
                    {
                        _service = service;

                        // Create a socket and connect to the target
                        _socket = new StreamSocket();
                        await _socket.ConnectAsync(
                            _service.ConnectionHostName,
                            _service.ConnectionServiceName,
                            SocketProtectionLevel
                                .BluetoothEncryptionAllowNullAuthentication);

                        // The socket is connected. At this point the App can wait for
                        // the user to take some action, e.g. click a button to send a
                        // file to the device, which could invoke the Picker and then
                        // send the picked file. The transfer itself would use the
                        // Sockets API and not the Rfcomm API, and so is omitted here for
                        // brevity.
                    }
                }
            }
            catch (Exception ex)
            {
                PostMessage("OBEX_Sender.Initialize", ex.Message);
            }
        }

        // This App requires a connection that is encrypted but does not care about
        // whether its authenticated.
        bool SupportsProtection(RfcommDeviceService service)
        {
            try
            {
                switch (service.ProtectionLevel)
                {
                    case SocketProtectionLevel.PlainSocket:
                        if ((service.MaxProtectionLevel == SocketProtectionLevel
                                .BluetoothEncryptionWithAuthentication)
                            || (service.MaxProtectionLevel == SocketProtectionLevel
                                .BluetoothEncryptionAllowNullAuthentication))
                        {
                            // The connection can be upgraded when opening the socket so the
                            // App may offer UI here to notify the user that Windows may
                            // prompt for a PIN exchange.
                            return true;
                        }
                        else
                        {
                            // The connection cannot be upgraded so an App may offer UI here
                            // to explain why a connection won't be made.
                            return false;
                        }
                    case SocketProtectionLevel.BluetoothEncryptionWithAuthentication:
                        return true;
                    case SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication:
                        return true;
                }
            }
            catch (Exception ex)
            {
                PostMessage("OBEX_Sender.SupportsProtection",ex.Message); 
            }
            return false;
        }

        // This App relies on CRC32 checking available in version 2.0 of the service.
        const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32
        const uint MINIMUM_SERVICE_VERSION = 200;
        private async Task<bool> IsCompatibleVersion(RfcommDeviceService service)
        {
            try
            {
                var attributes = await service.GetSdpRawAttributesAsync(
                    BluetoothCacheMode.Uncached);
                var attribute = attributes[SERVICE_VERSION_ATTRIBUTE_ID];
                var reader = DataReader.FromBuffer(attribute);

                // The first byte contains the attribute' s type
                byte attributeType = reader.ReadByte();
                if (attributeType == SERVICE_VERSION_ATTRIBUTE_TYPE)
                {
                    // The remainder is the data
                    uint version = reader.ReadUInt32();
                    return version >= MINIMUM_SERVICE_VERSION;
                }
            } catch (Exception ex)
            {
                PostMessage("OBEX_Sender.IsCompatibleVersion", ex.Message);
            }
            return false;
        }

        public async Task Send(string stringToSend, string filename)
        {
            try

            {
                // Create a DataWriter if we did not create one yet. Otherwise use one that is already cached.
                DataWriter writer;
                writer = new DataWriter(_socket.OutputStream);


                // Write first the length of the string as UINT32 value followed up by the string. 

                // Writing filename and file contents data to the writer will just store data in memory.
                writer.WriteUInt32(writer.MeasureString(filename));
                writer.WriteString(filename);
                writer.WriteUInt32(writer.MeasureString(stringToSend));
                writer.WriteString(stringToSend);

                // Write the locally buffered data to the network.
                await writer.StoreAsync();
            }

            catch (Exception ex)
            {
                PostMessage("OBEX_Sender.Send", ex.Message);
            }
        }
    }
}
