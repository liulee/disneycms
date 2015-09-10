using log4net;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DisneyCMS.cms
{
    public class CMSConfig
    {
        public const byte REG_START = 11;
        public const byte REG_SET_DOOR = 0;
        public const byte REG_GET_DOOR_OPEN = 1;
        public const byte REG_GET_DOOR_CLOSE = 2;
        public const byte REG_GET_DOOR_ERROR = 3;
        public const byte REG_GET_LCB = 4;
        public const byte REG_SET_GREEN = 5;
        public const byte REG_SET_RED = 6;
        public const byte REG_CMD_CNT = REG_SET_RED+1;

        private byte _regMin, _regMax;
        private static ILog log = LogManager.GetLogger("CMS");
        private static readonly string CFG_FILE = CMS.CFG_FILE;


        // reg.coil => door.
        private Dictionary<string, Door> _doorMap = new Dictionary<string, Door>();
        private string _ccsip;
        public String GetCcsIp()
        {
            return _ccsip;
        }

        private string GetValue(XElement elm, string att, string dvalue = null)
        {
            XAttribute xa = elm.Attribute(att);
            if (xa != null)
                return xa.Value;
            else
                return dvalue;
        }

        public byte RegMax { get { return _regMax; } }

        public bool IsValidReg(byte reg)
        {
            return reg >= _regMin && reg <= _regMax;
        }

        private int _zoneCnt = 0;
        public int ZoneCnt
        {
            get
            {
                return _zoneCnt;
            }
        }
        public IList<Zone> LoadConfig()
        {
            IList<Zone> _zones = new List<Zone>();
            log.InfoFormat("解析文件: '{0}' ...", CFG_FILE);
            var doorCount = 0;
            try
            {
                XDocument doc = XDocument.Load(CFG_FILE);
                var root = doc.Root;

                var ccse = root.Element("ccs");
                _ccsip = GetValue(ccse, "ip", "192.168.31.254");

                string ipPrefix = GetValue(root, "ipp", "192.168.31");
                byte zcnt = Convert.ToByte(GetValue(root, "zones"));
                _regMin = REG_START;
                _regMax = (byte)(REG_START + zcnt * REG_CMD_CNT); //共 7 组命令.

                byte _funcOffset = 0;
                foreach (var ze in root.Elements("zone"))
                {
                    byte zoneCoil = Convert.ToByte(GetValue(ze, "zreg")); //1~6
                    byte regStart = (byte)(_funcOffset + REG_START);
                    _funcOffset++;

                    Zone z = new Zone()
                    {
                        Name = GetValue(ze, "name"),
                        Reg = new RegInfo()
                        {
                            ZoneCoil = zoneCoil,
                            SetDoor = regStart,
                            GetOpenState = (byte)(regStart + zcnt * REG_GET_DOOR_OPEN),
                            GetCloseState = (byte)(regStart + zcnt * REG_GET_DOOR_CLOSE),
                            GetErrorState = (byte)(regStart + zcnt * REG_GET_DOOR_ERROR),
                            GetLCB = (byte)(regStart + zcnt * REG_GET_LCB),
                            SetGreen = (byte)(regStart + zcnt * REG_SET_GREEN), //5=开绿灯
                            SetRed = (byte)(regStart + zcnt * REG_SET_RED),   // 6=开红灯
                        },
                    };
                    _zones.Add(z);
                    // 该区域的门配置.
                    foreach (var de in ze.Elements("d"))
                    {
                        Door d = new Door();
                        string ip = GetValue(de, "ip", "");
                        if (ip.Length <= 3)
                        {
                            ip = ipPrefix + "." + ip;
                        }
                        d.DevId = Convert.ToByte(GetValue(de, "id", "1"));
                        d.Coil = Convert.ToByte(GetValue(de, "c")); //Coil/Bit
                        d.IpAddr = ip;
                        d.Enabled = GetValue(de, "enable", "1") == "1";
                        z.AddDoor(d);
                        UpdateDoorMap(z, d);
                    }
                    doorCount += z.TotalCnt;
                    log.DebugFormat("区域: {0}, doors={1}", z.Name, z.TotalCnt);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("读取配置文件 {0} 异常:{1}", CFG_FILE, e.Message));
            }
            _zoneCnt = _zones.Count;
            log.InfoFormat("配置已读取, 共 {0} 个区域, {1} 樘门.", _zoneCnt, doorCount);
            return _zones;
        }

        // Reg.Bit => Door
        void UpdateDoorMap(Zone z, Door d)
        {
            _doorMap[string.Format("{0}.{1}", z.Reg.SetDoor, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.SetGreen, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.SetRed, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.GetCloseState, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.GetOpenState, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.GetErrorState, d.Coil)] = d;
            _doorMap[string.Format("{0}.{1}", z.Reg.GetLCB, d.Coil)] = d;
        }
        internal Door FindDoorByAddr(byte reg, byte bit)
        {
            string k = string.Format("{0}.{1}", reg, bit);
            Door d = null;
            _doorMap.TryGetValue(k, out d);
            return d;
        }
    }
}
