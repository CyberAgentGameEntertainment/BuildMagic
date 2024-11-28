// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;

namespace BuildMagic.BuiltinTaskGenerator;

public abstract class DefineConstraint
{
    private static readonly Type[] Precedence =
    {
        typeof(DefineConstraintOr),
        typeof(DefineConstraintAnd),
        typeof(DefineConstraintNot),
        typeof(DefineConstraintSymbol)
    };

    // DefineConstraintのToString()を行う際に、意図した評価順にするために演算子の優先度を考慮して括弧をつける必要があり、その判定に使用する

    private int? _priority;
    public int Priority => _priority ??= Array.IndexOf(Precedence, GetType());

    public static DefineConstraint operator !(DefineConstraint a)
    {
        return new DefineConstraintNot(a);
    }

    public static DefineConstraint operator &(DefineConstraint lhs, DefineConstraint rhs)
    {
        return new DefineConstraintAnd(lhs, rhs);
    }

    public static DefineConstraint operator |(DefineConstraint lhs, DefineConstraint rhs)
    {
        return new DefineConstraintOr(lhs, rhs);
    }

    public static implicit operator DefineConstraint(string symbol)
    {
        return new DefineConstraintSymbol(symbol);
    }
}

internal static class DefineConstraintExtensions
{
    public static DefineConstraint ToDefine(this string symbol)
    {
        return new DefineConstraintSymbol(symbol);
    }
}

public class DefineConstraintSymbol : DefineConstraint
{
    public DefineConstraintSymbol(string symbol)
    {
        Symbol = symbol;
    }

    private string Symbol { get; }

    public override string ToString()
    {
        return Symbol;
    }
}

public class DefineConstraintNot : DefineConstraint
{
    public DefineConstraintNot(DefineConstraint operand)
    {
        Operand = operand;
    }

    private DefineConstraint Operand { get; }

    public override string ToString()
    {
        return Priority > Operand.Priority ? $"!({Operand})" : $"!{Operand}";
    }
}

public class DefineConstraintAnd : DefineConstraint
{
    public DefineConstraintAnd(DefineConstraint lhs, DefineConstraint rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    private DefineConstraint Lhs { get; }
    private DefineConstraint Rhs { get; }

    public override string ToString()
    {
        var l = Priority > Lhs.Priority ? $"({Lhs})" : $"{Lhs}";
        var r = Priority > Rhs.Priority ? $"({Rhs})" : $"{Rhs}";
        return $"{l} && {r}";
    }
}

public class DefineConstraintOr : DefineConstraint
{
    public DefineConstraintOr(DefineConstraint lhs, DefineConstraint rhs)
    {
        Lhs = lhs;
        Rhs = rhs;
    }

    private DefineConstraint Lhs { get; }
    private DefineConstraint Rhs { get; }

    public override string ToString()
    {
        var l = Priority > Lhs.Priority ? $"({Lhs})" : $"{Lhs}";
        var r = Priority > Rhs.Priority ? $"({Rhs})" : $"{Rhs}";
        return $"{l} || {r}";
    }
}
