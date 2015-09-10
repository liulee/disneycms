using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DisneyCMS
{
    public class TextBoxWriter : TextWriter
    {
        private readonly TextBox _txtBox;

        private delegate void VoidAction();

        private int msg_count = 0;
        private int max_count = 1000;

        public TextBoxWriter(TextBox box)
        {
            _txtBox = box; //transfer the enternal TextBox in
        }

        private delegate void OnCheckFull();
        private void CheckFull()
        {
            if (msg_count++ >= max_count)
            {               
                if (_txtBox.InvokeRequired)
                {
                    OnCheckFull func = new OnCheckFull(CheckFull);
                    _txtBox.Invoke(func, new object[] { });
                }
                else
                {
                    msg_count = 0;
                    _txtBox.Clear();
                }                
            }
        }

        public override void Write(char value)
        {
            CheckFull();
            VoidAction action = () => _txtBox.AppendText(value.ToString());
            _txtBox.BeginInvoke(action);
        }

        public override void Write(string str)
        {
            CheckFull();
            VoidAction action = () => _txtBox.AppendText(str);
            _txtBox.BeginInvoke(action);
        }

        public override void Write(string fmt, params object[] args)
        {
            CheckFull();
            VoidAction action = () => _txtBox.AppendText(string.Format(fmt, args));
            _txtBox.BeginInvoke(action);
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}
