using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace XafApiConverterTests {
    static class ProjectCompareHelper {
        static readonly HashSet<string> ignoreDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "bin", "obj"
        };
        static readonly HashSet<string> doNotCompareFilesWithExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".sln", ".md", ".user"
        };

        public static string CreateProjectCopy(string projectDirectory) {
            var tempDir = Directory.CreateTempSubdirectory().FullName;
            CopyDirectoryRecursive(projectDirectory, tempDir);
            return tempDir;
        }

        public static string FindSolutionDirectory(string projectDirectoryName) {
            string dir = Path.GetDirectoryName(typeof(ProjectCompareHelper).Assembly.Location)!;
            while (true) {
                string projectDir = Path.Combine(dir, projectDirectoryName);
                if (Directory.Exists(projectDir)) {
                    return projectDir;
                }
                var parentDir = Directory.GetParent(dir);
                if (parentDir == null)
                    throw new DirectoryNotFoundException(projectDirectoryName);
                dir = parentDir.FullName;
            }
        }

        public static void CompareProjectFiles(string etalonProjectDirectory, string convertedProjectDirectory) {
            List<string> etalonFiles = GetFilesRecursive(etalonProjectDirectory);
            List<string> convertedFiles = GetFilesRecursive(convertedProjectDirectory);
            if (etalonFiles.Count != convertedFiles.Count) {
                Assert.Fail($"Files count: expected {etalonFiles.Count} files, after conversion {convertedFiles.Count} files.");
            }
            List<string> differences = new List<string>();

            for (int i = 0; i < etalonFiles.Count; i++) {
                string fileEtalon = Path.GetFileName(etalonFiles[i]);
                string? fileConverted = convertedFiles.FirstOrDefault(t => t.EndsWith("\\" + fileEtalon));
                if (fileConverted == null) {
                    if (Path.GetExtension(fileEtalon) == ".csproj") {
                        fileConverted = convertedFiles.FirstOrDefault(t => t.EndsWith("\\" + fileEtalon.Replace(".Etalon.", ".")));
                    }
                }
                if (fileConverted == null) {
                    Assert.Fail($"File {fileEtalon} does not exist after conversion.");
                }
                string? compareResult = FileCompareHelper.CompareFiles(etalonFiles[i], convertedFiles[i]);
                if(!string.IsNullOrEmpty(compareResult)) {
                    differences.Add(compareResult);
                }
            }

            if(differences.Count > 0) {
                Assert.Fail(string.Join("\r\n", differences));
            }
        }

        static void CopyDirectoryRecursive(string sourceDir, string targetDir) {
            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly);
            foreach (string sourceFile in files) {
                string name = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetDir, name);
                File.Copy(sourceFile, targetFile, false);
            }
            var subdirs = Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly);
            foreach (string sourceSubDir in subdirs) {
                string name = Path.GetFileName(sourceSubDir);
                if (!IsIgnoredDirectory(sourceSubDir)) {
                    string targetSubDir = Path.Combine(targetDir, name);
                    CopyDirectoryRecursive(sourceSubDir, targetSubDir);
                }
            }
        }

        static List<string> GetFilesRecursive(string sourceDir) {
            var files = Directory.GetFiles(sourceDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(t => !doNotCompareFilesWithExtensions.Contains(Path.GetExtension(t)));
            var result = new List<string>(files);
            var subdirs = Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly);
            foreach (string sourceSubDir in subdirs) {
                if (!IsIgnoredDirectory(sourceSubDir)) {
                    var subdirFiles = GetFilesRecursive(sourceSubDir);
                    result.AddRange(subdirFiles);
                }
            }
            return result;
        }

        static bool IsIgnoredDirectory(string path) {
            string name = Path.GetFileName(path);
            return (name.StartsWith(".") || ignoreDirectories.Contains(name));
        }
    }

    static class FileCompareHelper {
        public static string? CompareFiles(string etalonPath, string targetPath) {
            string[] etalonLines = GetLines(etalonPath);
            string[] targetLines = GetLines(targetPath);

            for (int i = 0; i < Math.Max(etalonLines.Length, targetLines.Length); i++) {
                string etalonLine = i < etalonLines.Length ? etalonLines[i] : "";
                string targetLine = i < targetLines.Length ? targetLines[i] : "";
                if(etalonLine != targetLine) {
                    string etalonFileContent = File.ReadAllText(etalonPath);
                    string targetFileContent = File.ReadAllText(targetPath);
                    // File.WriteAllText(etalonPath, targetFileContent); // Updates etalon: for debug and maitenance
                    //Assert.Fail($"File {targetPath} does not math etalon file {etalonPath} at line {i + 1}.\r\nExpected: \"{etalonLine}\"\r\nActual:   \"{targetLine}\"");
                    return $"File {targetPath} does not math etalon file {etalonPath} at line {i + 1}.\r\nExpected: \"{etalonLine}\"\r\nActual:   \"{targetLine}\"";
                }
            }
            return null;
        }

        static string[] GetLines(string filePath) {
            string etalon = File.ReadAllText(filePath);
            string[] etalonLines = etalon.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>(etalonLines.Length);
            foreach (string line in etalonLines) {
                var trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine)) {
                    result.Add(trimmedLine);
                }
            }
            return result.ToArray();
        }
    }
}
