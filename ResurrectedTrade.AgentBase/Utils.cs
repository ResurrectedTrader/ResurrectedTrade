using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Win32;

namespace ResurrectedTrade.AgentBase
{
    public static class Utils
    {
        // Unsafe, but we know that the classes we serialize don't have dodgy code.
        private static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public static readonly Mutex SingleInstanceMutex = new Mutex(false, "Global\\ResurrectedTrade.Agent");

#if DEBUG
        public const string ApiAddress = "https://localhost:7044";
#else
        public const string ApiAddress = "https://api.resurrected.trade";
#endif
        public static readonly Uri ApiAddressUri = new Uri(ApiAddress);

        public static readonly RegistryKey AgentRegistryKey =
            Registry.CurrentUser.CreateSubKey(@"HKEY_CURRENT_USER\SOFTWARE\Resurrected Trade\Agent");

        private static readonly byte[] Nonce =
        {
            0xE8, 0x6E, 0x6B, 0x5F, 0x00, 0xB9, 0xFF, 0xFF, 0xFF, 0xFF, 0xE8, 0x90, 0x4B, 0x7C, 0x01
        };

        public static void OpenUrl(string url)
        {
            new Process { StartInfo = new ProcessStartInfo { FileName = url, UseShellExecute = true } }.Start();
        }

        private static byte[] SerializeToBytes(CookieContainer container)
        {
            using (var ms = new MemoryStream())
            {
#pragma warning disable SYSLIB0011
                BinaryFormatter.Serialize(ms, container);
#pragma warning restore SYSLIB0011
                return ProtectedData.Protect(ms.ToArray(), Nonce, DataProtectionScope.CurrentUser);
            }
        }

        private static CookieContainer DeserializeFromBytes(byte[] state)
        {
            byte[] cookieJarBytes = ProtectedData.Unprotect(state, Nonce, DataProtectionScope.CurrentUser);
#pragma warning disable SYSLIB0011
            return (CookieContainer)BinaryFormatter.Deserialize(new MemoryStream(cookieJarBytes));
#pragma warning restore SYSLIB0011
        }

        public static bool IsStatusCodeException(Exception exc, StatusCode code)
        {
            if (exc is AggregateException aExc)
            {
                foreach (var iExc in aExc.Flatten().InnerExceptions)
                {
                    if (IsStatusCodeException(iExc, code))
                    {
                        return true;
                    }
                }
            }

            if (exc is RpcException rExc)
            {
                if (rExc.StatusCode == code)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncStreamReader<T> streamReader)
            where T : class
        {
            List<T> result = new List<T>();
            while (await streamReader.MoveNext().ConfigureAwait(false))
            {
                result.Add(streamReader.Current);
            }

            return result;
        }

        public static CookieContainer LoadCookieContainer()
        {
            var state = (byte[])AgentRegistryKey.GetValue(
                "STATE",
                null
            );

            if (state != null)
            {
                var container = DeserializeFromBytes(state);
                var cookie = container.GetCookies(ApiAddressUri)["rt-cookie"];
                if (cookie != null && !cookie.Expired)
                {
                    return container;
                }
            }

            return new CookieContainer();
        }

        public static void SaveCookieContainer(CookieContainer container)
        {
            byte[] state = SerializeToBytes(container);
            AgentRegistryKey.SetValue(
                "STATE",
                state,
                RegistryValueKind.Binary
            );
        }

        public static bool HasValidCookie(CookieContainer container)
        {
            return container.Count > 0 &&
                   !(container.GetCookies(ApiAddressUri)["rt-cookie"]?.Expired ?? true);
        }
    }
}