using Xunit;
using XafApiConverter.Converter;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

namespace XafApiConverterTests {
    public class GenericTypeDetectionTests {
        [Fact]
        public void TestGenericTypeArgumentDetection_KpiDefinition() {
            // Arrange
            string code = @"
using System;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Kpi;

namespace Test {
    public class TestClass {
        public void TestMethod(IObjectSpace ObjectSpace) {
            KpiDefinition obj1 = ObjectSpace.FindObject<KpiDefinition>(CriteriaOperator.Parse(""Name='Sales'""));
            
            if(obj1 == null) {
                obj1 = ObjectSpace.CreateObject<KpiDefinition>();
            }
        }
    }
}";

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            // Act
            var collector = new XafApiConverter.SyntaxConverters.IdentifierNameSyntaxCollector();
            collector.Visit(classDecl);

            // Assert
            var kpiDefinitionUsages = collector.Identifiers
                .Where(id => id.Identifier.Text == "KpiDefinition")
                .ToList();

            // Should find at least 3 usages:
            // 1. KpiDefinition obj1 = ...
            // 2. FindObject<KpiDefinition>(...)
            // 3. CreateObject<KpiDefinition>()
            Assert.True(kpiDefinitionUsages.Count >= 3, 
                $"Expected at least 3 KpiDefinition usages, but found {kpiDefinitionUsages.Count}");
        }

        [Fact]
        public void TestGenericTypeArgumentDetection_DateRangeRepository() {
            // Arrange
            string code = @"
using DevExpress.ExpressApp.Kpi;

namespace Test {
    public class TestClass {
        public void TestMethod() {
            var range = DateRangeRepository.FindRange(""Rolling 1996"");
        }
    }
}";

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            // Act
            var collector = new XafApiConverter.SyntaxConverters.IdentifierNameSyntaxCollector();
            collector.Visit(classDecl);

            // Assert
            var dateRangeRepoUsages = collector.Identifiers
                .Where(id => id.Identifier.Text == "DateRangeRepository")
                .ToList();

            // Should find at least 1 usage: DateRangeRepository.FindRange(...)
            Assert.True(dateRangeRepoUsages.Count >= 1, 
                $"Expected at least 1 DateRangeRepository usage, but found {dateRangeRepoUsages.Count}");
        }
        
        [Fact]
        public void TestKpiDefinitionDetectionInProblemDetector() {
            // Arrange
            string code = @"
using System;
using DevExpress.ExpressApp;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.Kpi;

namespace Test {
    public class TestClass {
        public void TestMethod(IObjectSpace ObjectSpace) {
            KpiDefinition obj1 = ObjectSpace.FindObject<KpiDefinition>(CriteriaOperator.Parse(""Name='Sales'""));
            
            if(obj1 == null) {
                obj1 = ObjectSpace.CreateObject<KpiDefinition>();
                obj1.Range = DateRangeRepository.FindRange(""Rolling 1996"");
                obj1.MeasurementFrequency = TimeIntervalType.Month;
            }
        }
    }
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            
            // Extract using directives
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            // Act
            var problems = ProblemDetector.AnalyzeSingleClass(classDecl, null, usingDirectives);

            // Assert
            var kpiProblems = problems.Where(p => p.TypeName == "KpiDefinition").ToList();
            var dateRangeProblems = problems.Where(p => p.TypeName == "DateRangeRepository").ToList();
            var timeIntervalProblems = problems.Where(p => p.TypeName == "TimeIntervalType").ToList();

            Assert.True(kpiProblems.Count >= 1, 
                $"Expected KpiDefinition to be detected, but found {kpiProblems.Count} problems");
            Assert.True(dateRangeProblems.Count >= 1, 
                $"Expected DateRangeRepository to be detected, but found {dateRangeProblems.Count} problems");
            Assert.True(timeIntervalProblems.Count >= 1, 
                $"Expected TimeIntervalType to be detected, but found {timeIntervalProblems.Count} problems");
        }
    }
}
