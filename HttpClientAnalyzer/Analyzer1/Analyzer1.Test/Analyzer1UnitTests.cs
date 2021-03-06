﻿using Microsoft.CodeAnalysis;
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
        public void TestArgumentToInvocation()
        {
            var test = @"using System.Net.Http;

namespace ConsoleApp1
{
    class Program
    {
        public static void DoSomethingWithHttpClient(HttpClient client)
        {
           
        }

        static void Main(string[] args)
        {
            DoSomethingWithHttpClient(new HttpClient());
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "BlockHttpClientInstantiation",
                Message = "☹ To avoid socket exhaustion, DO NOT use new HttpClient()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 14, 39)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixedSource = @"using System.Net.Http;

namespace ConsoleApp1
{
    class Program
    {
        private System.Net.Http.IHttpClientFactory httpClientFactory;

        public static void DoSomethingWithHttpClient(HttpClient client)
        {
           
        }

        static void Main(string[] args)
        {
            DoSomethingWithHttpClient(httpClientFactory.CreateClient());
        }
    }
}";
            VerifyCSharpFix(test, fixedSource, allowNewCompilerDiagnostics: true);
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
                Message = "☹ To avoid socket exhaustion, DO NOT use new HttpClient()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 9, 40)
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
        public void TestInheritedWithVariableDeclaration()
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
                Message = "☹ To avoid socket exhaustion, DO NOT use new Foo()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 7, 29)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestInheritedWithConversion()
        {
            var test = @"using System.Net.Http;

class Program
{
    static void Main(string[] args)
    {
        using (HttpClient client = new Foo())
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
                Message = "☹ To avoid socket exhaustion, DO NOT use new Foo()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 7, 36)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void TestInheritedWithInvocation()
        {
            var test = @"using System;
using System.Net.Http;

class Program
{
    static void Main(string[] args)
    {
        using (var client = Activator.CreateInstance<Foo>())
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
                Message = "☹ To avoid socket exhaustion, DO NOT use Activator.CreateInstance<Foo>()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 8, 29)
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

    [TestClass]
    public class OtherTest : CodeFixVerifier
    {
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
            var fixedSource = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        private static HttpClient randomClient = new HttpClient();
    }
}";
            VerifyCSharpFix(test, fixedSource);
        }

        [TestMethod]
        public void TestInheritedWithFieldInitialization()
        {
            var test = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        private Foo randomClient = new Foo();
    }
    class Foo : HttpClient
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "EnforceSingletonHttpClientInstance",
                Message = "☹ To avoid socket exhaustion, DO NOT use = new Foo()",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 7, 34)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixedSource = @"using System.Net.Http;

namespace Analyzer1
{
    class Program
    {
        private static Foo randomClient = new Foo();
    }

    class Foo : HttpClient
    {
    }
}";
            VerifyCSharpFix(test, fixedSource);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CodeFix2();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new HttpClientCreationAnalyzer();
        }
    }
}
