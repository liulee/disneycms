using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization.Formatters;
using System.IO;
using log4net;
using System.Collections.Generic;
using System.Xml;
using System.Text;
namespace DisneyCMS
{

    [DataContract]
    internal class Config
    {

        private static string cfgFile = "client.conf";

        static ILog log = LogManager.GetLogger("CFG");
        [DataMember(Order = 0)]
        public ServerConfig Server { get; set; }

        [DataMember(Order = 1)]
        public List<Scene> Scenes { get; set; }

        public void Save()
        {
            FileStream fs = new FileStream(cfgFile, FileMode.Create);
            DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(Config));
            // XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(fs);
            // writer.Settings.Indent = true;
            _json.WriteObject(fs, this);
            //writer.Flush();
            fs.Close();
        }

        public static Config Load(int zcnt = 6, int dcnt = 12)
        {
            FileInfo fi = new FileInfo(cfgFile);
            if (fi.Exists)
            {
                using (FileStream fs = new FileStream(cfgFile, FileMode.Open))
                {
                    try
                    {
                        var serializer = new DataContractJsonSerializer(typeof(Config));
                        Config c = (Config)serializer.ReadObject(fs);
                        return c;
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                        return Config.Default(6, 12);
                    }
                }
            }
            else
            {
                return Config.Default(6, 12);
            }
        }

        static Config Default(int zoneCnt, int doorCnt)
        {
            Config cfg = new Config()
            {
                Server = new ServerConfig() { IP = "192.168.32.1", Port = 502 },
                Scenes = new List<Scene>(),
            };
            cfg.AddScene(Scene.Default());
            return cfg;
        }

        public void AddScene(Scene s)
        {
            if (Scenes == null)
            {
                Scenes = new List<Scene>();
            }
            Scenes.Add(s);
        }

        internal void RemoveScene(Scene s)
        {
            Scenes.Remove(s);
        }

        internal int SceneCount()
        {
            return Scenes != null ? Scenes.Count : 0;
        }
    }

    [DataContract]
    internal class ServerConfig
    {
        [DataMember]
        public string IP { get; set; }

        [DataMember]
        public int Port { get; set; }
    }


    [DataContract]
    internal class Scene
    {
        [DataMember(Order = 0)]
        public List<SceneZone> Zones { get; set; }

        [DataMember(Order = 1)]
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public static Scene Default()
        {
            Scene s = new Scene()
            {
                Zones = new List<SceneZone>(),
                Name = string.Format("场景")
            };
            return s;
        }

        /// Get Zone at [index]
        public SceneZone ZoneAt(int zid)
        {
            if (Zones == null)
            {
                Zones = new List<SceneZone>();
            }
            foreach (SceneZone sz in Zones)
            {
                if (sz.Index == zid)
                {
                    return sz;
                }
            }
            SceneZone zi = new SceneZone() { Index = zid };
            Zones.Add(zi);
            return zi;
        }

        internal void UpdateZone(int zid, bool isChecked)
        {
            SceneZone z = ZoneAt(zid);
            z.Enabled = isChecked;
        }
    }

    [DataContract]
    internal class SceneZone
    {
        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        public List<ZoneDoor> Doors { get; set; }

        /// Get Zone by [index]
        public ZoneDoor DoorOf(int index)
        {
            if (index < 0) return null;
            if (Doors == null)
            {
                Doors = new List<ZoneDoor>();
            }
            foreach (ZoneDoor zd in Doors)
            {
                if (zd.Index == index)
                {
                    return zd;
                }
            }
            ZoneDoor nd = new ZoneDoor()
            {
                Index = index
            };
            Doors.Add(nd);
            return nd;
        }

    }

    [DataContract]
    internal class ZoneDoor
    {
        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        public bool SwitchOn { get; set; }

        [DataMember]
        public bool GreenOn { get; set; }

        [DataMember]
        public bool RedOn { get; set; }

        public override string ToString()
        {
            return string.Format("D-{0}:{1}-{2}-{3}-{4}", Index, Enabled ? 1 : 0, SwitchOn ? 1 : 0, GreenOn ? 1 : 0, RedOn ? 1 : 0);
        }
    }
}
