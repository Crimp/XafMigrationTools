using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XafApiConverter.SyntaxConverters {
    class IdentifierNameSyntaxCollector : CSharpSyntaxWalker {
        public readonly HashSet<IdentifierNameSyntax> Identifiers = new HashSet<IdentifierNameSyntax>();
        
        public override void VisitIdentifierName(IdentifierNameSyntax node) {
            base.VisitIdentifierName(node);
            Identifiers.Add(node);
        }
        
        /// <summary>
        /// Visit generic names to collect type arguments.
        /// This allows detection of types used as generic arguments like:
        /// - ObjectSpace.FindObject<KpiDefinition>(...)
        /// - ObjectSpace.CreateObject<KpiDefinition>()
        /// Without this, generic type arguments are not collected.
        /// </summary>
        public override void VisitGenericName(GenericNameSyntax node) {
            base.VisitGenericName(node);
            
            // Collect type arguments from generic names
            // Example: FindObject<KpiDefinition> → collect "KpiDefinition"
            if (node.TypeArgumentList != null) {
                foreach (var typeArg in node.TypeArgumentList.Arguments) {
                    // Extract identifier names from type arguments
                    // Handles both simple names (T) and qualified names (Namespace.T)
                    CollectTypeArgumentIdentifiers(typeArg);
                }
            }
        }
        
        /// <summary>
        /// Recursively collect identifier names from type syntax
        /// </summary>
        private void CollectTypeArgumentIdentifiers(TypeSyntax typeSyntax) {
            switch (typeSyntax) {
                case IdentifierNameSyntax identifierName:
                    // Simple type: <KpiDefinition>
                    Identifiers.Add(identifierName);
                    break;
                    
                case QualifiedNameSyntax qualifiedName:
                    // Qualified type: <DevExpress.ExpressApp.Kpi.KpiDefinition>
                    // Collect the rightmost identifier
                    if (qualifiedName.Right is IdentifierNameSyntax rightIdentifier) {
                        Identifiers.Add(rightIdentifier);
                    }
                    break;
                    
                case GenericNameSyntax genericName:
                    // Nested generic: <List<KpiDefinition>>
                    if (genericName.TypeArgumentList != null) {
                        foreach (var nestedArg in genericName.TypeArgumentList.Arguments) {
                            CollectTypeArgumentIdentifiers(nestedArg);
                        }
                    }
                    break;
                    
                case ArrayTypeSyntax arrayType:
                    // Array type: <KpiDefinition[]>
                    CollectTypeArgumentIdentifiers(arrayType.ElementType);
                    break;
                    
                case NullableTypeSyntax nullableType:
                    // Nullable type: <KpiDefinition?>
                    CollectTypeArgumentIdentifiers(nullableType.ElementType);
                    break;
            }
        }
    }
}
