using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    // 门樘状态
    public class DoorState
    {
        // out 1+3: 门动作
        public RelayState DoorAction { get; set; }
        // OUT: 1
        public RelayState LeftAction { get; set; }
        // OUT: 3
        public RelayState RightAction { get; set; }

        // OUT 5: 绿灯
        public RelayState GreenLamp { get;set;}
        
        // OUT 6: 红灯
        public RelayState RedLamp{get;set;}
        
        // OUT 7: 蜂鸣器状态.
        public RelayState Beep { get; set; }

        // input: 1+3: 开门状态
        public OnOff OpenState { get; set; }
        // input: 2+4: 关门状态
        public OnOff CloseState { get; set; }

        // I: 1 : 左开到位
        public OnOff LeftOpenState{get;set;}
        // I: 2 : 左关到位
        public OnOff LeftCloseState{get;set;}
        // I: 3 : 右开到位
        public OnOff RightOpenState{get;set;}
        // I: 4 : 右关到位
        public OnOff RightCloseState{get;set;}
        // I: 5 : LCB状态
        public OnOff LCB { get; set; } 

        // 门扇错误状态.
        public DoorError Error { get; set; }
        public string ExtError { get; set; }

        // 可开关.
        public bool Controable { get; set; }

        public DoorState()
        {
            Reset();
        }

        public void Reset() {
            // Input
            LeftAction = RelayState.UNKNOWN;
            RightAction = RelayState.UNKNOWN;
            RedLamp = RelayState.UNKNOWN;
            GreenLamp = RelayState.UNKNOWN;
            Beep = RelayState.UNKNOWN;
            DoorAction = RelayState.UNKNOWN;

            // Output
            LeftOpenState = OnOff.UNKNOWN;
            RightOpenState = OnOff.UNKNOWN;
            OpenState = OnOff.UNKNOWN;

            LeftCloseState = OnOff.UNKNOWN;
            RightCloseState = OnOff.UNKNOWN;
            CloseState = OnOff.UNKNOWN;

            LCB = OnOff.UNKNOWN;

            Error = DoorError.UNKNOWN;
            ExtError = "";
            Controable = true;
        }

        public bool IsOpened
        {
            get
            {
                return RelayState.ACTION == DoorAction && OnOff.ON == OpenState;
            }
        }
        public bool IsClosed
        {
            get
            {
                return RelayState.RESET == DoorAction && OnOff.OFF == CloseState;
            }
        }
    }
}
