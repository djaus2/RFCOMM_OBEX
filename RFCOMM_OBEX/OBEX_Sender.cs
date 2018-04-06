using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace RFCOMM_OBEX
{
    class OBEX_Sender: IDisposable
    {
        Windows.Devices.Bluetooth.Rfcomm.RfcommDeviceService _service;
       
        private bool IsConnected = false;

        private void PostMessage(string method, string msg)
        {
            MainPage.root.PostMessage(method, msg);
        }

        public async Task<StreamSocket> InitializeSendSocket()
        {
            Windows.Networking.Sockets.StreamSocket _socket = null;
            try
            {
                // Enumerate devices with the object push service
                var services =
                    await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                        RfcommDeviceService.GetDeviceSelector(
                            RfcommServiceId.ObexObjectPush));

                PostMessage("OBEX_Sender.Initialize", string.Format("Services count = {0}", services.Count));

                if (services.Count > 0)
                {
                    // Initialize the target Bluetooth BR device
                    var service = await RfcommDeviceService.FromIdAsync(services[0].Id);

                    // Check that the service meets this App's minimum requirement
                    bool isCompatible = await IsCompatibleVersion(service);
                    if ((IgnoreAttributeErrors) && (!isCompatible))
                    {
                        PostMessage("OBEX_Sender.Initialize", "Ignoring attribute error.");
                        isCompatible = true;
                    }
                    if (SupportsProtection(service) && isCompatible)
                    {
                        _service = service;

                        // Create a socket and connect to the target
                        _socket = new StreamSocket();
                        PostMessage("OBEX_Sender.Initialize", "Connecting ...");
                        IsConnected = false;
                        await _socket.ConnectAsync(
                            _service.ConnectionHostName,
                            _service.ConnectionServiceName,
                            SocketProtectionLevel
                                .BluetoothEncryptionAllowNullAuthentication);
                        IsConnected = true;
                        PostMessage("OBEX_Sender.Initialize", "Is Connected");
                        // The socket is connected. At this point the App can wait for
                        // the user to take some action, e.g. click a button to send a
                        // file to the device, which could invoke the Picker and then
                        // send the picked file. The transfer itself would use the
                        // Sockets API and not the Rfcomm API, and so is omitted here for
                        // brevity.
                    }
                    else
                    {
                        IsConnected = false;
                        if (_socket != null)
                            _socket.Dispose();
                        PostMessage("OBEX_Sender.Initialize", "Service Not Compatible Or does not Support Protection Level ");
                    }
                }
                else
                {
                    IsConnected = false;
                    if (_socket != null)
                        _socket.Dispose();
                    PostMessage("OBEX_Sender.Initialize", "No Services");
                }

            }
            catch (Exception ex)
            {
                if (_socket != null)
                    _socket.Dispose();
                PostMessage("OBEX_Sender.Initialize", ex.Message);
            }
            return _socket;
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
                if (attributes != null)
                {
                    var lst = attributes.Keys.ToList<uint>();
                    if (attributes.Keys.Contains(SERVICE_VERSION_ATTRIBUTE_ID))
                    {
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
                    }
                    else
                    {
                        PostMessage("OBEX_Sender.IsCompatibleVersion", string.Format("Service Attribute Count: {0}", attributes.Count()));
                        PostMessage("OBEX_Sender.IsCompatibleVersion", "SERVICE_VERSION_ATTRIBUTE_ID not in Service Attributes");
                    }
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
                CancellationTokenSource source = new CancellationTokenSource();
                source.CancelAfter(TimeSpan.FromSeconds(FileDetail.Timeout));
                Task task = Task.Run(() => SendWithCancel(stringToSend, filename, source.Token), source.Token);
                await task;
            } catch (TaskCanceledException ex)
            {
                PostMessage("OBEX_Sender.Send", "Was cancelled or timed out");
            }
        }

        public async Task SendWithCancel(string stringToSend, string filename, CancellationToken cancellationToken)
        {
            StreamSocket _socket =  await InitializeSendSocket();

            if (_socket == null)
            {
                PostMessage("OBEX_Sender.Send", "Not connected");
            }
            else
            {
                try
                {
                    // Create a DataWriter if we did not create one yet. Otherwise use one that is already cached.
                    using (DataWriter writer = new DataWriter(_socket.OutputStream))
                    {
                        // Write first the length of the string as UINT32 value followed up by the string. 

                        // Writing filename and file contents data to the writer will just store data in memory.
                        writer.WriteUInt32(writer.MeasureString(filename));
                        writer.WriteString(filename);
                        writer.WriteUInt32(writer.MeasureString(stringToSend));
                        writer.WriteString(stringToSend);

                        // Write the locally buffered data to the network.
                        await writer.StoreAsync();
                        await _socket.OutputStream.FlushAsync();
                        writer.DetachStream();
                        PostMessage("OBEX_Sender.Send", "Message Sent");
                    }
                    _socket.Dispose();
                }
                catch (Exception ex)
                {
                    if (_socket != null)
                        _socket.Dispose();
                    PostMessage("OBEX_Sender.Send", ex.Message);
                }
                _socket = null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public static bool IgnoreAttributeErrors { get; internal set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (IsConnected)
                    {
                        IsConnected = false;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OBEX_Sender() {
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
        #endregion
    }
}
