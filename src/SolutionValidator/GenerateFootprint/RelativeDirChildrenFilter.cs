using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolutionValidator.GenerateFootprint
{
    public static class RelativeDirChildrenFilter
    {
        internal static IEnumerable<string> FilterChildrenAndAddDirSeparator(IEnumerable<string> directories)
        {
            var dirs = directories
                .Select(d => Path.GetFullPath($"{d}"))
                .ToArray();

            for (var i = 0; i < dirs.Length; i++)
            {
                if (IsChildOfAny(i, dirs))
                {
                    dirs[i] = null;
                }
            }

            return dirs
                .Where(d => d != null)
                // UNIX dir separator and added dir separator at the end
                .Select(p => p.Replace('\\', '/') + "/");
        }

        internal static bool IsChildOfAny(int index, string[] dirs)
        {
            var suspectedChild = dirs[index];
            if (suspectedChild == null)
            {
                return false;
            }

            for (var i = 0; i < dirs.Length; i++)
            {
                var dir = dirs[i];
                if (i == index || dir == null)
                {
                    continue;
                }

                if (suspectedChild.StartsWith(dir))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
