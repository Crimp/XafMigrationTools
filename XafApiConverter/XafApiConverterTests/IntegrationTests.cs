using Microsoft.Build.Locator;
using System;
using System.Collections.Generic;
using System.Text;
using XafApiConverter.Converter;
using Xunit;

namespace XafApiConverterTests {
    
    public class IntegrationTests {
        [Fact]
        [Trait("Category", "Integration")]
        public void FullPipeline_Conversion_And_TypeMigration() {
            string projectToConvert = ProjectCompareHelper.FindSolutionDirectory("TestProject");
            string projectEtalon = ProjectCompareHelper.FindSolutionDirectory("TestProject.Etalon");
            string projectAfterConversion = ProjectCompareHelper.CreateProjectCopy(projectToConvert);
            try {
                MSBuildLocator.RegisterDefaults();
                RunFullPipeline(projectAfterConversion);
                ProjectCompareHelper.CompareProjectFiles(projectEtalon, projectAfterConversion);
                RunFullPipeline(projectAfterConversion);
                ProjectCompareHelper.CompareProjectFiles(projectEtalon, projectAfterConversion);
            }
            finally {
                Directory.Delete(projectAfterConversion, true);
            }
        }

        static void RunFullPipeline(string projectDir) {
            string solutionPath = Directory.GetFiles(projectDir, "*.sln", SearchOption.TopDirectoryOnly).First();
            UnifiedMigrationCli.Run(new string[] { "--solution", solutionPath, "security-update", "migrate-types", "project-conversion" });
        }
    }
}
