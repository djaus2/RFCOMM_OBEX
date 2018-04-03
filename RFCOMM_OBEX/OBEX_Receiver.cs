using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace RFCOMM_OBEX
{
    class OBEX_Receiver
    {
        Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider _provider;
        StreamSocket _socket = null;
        DataReader reader = null;

        private void PostMessage(string method, string msg)
        {
            MainPage.root.PostMessage(method,msg);
        }

        public async Task Initialize()
        {
            try
            {
                // Initialize the provider for the hosted RFCOMM service
                _provider = await Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider.CreateAsync(RfcommServiceId.ObexObjectPush);
                reader = null;
                // Create a listener for this service and start listening
                StreamSocketListener listener = new StreamSocketListener();
                listener.ConnectionReceived += OnConnectionReceived;
                await listener.BindServiceNameAsync(
                    _provider.ServiceId.AsString(),
                    SocketProtectionLevel
                        .BluetoothEncryptionAllowNullAuthentication);

                // Set the SDP attributes and start advertising
                InitializeServiceSdpAttributes(_provider);
                _provider.StartAdvertising(listener);
            }
            catch (Exception ex)
            {
                PostMessage("OBEX_Receiver.Initialize", ex.Message);
            }
        }


        const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32
        const uint SERVICE_VERSION = 200;
        void InitializeServiceSdpAttributes(RfcommServiceProvider provider)
        {
            try
            { 
                var writer = new Windows.Storage.Streams.DataWriter();

                // First write the attribute type
                writer.WriteByte(SERVICE_VERSION_ATTRIBUTE_TYPE);
                // Then write the data
                writer.WriteUInt32(SERVICE_VERSION);

                var data = writer.DetachBuffer();
                provider.SdpRawAttributes.Add(SERVICE_VERSION_ATTRIBUTE_ID, data);
            }
            catch (Exception ex)
            { 
                PostMessage("OBEX_Receiver.InitializeServiceSdpAttributes", ex.Message);
            }
        }


        void  OnConnectionReceived(
            StreamSocketListener listener,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try {
                // Stop advertising/listening so that we're only serving one client
                _provider.StopAdvertising();
                listener.Dispose();
                _socket = args.Socket;
                reader = new DataReader(_socket.InputStream);

                // The client socket is connected. At this point the App can wait for
                // the user to take some action, e.g. click a button to receive a file
                // from the device, which could invoke the Picker and then save the
                // received file to the picked location. The transfer itself would use
                // the Sockets API and not the Rfcomm API, and so is omitted here for
                // brevity.
            }
            catch (Exception ex)
            { 
                PostMessage("OBEX_Receiver.OnConnectionReceived", ex.Message);
            }
        }



        public async Task<FileDetail> ReadAsync()
        {
            FileDetail fi = new FileDetail();
            try { 
                //Wait for connection
                while(reader == null);
                
                //Read filename then file contents
                for (int i = 0; i < 2; i++)
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        //remoteDisconnection = true;
                        return null;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        //remoteDisconnection = true;
                        return null;
                    }
                    string message = reader.ReadString(currentLength);
                    if (i == 0)
                        fi.filename = message;
                    else
                        fi.txt = message;
                }
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
            catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
            {
                PostMessage("OBEX_Receiver.ReadAsync", ex.Message);
                fi = null;
            }


            reader.DetachStream();
            return fi;
        }


    }

}
