using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using log4net;
using System.ComponentModel.Composition.Hosting;

namespace DisneyCMS.modbus
{
    public class MBFunHandlerMgr
    {
        private static readonly ILog Log = LogManager.GetLogger("MBFM");

        [ImportMany]
        private Lazy<MBFunHandler, MBFunHandlerAttribute>[] _handlers = null;

        private Dictionary<byte, MBFunHandler> _handlerMap;

        public void initialize(){
           var catalog = new AssemblyCatalog(typeof(MBFunHandlerMgr).Assembly);
            CompositionContainer cc = new CompositionContainer(catalog);
            cc.ComposeParts(this);

            _handlerMap = new Dictionary<byte, MBFunHandler>();
            foreach (Lazy<MBFunHandler, MBFunHandlerAttribute> fh in _handlers)
            {
                _handlerMap[fh.Metadata.FunCode] = fh.Value;
            }
        }
    }
}
