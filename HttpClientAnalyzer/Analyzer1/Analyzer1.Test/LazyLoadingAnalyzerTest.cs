﻿using System;
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
        public void TestLazyPropertyFromOuterScope()
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

            var fixedSource = @"using System;
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
            int objA = obj.A;
            int a = await GetSingleAsync(i => i == objA);
        }
    }
}";
            VerifyCSharpFix(test, fixedSource, allowNewCompilerDiagnostics:true);
        }

        [TestMethod]
        public void TestLazyProperty()
        {
            var test = @"using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class WithVirtualInt
    {
        public virtual int A { get; set; }
    }
    class Program
    {
        private static Task<WithVirtualInt> GetSingleAsync(Expression<Func<WithVirtualInt, bool>> whereClause)
        {
            return Task.FromResult<WithVirtualInt>(null);
        }
        public async Task Method()
        {
            WithVirtualInt a = await GetSingleAsync(i => i.A == 5);
        }
    }
}";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TestLazyPropertyNested()
        {
            var test = @"using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class WithVirtualInt
    {
        public virtual int A { get; set; }
    }
    class WithOther
    {
        public WithVirtualInt B { get; set; }
    }
    class Program
    {
        private static Task<int> GetSingleAsync(Expression<Func<int, bool>> whereClause)
        {
            return null;
        }
        public async Task Method()
        {
            WithOther b = new WithOther();
            int a = await GetSingleAsync(i => 3 == b.B.A);
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

            var fixedSource = @"using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class WithVirtualInt
    {
        public virtual int A { get; set; }
    }
    class WithOther
    {
        public WithVirtualInt B { get; set; }
    }
    class Program
    {
        private static Task<int> GetSingleAsync(Expression<Func<int, bool>> whereClause)
        {
            return null;
        }
        public async Task Method()
        {
            WithOther b = new WithOther();
            int bBA = b.B.A;
            int a = await GetSingleAsync(i => 3 == bBA);
        }
    }
}";
            VerifyCSharpFix(test, fixedSource, allowNewCompilerDiagnostics:true);
        }

        [TestMethod]
        public void TestNotEndOfChain()
        {
            var test = @"using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class WithVirtual
    {
        public virtual WithOther A { get; set; }
    }
    class WithOther
    {
        public int B { get; set; }
    }
    class Program
    {
        private static Task<int> GetSingleAsync(Expression<Func<int, bool>> whereClause)
        {
            return Task.FromResult(0);
        }
        public async Task Method()
        {
            WithVirtual b = new WithVirtual { };
            int a = await GetSingleAsync(i => 5 == b.A.B);
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

            var fixedSource = @"using System;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace ConsoleApp1
{
    class WithVirtual
    {
        public virtual WithOther A { get; set; }
    }
    class WithOther
    {
        public int B { get; set; }
    }
    class Program
    {
        private static Task<int> GetSingleAsync(Expression<Func<int, bool>> whereClause)
        {
            return Task.FromResult(0);
        }
        public async Task Method()
        {
            WithVirtual b = new WithVirtual { };
            ConsoleApp1.WithOther bA = b.A;
            int a = await GetSingleAsync(i => 5 == bA.B);
        }
    }
}";
            VerifyCSharpFix(test, fixedSource, allowNewCompilerDiagnostics:true);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LazyLoadingPropertyAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new LazyLoadingPropertyCodeFixProvider();
        }
    }
}
