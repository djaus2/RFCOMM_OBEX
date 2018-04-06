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
        //StreamSocket _socket = null;
        //DataReader reader = null;

        ~OBEX_Receiver()
        {
            
        }

        private void PostMessage(string method, string msg)
        {
            MainPage.root.PostMessage(method, msg);
        }

        public async Task Initialize()
        {
            try
            {
                // Initialize the provider for the hosted RFCOMM service
                _provider = await Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider.CreateAsync(RfcommServiceId.ObexObjectPush);
                //reader = null;
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
        const uint MINIMUM_SERVICE_VERSION = 200;
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
                //Check attributes
                try
                {
                    var attributes = provider.SdpRawAttributes;
                    // BluetoothCacheMode.Uncached);
                    var attribute = attributes[SERVICE_VERSION_ATTRIBUTE_ID];
                    var reader = DataReader.FromBuffer(attribute);

                    // The first byte contains the attribute' s type
                    byte attributeType = reader.ReadByte();
                    if (attributeType == SERVICE_VERSION_ATTRIBUTE_TYPE)
                    {
                        // The remainder is the data
                        uint version = reader.ReadUInt32();
                        bool ret = (version >= MINIMUM_SERVICE_VERSION);
                    }
                }
                catch (Exception ex)
                {
                    PostMessage("OBEX_Recv.InitializeServiceSdpAttributes_Check", ex.Message);
                }
            }
            catch (Exception ex)
            {
                PostMessage("OBEX_Receiver.InitializeServiceSdpAttributes", ex.Message);
            }
        }


        void OnConnectionReceived(
            StreamSocketListener listener,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try {
                // Stop advertising/listening so that we're only serving one client
                PostMessage("OBEX_Receiver.OnConnectionReceived", "Connection Received");
                _provider.StopAdvertising();
                //listener.Dispose();
                StreamSocket _socket = args.Socket;

                var t = Task.Run(async () =>
                {
                    await ReadAsync(_socket);
                });


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


        public static bool Connected {get; set;}= true;
        public async Task ReadAsync(StreamSocket _socket)
        {
            FileDetail fi = new FileDetail();
            Connected = true;
            MainPage.root.RecvConnected = true;
            using (DataReader reader = new DataReader(_socket.InputStream))
            {
                try
                {
                    while (Connected)
                    {
                        //Read filename then file contents
                        for (int i = 0; (i < 2) && Connected; i++)
                        {
                            if (_socket == null)
                                Connected = false;
                            else if (_socket.InputStream == null)
                                Connected = false; ;

                            if (Connected)
                            {
                                // Based on the protocol we've defined, the first uint is the size of the message
                                uint readLength = await reader.LoadAsync(sizeof(uint));

                                // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                                if (readLength < sizeof(uint))
                                {
                                    //remoteDisconnection = true;
                                    Connected = false;
                                }
                                else
                                {
                                    uint currentLength = reader.ReadUInt32();

                                    // Load the rest of the message since you already know the length of the data expected.  
                                    readLength = await reader.LoadAsync(currentLength);

                                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                                    if (readLength < currentLength)
                                    {
                                        Connected = false;
                                    }
                                    else
                                    {
                                        string message = reader.ReadString(currentLength);
                                        if (message == FileDetail.EndTransmission)
                                            Connected = false;
                                        else
                                        {
                                            if (i == 0)
                                                fi.filename = message;
                                            else
                                            {
                                                fi.txt = message;
                                                MainPage.root.SaveFile(fi);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    PostMessage("OBEX_Receiver.ReadAsync", ex.Message);
                    fi = null;
                    Connected = false;
                }
                reader.DetachStream();               
            }
            MainPage.root.RecvConnected = false; 
            if (_socket != null)
                _socket.Dispose();
        }
    }
}
