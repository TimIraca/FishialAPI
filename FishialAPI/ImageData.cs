using System.Security.Cryptography;

namespace FishialAPI
{
    public class ImageMetadata
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long ByteSize { get; set; }
        public string Checksum { get; set; }

        public ImageMetadata(string filePath)
        {
            FileName = Path.GetFileName(filePath);

            MimeType = GetMimeType(filePath);

            ByteSize = new FileInfo(filePath).Length;

            Checksum = GetMD5Checksum(filePath);
        }

        private static string GetMimeType(string filePath) // werkt niet op raspberry, gebruik later MIME van camera of webcam
        {
            string mimeType = "image/jpeg";
            // string ext = Path.GetExtension(filePath).ToLower();
            // Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            // if (regKey != null && regKey.GetValue("Content Type") != null)
            // mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        private static string GetMD5Checksum(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return Convert.ToBase64String(hash);
                }
            }
        }
    }
}
