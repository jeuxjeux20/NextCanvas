﻿using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace NextCanvas.Models.Content
{
    public class Resource
    {
        public Resource()
        {
            
        }
        /// <summary>
        /// Creates a resource with that name, and uses the stream to copy it inside the <see cref="Data"/>
        /// </summary>
        /// <param name="name">The name, usually something like file.ext</param>
        /// <param name="data">The deeta. Oh no, my system crashed, i lost my deeta !</param>
        public Resource(string name, Stream data)
        {
            Name = name;
            var stream = new MemoryStream();
            data.Position = 0;
            data.CopyTo(stream);
            Data = stream;
        }
        public string Name { get; set; }
        public ResourceType Type { get; set; }        
        
        [JsonIgnore]
        private Stream stream;
        /// <summary>
        /// The deeta
        /// </summary>
        /// <remarks>It can be any sort of stream for convenience.</remarks>
        [JsonIgnore]
        public Stream Data
        {
            get { return stream; }
            set { stream = value; ComputeMD5(); }
        }

        public static string ComputeMD5(Stream stream)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                stream.Position = 0;
                md5.Initialize();
                var hashBytes = md5.ComputeHash(stream);
                stream.Position = 0;
                // Convert the byte array to hexadecimal string
                // https://stackoverflow.com/questions/11454004/calculate-a-md5-hash-from-a-string
                var sb = new StringBuilder();
                foreach (var bytey in hashBytes)
                {
                    sb.Append(bytey.ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private void ComputeMD5()
        {
            DataMD5Hash = ComputeMD5(Data);
        }
        public string DataMD5Hash { get; set; }
    }
}