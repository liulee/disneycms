using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    // 分区.
    public class Zone
    {
        public string Name { get; set; }
        public RegInfo Reg { get; set; }
        private IList<Door> _doors = null;
        
        // 状态扫描.
        public ZoneScanner Scanner { get; set; }

        public ZoneState State { get; set; }

        public int TotalCnt
        {
            get
            {
                return _doors != null ? _doors.Count : 0;
            }
        }
        public int OpenCnt { get; set; }
        public int CloseCnt {get;set;}
        public int ErrorCnt { get; set; }
        public int UnknownCnt
        {
            get
            {
                return TotalCnt - ErrorCnt - OpenCnt - CloseCnt;
            }
        }

        public bool HasException
        {
            get
            {
                return ErrorCnt > 0 || UnknownCnt > 0;
            }
        }

        public string Statistics
        {
            get
            {
                return string.Format("门:{0}, 未知:{1}, 开:{2}, 关:{3}, 异常:{4}", TotalCnt, UnknownCnt, OpenCnt, CloseCnt, ErrorCnt);
            }
        }

        public Door[] Doors
        {
            get { return _doors != null ? _doors.ToArray<Door>() : new Door[0]; }
        }

        public void AddDoor(Door d)
        {
            if (_doors == null)
            {
                _doors = new List<Door>();
            }
            _doors.Add(d);
        }

        // 本区域所有门开启数> 50%, 则认为区域开启. 中控箱相关位置设置输出为 ACTION;
        public bool IsZoneOpen()
        {
            return OpenCnt >= TotalCnt / 2;
        }
    }
}
