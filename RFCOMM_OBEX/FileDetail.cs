using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFCOMM_OBEX
{
    public class Constants
    {
        //From RFCOMM Chat Server:
        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.

        // attribute = attributes[Constants.SERVICE_VERSION_ATTRIBUTE_ID];
        // attribute will be 5 bytes:
        // The first byte contains the attribute's type
        // The remainder is the data (the version as 4 bytes)

        // BluetoothCacheMode.Uncached:
        public const uint SERVICE_VERSION_ATTRIBUTE_ID = 0x0300;
        public const byte SERVICE_VERSION_ATTRIBUTE_TYPE = 0x0A;   // UINT32 1100
        public const uint SERVICE_VERSION = 200;
        public const uint MINIMUM_SERVICE_VERSION = 200;
        public const string EndTransmission = "__DONE__";
        public const double Timeout = 30;
    }

    public class FileDetail
    {
        public string filename { get; set; } = "";
        public string txt { get; set; } = "";
    }
}
