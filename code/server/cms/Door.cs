using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    public class Door
    {
        // Coil
        public byte Coil { get; set; }

        // JDQ Device ID
        public byte DevId { get; set; }

        // IP Address
        public string IpAddr { get; set; }

        // 是否启用.
        public bool Enabled { get; set; }

        // 状态.
        public DoorState State { get; set; }

    }
}
