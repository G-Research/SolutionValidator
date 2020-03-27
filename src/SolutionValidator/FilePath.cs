using System;
using System.IO;

namespace SolutionValidator
{
    public static class FilePathExtensions
    {
        public static bool IsFileNameMatch(this FilePath file, string fileName)
        {
            return string.Equals(file.FileName, Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFileNameMatch(this FilePath lhs, FilePath rhs)
        {
            return string.Equals(lhs.FileName, rhs.FileName, StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsFileNameMatch(this string lhs, string rhs)
        {
            return string.Equals(Path.GetFileName(lhs), Path.GetFileName(rhs), StringComparison.OrdinalIgnoreCase);
        }
    }

    public class DirectoryPath : FileSystemPath
    {
        public DirectoryPath Parent
        {
            get
            {
                var index = NormalisedPath.LastIndexOf(Path.DirectorySeparatorChar);

                if (index == -1)
                {
                    return null;
                }

                return new DirectoryPath(NormalisedPath.Substring(0, index));

            }
        }

        public DirectoryPath(string path) : base(path)
        {
        }
    }

    public class FileSystemPath
    {
        public string NormalisedPath { get; }

        public FileSystemPath(string path)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                path = path.Replace('\\', '/');
            }

            NormalisedPath = Path.GetFullPath(path);
        }

        public string GetRelativePath(FileSystemPath referencePath)
        {
            return Path.GetRelativePath(referencePath.NormalisedPath, NormalisedPath);
        }

        public override int GetHashCode()
        {
            return NormalisedPath.GetHashCode();
        }

        public static bool operator ==(FileSystemPath lhs, string rhs)
        {
            if (lhs is null)
            {
                return rhs == null;
            }

            return string.Equals(lhs.NormalisedPath, rhs, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(FileSystemPath lhs, string rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(string lhs, FileSystemPath rhs)
        {
            if (ReferenceEquals(null, rhs))
            {
                return ReferenceEquals(null, lhs);
            }

            return string.Equals(lhs, rhs.NormalisedPath, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(string lhs, FileSystemPath rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(FileSystemPath lhs, FileSystemPath rhs)
        {
            if (ReferenceEquals(null, lhs))
            {
                return ReferenceEquals(null, rhs);
            }

            if (ReferenceEquals(null, rhs))
            {
                return false;
            }

            return string.Equals(lhs.NormalisedPath, rhs.NormalisedPath, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(FileSystemPath lhs, FileSystemPath rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator string(FileSystemPath path) => path.NormalisedPath;

        public override bool Equals(object obj)
        {
            if (obj is string str)
            {
                return NormalisedPath == str;
            }

            if (obj is FilePath other)
            {
                return this == other;
            }

            return false;
        }

        public override string ToString()
        {
            return NormalisedPath;
        }
    }

    public class FilePath : FileSystemPath
    {
        /// <summary>
        /// File Name without extension
        /// </summary>
        public string Name { get; }

        public bool Exists => File.Exists(NormalisedPath);
        public string FileName { get; }
        public DirectoryPath Directory { get; }

        public FilePath(string path)
            : base(path)
        {
            Directory = new DirectoryPath(Path.GetDirectoryName(NormalisedPath));
            FileName = Path.GetFileName(NormalisedPath);
            Name = Path.GetFileNameWithoutExtension(NormalisedPath);
        }

        public static FilePath Create(params string[] pathFragments)
        {
            return new FilePath(Path.Combine(pathFragments));
        }

        public static FilePath Create(string part1, string part2)
        {
            return new FilePath(Path.Combine(part1, part2));
        }
    }
}
