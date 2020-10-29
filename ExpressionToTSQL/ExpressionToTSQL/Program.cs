﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace ExpressionToTSQL
{
    static class Program
    {
        static void Main(string[] args)
        {
            List<ExpressionResult> expressionResults = new List<ExpressionResult>();

            Expression<Func<SampleClass, bool>> expression = (x => x.Name == "Foo");
            expressionResults = GetExpressions(expression.Body, expressionResults);

            expressionResults.Clear();

            Expression<Func<SampleClass, bool>> expressionWithOr = (x => x.Name == "Foo" || x.Name == "Goo");
            expressionResults = GetExpressions(expressionWithOr.Body, expressionResults);

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionWithAnd = (x => x.Name == "Foo" && x.Name.Length == 3);
            expressionResults = GetExpressions(expressionWithAnd.Body, expressionResults);

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionWithParentheses = (x => x.Name == "Foo" || (x.Name == "Goo" && x.Year == 2020));
            expressionResults = GetExpressions(expressionWithParentheses.Body, expressionResults);

            expressionResults.Clear();
            expressionWithParentheses = (x => (x.Name == "Foo" || x.Name == "Goo") && x.Year == 2020);
            expressionResults = GetExpressions(expressionWithParentheses.Body, expressionResults);

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionWithNotEqual = (x => x.Name != "Foo");
            expressionResults = GetExpressions(expressionWithNotEqual.Body, expressionResults);

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionWithParenthesesNotEqual = (x => (x.Name != "Foo" && x.Name != "Goo") && x.Year == 2020);
            expressionResults = GetExpressions(expressionWithParenthesesNotEqual.Body, expressionResults);

            string rawText = expressionResults.ConvertToRawText(); // Result: ( ( Name != Foo and Name != Goo) and Year = 2020) 

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionLessThan = (x => x.Year < 2020);
            expressionResults = GetExpressions(expressionLessThan.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionGreaterThan = (x => x.Year > 2020);
            expressionResults = GetExpressions(expressionGreaterThan.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionLessThanOrEqual = (x => x.Year <= 2020);
            expressionResults = GetExpressions(expressionLessThanOrEqual.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionGreaterThanOrEqual = (x => x.Year >= 2020);
            expressionResults = GetExpressions(expressionGreaterThanOrEqual.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionToLower = (x => x.Name.ToLower() == "foo");
            expressionResults = GetExpressions(expressionToLower.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionToUpper = (x => x.Name.ToUpper() == "FOO");
            expressionResults = GetExpressions(expressionToUpper.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionSubString = (x => x.Name.Substring(0, 3) == "Fooooo");
            expressionResults = GetExpressions(expressionSubString.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionStartsWith = (x => x.Name.StartsWith('F'));
            expressionResults = GetExpressions(expressionStartsWith.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

            expressionResults.Clear();
            Expression<Func<SampleClass, bool>> expressionEndsWith = (x => x.Name.EndsWith("o"));
            expressionResults = GetExpressions(expressionEndsWith.Body, expressionResults);
            rawText = expressionResults.ConvertToRawText();

        }

        private static List<ExpressionResult> GetExpressions(object expression, List<ExpressionResult> toExpressionList)
        {
            ExpressionResult expressionResult = new ExpressionResult();

            if (expression is BinaryExpression)
            {
                BinaryExpression binaryExpression = expression as BinaryExpression;

                if (
                    binaryExpression.NodeType == ExpressionType.Equal
                    ||
                    binaryExpression.NodeType == ExpressionType.NotEqual
                    ||
                    binaryExpression.NodeType == ExpressionType.LessThan
                    ||
                    binaryExpression.NodeType == ExpressionType.GreaterThan
                    ||
                    binaryExpression.NodeType == ExpressionType.LessThanOrEqual
                    ||
                    binaryExpression.NodeType == ExpressionType.GreaterThanOrEqual)
                {
                    if (binaryExpression.Left is MemberExpression)
                    {
                        MemberExpression memberExpression = binaryExpression.Left as MemberExpression;

                        if (memberExpression.Expression != null && memberExpression.Expression.Type == typeof(SampleClass))
                        {
                            expressionResult.MemberName = memberExpression.Member.Name;                                               // Name
                        }
                        else
                        {
                            expressionResult.MemberName = (memberExpression.Expression as MemberExpression).Member.Name;              // Name
                            expressionResult.SubProperty = memberExpression.Member.Name;                                              // Name.Length (Name.Length == 3)
                        }
                    }
                    else if (binaryExpression.Left is MethodCallExpression)
                    {
                        MethodCallExpression methodCallExpression = binaryExpression.Left as MethodCallExpression;
                        expressionResult.MemberName = (methodCallExpression.Object as MemberExpression).Member.Name;    //Name                        

                        expressionResult.SubProperty = methodCallExpression.Method.Name;                                    // ToLower
                        if (methodCallExpression.Arguments != null && methodCallExpression.Arguments.Any())
                        {
                            expressionResult.SubPropertyArguments.AddRange(methodCallExpression.Arguments.Select(x => (x as ConstantExpression).Value).ToList());
                        }
                    }

                    expressionResult.Condition = binaryExpression.NodeType;                                      // ==
                    expressionResult.Value = (binaryExpression.Right as ConstantExpression).Value.ToString();    // Foo

                    toExpressionList.Add(expressionResult);
                }
                else if (binaryExpression.NodeType == ExpressionType.OrElse)
                {
                    toExpressionList.Add(new ExpressionResult() { Parentheses = "(" });                         // (
                    GetExpressions(binaryExpression.Left as BinaryExpression, toExpressionList);                // Name == Goo
                    toExpressionList.Add(new ExpressionResult() { Condition = ExpressionType.Or });             // Or
                    GetExpressions(binaryExpression.Right as BinaryExpression, toExpressionList);               // Year == 2020
                    toExpressionList.Add(new ExpressionResult() { Parentheses = ")" });                         // )
                }
                else if (binaryExpression.NodeType == ExpressionType.AndAlso)
                {
                    toExpressionList.Add(new ExpressionResult() { Parentheses = "(" });                         // (
                    GetExpressions(binaryExpression.Left as BinaryExpression, toExpressionList);                // (Name == Foo Or || Name == Goo)
                    toExpressionList.Add(new ExpressionResult() { Condition = ExpressionType.And });            // And
                    GetExpressions(binaryExpression.Right as BinaryExpression, toExpressionList);               // Year == 2020
                    toExpressionList.Add(new ExpressionResult() { Parentheses = ")" });                         // )
                }
            }
            else if (expression is MethodCallExpression)
            {
                MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                expressionResult.MemberName = (methodCallExpression.Object as MemberExpression).Member.Name;    //Name                        

                expressionResult.SubProperty = methodCallExpression.Method.Name;                                    // ToLower
                if (methodCallExpression.Arguments != null && methodCallExpression.Arguments.Any())
                {
                    expressionResult.SubPropertyArguments.AddRange(methodCallExpression.Arguments.Select(x => (x as ConstantExpression).Value).ToList());
                }

                toExpressionList.Add(expressionResult);
            }

            return toExpressionList;
        }

        private static string ConvertToRawText(this List<ExpressionResult> expressionResults)
        {
            StringBuilder sbText = new StringBuilder();

            foreach (var exp in expressionResults)
            {
                if (!string.IsNullOrEmpty(exp.Parentheses))
                {
                    sbText.Append(exp.Parentheses);
                }

                if (!sbText.ToString().EndsWith(" "))
                    sbText.Append(" ");

                if (!string.IsNullOrEmpty(exp.MemberName))
                {
                    sbText.Append(exp.MemberName);
                }

                if (!string.IsNullOrEmpty(exp.SubProperty))
                {
                    sbText.Append(".");
                    sbText.Append(exp.SubProperty);

                    if (exp.SubPropertyArguments.Any())
                    {
                        sbText.Append("(");
                        sbText.Append(String.Join(',', exp.SubPropertyArguments));
                        sbText.Append(")");
                    }
                }

                if (!sbText.ToString().EndsWith(" "))
                    sbText.Append(" ");

                switch (exp.Condition)
                {
                    case ExpressionType.Equal:
                        sbText.Append("=");
                        break;
                    case ExpressionType.NotEqual:
                        sbText.Append("!=");
                        break;
                    case ExpressionType.And:
                        sbText.Append("and");
                        break;
                    case ExpressionType.Or:
                        sbText.Append("or");
                        break;
                    case ExpressionType.LessThan:
                        sbText.Append("<");
                        break;
                    case ExpressionType.GreaterThan:
                        sbText.Append(">");
                        break;
                    case ExpressionType.LessThanOrEqual:
                        sbText.Append("<=");
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        sbText.Append(">=");
                        break;
                    default:
                        break;
                }

                if (!sbText.ToString().EndsWith(" "))
                    sbText.Append(" ");

                sbText.Append(exp.Value);
            }

            return sbText.ToString();
        }
    }
}
