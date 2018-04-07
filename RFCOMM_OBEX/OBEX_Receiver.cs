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
    class OBEX_Receiver: IDisposable
    {
        Windows.Devices.Bluetooth.Rfcomm.RfcommServiceProvider _provider;
        //StreamSocket _socket = null;
        //DataReader reader = null;



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
                PostMessage("OBEX_Receiver.Initialize", "Listening");
            }
            catch (Exception ex)
            {
                PostMessage("OBEX_Receiver.Initialize", ex.Message);
            }
        }


        void InitializeServiceSdpAttributes(RfcommServiceProvider provider)
        {
            try
            {
                var writer = new Windows.Storage.Streams.DataWriter();

                // First write the attribute type
                writer.WriteByte(Constants.SERVICE_VERSION_ATTRIBUTE_TYPE);
                // Then write the data
                writer.WriteUInt32(Constants.SERVICE_VERSION);

                var data = writer.DetachBuffer();
                provider.SdpRawAttributes.Add(Constants.SERVICE_VERSION_ATTRIBUTE_ID, data);
                //Check attributes
                try
                {
                    var attributes = provider.SdpRawAttributes;
                    // BluetoothCacheMode.Uncached);
                    var attribute = attributes[Constants.SERVICE_VERSION_ATTRIBUTE_ID];
                    var reader = DataReader.FromBuffer(attribute);

                    // The first byte contains the attribute' s type
                    byte attributeType = reader.ReadByte();
                    if (attributeType == Constants.SERVICE_VERSION_ATTRIBUTE_TYPE)
                    {
                        // The remainder is the data
                        uint version = reader.ReadUInt32();
                        bool ret = (version >= Constants.MINIMUM_SERVICE_VERSION);
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

                        //Read filename then file contents
                        for (int i = 0; (i < 2) ; i++)
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
                                        if (message == Constants.EndTransmission)
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
            Connected = false;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Connected = false;
                    MainPage.root.RecvConnected = false;
                    //if (_socket != null)
                    //    _socket.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OBEX_Receiver() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        internal void Cancel()
        {
            Connected = false;
        }
        #endregion
    }
}
