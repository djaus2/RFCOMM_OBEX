using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFCOMM_OBEX
{
    public class FileDetail
    {
        public const string EndTransmission = "__DONE__";
        public string filename { get; set; } = "";
        public string txt { get; set; } = "";
    }
}
