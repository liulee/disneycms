using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    /**
     * 各樘门寄存器信息
     */
    public class RegInfo
    {
        /// W: 总控中的 Coil, 0-5, 同时对应区域索引.
        public byte ZoneCoil { get; set; }

        /// W: 设置: 打开
        public byte SetDoor { get; set; }
        // R: 获取打开到位状态
        public byte GetOpenState { get; set; }
        // R: 获取关闭到位状态
        public byte GetCloseState { get; set; }
        // R: 获取异常状态
        public byte GetErrorState { get; set; }
        // R: 离线:LCB
        public byte GetLCB { get; set; }
        // RW: 绿灯
        public byte SetGreen { get; set; }
        // RW: 红灯
        public byte SetRed { get; set; }

    }
}
