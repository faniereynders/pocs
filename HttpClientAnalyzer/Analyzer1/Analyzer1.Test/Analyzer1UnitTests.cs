using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Analyzer1;

namespace Analyzer1.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestFieldInitialization()
        {
            var test = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        private HttpClient randomClient = new HttpClient();
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "EnforceSingletonHttpClientInstance",
                Message = "☹ To avoid socket exhaustion, DO NOT use = new HttpClient()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 7, 41)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestVariableDeclaration()
        {
            var test = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        void Method()
        {
            using (HttpClient client = new HttpClient())
            {
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "BlockHttpClientInstantiation",
                Message = "☹ To avoid socket exhaustion, DO NOT use = new HttpClient()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 9, 38)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixedSource = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        private System.Net.Http.IHttpClientFactory httpClientFactory;

        void Method()
        {
            using (HttpClient client = httpClientFactory.CreateClient())
            {
            }
        }
    }
}";
            VerifyCSharpFix(test, fixedSource, allowNewCompilerDiagnostics:true);
        }

        [TestMethod]
        public void TestInvocation()
        {
            var test = @"using System;
using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        void Method()
        {
            using (HttpClient client = Activator.CreateInstance<HttpClient>())
            {
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "BlockHttpClientInstantiation",
                Message = "☹ To avoid socket exhaustion, DO NOT use Activator.CreateInstance<HttpClient>()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 10, 40)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestInherited()
        {
            var test = @"using System.Net.Http;

class Program
{
    static void Main(string[] args)
    {
        using (var client = new Foo())
        {
        }
    }

    public class Foo : HttpClient
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "BlockInheritedHttpClientInstantiation",
                Message = "☹ To avoid socket exhaustion, DO NOT use = new Foo()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 7, 27)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new HttpClientFactoryCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new HttpClientCreationAnalyzer();
        }
    }
}
