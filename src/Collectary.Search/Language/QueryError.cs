namespace Collectary.Search;

public enum QueryErrorCode
{
    UnterminatedString,
    UnexpectedCharacter,
    UnexpectedToken,
    ExpectedField,
    ExpectedOperator,
    ExpectedValue,
    ExpectedClosingParen,
    ExpectedOrderField,
    UnknownField,
    FieldNotSearchable,
    OperatorNotSupported,
    InvalidValue,
}

public sealed record QueryError(QueryErrorCode Code, int Start, int Length, string Detail = "");
