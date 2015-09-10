using log4net;
using System.Collections.Generic;
using System.Threading;

namespace DisneyCMS.cms
{
    public delegate void DoorStateChangedCallback(Zone z,Door d);
    public delegate void ZoneStateChangedCallback(Zone z, bool allOn);

    public class ZoneScanner
    {
        private const int DEV_SCAN_PORT = 50000;    // 端口.
        private const int DEV_SCAN_INTERVAL = 2000; // 2s
        private const int DEV_SCAN_TIMEOUT = 1000;  // 应答超时.
        private static ILog log = LogManager.GetLogger("ZS");
        private Zone _zone;
        private Timer _timer;
        private Dictionary<string, SocketClient> _doorConnections = new Dictionary<string, SocketClient>();
        object mylock = new object();
        private bool _running = false;
        private JDQVisitor _visitor;

        // 被 CMS 订阅
        internal DoorStateChangedCallback OnDoorStateChanged;
        // 被 CMS 订阅.
        internal ZoneStateChangedCallback OnZoneStateChanged;

        public ZoneScanner(Zone z, JDQVisitor visitor)
        {
            this._zone = z;
            this._visitor = visitor;
            int dueTime = 1000;//Timeout.Infinite;
            _timer = new Timer(new TimerCallback(OnTimerCallback), null, dueTime, DEV_SCAN_INTERVAL);
        }

        public void Start()
        {
            _running = true;
        }

        public void Stop()
        {
            _running = false;
        }

        private void OnTimerCallback(object state)
        {
            if (_running)
            {
                lock (this)
                {
                    _zone.OpenCnt  = 0;
                    _zone.ErrorCnt = 0;
                    _zone.CloseCnt = 0;
                    
                    foreach (Door d in _zone.Doors)
                    {
                        if (d.Enabled)
                        {
                            // Connection c = _doorConnections[d];
                            JDQRequest req1 = new JDQRequest(d.DevId, JDQRequestType.ReadInput);
                            JDQResponse rsp1 = _visitor.Request(d.IpAddr, req1, DEV_SCAN_PORT, DEV_SCAN_TIMEOUT);
                            Thread.Sleep(10);
                            JDQRequest req2 = new JDQRequest(d.DevId, JDQRequestType.ReadOutput);
                            JDQResponse rsp2 = _visitor.Request(d.IpAddr, req2, DEV_SCAN_PORT, DEV_SCAN_TIMEOUT);
                            
                            // 更新门状态
                            UpdateDoorState(d, rsp1, rsp2);
                            
                            if (DoorError.Success != d.State.Error)
                            {
                                _zone.ErrorCnt++; //错误.
                            }
                            else
                            {
                                if (d.State.IsOpened) {
                                    _zone.OpenCnt++;
                                }
                                else if (d.State.IsClosed)
                                {
                                    _zone.CloseCnt++;
                                }
                            }

                            if (OnDoorStateChanged != null)
                            {
                                OnDoorStateChanged.Invoke(_zone, d); // 门变更.
                            }
                        }
                    }
                    UpdateZoneState(_zone); // 更新 Zone 状态.
                    if (OnZoneStateChanged != null)
                    {
                        OnZoneStateChanged.Invoke(_zone, false); // 区域变更.
                    }
                }
            }
        }

        private void UpdateDoorState(Door d, JDQResponse input, JDQResponse output)
        {
            /* in
            1	左扇开门到位信号
            2	左扇关门到位信号
            3	右扇开门到位信号
            4	右扇关门到位信号
            5	LCB状态信号
            OUT:
            1	左扇门开关门信号	继电器吸合：触发开左扇门，反之触发关左扇门	
            3	右扇门开关门信号	继电器吸合：触发开右扇门，反之触发关右扇门
            5	绿灯控制信号	继电器吸合：绿灯亮，反之绿灯灭
            6	红灯控制信号	继电器吸合：红灯亮，反之红灯灭
            7	蜂鸣器控制信号	继电器吸合：蜂鸣器开始叫，蜂鸣器停止叫
            */
            DoorState ds;
            if (d.State == null)
                d.State = ds = new DoorState();
            else
                ds = d.State;
            if (!input.IsOK || !output.IsOK)
            {
                ds.Reset();
                ds.Error = DoorError.SocketError;
                ds.ExtError = input.ExtError;
                return;
            }
            /// ==== OUTPUT State (继电器) ============
            ds.LeftAction  = output.GetRelayState(0);  
            ds.RightAction = output.GetRelayState(2);  
            // Lamp
            ds.GreenLamp = output.GetRelayState(4);
            ds.RedLamp = output.GetRelayState(5);
            // Beep
            ds.Beep = output.GetRelayState(6);
            /// ==== INPUT State (开关量) ============
            ds.LeftOpenState  = input.GetInputState(0);  // 左打开
            ds.LeftCloseState = input.GetInputState(1); // 左关闭
            ds.RightOpenState = input.GetInputState(2);  // 右打开
            ds.RightCloseState = input.GetInputState(3); // 右关闭
            // LCB
            ds.LCB = input.GetInputState(4);

            // 开关
            if (ds.LeftAction == RelayState.ACTION && ds.RightAction == RelayState.ACTION) {
                ds.DoorAction = RelayState.ACTION;
            }else if (ds.LeftAction == RelayState.RESET && ds.RightAction == RelayState.RESET) {
                ds.DoorAction = RelayState.RESET;
            }else {
                ds.DoorAction = RelayState.UNKNOWN; // 不一致.
            }
 
            // 错误判定.
            DoorError lerror = DoorError.UNKNOWN, rerror = DoorError.UNKNOWN;
            bool lOpenOk = false, lCloseOk = false, rOpenOk = false, rCloseOk = false;
            if (RelayState.ACTION == ds.LeftAction)
            { // 下达了左开门指令
                if (OnOff.ON == ds.LeftOpenState)
                    lOpenOk = true;
                else
                    lerror = DoorError.LeftOpenError; // 开左门未到位错误
            }
            else if (RelayState.RESET == ds.LeftAction)
            { // 下达了左关门指令.
                if (OnOff.ON == ds.LeftCloseState)
                    lCloseOk = true;
                else
                    lerror = DoorError.LeftCloseError; //关左门未到位错误
            }
            if (RelayState.ACTION == ds.RightAction)
            { // 下达了右开门指令
                if (OnOff.ON == ds.RightOpenState)
                    rOpenOk = true;
                else
                    rerror = DoorError.RightOpenError; // 右左门未到位错误
            }
            else if (RelayState.RESET == ds.RightAction)
            { // 下达了右关门指令.
                if (OnOff.ON == ds.RightCloseState)
                    rCloseOk = true;
                else
                    rerror = DoorError.RightCloseError; //关右门未到位错误
            }

            // 状态
            ds.Error = DoorError.Success;
            if (lOpenOk && rOpenOk)
            {
                ds.OpenState = OnOff.ON;
            }
            else if (lCloseOk && rCloseOk)
            {
                ds.CloseState = OnOff.ON;
            }

            // 错误
            if (lerror != DoorError.UNKNOWN || rerror != DoorError.UNKNOWN)
            {
                if (rerror == DoorError.UNKNOWN)
                { // 仅左门
                    ds.Error = lerror;
                }
                else if (lerror == DoorError.UNKNOWN)
                {
                    ds.Error = rerror;
                }
                else
                {
                    // 双门错误
                    if (DoorError.LeftOpenError == lerror)
                        ds.Error = DoorError.OpenError;
                    else
                        ds.Error = DoorError.CloseError;
                }
            }
            else
            {
                // none error?
                ds.Error = DoorError.Success;
            }
            return;
        }
        private void UpdateZoneState(Zone z) {

        }
    }
}
