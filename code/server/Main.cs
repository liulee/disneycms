using System;
using System.Windows.Forms;
using DisneyCMS.modbus;
using DisneyCMS.cms;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Collections.Concurrent;
using log4net;
using System.Net.Sockets;

namespace DisneyCMS
{
    public partial class Main : Form
    {
        private static ILog log = LogManager.GetLogger("GUI");
        private CMS _cms_server;
        private MBServer _mb_server;
        private MBMsgHandler _handler;
        private Dictionary<string, DataGridViewRow> _zoneRows = new Dictionary<string, DataGridViewRow>();
        private Dictionary<string, DataGridViewRow> _doorRows = new Dictionary<string, DataGridViewRow>();
        private Dictionary<int, DataGridViewRow> _zoneIndex = new Dictionary<int, DataGridViewRow>();
        private ConcurrentDictionary<string, ListViewItem> _clientRows = new ConcurrentDictionary<string, ListViewItem>();
        public Main()
        {
            _mb_server = new MBServer();
            _cms_server = new CMS(_mb_server);
            _handler = new MBMsgHandler(_cms_server);
            InitializeComponent();
        }
        DataGridViewColumn AddColumn(DataTable dt, string name, int w, Type type, string caption = "")
        {
            if (string.IsNullOrEmpty(caption)) caption = name;
            DataGridViewColumn c;
            if (type == typeof(Bitmap))
            {
                c = new DataGridViewImageColumn();
                c.Frozen = true;

                c.Width = 30;
            }
            else
            {
                c = new DataGridViewTextBoxColumn();
                c.HeaderText = caption;
            }
            c.Name = name;
            //dt.Columns.Add(c);
            dgv_doors.Columns.Add(c);
            return c;
        }
        private void Main_Load(object sender, EventArgs e)
        {
            timer1_Tick(null, null);
            Console.SetOut(new TextBoxWriter(this.textBox_log));
            _cms_server.init(); // Load Config

            lv_client.Items.Clear();

            dgv_doors.Rows.Clear();
            System.Drawing.Font f = dgv_doors.Font;
            System.Drawing.Font gf = new System.Drawing.Font(f.FontFamily, f.Size + 1, System.Drawing.FontStyle.Bold);
            //           dgv_doors.EnableHeadersVisualStyles = false;
            dgv_doors.ShowEditingIcon = false;
            foreach (Zone z in _cms_server.Zones())
            {
                // Zone Row.
                int rowIndex = dgv_doors.Rows.Add();// dt.NewRow();
                DataGridViewRow lvz = dgv_doors.Rows[rowIndex];
                lvz.Cells[ViewColumns.STATE].Value = Properties.Resources.off_24;
                lvz.Cells[ViewColumns.INDEX].Value = z.Name;
                DataGridViewTextBoxCellEx cell = (DataGridViewTextBoxCellEx)lvz.Cells[ViewColumns.INDEX];
                cell.ColumnSpan = 5;
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
                cell.Style.Font = gf;
                _zoneIndex[rowIndex] = lvz; // rowIndex=>row
                _zoneRows[z.Name] = lvz;    // name=>row
                // Doors
                foreach (Door d in z.Doors)
                {
                    DataGridViewRow lvi = dgv_doors.Rows[dgv_doors.Rows.Add()];
                    lvi.Cells[ViewColumns.STATE].Value = Properties.Resources.off_24;
                    lvi.Cells[ViewColumns.INDEX].Value = d.Coil + 1;
                    lvi.Cells[ViewColumns.NAME].Value = d.IpAddr;
                    _doorRows[d.IpAddr] = lvi;
                }
            }
            // dgv_doors.DataSource = dt;

            _cms_server.OnDoorStateChanged += OnDoorStateChanged;
            _cms_server.OnZoneStateChanged += OnZoneStateChanged;
            _cms_server.OnCcsStateChanged += OnCcsStateChanged;
            _mb_server.OnClientConnect += OnMBClientConnected;
            _mb_server.OnClientDisconnect += OnMBClientDisconnected;
            _mb_server.OnMsgDealed += this.OnMBClientRequest;

        }
        private OnOff _ibp = OnOff.UNKNOWN, _fas = OnOff.UNKNOWN;
        private bool _blink_fas, _blink_ibp;
        /// CCS 中控状态变更.
        private void OnCcsStateChanged(SocketError lastError, OnOff ibp, OnOff fas)
        {
            _ibp = ibp;
            _fas = fas;
            _blink_ibp = ibp == OnOff.ON;
            _blink_fas = fas == OnOff.ON;
            SwitchButtonIcon(lb_ibp, ibp);
            SwitchButtonIcon(lb_fas, fas); // IBP 接通.
        }
        private delegate void SwitchButtonIconCallback(Label btn, OnOff s);
        private void SwitchButtonIcon(Label btn, OnOff s) {
            if (btn.InvokeRequired)
            {
                SwitchButtonIconCallback cb = new SwitchButtonIconCallback(SwitchButtonIcon);
                try
                {
                    this.Invoke(cb, new object[] { btn, s });
                }
                catch (Exception) { }
            }
            else
            {
                // ON: 红色.
                // OFF: 绿色
                // Unknown: 黄色
                UpdateLabelImg(btn, s);
            }
        }
        private void UpdateLabelImg(Label l, OnOff s)
        {
            l.ImageKey = s == OnOff.ON ? "red" : (s == OnOff.OFF ? "green" : "disable");
        }


        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            _mb_server.OnClientConnect -= OnMBClientConnected;
            _mb_server.OnClientDisconnect -= OnMBClientDisconnected;
            _mb_server.Stop();
            _cms_server.StopService();
            _cms_server.OnDoorStateChanged -= OnDoorStateChanged;
        }

        private delegate void UpdateRowImgCallback(DataGridViewRow row, Bitmap img);
        private void UpdateRowImg(DataGridViewRow row, Bitmap img)
        {
            if (this.dgv_doors.InvokeRequired)
            {
                UpdateRowImgCallback cb = new UpdateRowImgCallback(UpdateRowImg);
                try
                {
                    this.Invoke(cb, new object[] { row, img });
                }
                catch (Exception) { }
            }
            else
            {
                row.Cells[ViewColumns.STATE].Value = img;
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
                    row.Cells[nameIndex].Value = text;
            }
        }

        // t: 0=add, 1=update, 2=del
        private delegate void UpdateMBClientCallback(MBConnect c, ClientAction t);
        private void UpdateMBClient(MBConnect c, ClientAction t)
        {
            if (lv_client.InvokeRequired)
            {
                UpdateMBClientCallback cb = new UpdateMBClientCallback(UpdateMBClient);
                this.Invoke(cb, new object[] { c, t });
            }
            else
            {
                ListViewItem item = null;
                if (t == ClientAction.ADD)
                {
                    item = new ListViewItem(new string[10]); //#,ip,recv, send, elapse, recv_bytes, send_bytes
                    _clientRows[c.IP] = item;
                    lv_client.Items.Add(item);
                }
                else
                {
                    item = _clientRows[c.IP];
                }
                if (t == ClientAction.ADD || t == ClientAction.UPDATE)
                {
                    item.SubItems[ClientColumns.IP].Text = c.IP;
                    item.SubItems[ClientColumns.REQ].Text = Convert.ToString(c.Req);
                    item.SubItems[ClientColumns.ACK].Text = Convert.ToString(c.Ack);
                    item.SubItems[ClientColumns.ELAPSE].Text = Convert.ToString(c.Elapsed);
                    item.SubItems[ClientColumns.RECV].Text = Convert.ToString(c.Recv);
                    item.SubItems[ClientColumns.SENT].Text = Convert.ToString(c.Sent);
                    item.ImageKey = "green";
                }
                else
                {
                    lv_client.Items.Remove(item);
                }
            }
        }

        private void OnMBClientConnected(MBConnect c)
        {
            UpdateMBClient(c, ClientAction.ADD);
        }

        private void OnMBClientDisconnected(MBConnect c)
        {
            UpdateMBClient(c, ClientAction.REMOVE);
        }

        private MBMessage OnMBClientRequest(MBConnect c, MBMessage req)
        {
            UpdateMBClient(c, ClientAction.UPDATE);
            if (_handler != null)
            {
                return _handler.DealRequest(c, req);
            }
            return null;
        }

        // 区域状态变化回调
        private void OnZoneStateChanged(Zone z, bool totalOn)
        {
            DataGridViewRow zi = _zoneRows[z.Name];
            if (zi != null)
            {
                UpdateRowImg(zi, Properties.Resources.green_24);
                UpdateRowText(zi, ViewColumns.NAME, z.Name + "(" + z.Statistics + ")");
                Label li;
                if (z.Reg.ZoneCoil == 0) li = lb_z1;
                else if (z.Reg.ZoneCoil == 1) li = lb_z2;
                else if (z.Reg.ZoneCoil == 2) li = lb_z3;
                else if (z.Reg.ZoneCoil == 3) li = lb_z4;
                else if (z.Reg.ZoneCoil == 4) li = lb_z5;
                else li = lb_z6;
                this.UpdateLabelImg(li, z.IsZoneOpen() ? OnOff.ON : OnOff.OFF);
                this.UpdateLabelImg(lb_z_all, totalOn ? OnOff.ON : OnOff.OFF);
            }
        }

        // 门状态变化回调
        private void OnDoorStateChanged(Zone z, Door d)
        {
            DataGridViewRow di = _doorRows[d.IpAddr];
            if (di != null)
            {
                UpdateRowText(di, ViewColumns.ACTION, EnumHelper.ToString(d.State.DoorAction));
                UpdateRowText(di, ViewColumns.OPEN_STATE, EnumHelper.ToString(d.State.OpenState));
                UpdateRowText(di, ViewColumns.CLOSE_STATE, EnumHelper.ToString(d.State.CloseState));
                UpdateRowText(di, ViewColumns.GREEN_LAMP, EnumHelper.ToString(d.State.GreenLamp));
                UpdateRowText(di, ViewColumns.RED_LAMP, EnumHelper.ToString(d.State.RedLamp));
                UpdateRowText(di, ViewColumns.LCB, EnumHelper.ToString(d.State.LCB));
                UpdateRowText(di, ViewColumns.BEEP, EnumHelper.ToString(d.State.Beep));
                UpdateRowText(di, ViewColumns.ERROR, EnumHelper.ToString(d.State.Error, d.State.ExtError));

                if (DoorError.Success != d.State.Error)
                {
                    UpdateRowImg(di, Properties.Resources.yellow_24);
                }
                else
                {
                    UpdateRowImg(di, Properties.Resources.green_24);
                }
            }
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            if (!button_start.Enabled) return;
            button_start.Enabled = false;
            if (button_start.Text == "启动")
            {
                if (_cms_server.StartService())
                {
                    button_start.Text = "停止";
                }
            }
            else
            {
                if (_cms_server.StopService())
                {
                    button_start.Text = "启动";
                }
            }
            button_start.Enabled = true;
        }

        private int ticket = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime dt = System.DateTime.Now;
            label_time.Text = dt.ToLongDateString().ToString() + " " + string.Format("{0:T}", dt);
            ticket++;
            if (_blink_fas ) {
                if (ticket % 2 == 0)
                    lb_fas.ImageKey = "disable";
                else
                    UpdateLabelImg(lb_fas, _fas);
            }
            if (_blink_ibp) {
                if (ticket % 2 == 0)
                    lb_ibp.ImageKey = "disable";
                else
                    UpdateLabelImg(lb_ibp, _ibp);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            log.InfoFormat("关闭服务.");
            if (_cms_server.StopService())
            {
                this.Close();
            }
        }

        private void timerAutoStart_Tick(object sender, EventArgs e)
        {
            timerAutoStart.Enabled = false;
            if (!_mb_server.IsRunning())
            {
                button_start_Click(null, null);
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
    class ClientColumns
    {
        public const int STATE = 0;
        public const int IP = 1;
        public const int REQ = 2;
        public const int ACK = 3;
        public const int ELAPSE = 4;
        public const int RECV = 5;
        public const int SENT = 6;
    }
    public enum ClientAction
    {
        ADD,
        UPDATE,
        REMOVE
    }
}
