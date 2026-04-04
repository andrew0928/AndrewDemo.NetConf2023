using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public sealed class NoDirectCurrentTimeUsageTests
    {
        private static readonly Regex ForbiddenPattern = new(
            @"\bDateTime\.(Now|UtcNow|Today)\b|\bDateTimeOffset\.Now\b",
            RegexOptions.Compiled);

        [Fact]
        public void ProductionSource_ShouldNotUseDirectCurrentTimeApis()
        {
            var repoRoot = ResolveRepositoryRoot();
            var srcRoot = Path.Combine(repoRoot.FullName, "src");
            var offenders = new List<string>();

            foreach (var file in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = File.ReadAllText(file);
                if (ForbiddenPattern.IsMatch(content))
                {
                    offenders.Add(Path.GetRelativePath(repoRoot.FullName, file));
                }
            }

            Assert.True(
                offenders.Count == 0,
                $"Direct current-time APIs are forbidden in src. Offenders:{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
        }

        private static DirectoryInfo ResolveRepositoryRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "src", "AndrewDemo.NetConf2023.slnx")))
                {
                    return current;
                }

                current = current.Parent;
            }

            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
