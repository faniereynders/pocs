using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace Analyzer1.Test
{
    [TestClass]
    public class LazyLoadingAnalyzerTest : CodeFixVerifier
    {
        [TestMethod]
        public void TestLazyProperty()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LazyLoadingPropertyAnalyzer.Test
{
    class WithVirtualInt
    {
        public virtual int A { get; set; }
    }

    class TestProgram
    {
        private static Task<int> GetSingleAsync(Expression<Func<int, bool>> whereClause)
        {
            return Task.FromResult(new List<int>());
        }

        static async Task TestMain()
        {
            WithVirtualInt obj = new WithVirtualInt{A = 5};
            int a = await GetSingleAsync(i => i == obj.A);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "LazyEvaluationInExpression",
                Message = "Property \"A\" might be loaded asynchronously when accessed during asynchronous evaluation in method \"GetSingleAsync\".",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 24, 52)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LazyLoadingPropertyAnalyzer();
        }
    }
}
