﻿using System;
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

        public async Task Initialize()
        {
            // Initialize the provider for the hosted RFCOMM service
            _provider = await Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider.CreateAsync(RfcommServiceId.ObexObjectPush);

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

        StreamSocketListener listener2;


        const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32
        const uint SERVICE_VERSION = 200;
        void InitializeServiceSdpAttributes(RfcommServiceProvider provider)
        {
            var writer = new Windows.Storage.Streams.DataWriter();

            // First write the attribute type
            writer.WriteByte(SERVICE_VERSION_ATTRIBUTE_TYPE);
            // Then write the data
            writer.WriteUInt32(SERVICE_VERSION);

            var data = writer.DetachBuffer();
            provider.SdpRawAttributes.Add(SERVICE_VERSION_ATTRIBUTE_ID, data);
        }

        StreamSocket _socket;
        DataReader reader;

        void  OnConnectionReceived(
            StreamSocketListener listener,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Stop advertising/listening so that we're only serving one client
            _provider.StopAdvertising();
            listener.Dispose();
            _socket = args.Socket;
            var reader = new DataReader(_socket.InputStream);

            // The client socket is connected. At this point the App can wait for
            // the user to take some action, e.g. click a button to receive a file
            // from the device, which could invoke the Picker and then save the
            // received file to the picked location. The transfer itself would use
            // the Sockets API and not the Rfcomm API, and so is omitted here for
            // brevity.
        }



        public async Task<FileDetail> ReadAsync()
        {
            FileDetail fi = new FileDetail();
            try
            {
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
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                //    //MP.NotifyUser("Client Disconnected Successfully", NotifyType.StatusMessage);
                //    status.Text = string.Format("{0}: {1}", MainPage.NotifyType.StatusMessage, "Client Disconnected Successfully");
                //});
                fi = null;
            }
            

            reader.DetachStream();
            return fi;
        }
    }

    public class FileDetail
    {
        public string filename { get; set; } = "";
        public string txt { get; set; } = "";
    }
}