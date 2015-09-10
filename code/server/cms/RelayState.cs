using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    // 继电器状态.
    public enum RelayState
    {
        RESET,  // 0=RESET
        ACTION, // 1=Action(动作)
        UNKNOWN
    }

    public enum InputState
    {
        OFF, //0=Off
        ON, // 1=On
        UNKNOWN = -1
    }
}
