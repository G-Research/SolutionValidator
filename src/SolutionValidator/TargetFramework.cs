using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace SolutionValidator
{
    public enum FrameworkType
    {
        NetFramework = 0,
        NetStandard = 1,
        NetCore = 2,
        DotNet = 3
    }

    public static class TargetFrameworkExtensions
    {
        public static TargetFramework GetResultantFramework(this TargetFramework targetFramework, TargetFramework referenceFramework)
        {
            if (referenceFramework.FrameworkType == targetFramework.FrameworkType)
            {
                return referenceFramework;
            }

            if (referenceFramework.FrameworkType == FrameworkType.NetStandard)
            {
                return targetFramework;
            }

            return referenceFramework;
        }

        public static bool TryGetBestMatchingTargetFramework(this TargetFramework targetFramework, IReadOnlyList<TargetFramework> referenceFrameworks, out TargetFramework matchingFramework)
        {
            // Happy path
            if (referenceFrameworks.Contains(targetFramework))
            {
                matchingFramework = targetFramework;
                return true;
            }

            matchingFramework = referenceFrameworks.Where(tf =>
                tf.FrameworkType == targetFramework.FrameworkType
                && tf.Version <= targetFramework.Version
                && (tf.TargetPlatformIdentifier == TargetPlatformIdentifier.PlatformAgnostic || (tf.TargetPlatformIdentifier == targetFramework.TargetPlatformIdentifier)))
                .OrderBy(tf => tf.Version).LastOrDefault();

            if (matchingFramework != null)
            {
                return true;
            }

            if (targetFramework.FrameworkType == FrameworkType.NetStandard)
            {
                // While in general .net seems to allows netstandard -> netframework references this adds complexity and confusion and NU1702 errors
                // We want .net standard all the way down the dependency graph so it is clear without additional checks like this tool that I can depend upon
                // the project from any of the traget framework types.
                // To re-enable we would need:
                //    matchingFramework = referenceFrameworks.Where(tf => tf.FrameworkType == FrameworkType.NetFramework).OrderBy(tf => tf.Version).SingleOrDefault();
                //    return matchingFramework != null;
                return false;
            }

            Version netstandardVersion = Version.Parse("2.0");
            if (targetFramework.FrameworkType == FrameworkType.NetCore && targetFramework.Version >= Version.Parse("3.0"))
            {
                netstandardVersion = Version.Parse("2.1");
            }

            matchingFramework = referenceFrameworks.Where(tf => tf.FrameworkType == FrameworkType.NetStandard && tf.Version <= netstandardVersion).OrderBy(tf => tf.Version).FirstOrDefault();
            return matchingFramework != null;
        }

        public static bool TryGetClosestHigherFrameworkMatch(this TargetFramework targetFramework, IReadOnlyList<TargetFramework> referenceFrameworks, out TargetFramework higherFramework)
        {
            higherFramework = referenceFrameworks.Where(t => t.FrameworkType == targetFramework.FrameworkType
                           && (targetFramework.TargetPlatformIdentifier == TargetPlatformIdentifier.PlatformAgnostic || targetFramework.TargetPlatformIdentifier == t.TargetPlatformIdentifier)
                           && t.Version >= targetFramework.Version).OrderBy(t => t).FirstOrDefault();

            return higherFramework != null;
        }

        public static bool IsCompatibleFrameworkReference(this TargetFramework targetFramework, TargetFramework referenceFramework)
        {
            if (targetFramework == referenceFramework)
            {
                return true;
            }

            if (targetFramework.FrameworkType == referenceFramework.FrameworkType)
            {
                return targetFramework.Version >= referenceFramework.Version && (referenceFramework.TargetPlatformIdentifier == TargetPlatformIdentifier.PlatformAgnostic || targetFramework.TargetPlatformIdentifier == referenceFramework.TargetPlatformIdentifier);
            }

            if (referenceFramework.FrameworkType == FrameworkType.NetStandard)
            {
                if (referenceFramework.Version > Version.Parse("2.0"))
                {
                    return targetFramework.FrameworkType == FrameworkType.NetCore && targetFramework.Version >= Version.Parse("3.0");
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public enum TargetPlatformIdentifier
    {
        PlatformAgnostic,
        Windows,
        Linux,
        Unknown
    }

    public class TargetFramework : IComparable<TargetFramework>, IEquatable<TargetFramework>
    {
        // This is quite complicated due to changes to merge runtime identifier into target framework. At the moment we will assume default until required.
        private readonly Regex Net5Regex = new Regex("net(?<version>\\d+\\.?\\d*)(-(?<platform>\\D+))?(?<platformversion>\\d+.\\d+)?");
        private readonly string NetFrameworkPrefix = "net4";
        private readonly string NetStandardPrefix = "netstandard";
        private readonly string NetCorePrefix = "netcoreapp";

        public string Framework { get; }
        public Version Version { get; }
        public FrameworkType FrameworkType { get; }
        public TargetPlatformIdentifier TargetPlatformIdentifier { get; } = TargetPlatformIdentifier.PlatformAgnostic;

        public string TargetFrameworkIndentifier
        {
            get
            {
                switch (FrameworkType)
                {
                    case FrameworkType.NetCore:
                        {
                            return ".NETCoreApp";
                        }
                    case FrameworkType.NetFramework:
                        {
                            return ".NETFramework";
                        }
                    case FrameworkType.NetStandard:
                        {
                            return ".NETStandard";
                        }
                    case FrameworkType.DotNet:
                        {
                            return ".NETCoreApp"; // Yes it is true!
                        }
                    default:
                        return null;
                }
            }
        }

        public int FrameworkId
        {
            get
            {
                return (int)FrameworkType // FrameworkType currently 0-3
                    + 10 * Version.Major // From 1 - 99
                    + 1000 * Version.Minor // From 0-9
                    + 10000 * (Version.Build == -1 ? 0 : Version.Build) // 0-9
                    + 100000 * (int)TargetPlatformIdentifier; // 0-3
                                                              // TargetPlatformVersion can be added to the end if required BUT doing so will require ProjectTargetId to be adjusted to avoid overflow
            }
        }

        public TargetFramework(string framework)
        {
            Framework = framework;

            if (framework.StartsWith(NetCorePrefix))
            {
                FrameworkType = FrameworkType.NetCore;
                Version = GetVersion(framework, NetCorePrefix);
            }
            else if (framework.StartsWith(NetStandardPrefix))
            {
                FrameworkType = FrameworkType.NetStandard;
                Version = GetVersion(framework, NetStandardPrefix);
            }
            else if (framework.StartsWith(NetFrameworkPrefix))
            {
                FrameworkType = FrameworkType.NetFramework;
                var versionSuffix = string.Join('.', framework.Substring(3).AsEnumerable());
                Version = Version.Parse(versionSuffix);
            }
            else
            {
                var match = Net5Regex.Match(framework);
                if (match.Success)
                {
                    FrameworkType = FrameworkType.NetCore;
                    string versionString = match.Groups["version"].Value;
                    if (versionString.Contains("."))
                    {
                        Version = Version.Parse(match.Groups["version"].Value);
                    }
                    else
                    {
                        Version = new Version(int.Parse(versionString), 0);
                    }
                    if (match.Groups["platform"].Success)
                    {
                        if (Enum.TryParse(match.Groups["platform"].Value, true, out TargetPlatformIdentifier targetPlatformIdentifier))
                        {
                            TargetPlatformIdentifier = targetPlatformIdentifier;
                        }
                        else
                        {
                            TargetPlatformIdentifier = TargetPlatformIdentifier.Unknown;
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unsupported framework type {framework}");
                }
            }
        }

        private Version GetVersion(string framework, string prefix)
        {
            return Version.Parse(framework.Substring(prefix.Length));
        }

        public override int GetHashCode()
        {
            return Framework.GetHashCode();
        }

        public static bool operator ==(TargetFramework lhs, TargetFramework rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            if (rhs is null)
            {
                return false;
            }

            return lhs.Framework == rhs.Framework;
        }

        public static bool operator !=(TargetFramework lhs, TargetFramework rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj is string str)
            {
                return Framework == str;
            }

            if (obj is TargetFramework other)
            {
                return Framework == other.Framework;
            }

            return false;
        }

        public override string ToString()
        {
            return Framework;
        }

        public int CompareTo(TargetFramework other)
        {
            if (TargetFrameworkIndentifier != other.TargetFrameworkIndentifier)
            {
                return TargetFrameworkIndentifier.CompareTo(other.TargetFrameworkIndentifier);
            }

            int versionComparision = Version.CompareTo(other.Version);
            if (versionComparision != 0)
            {
                return versionComparision;
            }

            return TargetPlatformIdentifier.CompareTo(other.TargetPlatformIdentifier);
        }

        public bool Equals(TargetFramework other)
        {
            return Framework == other.Framework;
        }
    }
}
