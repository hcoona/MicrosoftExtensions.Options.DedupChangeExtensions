using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;

namespace Microsoft.Extensions.Options
{
    public static class OptionsDedupChangeExtensions
    {
        private readonly static ThreadLocal<BinaryFormatter> binaryFormatterLocal =
            new ThreadLocal<BinaryFormatter>(() => new BinaryFormatter());
        private readonly static ThreadLocal<HashAlgorithm> hashAlgorithmLocal =
            new ThreadLocal<HashAlgorithm>(SHA1.Create);

        public static IDisposable OnChangeDedup<TOptions>(
            this IOptionsMonitor<TOptions> monitor,
            string name,
            Action<TOptions, string> listener)
        {
            var originValueHashToken = GetHashToken(monitor.Get(name));
            return monitor.OnChange((newValue, key) =>
            {
                if (key == name && !IsHashTokenEqual(originValueHashToken, GetHashToken(newValue)))
                {
                    listener(newValue, key);
                }
            });
        }

        public static IDisposable OnChangeDedup<TOptions>(
            this IOptionsMonitor<TOptions> monitor,
            Action<TOptions> listener)
        {
            return OnChangeDedup(
                monitor,
                Options.DefaultName,
                (options, _) => listener(options));
        }

        private static byte[] GetHashToken(object graph)
        {
            using (var stream = new MemoryStream())
            {
                binaryFormatterLocal.Value.Serialize(stream, graph);
                stream.Seek(0, SeekOrigin.Begin);
                return hashAlgorithmLocal.Value.ComputeHash(stream);
            }
        }

        private static bool IsHashTokenEqual(byte[] lhs, byte[] rhs)
        {
            return System.Linq.Enumerable.SequenceEqual(lhs, rhs);
        }
    }
}
