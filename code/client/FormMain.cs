using DisneyCMS.modbus;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;

namespace DisneyCMS
{
    public partial class FormMain : Form
    {
        static ILog log = LogManager.GetLogger("C");
        SocketClient _client;
        public FormMain()
        {
            InitializeComponent();
        }
        private string[] zones = new string[] { "东主入口", "东售票厅", "南主入口", "西主入口", "西售票厅", "北主入口" };
        private Dictionary<string, DataGridViewRow> _zoneRows = new Dictionary<string, DataGridViewRow>();
        private Dictionary<string, DataGridViewRow> _doorRows = new Dictionary<string, DataGridViewRow>();
        private Dictionary<int, DataGridViewRow> _zoneIndex = new Dictionary<int, DataGridViewRow>();

        private Config _config;
        private Scene _selected_scene;    // current Scene.
        private SceneZone _selected_zone = null; // current Zone.

        private void FormMain_Load(object sender, EventArgs e)
        {
            _initializing = true;
            _config = Config.Load();
            textBox_ip.Text = _config.Server.IP;
            textBox_port.Text = Convert.ToString(_config.Server.Port);
            lb_scenes.Items.AddRange(_config.Scenes.ToArray());
            UpdateSceneCnt();

            Console.SetOut(new TextBoxWriter(this.textBoxLog));
            System.Drawing.Font f = dgv_doors.Font;
            System.Drawing.Font gf = new System.Drawing.Font(f.FontFamily, f.Size + 1, System.Drawing.FontStyle.Bold);
            int zIndex = 0;
            foreach (string zName in zones)
            {
                // Zone Row.
                int rowIndex = dgv_doors.Rows.Add();// dt.NewRow();
                DataGridViewRow lvz = dgv_doors.Rows[rowIndex];
                lvz.Cells[ViewColumns.STATE].Value = client.Properties.Resources.off_24;
                lvz.Cells[ViewColumns.INDEX].Value = zName;
                DataGridViewTextBoxCellEx cell = (DataGridViewTextBoxCellEx)lvz.Cells[ViewColumns.INDEX];
                cell.ColumnSpan = 5;
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cell.Style.Font = gf;
                _zoneIndex[zIndex] = lvz; // rowIndex=>row
                _zoneRows[zName] = lvz;    // name=>row
                for (int di = 0; di < 12; di++)
                {
                    DataGridViewRow lvi = dgv_doors.Rows[dgv_doors.Rows.Add()];
                    lvi.Cells[ViewColumns.STATE].Value = client.Properties.Resources.off_24;
                    lvi.Cells[ViewColumns.INDEX].Value = di + 1;
                    lvi.Cells[ViewColumns.NAME].Value = string.Format("门-{0}", di + 1);
                    _doorRows[string.Format("{0}.{1}", zIndex, di)] = lvi;
                }
                zIndex++;
            }
            _initializing = false;
            if (lb_scenes.Items.Count > 0)
            {
                lb_scenes.SelectedIndex = 0;
            }
        }

        private void UpdateSceneName()
        {
            int sindex = lb_scenes.SelectedIndex;
            Scene si = (Scene)lb_scenes.SelectedItem;
            if (si != null)
            {
                si.Name = txt_sname.Text;
                txt_sname.Visible = false;
                lb_scenes.Refresh();
                lb_scenes.Items[sindex] = si;
            }
        }

        private void _sceneEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                UpdateSceneName();
            }
            if (e.KeyCode == Keys.Escape)
                txt_sname.Visible = false;

        }
        private bool _initializing = true;

        // 显示当前场景.
        private void ShowScene(Scene s)
        {
            _initializing = true;
            ResetSceneZone();
            _selected_scene = s;
            _pre_selected_zone = -1;
            _selected_zone = null;
            cl_zone.SelectedIndex = -1;
            for (int i = 0; i < cl_zone.Items.Count; i++)
            {
                cl_zone.SetItemChecked(i, false);
            }
            if (s != null && s.Zones != null)
                foreach (SceneZone sz in s.Zones)
                {
                    cl_zone.SetItemChecked(sz.Index, sz.Enabled);
                }
            _initializing = false;
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            _config.Server.IP = textBox_ip.Text;
            _config.Server.Port = Convert.ToUInt16(textBox_port.Text);

            button_save_scene_Click(null, null);

            if (_client == null)
            {
                _client = new SocketClient(textBox_ip.Text, Convert.ToUInt16(textBox_port.Text));
                if (_client.Connect())
                {
                    buttonConnect.Text = "断开";
                    timer1.Enabled = true;
                }
            }
            else
            {
                buttonConnect.Text = "连接";
                _client.Close();
                _client = null;
                timer1.Enabled = false;
            }
        }
        void DoSelect(bool s)
        {
            cb_d1e.Checked = s;
            cb_d2e.Checked = s;
            cb_d3e.Checked = s;
            cb_d4e.Checked = s;
            cb_d5e.Checked = s;
            cb_d6e.Checked = s;
            cb_d7e.Checked = s;
            cb_d8e.Checked = s;
            cb_d9e.Checked = s;
            cb_d10e.Checked = s;
            cb_d11e.Checked = s;
            cb_d0e.Checked = s;
        }
        private void buttonSelectAll_Click(object sender, EventArgs e)
        {
            if (_selected_zone != null)
                DoSelect(true);
        }

        private void buttonSelectNone_Click(object sender, EventArgs e)
        {
            if (_selected_zone != null)
                DoSelect(false);
        }

        // 扫描
        private void buttonScan_Click(object sender, EventArgs e)
        {
            if (_client == null) return;
            MBMessage req = new MBMessage();
            req.TID = 0xFF;
            req.UID = 0x01;
            req.PID = 0;
            req.FC = 0x01;
            req.SetBodySize(4);
            req.SetWord(0, 0);     // x: 0x0000
            req.SetWord(2, 53 * 16); // 全部读取.
            MBMessage resp = SendRequest(req);
            int DOF = 1;
            if (resp == null) return;
            int zcnt = 6;
            int scnt = 7; //状态列: action,so,sc,err,lcb,green,red
            int regStart = 0x0B;
            int reg;
            int cs = ViewColumns.ACTION;

            for (int c = 0; c < scnt; c++)
            {
                //               log.DebugFormat("state {0}", c);
                for (int z = 0; z < zcnt; z++)
                {
                    reg = regStart + zcnt * c + z; // z=0: 11, 11+ 6*1,
                    //                    log.DebugFormat("{0}", reg);
                    byte b1 = resp.GetByte(reg * 2 + DOF);      // Z0, 动作, 0-7 号门
                    byte b2 = resp.GetByte(reg * 2 + 1 + DOF); // Z0, 动作, 8-11号门
                    UpdateDoorState(z, c + cs, b1, 0, 8);
                    UpdateDoorState(z, c + cs, b1, 8, 4);
                }
            }
        }

        private void UpdateDoorState(int zi, int col, byte bv, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var di = string.Format("{0}.{1}", zi, offset + i);
                DataGridViewRow row = _doorRows[di];
                UpdateRowText(row, col, BitOn(bv, (byte)i) ? "开" : "关");
            }
        }

        private delegate void UpdateRowTextCallback(DataGridViewRow row, int nameIndex, string value);
        private void UpdateRowText(DataGridViewRow row, int nameIndex, string text)
        {
            if (this.dgv_doors.InvokeRequired)
            {
                UpdateRowTextCallback cb = new UpdateRowTextCallback(UpdateRowText);
                try
                {
                    this.Invoke(cb, new object[] { row, nameIndex, text });
                }
                catch (Exception) { }
            }
            else
            {
                if (row.Cells[nameIndex] != null)
                {
                    row.Cells[nameIndex].Value = text;
                    Color c = Color.DarkGray;
                    if (text == "开")
                    {
                        c = Color.Green;
                    }
                    row.Cells[nameIndex].Style.ForeColor = c;
                }
            }
        }

        bool BitOn(byte v, byte bit)
        {
            byte iv = (byte)(1 << bit);
            return (v & iv) == iv;
        }

        private MBMessage SendRequest(MBMessage req)
        {
            try
            {
                SocketError err;
                byte[] buff = req.encode();
                //log.DebugFormat("> {0:##}", ValueHelper.BytesToHexStr(buff));
                byte[] ack = _client.SSend(req.encode(), out err);
                //log.DebugFormat("< {0:##}, err={1}", ValueHelper.BytesToHexStr(ack), err);
                if (SocketError.Success != err)
                {
                    log.ErrorFormat("异常: {0}", err);
                }
                MBMessage resp = new MBMessage(ack);
                if (resp.FC > 0x80)
                {
                    byte ec = resp.GetByte(0);
                    log.ErrorFormat("Modbus 异常: {0}-{1} ", ec, MBException.NameOf(ec));
                }
                return resp;
            }
            catch (Exception)
            {
                return null;
            }
        }

        MBMessage NewWriteCoil(ushort addr, bool onOff)
        {
            MBMessage req = new MBMessage();
            req.TID = 0xFF;
            req.UID = 0x01;
            req.PID = 0;
            req.FC = 0x05;
            req.SetBodySize(4);
            req.SetWord(0, addr); // 单樘门开门控制信号 - 读/写	0x11
            req.SetByte(2, onOff ? (byte)0xFF : (byte)0x00); // 读取16*6个字节
            req.SetByte(3, 0x00);
            return req;
        }

        void DoActionDoor(int reg, CheckBox door, bool on = true)
        {
            if (door.Checked)
            {
                int bit = Convert.ToByte(door.Tag.ToString());
                MBMessage req = NewWriteCoil((ushort)(reg << 4 | bit), on);
                SendRequest(req);
            }
        }

        void btn_door_action(int reg, bool on)
        {
            if (_client == null) return;
            bool timerEnabled = timer1.Enabled;
            timer1.Enabled = false;

            DoActionDoor(reg, cb_d1e, on);
            DoActionDoor(reg, cb_d2e, on);
            DoActionDoor(reg, cb_d3e, on);
            DoActionDoor(reg, cb_d4e, on);
            DoActionDoor(reg, cb_d5e, on);
            DoActionDoor(reg, cb_d6e, on);
            DoActionDoor(reg, cb_d7e, on);
            DoActionDoor(reg, cb_d8e, on);
            DoActionDoor(reg, cb_d9e, on);
            DoActionDoor(reg, cb_d10e, on);
            DoActionDoor(reg, cb_d11e, on);
            DoActionDoor(reg, cb_d0e, on);
            timer1.Enabled = timerEnabled;
        }
        private const int REG_START = 0x0B;
        
        private void control_zone(byte reg, int zone, bool on)
        {
            bool timerEnabled = timer1.Enabled;
            if (_client == null)
            {
                log.ErrorFormat("未连接.");
                return;
            }
            timer1.Enabled = false;
            MBMessage req = NewWriteCoil((ushort)(reg << 4 | zone), on);
            MBMessage resp = SendRequest(req);
            timer1.Enabled = timerEnabled;
        }

        private void btn_open_zone_Click(object sender, EventArgs e)
        {
            if (_selected_zone == null)
            {
                log.ErrorFormat("未选择区域.");
                return;
            } 
            control_zone(0x05, cl_zone.SelectedIndex, true);
        }

        private void btn_close_zone_Click(object sender, EventArgs e)
        {
            if (_selected_zone == null)
            {
                log.ErrorFormat("未选择区域.");
                return;
            }
            control_zone(0x05, cl_zone.SelectedIndex, false);
        }

        private void btn_open_zone_all_Click(object sender, EventArgs e)
        {
            control_zone(0x05, 6, true);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            control_zone(0x05, 6, false);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            buttonScan_Click(null, null);
        }


        // door Switch/Lamp changed.
        private void cbd_changed(object sender, EventArgs e)
        {
            if (_initializing) return;
            if (_selected_zone == null) return;
            // cb_dxxc
            CheckBox c = (CheckBox)sender;
            string type = c.Name.Substring(c.Name.Length - 1);
            int len = c.Name.Length;
            int di = -1;
            if (len == 6)
                di = Convert.ToInt32(c.Name.Substring(4, 1));
            else
                di = Convert.ToInt32(c.Name.Substring(4, 2));
            //           log.DebugFormat("Type={0}, index={1}, stat=%s", type, di, c.Checked);

            ZoneDoor zd = _selected_zone.DoorOf(di);
            if (type == "s")
            {
                zd.SwitchOn = c.Checked;
                if (c.Checked)
                    UpdateCheckBox("cb_d{0}e", di, true);
            }
            if (type == "r")
            {
                zd.RedOn = c.Checked;
                if (c.Checked)
                    UpdateCheckBox("cb_d{0}e", di, true);
            } if (type == "g")
            {
                zd.GreenOn = c.Checked;
                if (c.Checked)
                    UpdateCheckBox("cb_d{0}e", di, true);
            }
            if (type == "e")
            {
                zd.Enabled = c.Checked;
            }
        }

        private void button_save_scene_Click(object sender, EventArgs e)
        {
            _config.Save();
        }

        private void lb_scenes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lb_scenes.SelectedIndex >= 0)
            {
                _selected_scene = (Scene)lb_scenes.SelectedItem;
                ShowScene(_selected_scene);
            }
            else
            {
                ShowScene(null);
            }
        }

        private void UpdateSceneCnt()
        {
            l_scnt.Text = Convert.ToString(lb_scenes.Items.Count);
        }

        private void button_scene_add_Click(object sender, EventArgs e)
        {
            Scene s = Scene.Default();
            s.Name = string.Format("场景:{0}", _config.SceneCount() + 1);
            _config.AddScene(s);
            lb_scenes.Items.Add(s);
            lb_scenes.SelectedItem = s;
            UpdateSceneCnt();
        }

        private void lb_scenes_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int itemSelected = lb_scenes.SelectedIndex;
            string itemText = lb_scenes.Items[itemSelected].ToString();

            Rectangle rect = lb_scenes.GetItemRectangle(itemSelected);
            rect.Height = rect.Height + 7;
            txt_sname.Parent = lb_scenes;
            txt_sname.Bounds = rect;
            txt_sname.Multiline = true;
            txt_sname.Visible = true;
            txt_sname.Text = itemText;
            txt_sname.Focus();
            txt_sname.SelectAll();
        }

        private void lb_scenes_MouseClick(object sender, MouseEventArgs e)
        {
            txt_sname.Visible = false;
        }

        private int _pre_selected_zone = -1;

        // Selected Index.
        private void cl_zone_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            int zid = cl_zone.SelectedIndex; // 
            if (zid < 0) return;
            bool isChecked = cl_zone.GetItemChecked(zid);
            if (_pre_selected_zone == zid)
            {
                return;
            }
            _pre_selected_zone = zid;
            //         log.DebugFormat("Selected Item {0} - {1}", zid, isChecked);
            _selected_zone = _selected_scene.ZoneAt(zid);
            ShowSceneZone(_selected_zone);
        }

        private void ShowDoor(ZoneDoor zd, int i)
        {
            if (zd != null)
            {
                UpdateCheckBox("cb_d{0}e", i, zd.Enabled);
                UpdateCheckBox("cb_d{0}s", i, zd.SwitchOn);
                UpdateCheckBox("cb_d{0}g", i, zd.GreenOn);
                UpdateCheckBox("cb_d{0}r", i, zd.RedOn);
            }
            else
            {
                UpdateCheckBox("cb_d{0}e", i, false);
                UpdateCheckBox("cb_d{0}s", i, false);
                UpdateCheckBox("cb_d{0}g", i, false);
                UpdateCheckBox("cb_d{0}r", i, false);
            }
        }

        private void ResetSceneZone()
        {
            for (int i = 0; i < 12; i++)
            {
                ShowDoor(null, i);
            }
        }

        private void ShowSceneZone(SceneZone z)
        {
            _initializing = true;
            for (int i = 0; i < 12; i++)
            {
                ShowDoor(z.DoorOf(i), i);
            }
            _initializing = false;
        }

        private void UpdateCheckBox(string fmt, int index, bool state)
        {
            string n = string.Format(fmt, index);
            CheckBox cb = FindCheckBox(n);
            if (cb == null)
            {
                log.ErrorFormat("CheckBox: {0} not found!", n);
            }
            else
            {
                cb.Checked = state;
            }
        }
        private CheckBox FindCheckBox(string n)
        {
            Control[] controls = Controls.Find(n, true);
            return (CheckBox)(controls.Length > 0 ? controls[0] : null);
        }

        private void cl_zone_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initializing) return;
            int zid = cl_zone.SelectedIndex; // 
            if (zid < 0) return;
            bool isChecked = cl_zone.GetItemChecked(zid);
            _selected_scene.UpdateZone(zid, !isChecked); // 更新 Zone.
            //  log.DebugFormat("Item {0} check to {1}", zid, !isChecked);
        }

        private void button_scene_del_Click(object sender, EventArgs e)
        {
            if (lb_scenes.SelectedItem == null)
            {
                return;
            }
            Scene s = (Scene)lb_scenes.SelectedItem;
            if (MessageBox.Show(string.Format("将删除场景 {0} , 确认吗?", s.Name), "删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _config.RemoveScene(s);
                lb_scenes.Items.Remove(s);
                lb_scenes.SelectedIndex = -1;
                ShowScene(null);
                UpdateSceneCnt();
            }
        }

        private void txt_sname_Leave(object sender, EventArgs e)
        {
            UpdateSceneName();
        }

        private void btn_runscene_Click(object sender, EventArgs e)
        {
            if (_client == null || !_client.IsConnected)
            {
                log.ErrorFormat("未连接.");
                return;
            }
            // 分区域/动作.
            if (_selected_scene == null)
            {
                log.ErrorFormat("未选择场景.");
                return;
            }
            foreach (SceneZone sz in _selected_scene.Zones)
            {
                if (sz.Enabled) 
                    SendSceneZone(sz);
            }
        }
        private const int OFFSET_SWITCH = 0;
        private const int OFFSET_GREEN  = 5;
        private const int OFFSET_RED    = 6;

        private void SendSceneZone(SceneZone z)
        {
            int zcnt = 6;
            foreach (ZoneDoor d in z.Doors)
            {
                if (!d.Enabled)  continue;
                // Switch
                int reg =  z.Index + zcnt * OFFSET_SWITCH + REG_START; //开门.
                MBMessage req = NewWriteCoil((ushort)(reg << 4 | d.Index), d.SwitchOn);
                MBMessage resp = SendRequest(req);
                // Green
                reg = z.Index + zcnt * OFFSET_GREEN + REG_START; //绿灯
                req = NewWriteCoil((ushort)(reg << 4 | d.Index), d.GreenOn);
                resp = SendRequest(req);
                // Red
                reg = z.Index + zcnt * OFFSET_GREEN + REG_START; //红灯.
                req = NewWriteCoil((ushort)(reg << 4 | d.Index), d.RedOn);
                resp = SendRequest(req);
            }
        }

    }

    class ViewColumns
    {
        public const int STATE = 0;
        public const int INDEX = 1;
        public const int NAME = 2;
        public const int ACTION = 3;
        public const int OPEN_STATE = 4;
        public const int CLOSE_STATE = 5;
        public const int LCB = 6;
        public const int GREEN_LAMP = 7;
        public const int RED_LAMP = 8;
        public const int BEEP = 9;
        public const int ERROR = 10;
    };
}
