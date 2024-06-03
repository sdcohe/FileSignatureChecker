using System;
using System.Collections.Generic;
using System.IO;

namespace FileSignatureChecker.Classes
{
    public class FileSignature : IComparable<FileSignature>, IEqualityComparer<FileSignature>
    {
        public string FileName { get; set; } = "";
        public long Length { get; set; } = 0;
        public string Hash { get; set; } = "";
        public FileSignature() { }
        public FileSignature(FileInfo fileInfo) 
        { 
            FileName = fileInfo.Name;
            Length = fileInfo.Length;
            string fullPath = Path.Combine(fileInfo.Directory.FullName, FileName);
            Hash = UtilityFunctions.CalculateSHA256(fullPath);
        }

        public override string ToString()
        {
            return string.Format("Name: {0}, Length: {1}, Hash: {2}", FileName, Length, Hash);
        }

        public int CompareTo(FileSignature other)
        {
            if (other is null) throw new ArgumentNullException(nameof(other));

            int result = FileName.CompareTo(other.FileName);
            if (result == 0)
            {
                result = Length.CompareTo(other.Length);
            }
            if (result == 0)
            {
                result = Hash.CompareTo(other.Hash);
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            return obj is FileSignature file &&
                   FileName == file.FileName &&
                   Length == file.Length &&
                   Hash == file.Hash;
        }

        public override int GetHashCode()
        {
            int hashCode = 1482071670;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Hash);
            return hashCode;
        }

        public bool Equals(FileSignature x, FileSignature y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(FileSignature obj)
        {
            return obj.GetHashCode();
        }
    }
}