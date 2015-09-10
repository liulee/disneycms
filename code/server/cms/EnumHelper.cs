using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DisneyCMS.cms
{
    partial class EnumHelper
    {
        public static string ToString(LampState ls)
        {
            switch (ls)
            {
                case  LampState.GREEN:
                    return "绿";
                case LampState.RED:
                    return "红";
                case LampState.OFF:
                    return "灭";
                default:
                    return "未知";
            }
        }

        public static string ToString(OnOff oo)
        {
            switch (oo)
            {
                case OnOff.ON:
                    return "开";
                case OnOff.OFF:
                    return "关";
                default:
                    return "未知";
            }
        }

        public static string ToString(RelayState rs)
        {
            switch (rs)
            {
                case RelayState.ACTION:
                    return "开";
                case RelayState.RESET:
                    return "关";
                default:
                    return "未知";
            }
        }
        public static string ToString(DoorError de, string errmsg)
        {
            string err = "";
            switch (de)
            {
                case DoorError.Success:
                    { err = "正常"; break; }
                case DoorError.CloseError:
                    { err =  "关闭异常"; break; }
                case DoorError.LeftCloseError:
                    { err =  "关闭左门异常"; break; }
                case DoorError.RightCloseError:
                    { err =  "关闭右门异常"; break; }
                case DoorError.OpenError:
                    { err =  "开启异常"; break; }
                case DoorError.LeftOpenError:
                    { err =  "开启左门异常"; break; }
                case DoorError.RightOpenError:
                    { err =  "开启右门异常"; break; }
                case DoorError.SocketError:
                    { err = "连接异常:"; break; }
                default:
                    { err =  "未知"; break; }
            }
            if (!string.IsNullOrEmpty(errmsg)) {
                err += ":" + errmsg;
            }
            return err;
        }
    }
}
