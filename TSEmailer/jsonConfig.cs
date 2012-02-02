using System;
using System.IO;
using Newtonsoft.Json;

namespace jsonConfig
{
    public class jsConfig
    {
        //Add public variables here (NOT STATIC)
        public string emailaddress = "";
        public string sendas = "";
        public string smtpserver = "";
        public int smtpport = 25;
        public string mapiserver = "";
        public int mapiport = 123;
        public string smtpuser = "";
        public string smtppass = "";
        public string mapiuser = "";
        public string mapipass = "";
        public int mailtimer = 0;
        public int jointimer = 0;
        public bool newplayer = true;

        public static jsConfig Read(string path)
        {
            if (!File.Exists(path))
                return new jsConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static jsConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<jsConfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<jsConfig> ConfigRead;
    }
}