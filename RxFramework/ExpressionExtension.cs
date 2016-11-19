using System;
using System.Linq.Expressions;

namespace RxFramework
{
    public static class ExpressionExtension
    {
        public static string GetMemberName<U,T>(this Expression<Func<U,T>> expresion)
        {
            var body = expresion.Body as MemberExpression;
            if (body == null)
            {
                throw new ArgumentException("Lambda must have body");
            }
            return body.Member.Name;
        }
    }
}
