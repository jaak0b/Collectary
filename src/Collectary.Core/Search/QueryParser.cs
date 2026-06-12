namespace Collectary.Core.Search;

public class QueryParser
{
    private readonly QueryLexer _lexer;

    public QueryParser(QueryLexer lexer) => _lexer = lexer;

    public QueryParseResult Parse(string text)
    {
        var lexed = _lexer.Tokenize(text);
        if (lexed.Errors.Count > 0)
            return new QueryParseResult(null, lexed.Errors);
        return new Run(lexed.Tokens).Parse();
    }

    private sealed class Run
    {
        private readonly IReadOnlyList<QueryToken> _tokens;
        private readonly List<QueryError> _errors = new();
        private int _index;

        public Run(IReadOnlyList<QueryToken> tokens) => _tokens = tokens;

        private QueryToken Current => _tokens[_index];

        private void Advance() => _index++;

        public QueryParseResult Parse()
        {
            QueryNode? root = null;
            if (Current.Kind != QueryTokenKind.End && !AtOrderBy())
            {
                root = ParseOr();
                if (root is null) return Fail();
            }

            var orderBy = new List<OrderByField>();
            if (AtOrderBy() && !ParseOrderBy(orderBy)) return Fail();

            if (Current.Kind != QueryTokenKind.End)
            {
                _errors.Add(new QueryError(QueryErrorCode.UnexpectedToken, Current.Start, Current.Length, Current.Text));
                return Fail();
            }

            return new QueryParseResult(new ParsedQuery { Root = root, OrderBy = orderBy }, _errors);
        }

        private QueryParseResult Fail() => new(null, _errors);

        private QueryNode? ParseOr()
        {
            var left = ParseAnd();
            while (left is not null && IsKeyword("or"))
            {
                Advance();
                var right = ParseAnd();
                if (right is null) return null;
                left = new OrNode(left, right);
            }
            return left;
        }

        private QueryNode? ParseAnd()
        {
            var left = ParseUnary();
            while (left is not null && IsKeyword("and"))
            {
                Advance();
                var right = ParseUnary();
                if (right is null) return null;
                left = new AndNode(left, right);
            }
            return left;
        }

        private QueryNode? ParseUnary()
        {
            if (IsKeyword("not"))
            {
                Advance();
                var operand = ParseUnary();
                return operand is null ? null : new NotNode(operand);
            }
            if (Current.Kind == QueryTokenKind.OpenParen)
            {
                Advance();
                var inner = ParseOr();
                if (inner is null) return null;
                if (Current.Kind != QueryTokenKind.CloseParen)
                {
                    _errors.Add(new QueryError(QueryErrorCode.ExpectedClosingParen, Current.Start, Current.Length));
                    return null;
                }
                Advance();
                return inner;
            }
            return ParseCondition();
        }

        private QueryNode? ParseCondition()
        {
            if (Current.Kind is not (QueryTokenKind.Word or QueryTokenKind.String))
            {
                _errors.Add(new QueryError(QueryErrorCode.ExpectedField, Current.Start, Current.Length, Current.Text));
                return null;
            }
            var field = Current;
            Advance();

            var symbolOperator = SymbolOperator(Current.Kind);
            if (symbolOperator is { } op)
            {
                Advance();
                var operand = ParseOperand();
                return operand is null ? null : NewCondition(field, op, [operand]);
            }
            if (IsKeyword("in"))
            {
                Advance();
                return ParseInList(field);
            }
            if (IsKeyword("not") && NextIsKeyword("in"))
            {
                Advance();
                Advance();
                var inCondition = ParseInList(field);
                return inCondition is null ? null : new NotNode(inCondition);
            }
            if (IsKeyword("is"))
            {
                Advance();
                return ParseIsEmpty(field);
            }

            _errors.Add(new QueryError(QueryErrorCode.ExpectedOperator, Current.Start, Current.Length, Current.Text));
            return null;
        }

        private QueryNode? ParseInList(QueryToken field)
        {
            if (Current.Kind != QueryTokenKind.OpenParen)
            {
                _errors.Add(new QueryError(QueryErrorCode.ExpectedValue, Current.Start, Current.Length, Current.Text));
                return null;
            }
            Advance();
            var operands = new List<QueryOperand>();
            while (true)
            {
                var operand = ParseOperand();
                if (operand is null) return null;
                operands.Add(operand);
                if (Current.Kind == QueryTokenKind.Comma)
                {
                    Advance();
                    continue;
                }
                break;
            }
            if (Current.Kind != QueryTokenKind.CloseParen)
            {
                _errors.Add(new QueryError(QueryErrorCode.ExpectedClosingParen, Current.Start, Current.Length));
                return null;
            }
            Advance();
            return NewCondition(field, QueryOperatorKind.In, operands);
        }

        private QueryNode? ParseIsEmpty(QueryToken field)
        {
            var negated = false;
            if (IsKeyword("not"))
            {
                negated = true;
                Advance();
            }
            if (!IsKeyword("empty"))
            {
                _errors.Add(new QueryError(QueryErrorCode.UnexpectedToken, Current.Start, Current.Length, Current.Text));
                return null;
            }
            Advance();
            return NewCondition(field, negated ? QueryOperatorKind.IsNotEmpty : QueryOperatorKind.IsEmpty, []);
        }

        private QueryOperand? ParseOperand()
        {
            if (Current.Kind is not (QueryTokenKind.Word or QueryTokenKind.String))
            {
                _errors.Add(new QueryError(QueryErrorCode.ExpectedValue, Current.Start, Current.Length, Current.Text));
                return null;
            }
            var operand = new QueryOperand(
                Current.Text, Current.Kind == QueryTokenKind.String, Current.Start, Current.Length);
            Advance();
            return operand;
        }

        private bool ParseOrderBy(List<OrderByField> orderBy)
        {
            Advance();
            Advance();
            while (true)
            {
                if (Current.Kind is not (QueryTokenKind.Word or QueryTokenKind.String))
                {
                    _errors.Add(new QueryError(QueryErrorCode.ExpectedOrderField, Current.Start, Current.Length, Current.Text));
                    return false;
                }
                var field = Current;
                Advance();
                var descending = false;
                if (IsKeyword("desc"))
                {
                    descending = true;
                    Advance();
                }
                else if (IsKeyword("asc"))
                {
                    Advance();
                }
                orderBy.Add(new OrderByField(field.Text, descending, field.Start, field.Length));
                if (Current.Kind == QueryTokenKind.Comma)
                {
                    Advance();
                    continue;
                }
                return true;
            }
        }

        private QueryNode NewCondition(QueryToken field, QueryOperatorKind op, IReadOnlyList<QueryOperand> operands) =>
            new ConditionNode
            {
                Field = field.Text,
                FieldStart = field.Start,
                FieldLength = field.Length,
                Operator = op,
                Operands = operands,
            };

        private QueryOperatorKind? SymbolOperator(QueryTokenKind kind) => kind switch
        {
            QueryTokenKind.Equals => QueryOperatorKind.Equals,
            QueryTokenKind.NotEquals => QueryOperatorKind.NotEquals,
            QueryTokenKind.Less => QueryOperatorKind.Less,
            QueryTokenKind.LessOrEqual => QueryOperatorKind.LessOrEqual,
            QueryTokenKind.Greater => QueryOperatorKind.Greater,
            QueryTokenKind.GreaterOrEqual => QueryOperatorKind.GreaterOrEqual,
            QueryTokenKind.Contains => QueryOperatorKind.Contains,
            QueryTokenKind.NotContains => QueryOperatorKind.NotContains,
            _ => null,
        };

        private bool IsKeyword(string keyword) =>
            Current.Kind == QueryTokenKind.Word
            && string.Equals(Current.Text, keyword, StringComparison.OrdinalIgnoreCase);

        private bool NextIsKeyword(string keyword) =>
            _index + 1 < _tokens.Count
            && _tokens[_index + 1].Kind == QueryTokenKind.Word
            && string.Equals(_tokens[_index + 1].Text, keyword, StringComparison.OrdinalIgnoreCase);

        private bool AtOrderBy() => IsKeyword("order") && NextIsKeyword("by");
    }
}
