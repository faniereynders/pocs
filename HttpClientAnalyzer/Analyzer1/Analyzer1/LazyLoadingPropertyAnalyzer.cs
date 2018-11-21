using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer1
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LazyLoadingPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LazyEvaluationInExpression";
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private const string Category = "Best practices";

        private static DiagnosticDescriptor LazyEvaluationInExpressionRule = new DiagnosticDescriptor(DiagnosticId, "Be careful about accessing virtual properties here", "Property \"{0}\" might be loaded asynchronously when accessed during asynchronous evaluation in method \"{1}\".", Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: "use of lazy-loaded property inside expression that is passed to asynchronous method");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(LazyEvaluationInExpressionRule); } }

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            IInvocationOperation operation = (IInvocationOperation)context.Operation;
            if (!ReturnsTask(operation.TargetMethod))
            {
                return;
            }
            foreach (IArgumentOperation argument in operation.Arguments)
            {
                AnalyzeArgument(argument, context, operation.TargetMethod.Name);
            }
        }

        private static void AnalyzeArgument(IArgumentOperation argument, OperationAnalysisContext context, string methodName)
        {
            if (!HasLambdaExpression(argument))
            {
                return;
            }
            FindAndAnalyzePropertyReference(argument, context, methodName);
        }

        private static void FindAndAnalyzePropertyReference(IOperation operation, OperationAnalysisContext context, string methodName)
        {
            if (operation.Kind == OperationKind.PropertyReference)
            {
                AnalyzePropertyReference((IPropertyReferenceOperation)operation, context, methodName);
            }
            else
            {
                foreach (IOperation childOperation in operation.Children)
                {
                    FindAndAnalyzePropertyReference(childOperation, context, methodName);
                }
            }
        }

        private static bool IsLocalReference(IOperation operation)
        {
            if (operation.Kind == OperationKind.LocalReference)
            {
                return true;
            }
            IPropertyReferenceOperation propertyReference = operation as IPropertyReferenceOperation;
            if (propertyReference == null)
            {
                return false;
            }
            return IsLocalReference(propertyReference.Instance);
        }

        private static IPropertyReferenceOperation FindVirtualPropertyReference(IPropertyReferenceOperation operation)
        {
            if (operation.Property.IsVirtual)
            {
                return operation;
            }
            IPropertyReferenceOperation instancePropertyReference = operation.Instance as IPropertyReferenceOperation;
            if (instancePropertyReference == null)
            {
                return null;
            }
            return FindVirtualPropertyReference(instancePropertyReference);
        }

        private static void AnalyzePropertyReference(IPropertyReferenceOperation operation, OperationAnalysisContext context, string methodName)
        {
            operation = FindVirtualPropertyReference(operation);
            if (operation == null)
            {
                return;
            }
            if (IsLocalReference(operation.Instance))
            {
                var diagnostic = Diagnostic.Create(LazyEvaluationInExpressionRule, operation.Syntax.GetLocation(), operation.Property.Name,
                    methodName);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool HasLambdaExpression(IArgumentOperation argument)
        {
            ArgumentSyntax syntax = argument.Syntax as ArgumentSyntax;
            if (syntax == null)
            {
                return false;
            }
            LambdaExpressionSyntax lambdaSyntax = syntax.Expression as LambdaExpressionSyntax;
            if (lambdaSyntax == null)
            {
                return false;
            }
            return true;
        }

        private static bool ReturnsTask(IMethodSymbol method)
        {
            string returnType = method.ReturnType.ToString();
            return returnType.Contains("System.Threading.Tasks.Task");
        }
    }
}
