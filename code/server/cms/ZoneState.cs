using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    public class ZoneState
    {
        // 开/关.
        RelayState Action { get; set; }

        // 状态.
        OnOff State { get; set; }

        // 异常.
        OnOff Error {get;set;}
    }
}
