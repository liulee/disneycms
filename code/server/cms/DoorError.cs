using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    public enum DoorError
    {
        Success,          // 正常.
        LeftOpenError,    // 左侧开启未到位
        LeftCloseError,   // 左侧关闭未到位
        RightOpenError,   // 左侧异常
        RightCloseError,  // 右侧异常
        OpenError,        // 双门开启异常
        CloseError,       // 双门关闭异常
        SocketError,      // 连接错误.
        UNKNOWN = 0xFF,   // 未知
    }
}
