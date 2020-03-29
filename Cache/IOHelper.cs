using System.Collections.Generic;
using System.IO;

namespace Cache
{
    static class IOHelper
    {
        public static IEnumerable<string> Files(string dir)
        {
            if (File.Exists(dir))
            {
                yield return dir;
            }
            else
            {
                List<string> ret = new List<string>();
                foreach (var d in Directory.GetDirectories(dir))
                {
                    foreach (var f in Files(d))
                    {
                        yield return f;
                    }
                }
                foreach (var f in Directory.GetFiles(dir))
                {
                    yield return f;
                }
            }
        }
    }
}
