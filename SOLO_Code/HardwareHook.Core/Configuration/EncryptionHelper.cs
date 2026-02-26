using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HardwareHook.Core.Configuration
{
    /// <summary>
    /// 加密辅助类
    /// </summary>
    public class EncryptionHelper
    {
        private const string DefaultKey = "HardwareHook2026!@#$1234";
        private const string DefaultIV = "HookTool2026!@#$";

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="plainText">明文</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns>加密后的字符串</returns>
        public static string Encrypt(string plainText, string key = null, string iv = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key ?? DefaultKey);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv ?? DefaultIV);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="cipherText">密文</param>
        /// <param name="key">密钥</param>
        /// <param name="iv">初始化向量</param>
        /// <returns>解密后的字符串</returns>
        public static string Decrypt(string cipherText, string key = null, string iv = null)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key ?? DefaultKey);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv ?? DefaultIV);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 加密配置文件
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        /// <param name="encryptedPath">加密后的文件路径</param>
        /// <returns>是否加密成功</returns>
        public static bool EncryptConfigFile(string configPath, string encryptedPath)
        {
            try
            {
                string plainText = File.ReadAllText(configPath);
                string encryptedText = Encrypt(plainText);
                File.WriteAllText(encryptedPath, encryptedText);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解密配置文件
        /// </summary>
        /// <param name="encryptedPath">加密文件路径</param>
        /// <param name="decryptedPath">解密后的文件路径</param>
        /// <returns>是否解密成功</returns>
        public static bool DecryptConfigFile(string encryptedPath, string decryptedPath)
        {
            try
            {
                string encryptedText = File.ReadAllText(encryptedPath);
                string decryptedText = Decrypt(encryptedText);
                File.WriteAllText(decryptedPath, decryptedText);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
