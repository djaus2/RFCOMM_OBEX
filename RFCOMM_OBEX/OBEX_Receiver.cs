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

        void  OnConnectionReceived(
            StreamSocketListener listener,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Stop advertising/listening so that we're only serving one client
            _provider.StopAdvertising();
            listener.Dispose();
            _socket = args.Socket;

            // The client socket is connected. At this point the App can wait for
            // the user to take some action, e.g. click a button to receive a file
            // from the device, which could invoke the Picker and then save the
            // received file to the picked location. The transfer itself would use
            // the Sockets API and not the Rfcomm API, and so is omitted here for
            // brevity.
        }

        private async void ConnectSocket_Click(object sender, RoutedEventArgs e)
        {
        //    if (CoreApplication.Properties.ContainsKey("clientSocket"))
        //    {
        //        rootPage.NotifyUser(
        //            "This step has already been executed. Please move to the next one.",
        //            NotifyType.ErrorMessage);
        //        return;
        //    }

        //    if (String.IsNullOrEmpty(ServiceNameForConnect.Text))
        //    {
        //        rootPage.NotifyUser("Please provide a service name.", NotifyType.ErrorMessage);
        //        return;
        //    }

        //    // By default 'HostNameForConnect' is disabled and host name validation is not required. When enabling the
        //    // text box validating the host name is required since it was received from an untrusted source
        //    // (user input). The host name is validated by catching ArgumentExceptions thrown by the HostName
        //    // constructor for invalid input.
        //    HostName hostName;
        //    try
        //    {
        //        hostName = new HostName(HostNameForConnect.Text);
        //    }
        //    catch (ArgumentException)
        //    {
        //        rootPage.NotifyUser("Error: Invalid host name.", NotifyType.ErrorMessage);
        //        return;
        //    }

        //    StreamSocket socket = new StreamSocket();

        //    // If necessary, tweak the socket's control options before carrying out the connect operation.
        //    // Refer to the StreamSocketControl class' MSDN documentation for the full list of control options.
        _socket.Control.KeepAlive = false;

        //    // Save the socket, so subsequent steps can use it.
        //    CoreApplication.Properties.Add("clientSocket", socket);
        //    try
        //    {
        //        if (adapter == null)
        //        {
        //            rootPage.NotifyUser("Connecting to: " + HostNameForConnect.Text, NotifyType.StatusMessage);

        //            // Connect to the server (by default, the listener we created in the previous step).
        //            await socket.ConnectAsync(hostName, ServiceNameForConnect.Text);

        //            rootPage.NotifyUser("Connected", NotifyType.StatusMessage);
        //        }
        //        else
        //        {
        //            rootPage.NotifyUser(
        //                "Connecting to: " + HostNameForConnect.Text +
        //                " using network adapter " + adapter.NetworkAdapterId,
        //                NotifyType.StatusMessage);

        //            // Connect to the server (by default, the listener we created in the previous step)
        //            // limiting traffic to the same adapter that the user specified in the previous step.
        //            // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
        //await _socket.ConnectAsync(
        //    hostName,
        //    ServiceNameForConnect.Text,
        //    SocketProtectionLevel.PlainSocket,
        //    adapter);
        //    _socket.InputStream.

        //            rootPage.NotifyUser(
        //                "Connected using network adapter " + adapter.NetworkAdapterId,
        //                NotifyType.StatusMessage);
        //        }

        //        // Mark the socket as connected. Set the value to null, as we care only about the fact that the 
        //        // property is set.
        //        CoreApplication.Properties.Add("connected", null);
        //    }
        //    catch (Exception exception)
        //    {
        //        // If this is an unknown status it means that the error is fatal and retry will likely fail.
        //        if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
        //        {
        //            throw;
        //        }

        //        rootPage.NotifyUser("Connect failed with error: " + exception.Message, NotifyType.ErrorMessage);
        //    }
        }
    }
}
