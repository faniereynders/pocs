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
        public const string BlockInheritedHttpClientInstantiationRule = "BlockInheritedHttpClientInstantiation";
        private static DiagnosticDescriptor EnforceSingletonHttpClientInstanceDiagnostic = new DiagnosticDescriptor(EnforceSingletonHttpClientInstanceRule, "Non-static HttpClient instances are not allowed", "☹ To avoid socket exhaustion, DO NOT use {0}", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Naughty HttpClient creation");
        private static DiagnosticDescriptor BlockHttpClientInstantiationDiagnostic = new DiagnosticDescriptor(BlockHttpClientInstantiationRule, "Instantiation of HttpClient instances are not allowed", "☹ To avoid socket exhaustion, DO NOT use {0}", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Naughty HttpClient creation");
        private static DiagnosticDescriptor BlockInheritedHttpClientInstantiationDiagnostic = new DiagnosticDescriptor(BlockInheritedHttpClientInstantiationRule, "Instantiation of HttpClient instances are not allowed", "☹ To avoid socket exhaustion, DO NOT use {0}", Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: "Naughty HttpClient creation");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    EnforceSingletonHttpClientInstanceDiagnostic,
                    BlockHttpClientInstantiationDiagnostic,
                    BlockInheritedHttpClientInstantiationDiagnostic
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

        private IVariableInitializerOperation FindVariableInitializer(IOperation operation)
        {
            IVariableInitializerOperation result = operation as IVariableInitializerOperation;
            if (result != null)
            {
                return result;
            }
            return FindVariableInitializer(operation.Parent);
        }

        private void AnalyzeVariableDeclarationOperation(OperationAnalysisContext context)
        {
            var operation = FindVariableInitializer(context.Operation);
            
            CheckInstantiation(context.Operation.Type, operation, context);
        }

        private static bool IsHttpClientItself(ITypeSymbol type)
        {
            return type.ToString().Equals(HttpClient);
        }

        private void CheckInstantiation(ITypeSymbol type, IOperation operation, OperationAnalysisContext context)
        {
            if (IsHttpClientItself(type))
            {
                var diagnostic = Diagnostic.Create(BlockHttpClientInstantiationDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
            else if (IsInheritedHttpClient(type))
            {
                var diagnostic = Diagnostic.Create(BlockInheritedHttpClientInstantiationDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
        }

        private bool IsInheritedHttpClient(ITypeSymbol type)
        {
            ITypeSymbol originalDefinition = type.OriginalDefinition;
            if (originalDefinition.BaseType == null)
            {
                return false;
            }
            return IsHttpClientItself(originalDefinition.BaseType) ||
                   IsInheritedHttpClient(originalDefinition.BaseType);
        }

        private void AnalyzeInvocationOperation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation) context.Operation;
            if (!operation.TargetMethod.ContainingType.ToString().Equals("System.Activator") || !operation.TargetMethod.Name.Equals("CreateInstance"))
            {
                return;
            }
            CheckInstantiation(operation.TargetMethod.ReturnType, operation, context);
        }
        private void AnalyzeFieldInitializerOperation(OperationAnalysisContext context)
        {
            var operation = (IFieldInitializerOperation) context.Operation.Parent;
            var fields = operation.InitializedFields;
            if (!fields.Any(f => f.IsStatic) && fields.Any(f => IsHttpClientItself(f.Type) || IsInheritedHttpClient(f.Type)))
            {
                var diagnostic = Diagnostic.Create(EnforceSingletonHttpClientInstanceDiagnostic, operation.Syntax.GetLocation(), operation.Syntax.GetText());
                context.ReportDiagnostic(diagnostic);
            }
        }

    }
}
