using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WindowsPackageDownloader.Core
{
    //
    // Source: https://raw.githubusercontent.com/ADeltaX/ProtoBuildBot/5cce37197c44792f3401b63d876795b5bc2072a4/src/BuildChecker/Classes/Helpers/EsrpDecryptor.cs
    // Released under the MIT License (as of 2020-11-04)
    // And Updated with original author input on 2021-04-15
    //
    public class EsrpDecryptor : IDisposable
    {
        private readonly Aes aes;
        private readonly byte[] key;

        public EsrpDecryptor(string key2)
        {
            key = new byte[32];
            Array.Copy(Convert.FromBase64String(key2), 0, key, 0, 32);

            aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = key;
            aes.Padding = PaddingMode.None;
        }

        public async Task DecryptBufferToStreamAsync(byte[] buffer, Stream to, int bufferLength, long previousSumBlockLength,
            bool isPadded, CancellationToken cancellationToken = default)
        {
            byte[] offsetBytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(previousSumBlockLength), offsetBytes, 8);

            using ICryptoTransform ivCrypter = aes.CreateEncryptor(key, new byte[16]);
            byte[] newIv = ivCrypter.TransformFinalBlock(offsetBytes, 0, 16);

            if (isPadded)
            {
                aes.Padding = PaddingMode.PKCS7;
            }

            using ICryptoTransform dec = aes.CreateDecryptor(key, newIv);
            using MemoryStream ms = new(buffer, 0, bufferLength);
            using CryptoStream cs = new(ms, dec, CryptoStreamMode.Read);

#if NET5_0_OR_GREATER
            await cs.CopyToAsync(to, cancellationToken);
#else
            await cs.CopyToAsync(to);
#endif
        }

        public async Task DecryptStreamFullAsync(Stream encryptedFile, Stream decryptedFile, ulong encryptedSize,
            CancellationToken cancellationToken = default)
        {
            int readBytes;
            byte[] buffer = new byte[65536];
#if NET5_0_OR_GREATER
            while ((readBytes = await encryptedFile.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
#else
            while ((readBytes = await encryptedFile.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
#endif
            {
                bool needsPaddingMode = encryptedSize == (ulong)encryptedFile.Position;
                long previousSumBlockLength = encryptedFile.Position - readBytes;
                await DecryptBufferToStreamAsync(buffer, decryptedFile, readBytes, previousSumBlockLength, needsPaddingMode, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async Task DecryptFileAsync(string encryptedFilePath, string decryptedFilePath,
            CancellationToken cancellationToken = default)
        {
            using FileStream encryptedFile = File.OpenRead(encryptedFilePath);
            using FileStream decryptedFile = File.OpenWrite(decryptedFilePath);
            await DecryptStreamFullAsync(encryptedFile, decryptedFile, (ulong)encryptedFile.Length, cancellationToken);
        }

        public void Dispose()
        {
            aes.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
