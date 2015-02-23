﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

//using Newtonsoft.Json;

namespace WGestures.Core.Persistence.Impl
{

    public class JsonGestureIntentStore : IGestureIntentStore
    {
        public string FileVersion { get; set; }
        public Dictionary<string, ExeApp> Apps { get; set; }
        public GlobalApp GlobalApp { get; set; }

        private string jsonPath;
        private JsonSerializer ser = new JsonSerializer();

        private JsonGestureIntentStore() { }

        public JsonGestureIntentStore(string jsonPath, string fileVersion)
        {
            FileVersion = fileVersion;
            this.jsonPath = jsonPath;
            SetupSerializer();


            if (File.Exists(jsonPath))
            {
                Deserialize();
            }
            else
            {
                Apps = new Dictionary<string, ExeApp>();
                GlobalApp = new GlobalApp();
            }
        }

        public JsonGestureIntentStore(Stream stream, bool closeStream, string fileVersion)
        {
            FileVersion = fileVersion;
            SetupSerializer();
            Deserialize(stream, closeStream);
        }

        private void Deserialize(Stream stream, bool closeStream)
        {
            if(stream == null || !stream.CanRead) throw new ArgumentException("stream");
            try
            {
                using (var txtReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(txtReader))
                {
                    var ser = new JsonSerializer();
                    ser.Formatting = Formatting.None;
                    ser.TypeNameHandling = TypeNameHandling.Auto;

                    if (FileVersion.Equals("1"))
                    {
                        ser.Converters.Add(new GestureIntentConverter_V1());

                    }
                    else if (FileVersion.Equals("2"))
                    {
                        ser.Converters.Add(new GestureIntentConverter());

                    }
                    var result = ser.Deserialize<SerializeWrapper>(jsonReader);

                    FileVersion = result.FileVersion;
                    GlobalApp = result.Global;
                    Apps = result.Apps;


                }
            }
            finally
            {
                if (closeStream) stream.Dispose();
            }

            //todo: 完全在独立domain中加载json.net?
            /*var deserializeDomain = AppDomain.CreateDomain("jsonDeserialize");
            deserializeDomain.UnhandledException += (sender, args) => { throw new IOException(args.ExceptionObject.ToString()); };
            deserializeDomain.DomainUnload += (sender, args) =>
            {
                Console.WriteLine("deserializeDomain Unloaded");
            };
            var wrapperRef = (ISerializeWrapper)deserializeDomain.CreateInstanceAndUnwrap("SerializeWrapper", "SerializeWrapper.SerializeWrapper");

            wrapperRef.DeserializeFromStream(stream, FileVersion, closeStream);

            GlobalApp = wrapperRef.Global;
            Apps = wrapperRef.Apps;

            wrapperRef = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            AppDomain.Unload(deserializeDomain);
            deserializeDomain = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();*/
            
        }

        private void Serialize()
        {
            using (var fs = new StreamWriter(jsonPath))
            {
                using (var writer = new JsonTextWriter(fs))
                {
                 
                    ser.Serialize(writer,new SerializeWrapper(){Apps = Apps, FileVersion = FileVersion, Global = GlobalApp});
                }
            }

            //todo: 完全在独立domain中加载json.net?
            /*var serializeDomain = AppDomain.CreateDomain("jsonDeserialize");
            serializeDomain.UnhandledException += (sender, args) => { throw new IOException(args.ExceptionObject.ToString()); };

            serializeDomain.DomainUnload += (sender, args) =>
            {
                Console.WriteLine("serializeDomain Unloaded");
            };
            var wrapperRef = (ISerializeWrapper)serializeDomain.CreateInstanceAndUnwrap("SerializeWrapper", "SerializeWrapper.SerializeWrapper");

            wrapperRef.FileVersion = FileVersion;
            wrapperRef.Apps = Apps;
            wrapperRef.Global = GlobalApp;
            wrapperRef.SerializeTo(jsonPath);

            wrapperRef = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            AppDomain.Unload(serializeDomain);
            serializeDomain = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();*/
        }

        private void Deserialize()
        {
            using (var file = new FileStream(jsonPath, FileMode.Open))
            {
                Deserialize(file, false);
            }

        }

        private void SetupSerializer()
        {
            ser.Formatting = Formatting.None;
            ser.TypeNameHandling = TypeNameHandling.Auto;

            if (FileVersion.Equals("1"))
            {
                ser.Converters.Add(new GestureIntentConverter_V1());

            }else if (FileVersion.Equals("2"))
            {
                ser.Converters.Add(new GestureIntentConverter());

            }
        }


        public bool TryGetExeApp(string key, out ExeApp found)
        {
            return Apps.TryGetValue(key, out found);
        }

        public ExeApp GetExeApp(string key)
        {
            return Apps[key];
        }


        public void Remove(string key)
        {
            Apps.Remove(key);
        }

        public void Remove(ExeApp app)
        {
            Remove(app.ExecutablePath);
        }

        public void Add(ExeApp app)
        {
            Apps.Add(app.ExecutablePath, app);
        }

        public void Save()
        {
            Serialize();

        }

        public JsonGestureIntentStore Clone()
        {
            var ret = new JsonGestureIntentStore();
            ret.GlobalApp = GlobalApp;
            ret.Apps = Apps;
            ret.FileVersion = FileVersion;
            ret.jsonPath = jsonPath;

            return ret;
        }

        public void Import(JsonGestureIntentStore from, bool replace=false)
        {
            if (from == null) return;

            if (replace)
            {
                GlobalApp.GestureIntents.Clear();
                GlobalApp.IsGesturingEnabled = from.GlobalApp.IsGesturingEnabled;
                Apps.Clear();
            }

            GlobalApp.ImportGestures(from.GlobalApp);
            
            foreach (var kv in from.Apps)
            {
                ExeApp appInSelf;
                //如果应用程序已经在列表中，则合并手势
                if (TryGetExeApp(kv.Key, out appInSelf))
                {
                    appInSelf.ImportGestures(kv.Value);
                    appInSelf.IsGesturingEnabled = appInSelf.IsGesturingEnabled && kv.Value.IsGesturingEnabled;
                }
                else//否则将app添加到列表中
                {
                    Add(kv.Value);
                }
            }
        }

        public IEnumerator<ExeApp> GetEnumerator()
        {
            return Apps.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal class SerializeWrapper
        {
            //[JsonProperty("FileVersion")]
            public string FileVersion { get; set; }

            //[JsonProperty("Apps")]
            public Dictionary<string, ExeApp> Apps { get; set; }
            
            //[JsonProperty("Global")]
            public GlobalApp Global { get; set; }

        }

        internal class GestureIntentConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dict = value as GestureIntentDict;
                serializer.Serialize(writer, dict.Values.ToList());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var dict = new GestureIntentDict();
                var list = serializer.Deserialize<List<GestureIntent>>(reader);
                foreach (var i in list)
                {
                    dict.Add(i);
                }

                return dict;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GestureIntentDict);
            }
        }

        //.json
        internal class GestureIntentConverter_V1 : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var dict = value as GestureIntentDict;
                serializer.Serialize(writer, dict.Values.ToList());
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var dict = new GestureIntentDict();
                var list = serializer.Deserialize<List<KeyValuePair<Gesture, GestureIntent>>>(reader);
                foreach (var i in list)
                {
                    Debug.WriteLine("Add Gesture: " + i.Value.Gesture);
                    dict.Add(i.Value);
                }

                return dict;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(GestureIntentDict);
            }
        }
        /*
        public interface ISerializeWrapper
        {
             string FileVersion { get; set; }
             Dictionary<string, ExeApp> Apps { get; set; }
             GlobalApp Global { get; set; }

             void DeserilizeFromFile(string filename, string version);
            void DeserializeFromStream(Stream s, string version, bool close = false);
             void SerializeTo(string fileName);
        }*/
    }
}
