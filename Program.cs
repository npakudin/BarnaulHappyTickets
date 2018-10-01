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
        Mult,
        Div,
        Power,
        Concat,
    }
    
    abstract class Node
    {
        //public string Value;
        public Node Left;
        public Node Right;

        public virtual string GetValue()
        {
            return "";
        }
        
        public virtual double Evaluate()
        {
            return 0;
        }

        public virtual bool IsConcatenable()
        {
            return false;
        }

        public string Print()
        {
            var sb = new StringBuilder();
            if (Left != null)
            {
                sb.Append("(");
                sb.Append(Left.Print());
            }

            sb.Append(GetValue());

            if (Right != null)
            {
                sb.Append(Right.Print());
                sb.Append(")");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return Print();
        }
    }

    class NumberNode : Node
    {
        public int Number;

        public override double Evaluate()
        {
            return Number;
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
        
        public override bool IsConcatenable()
        {
            return Operation == Operation.Concat && Left.IsConcatenable() && Right.IsConcatenable();
        }
        
        double Cached = Double.NaN;
        private bool IsCached = false;

        public override double Evaluate()
        {
            if (!IsCached)
            {
                IsCached = true;
                Cached = InnerEvaluate();
            }

            return Cached;
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
                case Operation.Mult:
                    return leftValue * rightValue;
                case Operation.Div:
//                    if (rightValue == 0)
//                    {
//                        return 0x7FFFFFFF;
//                    }
                    return leftValue / rightValue;
                case Operation.Power:
                    return Math.Pow(leftValue, rightValue);
                case Operation.Concat:
                    //if (Left.IsConcatenable() && Right.IsConcatenable())
                    {
                        var log10 = Math.Ceiling(Math.Log10(rightValue));
                        return leftValue * Math.Pow(10, log10) + rightValue;
                    }
//                    else
//                    {
//                        throw new Exception("Cannot concat");
//                    }
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
                case Operation.Mult:
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
    }

    internal class Program
    {
        public static int DigitByIndex(int index)
        {
            return index + 1;
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
//                            var l = left.Evaluate();
//                            var r = right.Evaluate();
                            
                            
                            
                            if ((Operation)value == Operation.Concat &&
                                !(left.IsConcatenable() && right.IsConcatenable()))
                            {
                                continue;
                            }
                            
                            yield return new OperationNode() {Left = left, Right = right, Operation = (Operation)value};                            
                        }
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            var n = 6;
            
            long totalExpr = 0;
            var map = new Dictionary<double, string>();

            var startTime = DateTime.Now;
            
            var trees = Braces(n, 0);
            foreach (var tree in trees)
            {
                totalExpr++;

                try
                {
                    var res = tree.Evaluate();

                    if (res - Math.Floor(res) > 1.0E-12)
                    {
                        // ignore with fraction part
                        continue;
                    }

                    if (Math.Abs(res) > 50_000)
                    {
                        // ignore too large
                        continue;
                    }

                    if (Math.Abs(res) < 1.0E-12)
                    {
                        // ignore numbers like 1.0E-117
                        // NOTE: zero is ignored too! 
                        continue;
                    }

                    if (!map.ContainsKey(res))
                    {
                        map[res] = tree.Print();
                    }
                }
                catch (DivideByZeroException)
                {
                }
                
                if (totalExpr % 100000 == 0)
                {
                    Console.WriteLine($"totalExpr = {totalExpr}");
                    //var expr = tree.Print();
                    //Console.WriteLine($"{expr} = {resStr}");
                }
            }
            
            var diffTime = DateTime.Now - startTime;

            var keys = map.Keys.ToArray();
            Array.Sort(keys);
            
            var fileInfo = new FileInfo($"res-{n}.txt");
            using (var sr = fileInfo.CreateText())
            {
                sr.WriteLine($"=============================");
                var prev = 2.0;
                var lastOk = 2.0;
                foreach (var key in keys)
                {
                    //Console.WriteLine($"{key} = {map[key]}");
                    sr.WriteLine($"{key} = {map[key]}");

                    if (key >= prev && lastOk == 2.0)
                    {
                        if ((Math.Abs(key - prev - 1.0) >= 1.0E-10) && (Math.Abs(key - prev) >= 1.0E-10))
                        {
                            // not OK
                            lastOk = prev;
                        }
                        prev = key;
                    }
                }
                sr.WriteLine($"lastOk: {lastOk}");
                sr.WriteLine($"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                sr.WriteLine($"real {map.Count} in {diffTime.TotalMilliseconds} => {map.Count / diffTime.TotalMilliseconds} records/ms");
                
                Console.WriteLine($"lastOk: {lastOk}");
                Console.WriteLine($"total {totalExpr} in {diffTime.TotalMilliseconds} => {totalExpr / diffTime.TotalMilliseconds} records/ms");
                Console.WriteLine($"real {map.Count} in {diffTime.TotalMilliseconds} => {map.Count / diffTime.TotalMilliseconds} records/ms");
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
