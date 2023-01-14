using System;
using System.IO;
using ProtoBuf;

namespace SeamlessClientPlugin.Utilities
{
    public static class Utility
    {
        public static byte[] Serialize<T>(T instance)
        {
            if (instance == null) return null;

            using (var m = new MemoryStream())
            {
                // m.Seek(0, SeekOrigin.Begin);
                Serializer.Serialize(m, instance);

                return m.ToArray();
            }
        }


        public static T Deserialize<T>(byte[] data)
        {
            if (data == null) return default;

            using (var m = new MemoryStream(data))
                return Serializer.Deserialize<T>(m);
        }

        public static dynamic CastToReflected(this object o, Type type) => Convert.ChangeType(o, type);
    }
}
