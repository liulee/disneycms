using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.modbus
{
    public class MBException
    {
        public const byte MB_SUCCESS = 0;
        public const byte E01_ILLEGAL_FUNCTION = 0x01;
        public const byte E02_ILLEGAL_DATA_ADDRESS = 0x02;
        public const byte E03_ILLEGAL_DATA_VALUE = 0x03;
//        public const byte E04_ILLEGAL_RESPONSE_LENGTH = 0x04;
//        public const byte E05_ACKNOWLEDGE = 0x05;
//        public const byte E06_SLAVE_DEVlCE_BUSY = 0x06;
//        public const byte E07_NEGATlVE_ACKNOWLEDGE = 0x07;
//        public const byte E08_MEMORY_PARlTY_ERROR = 0x08;
//        public const byte E0A_GATEWAY_PATH_UNAVAILABLE = 0x0A;
        public const byte E0B_GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0B;



        public static string NameOf(byte err)
        {
            switch (err)
            {
                case MB_SUCCESS:
                    return "无";
                case E01_ILLEGAL_FUNCTION:
                    return "无效功能码";
                case E02_ILLEGAL_DATA_ADDRESS:
                    return "无效数据地址";
                case E03_ILLEGAL_DATA_VALUE:
                    return "无效数据值";
                case E0B_GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND:
                    return "Gateway target device failed to response.";
            }
            return "";
        }
    }

}
