using SFML.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MMXOnline
{
    public static class Extensions
    {
        public static Point toPoint(this Vector2f vector2f)
        {
            return new Point(vector2f.X, vector2f.Y);
        }

        public static T GetRandomItem<T>(this IList<T> list)
        {
            if (list.Count == 0) return default(T);
            int randomIndex = Helpers.randomRange(0, list.Count - 1);
            return list[randomIndex];
        }

        public static string SplitPopJoin(this string str, char delim)
        {
            var pieces = str.Split(delim).ToList();
            pieces.Pop();
            return string.Join(delim, pieces);
        }

        public static T PopFirst<T>(this List<T> list)
        {
            if (list.Count == 0)
            {
                return default(T);
            }
            T item = list[0];
            list.RemoveAt(0);
            return item;
        }

        public static T Pop<T>(this List<T> list)
        {
            if (list.Count == 0)
            {
                return default(T);
            }
            T item = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return item;
        }

        private static Random rng = new Random();
        public static List<T> Shuffle<T>(this List<T> list)
        {
            var newList = new List<T>(list);
            int n = newList.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = newList[k];
                newList[k] = newList[n];
                newList[n] = value;
            }
            return newList;
        }

        public static string RemovePrefix(this string s, string prefix)
        {
            if (s == null || s.Length < prefix.Length) return s;
            return s.Substring(prefix.Length);
        }

        public static string Truncate(this string s, int maxLength)
        {
            if (s.Length <= maxLength)
            {
                return s;
            }
            return s.Substring(0, maxLength) + "...";
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default;
        }

        public static TValue GetValueOrCreate<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            dictionary[key] = defaultValue;
            return dictionary[key];
        }

        /*
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }
        */

        public static bool IsValidIpAddress(this string ipAddressString)
        {
            if (string.IsNullOrEmpty(ipAddressString)) return false;
            return IPAddress.TryParse(ipAddressString, out _);
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> array)
        {
            return (array == null || array.Count == 0);
        }

        public static bool HasDuplicates<T>(this IEnumerable<T> enumerable)
        {
            var knownKeys = new HashSet<T>();
            return enumerable.Any(item => !knownKeys.Add(item));
        }

        public static bool InRange<T>(this List<T> array, int index)
        {
            return index >= 0 && index < array.Count;
        }

        public static bool InRange<T>(this T[] array, int index)
        {
            return index >= 0 && index < array.Length;
        }

        public static Actor actor(this IDamagable damagable)
        {
            return damagable as Actor;
        }

        public static void SendStringMessage(this TcpClient client, string message, NetworkStream networkStream)
        {
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            SendMessage(client, messageBytes, networkStream);
        }

        public static void SendMessage(this TcpClient client, byte[] messageBytes, NetworkStream networkStream)
        {
            // determine length of message
            int length = messageBytes.Length;

            // convert the length into bytes using BitConverter (encode)
            byte[] lengthBytes = System.BitConverter.GetBytes(length);

            // flip the bytes if we are a little-endian system: reverse the bytes in lengthBytes to do so
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            // send length
            networkStream.Write(lengthBytes, 0, lengthBytes.Length);

            // send message
            networkStream.Write(messageBytes, 0, length);
        }


        public static string ReadStringMessage(this TcpClient client, NetworkStream networkStream)
        {
            var messageBytes = ReadMessage(client, networkStream);

            string message = System.Text.Encoding.ASCII.GetString(messageBytes);

            return message;
        }

        public static byte[] ReadMessage(this TcpClient client, NetworkStream networkStream)
        {
            // read length bytes, and flip if necessary
            byte[] lengthBytes = ReadBytes(sizeof(int), networkStream); // int is 4 bytes
            if (System.BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            // decode length
            int length = System.BitConverter.ToInt32(lengthBytes, 0);

            // read message bytes
            byte[] messageBytes = ReadBytes(length, networkStream);

            return messageBytes;
        }

        private static byte[] ReadBytes(int count, NetworkStream networkStream)
        {
            byte[] bytes = new byte[count]; // buffer to fill (and later return)
            int readCount = 0; // bytes is empty at the start

            // while the buffer is not full
            while (readCount < count)
            {
                // ask for no-more than the number of bytes left to fill our byte[]
                int left = count - readCount; // we will ask for `left` bytes
                int r = networkStream.Read(bytes, readCount, left); // but we are given `r` bytes (`r` <= `left`)

                if (r == 0)
                {
                    // I lied, in the default configuration, a read of 0 can be taken to indicate a lost connection
                    throw new Exception("Lost Connection during read");
                }

                readCount += r; // advance by however many bytes we read
            }

            return bytes;
        }
    }
}
