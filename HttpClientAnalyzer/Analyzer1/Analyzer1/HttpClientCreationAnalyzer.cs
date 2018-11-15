using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class HttpClientCreationAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Best Practises";
        private const string HttpClient = "System.Net.Http.HttpClient";
        public const string EnforceSingletonHttpClientInstanceRule = "EnforceSingletonHttpClientInstance";
        public const string BlockHttpClientInstantiationRule = "BlockHttpClientInstantiation";
        private static DiagnosticDescriptor EnforceSingletonHttpClientInstanceDiagnostic = new DiagnosticDescriptor(EnforceSingletonHttpClientInstanceRule, "Non-static HttpClient instances are not allowed", "☹ To avoid socket exhaustion, DO NOT use {0}", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Naughty HttpClient creation");
        private static DiagnosticDescriptor BlockHttpClientInstantiationDiagnostic = new DiagnosticDescriptor(BlockHttpClientInstantiationRule, "Instantiation of HttpClient instances are not allowed", "☹ To avoid socket exhaustion, DO NOT use {0}", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Naughty HttpClient creation");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    EnforceSingletonHttpClientInstanceDiagnostic,
                    BlockHttpClientInstantiationDiagnostic
                    );
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            //context.RegisterSyntaxTreeAction(AnalyzeSymbol);
            context.RegisterOperationAction(this.AnalyzeObjectCreationOperation, OperationKind.ObjectCreation, OperationKind.Invocation);
      //      context.RegisterOperationAction(this.AnalyzeVariableDeclarationOperation, OperationKind.ObjectCreation);
        }

        private void AnalyzeObjectCreationOperation(OperationAnalysisContext context)
        {
         // var operation = (IObjectCreationOperation) context.Operation;
            if (context.Operation.Parent is IFieldInitializerOperation)
            {
                AnalyzeFieldInitializerOperation(context);
            }
            else if (context.Operation is IObjectCreationOperation)
            {
                AnalyzeVariableDeclarationOperation(context);
            }
            else if (context.Operation is IInvocationOperation)
            {
                AnalyzeInvocationOperation(context);
            }
       
        }

        private static void AnalyzeVariableDeclarationOperation(OperationAnalysisContext context)
        {
            var operation = (IVariableInitializerOperation) context.Operation.Parent;
            
            if (operation.Value.Type.ToString().Equals(HttpClient))
            {
                var diagnostic = Diagnostic.Create(BlockHttpClientInstantiationDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
           
        }
        private static void AnalyzeInvocationOperation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation) context.Operation;
            if (operation.TargetMethod.ContainingType.ToString().Equals("System.Activator") && operation.TargetMethod.Name.Equals("CreateInstance") && operation.TargetMethod.ReturnType.ToString().Equals(HttpClient))
            {
                var diagnostic = Diagnostic.Create(BlockHttpClientInstantiationDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
          

        }
        private static void AnalyzeFieldInitializerOperation(OperationAnalysisContext context)
        {
            var operation = (IFieldInitializerOperation) context.Operation.Parent;
            var fields = operation.InitializedFields;
            if (!fields.Any(f => f.IsStatic) && fields.Any(f => f.Type.ToString().Equals(HttpClient)))
            {
                var diagnostic = Diagnostic.Create(EnforceSingletonHttpClientInstanceDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
        }

    }
}
