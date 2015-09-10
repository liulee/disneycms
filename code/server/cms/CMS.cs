using DisneyCMS.modbus;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml.Linq;

namespace DisneyCMS.cms
{

    public delegate void OnCcsStateChangedCallback(SocketError lastError, OnOff LBP, OnOff FAS);
    public class CMS
    {
        private static readonly uint CONN_TIMEOUT = 1000; //连接超时: 1秒.
        private static readonly ushort CONN_PORT = 50000; //连接端口, 

        private const byte REG_SUMMARY = 1;
        private const byte REG_ZONE_SUMMARY = 6;
        private const byte RELAY_OPEN_LEFT = 0; // 1=左
        private const byte RELAY_OPEN_RIGHT = 2; //3=右
        private const byte RELAY_GREEN = 4; // 5=绿灯
        private const byte RELAY_RED = 5;  // 6=红灯
        private const byte RELAY_BEEP = 6; // 7= 蜂鸣器

        public static readonly string CFG_FILE = "cms.xml";
        private SocketClient _ccs; // 中控.
        private MBServer _mbserver;
        private CMSConfig _config;
        private static ILog log = LogManager.GetLogger("CMS");
        private List<Zone> _zones = new List<Zone>();
        private bool _initialized = false;
        private bool _running = false;
        private System.Threading.Timer _ccsTimer;
        private JDQVisitor _jdqVisitor = new JDQVisitor();

        // 点表,读取时, 直接返回, 
        // 由 ZoneScanner 更新状态;
        // 由 MBServer  写入
        // 由 CMSServer 执行动作.
        public const byte REG_CNT = 53;
        public const byte INVALID_COIL_VAL = 0xFF;
        private PointTable _ptable = new PointTable(REG_CNT);

        // 门状态变化: 订阅出去.
        public DoorStateChangedCallback OnDoorStateChanged;
        public ZoneStateChangedCallback OnZoneStateChanged;
        public OnCcsStateChangedCallback OnCcsStateChanged;
        public CMS(MBServer _mbserver)
        {
            _config = new CMSConfig();
            this._mbserver = _mbserver;    //Modbus服务.
            int dueTime = 2000; // Don't start.
            this._ccsTimer = new System.Threading.Timer(new TimerCallback(OnTimerCallback), null, dueTime, 1000);
            this.IBP = OnOff.UNKNOWN;
            this.FAS = OnOff.UNKNOWN;
        }

        /**
         * 初始化:
         * 1. 读取配置;
         * 2. 绑定 门/区域变更委托关系;
         */
        public bool init()
        {
            if (_initialized) return _initialized;
            _initialized = false;
            try
            {
                IList<Zone> zones = _config.LoadConfig();
                _zones.AddRange(zones);
                foreach (Zone z in zones)
                {
                    z.Scanner = new ZoneScanner(z, this._jdqVisitor);
                    z.Scanner.OnDoorStateChanged += this._OnDoorStateChanged;
                    z.Scanner.OnZoneStateChanged += this._OnZoneStateChanged;
                }
                _initialized = true;
            }
            catch (Exception e)
            {
                log.ErrorFormat("加载配置异常: {0}", e.Message);
            }

            // 中控连接.
            return _initialized;
        }

        public OnOff IBP { get; set; }
        public OnOff FAS { get; set; }

        private void OnTimerCallback(object state)
        {
            if (_running)
            {
                if (_ccs == null)
                {
                    return;
                }
                lock (_ccs)
                {
                    try
                    {
                        JDQRequest req1 = new JDQRequest(1, JDQRequestType.ReadInput); // Read FAS/IBP
                        SocketError err;
                        byte[] recv = _ccs.SSend(req1.Encode(), out err);
                        OnOff ibp, fas;
                        if (SocketError.Success == err)
                        {
                            // IN1	IBP触发信号	信号有效：IBP被触发
                            // IN2	FAS触发信号	信号有效：FAS被触发
                            JDQResponse resp = new JDQResponse(JDQRequestType.ReadInput, recv);
                            if (resp.GetLength() == 0) return;
                            ibp = resp.GetInputState(0);
                            fas = resp.GetInputState(1);
                        }
                        else
                        {
                            ibp = OnOff.UNKNOWN;
                            fas = OnOff.UNKNOWN;
                        }
                        TryNotifyIpbFas(err, ibp, fas);
                        Thread.Sleep(10);
                    }
                    catch (Exception) { }
                }
            }
        }

        private bool _fasNotified, _ibpNotified;
        /// 收到 IBP / FAS
        private void TryNotifyIpbFas(SocketError err, OnOff ibp, OnOff fas)
        {
            // 更新寄存器位.
            _ptable.SetValueAt(0x02, 0, OnOff.ON == ibp);
            _ptable.SetValueAt(0x02, 1, OnOff.ON == fas);

            bool fasChanged = FAS != fas;
            bool ibpChanged = IBP != ibp;
            if (fasChanged || ibpChanged)
            {
                // CHANGED.
                if (OnCcsStateChanged != null)
                {
                    OnCcsStateChanged(err, ibp, fas);
                }
            }
            IBP = ibp;
            FAS = fas;
            // FAS
            if (FAS == OnOff.ON)
            {
                if (fasChanged)
                {
                    log.WarnFormat("中控 FAS 告警!!!: FAS = {0}", FAS);
                    _fasNotified = true;
                }
                ControlAllZone(OnOff.OFF);
            }
            else if (fas == OnOff.OFF)
            {
                if (fasChanged && _fasNotified)
                {
                    log.WarnFormat("中控 FAS 告警解除!!!: FAS = {0}", FAS);
                    _fasNotified = false;
                }
            }
            // IBP
            if (IBP == OnOff.ON)
            {
                if (ibpChanged)
                {
                    _ibpNotified = true;
                    log.WarnFormat("中控 IBP 触发: IBP = {0}", IBP);
                }
                ControlAllZone(OnOff.OFF);
            }
            else if (ibp == OnOff.OFF)
            {
                if (ibpChanged && _ibpNotified)
                {
                    log.WarnFormat("中控 IBP 关闭: IBP = {0}", IBP);
                    _ibpNotified = false;
                }
            }
        }

        private void _OnZoneStateChanged(Zone _z, bool allOn)
        {
            bool isTotalOpen = UpdateFunctionTableByZone(_z);
            // 设置 中控 输出.
            lock (_ccs)
            {
                try
                {
                    JDQRequest req1 = new JDQRequest(1, JDQRequestType.SetOutput); // Read FAS/IBP
                    req1.SetOutput(_z.Reg.ZoneCoil, _z.IsZoneOpen());
                    req1.SetOutput(6, isTotalOpen); // 全区域.
                    SocketError err;
                    byte[] recv = _ccs.SSend(req1.Encode(), out err, 500);
                }
                catch (Exception) { }
            }
            if (this.OnZoneStateChanged != null)
            {
                OnZoneStateChanged.Invoke(_z, isTotalOpen); // 发出通知.
            }
        }

        private void _OnDoorStateChanged(Zone _z, Door _d)
        {
            this.CloseAlarmAsNecessary(_d);

            UpdateFunctionTableByDoor(_z, _d);

            if (this.OnDoorStateChanged != null)
            {
                OnDoorStateChanged.Invoke(_z, _d);
            }
            // log.DebugFormat("Zone: {0}, Door {0} changed: ", _z.Name, ValueHelper.GetIpLastAddr(d.IpAddr));
        }

        public SocketClient GetConneciton(string ip)
        {
            return _jdqVisitor.TryConnect(ip, CONN_PORT, CONN_TIMEOUT);
        }

        public Zone[] Zones()
        {
            return _zones != null ? _zones.ToArray() : new Zone[0];
        }

        // 开始服务
        List<ZoneScanner> _scanner = new List<ZoneScanner>();

        /**
         * 启动服务: 
         * 1. 启动 Modbus侦听服务;
         * 2. 启动 区域状态扫描;
         */
        public bool StartService()
        {
            // 启动 Modbus Server
            _mbserver.Start();
            this._ccs = _jdqVisitor.TryConnect(_config.GetCcsIp(), CONN_PORT);

            // 启动各个 zone 的自动扫描.
            if (_running) return _running;
            foreach (Zone z in this._zones)
            {
                z.Scanner.Start();
            }
            _running = true;
            return _running;
        }

        // 停止服务.
        public bool StopService()
        {
            if (!_running) return true;
            _jdqVisitor.CloseConnect(this._ccs, true);
            this._ccs = null;

            _mbserver.Stop();

            _running = false;
            foreach (Zone z in this._zones)
            {
                z.Scanner.OnDoorStateChanged -= this._OnDoorStateChanged;
                z.Scanner.Stop();
            }

            if (OnCcsStateChanged != null)
            {
                OnCcsStateChanged.Invoke(SocketError.NotConnected, IBP = OnOff.UNKNOWN, FAS = OnOff.UNKNOWN);
            }
            return true;
        }

        // 更新点表 - 门
        void UpdateFunctionTableByDoor(Zone _z, Door _d)
        {
            DoorState ds = _d.State;
            // 开门/关门
            _ptable.SetValueAt(_z.Reg.SetDoor, _d.Coil, ds.DoorAction == RelayState.ACTION);
            // 开门到位
            _ptable.SetValueAt(_z.Reg.GetOpenState, _d.Coil, ds.OpenState == OnOff.ON);
            // 关门到位
            _ptable.SetValueAt(_z.Reg.GetCloseState, _d.Coil, ds.CloseState == OnOff.ON);
            // 异常
            _ptable.SetValueAt(_z.Reg.GetErrorState, _d.Coil, ds.Error != DoorError.Success);
            // LCB
            _ptable.SetValueAt(_z.Reg.GetLCB, _d.Coil, ds.LCB == OnOff.ON);
            // 绿灯
            _ptable.SetValueAt(_z.Reg.SetGreen, _d.Coil, ds.GreenLamp == RelayState.ACTION);
            // 红灯
            _ptable.SetValueAt(_z.Reg.SetRed, _d.Coil, ds.RedLamp == RelayState.ACTION);

            // 按需关闭蜂鸣器.

        }


        /// 更新点表, 用于读取返回.
        bool UpdateFunctionTableByZone(Zone _z)
        {
            // 更新 区域信息.
            // 0x01 0~6 通讯故障
            // 更新 主控.
            int totalOpened = 0, totalCnt = 0, errorCnt = 0;
            foreach (Zone z in _zones)
            {
                totalOpened += z.OpenCnt;
                totalCnt += z.TotalCnt;
                errorCnt += z.HasException? 1:0;
            }
            bool isTotalOpen = totalOpened >= totalCnt / 2;
            // 全区域异常:
            _ptable.SetValueAt(REG_SUMMARY, 0, errorCnt > 0);
            // 当前区域异常.
            _ptable.SetValueAt(REG_SUMMARY, _z.Reg.ZoneCoil, _z.HasException);
            // 当前区域状态
            _ptable.SetValueAt(REG_ZONE_SUMMARY, _z.Reg.ZoneCoil, _z.IsZoneOpen());
            // 全区域开?
            _ptable.SetValueAt(REG_ZONE_SUMMARY, 6, isTotalOpen); 

            return isTotalOpen;
        }

        /// 有效寄存器地址
        internal bool IsValidRegAddress(byte r)
        {
            if (r == 0) return true;
            // 1-2, 5-6 系统
            // 其他: 门
            return (r >= 1 && r <= 4) || r == 5 || r == 6 || _config.IsValidReg(r);
        }

        /// FC=01 读取线圈 
        internal byte[] MB_ReadCoils(ushort refNum, ushort bitCount)
        {
            byte reg = (byte)(refNum >> 4);     //x
            byte bit = (byte)(refNum & 0x000F); //y

            return _ptable.GetValues(reg, bit, bitCount);
        }

        /// FC=05 写入一个线圈
        internal byte MB_WriteCoil(ushort refNum, byte val)
        {
            byte reg = (byte)(refNum >> 4);     // 寄存器
            byte bit = (byte)(0x000F & refNum); // 位
            if (!IsRegWritable(reg, bit))
            {
                return MBException.E02_ILLEGAL_DATA_ADDRESS; //无效数据地址, 不可写入.
            }
            OnOff switchTo = val == 0xFF ? OnOff.ON : OnOff.OFF;
            if (reg == 05)
            {
                // 区域开关, 更新点表(没有继电器).
                _ptable.SetValueAt(5, bit, val == 0xFF);
                return DealZoneRequest(bit, switchTo);
            }
            // 找到 Door
            Door door = _config.FindDoorByAddr(reg, bit);
            if (door == null)
            {
                return MBException.E02_ILLEGAL_DATA_ADDRESS;
            }
            if (door.State!=null && !door.State.Controable)
            {
                // return MBException.E03_ILLEGAL_DATA_VALUE; //不可控制.(开门未到位或关门未到位)
            }
            JDQRequest req = new JDQRequest(JDQRequest.DEV_ID, JDQRequestType.SetOutput);
            byte[] addOnbits, addOffbits; //要附加操作的位.
            byte[] bits = GetRelayIndex(reg, out addOnbits, out addOffbits);
            req.SetOutputs(bits, val == 0xFF);
            if (addOnbits != null)
                req.SetOutputs(addOnbits, true); // 附加打开操作.
            if (addOffbits != null)
                req.SetOutputs(addOffbits, false); //附加关闭操作
            return Request(door, req);
        }

        /// 处理区域控制请求.
        byte DealZoneRequest(byte bit, OnOff to)
        {
            if (bit == 6)
            {
                ControlAllZone(to);
            }
            else
            {
                ControllOneZone(_zones[bit], to);
            }
            return MBException.MB_SUCCESS;
        }

        /// 控制所有区域.
        void ControlAllZone(OnOff to)
        {
            foreach (Zone z in _zones)
            {
                foreach (Door d in z.Doors)
                {
                    if (d.Enabled)
                    {
                        ControlDoor(d, to);
                    }
                }
            }
        }

        ///  控制某区域.
        void ControllOneZone(Zone z, OnOff to)
        {
            foreach (Door d in z.Doors)
            {
                if (d.Enabled)
                {
                    ControlDoor(d, to);
                }
            }
        }

        bool IsActionRequired(Door d, OnOff newAction)
        {
            if (d.State.OpenState == OnOff.ON && newAction == OnOff.ON)
            {
                return false; //门开着.
            }
            if (d.State.CloseState == OnOff.ON && newAction == OnOff.OFF)
            {
                return false; //门关着, 无需关门.
            }
            return true;
        }

        // 关闭警告指示(红灯, 蜂鸣器)
        private void CloseAlarmAsNecessary(Door d)
        {
            if (d.State.Beep == RelayState.ACTION)
            {
                // 蜂鸣器闭合.
                if ((d.State.DoorAction == RelayState.ACTION && d.State.OpenState == OnOff.ON) ||
                   (d.State.DoorAction == RelayState.RESET && d.State.CloseState == OnOff.OFF))
                {
                    log.DebugFormat("门 {0} 动作已到位, 自动关闭: 继电器/红灯.", d);
                    // 开门动作, 且已到位. 或 关门动作, 已到位.
                    JDQRequest req = new JDQRequest(JDQRequest.DEV_ID, JDQRequestType.SetOutput);
                    byte[] bits = new byte[] { RELAY_RED, RELAY_BEEP }; //继电器, 红灯, 蜂鸣器)
                    req.SetOutputs(bits, false);
                    _jdqVisitor.Request(d.IpAddr, req);
                }
            }
        }

        private byte Request(Door d, JDQRequest req)
        {
            JDQResponse resp = _jdqVisitor.Request(d.IpAddr, req);
            if (resp.IsOK)
            {
                return MBException.MB_SUCCESS;
            }
            else
            {
                return MBException.E0B_GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND; // 设置异常.
            }
        }

        /// 控制一樘门
        public byte ControlDoor(Door d, OnOff to)
        {
            if (!IsActionRequired(d, to)) {
                //log.DebugFormat("无需操作: {0}", d);
                //return MBException.MB_SUCCESS;
            }
            JDQRequest req = new JDQRequest(JDQRequest.DEV_ID, JDQRequestType.SetOutput);
            byte[] bits = new byte[] { RELAY_OPEN_LEFT, RELAY_OPEN_RIGHT}; //0-2 (继电器, )
            req.SetOutputs(bits, to == OnOff.ON);
            req.SetOutputs(new byte[]{RELAY_RED, RELAY_BEEP }, true ) ; //红灯, 蜂鸣器

            return Request(d, req);
        }

        /* 根据请求地址, 获取对应的输出位.
         1	左扇门开关门信号
         2	备用 
         3	右扇门开关门信号
         4	备用 
         5	绿灯控制信号
         6	红灯控制信号
         7	蜂鸣器控制信号
         8	备用 
        */
        byte[] GetRelayIndex(byte reg, out byte[] addonbits, out byte[] addoffbits)
        {
            addonbits = null;
            addoffbits = null;
            List<byte> bs = new List<byte>();
            var offset = (reg - CMSConfig.REG_START) / _config.ZoneCnt;
            if (offset == CMSConfig.REG_SET_DOOR) // SetDoor
            {
                addonbits = new byte[] { RELAY_RED, RELAY_BEEP};  //需要附加打开.
                return new byte[] { RELAY_OPEN_LEFT, RELAY_OPEN_RIGHT }; 
            }
            else if (offset == CMSConfig.REG_SET_GREEN) // SetGreen
            {
                return new byte[] { RELAY_GREEN }; // 5
            }
            else if (offset == CMSConfig.REG_SET_RED)
            {
                return new byte[] { RELAY_RED }; // 6
            }
            else
            {
                return new byte[0];
            }
        }


        internal bool IsRegWritable(byte reg, byte bit)
        {
            // 5
            // 11-22
            // 47-64
            if (reg == 5)
            {
                return bit <= 6;
            }
            else if (InRange(reg, 11, 22) || InRange(reg, 41, 52))
            {
                return bit <= 12;
            }
            return false;
        }


        bool InRange(byte v, byte minv, byte maxv)
        {
            return v >= minv && v <= maxv;
        }
    }
}
