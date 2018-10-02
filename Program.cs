using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    enum Operation
    {
        Plus,
        Minus,
        Multiply,
        Div,
        Power,
        Concat,
    }

    abstract class Node
    {
        public Node Left;
        public Node Right;

        public abstract string GetValue();

        public abstract double Evaluate();

        public abstract int GetWidth();

        public abstract bool IsConcatenable();

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Left != null)
            {
                sb.Append("(");
                sb.Append(Left);
            }

            sb.Append(GetValue());

            if (Right != null)
            {
                sb.Append(Right);
                sb.Append(")");
            }

            return sb.ToString();
        }
    }

    class NumberNode : Node
    {
        public int Number;

        public override double Evaluate()
        {
            return Number;
        }

        public override int GetWidth()
        {
            return 1;
        }

        public override bool IsConcatenable()
        {
            return true;
        }

        public override string GetValue()
        {
            return Number.ToString();
        }
    }

    class OperationNode : Node
    {
        public Operation Operation;

        private double _cached = double.NaN;
        private bool _isCached;

        public override double Evaluate()
        {
            if (!_isCached)
            {
                _isCached = true;
                _cached = InnerEvaluate();
            }

            return _cached;
        }

        private double InnerEvaluate()
        {
            var leftValue = Left.Evaluate();
            var rightValue = Right.Evaluate();

            switch (Operation)
            {
                case Operation.Plus:
                    return leftValue + rightValue;
                case Operation.Minus:
                    return leftValue - rightValue;
                case Operation.Multiply:
                    return leftValue * rightValue;
                case Operation.Div:
                    return leftValue / rightValue;
                case Operation.Power:
                    return Math.Pow(leftValue, rightValue);
                case Operation.Concat:
                    // let | is concat operator
                    // 1|(2|3) = 1|(2*10^1 + 3) = 1|(23) = 1 * 10^2 + 23 = 123 
                    var log10 = Right.GetWidth();
                    return leftValue * Math.Pow(10, log10) + rightValue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override string GetValue()
        {
            switch (Operation)
            {
                case Operation.Plus:
                    return "+";
                case Operation.Minus:
                    return "-";
                case Operation.Multiply:
                    return "*";
                case Operation.Div:
                    return "/";
                case Operation.Power:
                    return "^";
                case Operation.Concat:
                    return "";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool IsConcatenable()
        {
            return Operation == Operation.Concat && Left.IsConcatenable() && Right.IsConcatenable();
        }

        public override int GetWidth()
        {
            return Left.GetWidth() + Right.GetWidth();
        }
    }

    internal class Program
    {
        const double Epsilon = 1.0E-20;
        private static string _s = "000000";

        public static int DigitByIndex(int index)
        {
            return _s[index] - 48;
        }

        public static IEnumerable<Node> Braces(int n, int begin)
        {
            if (n == 0)
            {
                yield return new NumberNode() {Number = DigitByIndex(begin)};
            }

            for (var i = 0; i < n; i++)
            {
                var lefts = Braces(i, begin);
                foreach (var left in lefts)
                {
                    var rights = Braces(n - i - 1, begin + i + 1);
                    foreach (var right in rights)
                    {
                        foreach (var value in Enum.GetValues(typeof(Operation)))
                        {
                            if ((Operation) value == Operation.Concat &&
                                !(left.IsConcatenable() && right.IsConcatenable()))
                            {
                                // cannot concat 1|(2+3)
                                continue;
                            }

                            if ((Operation) value == Operation.Power &&
                                Math.Abs(left.Evaluate()) <= Epsilon && Math.Abs(right.Evaluate()) <= Epsilon)
                            {
                                // cannot calculate 0^0
                                continue;
                            }
                            
                            if ((Operation) value == Operation.Div &&
                                Math.Abs(right.Evaluate()) <= double.Epsilon)
                            {
                                // cannot divide by zero
                                continue;
                            }

                            yield return new OperationNode
                                {Left = left, Right = right, Operation = (Operation) value};
                        }
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            var n = 5;

            long totalExpr = 0;
            var map = new Dictionary<double, string>();

            var startTime = DateTime.Now;
            var fileInfo = new FileInfo($"res-{n}.txt");
            using (var sr = fileInfo.CreateText())
            {
                for (int i = 729_802; i <= 999_999; i++)
                {
                    _s = i.ToString("000000");
                    var trees = Braces(n, 0);
                    foreach (var tree in trees)
                    {
                        totalExpr++;

                        try
                        {
                            var res = tree.Evaluate();

                            if (Math.Abs(res - 100) < 1.0E-15)
                            {
                                map[i] = tree.ToString();
                                sr.WriteLine($"{i} = {map[i]}");
                                break;
                            }

//                        if (res - Math.Floor(res) > 1.0E-15)
//                        {
//                            // ignore with fraction part
//                            continue;
//                        }
//
//                        if (Math.Abs(res) > 50_000)
//                        {
//                            // ignore too large
//                            continue;
//                        }
//
//                        if (Math.Abs(res) < 1.0E-15)
//                        {
//                            // ignore numbers like 1.0E-117
//                            // NOTE: zero is ignored too! 
//                            continue;
//                        }
//
//                        if (!map.ContainsKey(res))
//                        {
//                            map[res] = tree.Print();
//                        }
                        }
                        catch (DivideByZeroException)
                        {
                        }

                        if (totalExpr % 1000000 == 0)
                        {
                            sr.Flush();
                            Console.WriteLine($"totalExpr = {totalExpr}");
                            //var expr = tree.Print();
                            //Console.WriteLine($"{expr} = {resStr}");
                        }
                    }
                }

                var diffTime = DateTime.Now - startTime;

                var keys = map.Keys.ToArray();
                Array.Sort(keys);

                sr.WriteLine($"=============================");
//                var prev = 2.0;
//                var lastOk = 2.0;
//                foreach (var key in keys)
//                {
//                    //Console.WriteLine($"{key} = {map[key]}");
//                    sr.WriteLine($"{key} = {map[key]}");
//
//                    if (key >= prev && lastOk == 2.0)
//                    {
//                        if ((Math.Abs(key - prev - 1.0) >= 1.0E-10) && (Math.Abs(key - prev) >= 1.0E-10))
//                        {
//                            // not OK
//                            lastOk = prev;
//                        }
//
//                        prev = key;
//                    }
//                }
//
//                sr.WriteLine($"lastOk: {lastOk}");
                sr.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                sr.WriteLine(
                    $"real {map.Count} in {diffTime.TotalMilliseconds} => {map.Count / diffTime.TotalMilliseconds} records/ms");

                //1Console.WriteLine($"lastOk: {lastOk}");
                Console.WriteLine(
                    $"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                Console.WriteLine(
                    $"real {map.Count} in {diffTime.TotalMilliseconds} => {map.Count / diffTime.TotalMilliseconds} records/ms");
            }
        }
    }
}

// 0 => 1
// 1 => 1
// 2 => 2
// 3 => 5
// 4 => 14 * 6^5
// 5 => 42 * 6^6
// 6 => 132 * 6^7
// 7 => 429 * 6^8
// 8 => 1430 - Catalan(8) * 6^9
// 9 => 4862 - Catalan(9)
// 10 => 16796 - Catalan(10)

// for 1..9
// total variants: <= operations * braces = 6^9 * 1430 ~= 1.4 * 10^10
// <= because + and * are commutative

// for 1..6
// total variants: <= operations * braces = 6^9 * 1430 ~= 1.4 * 10^10