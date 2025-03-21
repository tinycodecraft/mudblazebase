using blazelogBase.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static blazelogBase.Shared.Constants;

namespace blazelogBase.Shared.Tools
{
    

    /// <summary>
    /// </summary>
    /// <see cref="http://stackoverflow.com/questions/17960103/reduce-an-expression-by-inputting-a-parameter"/>
    public class ResolveParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _param;
        private readonly object _value;

        public ResolveParameterVisitor(ParameterExpression param, object value)
        {
            _param = param;
            _value = value;
        }

        public Expression ResolveLocalValues(Expression exp)
        {
            return Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Type == _param.Type && node.Name == _param.Name
                && node.Type.IsSimpleType())
            {
                return Expression.Constant(_value);
            }

            return base.VisitParameter(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var parameters = node.Parameters.Where(p => p.Name != _param.Name && p.Type != _param.Type).ToList();
            return Expression.Lambda(Visit(node.Body), parameters);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null
                && m.Expression.NodeType == ExpressionType.Parameter
                && m.Expression.Type == _param.Type && ((ParameterExpression)m.Expression).Name == _param.Name)
            {
                object newVal;
                if (m.Member is FieldInfo)
                    newVal = ((FieldInfo)m.Member).GetValue(_value);
                else if (m.Member is PropertyInfo)
                    newVal = ((PropertyInfo)m.Member).GetValue(_value, null);
                else
                    newVal = null;
                return Expression.Constant(newVal);
            }

            return base.VisitMember(m);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            return base.VisitUnary(node);
        }
    }
    public class ParameterVisitor : ExpressionVisitor
    {
        public Expression Parameter
        {
            get; private set;
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            Parameter = node;
            return node;
        }
    }



    internal class AsyncQueryable<T> : IAsyncEnumerable<T>, IQueryable<T>
    {
        private IQueryable<T> Source;

        public AsyncQueryable(IQueryable<T> source)
        {
            Source = source;
        }

        public Type ElementType => typeof(T);

        public Expression Expression => Source.Expression;

        public IQueryProvider Provider => new AsyncQueryProvider<T>(Source.Provider);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumeratorWrapper<T>(Source.GetEnumerator());
        }

        public IEnumerator<T> GetEnumerator() => Source.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class AsyncQueryProvider<T> : IQueryProvider
    {
        private readonly IQueryProvider Source;

        public AsyncQueryProvider(IQueryProvider source)
        {
            Source = source;
        }

        public IQueryable CreateQuery(Expression expression) =>
            Source.CreateQuery(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new AsyncQueryable<TElement>(Source.CreateQuery<TElement>(expression));

        public object Execute(Expression expression) => Execute<T>(expression);

        public TResult Execute<TResult>(Expression expression) =>
            Source.Execute<TResult>(expression);
    }



    internal class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> Source;

        public AsyncEnumeratorWrapper(IEnumerator<T> source)
        {
            Source = source;
        }

        public T Current => Source.Current;

        public ValueTask DisposeAsync()
        {
            return new ValueTask(Task.CompletedTask);
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(Source.MoveNext());
        }
    }

    public static class QueriesExtensions

    {
        public static readonly MethodInfo InListMethod = MethodsOf((IEnumerable<string> q) => q.Contains((string)null));
        public static readonly MethodInfo ContainsMethod = MethodOf(() => "".Contains(default(string)));
        public static readonly MethodInfo StartsWithMethod = MethodOf(() => "".StartsWith(default(string)));
        public static readonly MethodInfo EndsWithMethod = MethodOf(() => "".EndsWith(default(string)));
        public static readonly MethodInfo QyLikeMethod = MethodOf(()=> EF.Functions.Like(default(string),default(string)));
        public static readonly MethodInfo AnyMethod = MethodOf(() => Enumerable.Any(default(IEnumerable<object>), default(Func<object, bool>))).GetGenericMethodDefinition();

        //public static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", new[] { typeof(DbFunctions), typeof(string), typeof(string) });

        public static IQueryable<T> AsAsyncQueryable<T>(this ICollection<T> source) =>
            new AsyncQueryable<T>(source.AsQueryable());

        public static MethodInfo MethodsOf<E,T>(Expression<Func<E,T>> method)
        {
            MethodCallExpression mce = (MethodCallExpression)method.Body;
            MethodInfo mi = mce.Method;

            return mi;
        }
        public static MethodInfo MethodOf<T>(Expression<Func<T>> method)
        {
            
            
            MethodCallExpression mce = (MethodCallExpression)method.Body;
            MethodInfo mi = mce.Method;
            
            return mi;
        }

        public static NameValueCollection AddQueryParam<T,V>(this NameValueCollection nv,IQueryable<T> query, Expression<Func<T, V>> GetPropertyLambda,string value,string op=Op.itLikes,string sep="|")
        {
            if(nv== null)
                nv = new NameValueCollection();
            var MemberName = GetPropertyLambda.GetMemberName();
            if (string.IsNullOrEmpty(value))
                return nv;
            string value1 = value;
            string value2 = string.Empty;
            if(value.Contains(sep) && op==Op.Between)
            {
                value2 = value.Substring(value.IndexOf(sep)+1);
                value1 = value.Substring(0,value.IndexOf(sep));

            }
            else if(value.Contains(sep) && op==Op.Within)
            {
                value = string.Join("||", value.ItSplit(sep));
            }

            switch(op)
            {
                case "==":
                case Op.equal:
                    nv.Add($"__{nameof(QueryOpType.Equal)}__{MemberName}", value);
                    break;
                case ">=":
                case Op.greaterThanOrEqual:
                    nv.Add($"__{nameof(QueryOpType.GreaterOrEq)}__{MemberName}", value);
                    break;
                case "<=":
                case Op.lessThanOrEqual:
                    nv.Add($"__{nameof(QueryOpType.LessOrEq)}__{MemberName}", value);
                    break;
                case "<":
                case  Op.lessThan:                   
                    nv.Add($"__{nameof(QueryOpType.Less)}__{MemberName}", value);
                    break;
                case Op.itLikes:
                    nv.Add($"__{nameof(QueryOpType.LikesWith)}__{MemberName}", value);
                    break;
                case Op.Between:
                    if (!string.IsNullOrEmpty(value1))
                        nv.Add($"__{nameof(QueryOpType.GreaterOrEq)}__{MemberName}", value1);
                    if (!string.IsNullOrEmpty(value2))
                        nv.Add($"__{nameof(QueryOpType.LessOrEq)}__{MemberName}", value2);
                    break;
                case Op.BeginWith:
                    nv.Add($"__{nameof(QueryOpType.StartsWith)}__{MemberName}", value);
                    break;
                case Op.EndWith:
                    nv.Add($"__{nameof(QueryOpType.EndsWith)}__{MemberName}", value);
                    break;
                case Op.Within:
                    nv.Add($"__{nameof(QueryOpType.ContainsWith)}__{MemberName}", value);
                    break;
                case Op.CheckListIn:
                    nv.Add($"__{nameof(QueryOpType.InListOp)}__{MemberName}", value);
                    break;
                default:
                    nv.Add($"{MemberName}", value);
                    break;

            }

            return nv;
        }




        /// <summary>
        /// Convert an expression with two parameters to one parameter, 
        /// by filling in (resolving) the first parameter with an instance provided.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <see cref="http://stackoverflow.com/questions/17960103/reduce-an-expression-by-inputting-a-parameter"/>
        public static Expression<Func<TData, bool>> ConvertExpression<TData>(
            this Expression<Func<TData, TData, bool>> expression,
            TData model)
        {
            ParameterExpression modelParameterEx = expression.Parameters.FirstOrDefault();
            if (modelParameterEx == null)
                throw new InvalidOperationException("Unable to find paremeter of the expression.");
            var newExpression = Expression.Lambda<Func<TData, bool>>(
                new ResolveParameterVisitor(modelParameterEx, model)
                .ResolveLocalValues(expression.Body), expression.Parameters.Where(p => p != modelParameterEx));
            return newExpression;
        }


        public static IQueryable<T> BuildOrder<T>(this IQueryable<T> source, params SortDescription[] properties)
        {
            if (properties == null || properties.Length == 0) return source;

            var thenBy = false;
            var queryExpr = source.Expression;
            var worktype = typeof(T);
            foreach (var item in properties
                .Select(prop => new { PropertyInfo = worktype.GetProperty(prop.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance), prop.Direction }))
            {
                var paremeterExpr = Expression.Parameter(worktype, "o");
                var propertyInfo = item.PropertyInfo;
                //property = "SomeProperty"
                var propertyExpr = Expression.Property(paremeterExpr, item.PropertyInfo);
                var selectorExpr = Expression.Lambda(propertyExpr, paremeterExpr);
                var propertyType = propertyInfo.PropertyType;
                var isAscending = item.Direction == ListSortDirection.Ascending;
                var currentOrderIs = !thenBy ? nameof(QueryOpType.OrderBy) : nameof(QueryOpType.ThenBy);


                queryExpr = Expression.Call(
                        //type to call method on
                        typeof(Queryable),
                        //method to call
                        isAscending ? currentOrderIs : $"{currentOrderIs}Descending",
                        //generic types of the order by method
                        new Type[] {
                source.ElementType,
                propertyType },
                        //existing expression to call method on
                        queryExpr,
                        //method parameter, in our case which property to order on
                        selectorExpr);

                thenBy = true;

            }
            return source.Provider.CreateQuery<T>(queryExpr);
        }

        /// <summary>
        /// return sort of queryable
        /// </summary>
        /// <param name="source">queryable</param>
        /// <param name="worktype">type of queryable</param>
        /// <param name="properties">sorting arguments</param>
        /// <returns></returns>
        public static IQueryable BuildSort(this IQueryable source, Type worktype, params SortDescription[] properties)
        {

            if (properties == null || properties.Length == 0) return source;

            var thenBy = false;
            var queryExpr = source.Expression;

            foreach (var item in properties
                .Select(prop => new { PropertyInfo = worktype.GetProperty(prop.PropertyName), prop.Direction }))
            {
                var paremeterExpr = Expression.Parameter(worktype, "o");
                var propertyInfo = item.PropertyInfo;
                //property = "SomeProperty"
                var propertyExpr = Expression.Property(paremeterExpr, item.PropertyInfo);
                var selectorExpr = Expression.Lambda(propertyExpr, paremeterExpr);
                var propertyType = propertyInfo.PropertyType;
                var isAscending = item.Direction == ListSortDirection.Ascending;
                var currentOrderIs = !thenBy ? nameof(QueryOpType.OrderBy) : nameof(QueryOpType.ThenBy);


                queryExpr = Expression.Call(
                        //type to call method on
                        typeof(Queryable),
                        //method to call
                        isAscending ? currentOrderIs : $"{currentOrderIs}Descending",
                        //generic types of the order by method
                        new Type[] {
                source.ElementType,
                propertyType },
                        //existing expression to call method on
                        queryExpr,
                        //method parameter, in our case which property to order on
                        selectorExpr);

                thenBy = true;

            }
            return source.Provider.CreateQuery(queryExpr);
        }

        private static Expression GenerateLambda(Expression exprBase, Expression predicate)
        {
            var resultParameterVisitor = new ParameterVisitor();
            resultParameterVisitor.Visit(exprBase);
            var resultParameter = resultParameterVisitor.Parameter;
            return Expression.Lambda(predicate, (ParameterExpression)resultParameter);
        }

        private static Expression GenerateCompareLambda(string searchString, Type dbType, MemberExpression dbFieldMember, QueryOpType methodCall = QueryOpType.LikesWith)
        {
            // Check if a search criterion was provided
            // Then "and" it to the predicate.
            // e.g. predicate = predicate.And(x => x.firstName.Contains(searchCriterion.FirstName)); ...
            // Create an "x" as TDbType
            var dbTypeParameter = Expression.Parameter(dbType, @"x");
            // Get at x.firstName
            //var dbFieldMember = Expression.MakeMemberAccess(dbTypeParameter, dbFieldMemberInfo);
            // Create the criterion as a constant
            searchString = string.Format("{0}", searchString);



            var criterionConstant = new Expression[] { Expression.Constant(searchString),Expression.Constant($"%{searchString}%"),Expression.Constant(searchString.ItSplit("||")) };
            // Create the MethodCallExpression like x.firstName.Contains(criterion)
            MethodInfo methodHandler = QyLikeMethod;
            int constantIndex = 0;
            #region
            switch (methodCall)
            {
                case QueryOpType.ContainsWith:
                    constantIndex = 2;
                    methodHandler = InListMethod;
                    break;
                case QueryOpType.StartsWith:
                    constantIndex = 0;
                    methodHandler = StartsWithMethod;
                    break;
                case QueryOpType.EndsWith:
                    constantIndex = 0;
                    methodHandler = EndsWithMethod;
                    break;
                default:
                    constantIndex = 1;
                    methodHandler = QyLikeMethod;
                    break;
            }
            #endregion

            if (methodCall != QueryOpType.LikesWith)
            {
                var containsCall = Expression.Call(methodHandler, criterionConstant[constantIndex], dbFieldMember);
                return containsCall;
            }
            else
            {
                //The following code example is also valid
                //var containsCall = Expression.Call(typeof(DbFunctionsExtensions), nameof(DbFunctionsExtensions.Like), null, Expression.Constant(EF.Functions), dbFieldMember, criterionConstant[0]);
                var containsCall = Expression.Call(QyLikeMethod,
                                    Expression.Property(null, typeof(EF), nameof(EF.Functions)), dbFieldMember, criterionConstant[constantIndex]);


                return containsCall;
            }




        }


        public static IEnumerable<Expression> GenerateExpr(Expression exprBase, NameValueCollection nvSetValues, string prefix = "__")
        {
            Type entityType = exprBase.Type;
            var props = entityType.GetProperties();
            foreach (var key in nvSetValues.AllKeys)
            {
                string functionKey = string.Empty;
                string convertedKey = key;
                if (key.StartsWith("__"))
                {

                    convertedKey = key.Substring(key.LastIndexOf(prefix) + prefix.Length);
                    functionKey = key.Replace(convertedKey, string.Empty).Replace(prefix, string.Empty);

                }

                var columnProperty = props.FirstOrDefault(x => x.Name.Equals(convertedKey, StringComparison.InvariantCultureIgnoreCase));

                //skip if no corresponding property
                if (columnProperty == null)
                    continue;
                //throw new Exception("Cannot find the property:" + convertedKey);

                object paramValue = Convert.ChangeType(nvSetValues[key], PropsExtensions.BaseType(columnProperty.PropertyType));
                MemberExpression columnExpr = Expression.Property(exprBase, columnProperty);
                ////barbara
                //if (paramValue.ToString().Contains("%") || paramValue.ToString().Contains("_"))
                //    functionKey = nameof(QueryOpType.LikesWith);


                QueryOpType funcKeyType = SubStringExtensions.GetEnum<QueryOpType>(functionKey);
                switch (funcKeyType)
                {
                    case QueryOpType.Equal:
                        yield return Expression.Equal(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                    case QueryOpType.NotEq: //Add By Jonathan LO
                        yield return Expression.NotEqual(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                    case QueryOpType.GreaterOrEq:
                        yield return Expression.GreaterThanOrEqual(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                    case QueryOpType.LessOrEq:
                        yield return Expression.LessThanOrEqual(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                    //20170807 Barbara Add Less for DateTime To Search
                    case QueryOpType.Less:
                        yield return Expression.LessThan(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                    // Please note the contains with only work for array of contants (which is forced to sep => `||`
                    case QueryOpType.ContainsWith:
                    case QueryOpType.LikesWith:
                    case QueryOpType.StartsWith:
                    case QueryOpType.EndsWith:
                        yield return GenerateCompareLambda(paramValue.ToString(), entityType, columnExpr, funcKeyType);
                        break;
                    case QueryOpType.InListOp:
                        Expression OrExp = Expression.Equal(Expression.Constant(0), Expression.Constant(1));
                        var valuelist = paramValue.ToString().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var val in valuelist)
                        {
                            OrExp = Expression.OrElse(OrExp, Expression.Equal(columnExpr, Expression.Constant(val, typeof(string))));
                        }
                        if (valuelist.Length > 0)
                        {
                            yield return OrExp;
                        }
                        break;
                    default:
                        yield return Expression.Equal(columnExpr, Expression.Constant(paramValue, columnProperty.PropertyType));
                        break;
                }
            }
        }



        public static Expression GetNavigationPropertyExpression(Expression exprBase, NameValueCollection nvSetValues, params string[] properties)
        {
            Expression resultExpression = null;
            Expression childParameter, navigationPropertyPredicate;
            Type childType = null;

            if (properties.Count() > 0)
            {
                // arrange the base path
                exprBase = Expression.Property(exprBase, properties[0]);
                var isCollection = typeof(IEnumerable).IsAssignableFrom(exprBase.Type);
                // check if it´s a collection for further use as predicate during MethodCallExpression
                if (isCollection)
                {
                    childType = exprBase.Type.GetGenericArguments()[0];
                    childParameter = Expression.Parameter(childType, childType.Name);
                }
                else
                    childParameter = exprBase;

                // skip current property and get navigation property expression recursively
                if (properties.Count() > 1)
                {
                    var innerProperties = properties.Skip(1).ToArray();
                    navigationPropertyPredicate = GetNavigationPropertyExpression(childParameter, nvSetValues, innerProperties);
                    if (isCollection)
                    {
                        //build methodexpressioncall                    
                        var anyMethod = AnyMethod.MakeGenericMethod(childType);
                        navigationPropertyPredicate = Expression.Call(anyMethod, exprBase, navigationPropertyPredicate);
                        resultExpression = GenerateLambda(exprBase, navigationPropertyPredicate);
                    }
                    else
                        resultExpression = navigationPropertyPredicate;
                }
                else
                {
                    resultExpression = GetNavigationPropertyExpression(childParameter, nvSetValues);
                    if (isCollection)
                    {
                        var anyMethod = AnyMethod.MakeGenericMethod(childType);
                        navigationPropertyPredicate = Expression.Call(anyMethod, exprBase, resultExpression);
                        resultExpression = GenerateLambda(exprBase, navigationPropertyPredicate);
                    }
                }
            }
            else
            {
                var equalExpr = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
                foreach (var exp in GenerateExpr(exprBase, nvSetValues))
                {
                    //if (exp.ToString().ToUpper().Contains("PATINDEX"))
                    //{
                    //    var value = Expression.Constant(0, typeof(int?));
                    //    var greaterThan = Expression.GreaterThan(exp, value);
                    //    equalExpr = Expression.AndAlso(equalExpr, greaterThan);
                    //}
                    //else
             
                    equalExpr = Expression.AndAlso(equalExpr, exp);
                }
                resultExpression = GenerateLambda(exprBase, equalExpr);
            }
            return resultExpression;
        }


        public static IQueryable Search(this IQueryable query, Expression exprBase, NameValueCollection nvSet, string tablename)
        {
            Expression lamdaWhereClause = null;
            if (!string.IsNullOrEmpty(tablename))
                lamdaWhereClause = GetNavigationPropertyExpression(exprBase, nvSet, tablename.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries));
            // this case never happen?!
            else
                lamdaWhereClause = GetNavigationPropertyExpression(exprBase, nvSet);

            MethodCallExpression whereMthdCall = Expression.Call(typeof(Queryable), "Where", new Type[] { exprBase.Type }, query.Expression, lamdaWhereClause);
            query = query.Provider.CreateQuery(whereMthdCall);
            return query;
        }

        public static Expression CreateBaseExpr(Type paramtype, string symbol = "e")
        {

            return Expression.Parameter(paramtype, symbol);
        }

        public static IQueryable<T> GetSearch<T>(this DbContext MyContext, NameValueCollection nv) where T : class
        {

            var exprbase = CreateBaseExpr(typeof(T));

            var query = MyContext.Set<T>().AsQueryable() as IQueryable;
            query = query.Search(exprbase, nv, null);

            return query as IQueryable<T>;

        }


        #region Helper for preload objects 

        /// <summary>
        /// load the navigation properties
        /// </summary>
        /// <param name="ctx">db context</param>
        /// <param name="result">starting entity </param>
        /// <param name="navpaths">navigation paths of entity</param>
        public static void LoadRelatedFor(DbContext ctx, object result, params string[] navpaths)
        {
            navpaths = navpaths.Select((x, i) => $"{i}!{x}").ToArray();
            var instanceBag = new Queue<object>();
            var navStack = new Stack<string>(navpaths.Reverse());
            instanceBag.Enqueue(result);
            while (navStack.Count > 0)
            {
                var p = navStack.Pop();
                var nextnav = string.Empty;
                if (p.Contains("."))
                {
                    var navs = p.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    nextnav = navs[1];
                    p = navs[0];
                }
                while (instanceBag.Count > 0)
                {
                    var instance = instanceBag.Dequeue();

                    var typeOfinst = instance.GetType();
                    var nameOfproperty = p.Substring(p.IndexOf("!") + 1);
                    if (!typeOfinst.GetProperties().Any(y => y.Name.Equals(nameOfproperty)))
                        break;

                    var propOfp = typeOfinst.GetProperty(nameOfproperty);
                    var isCollectionOfChild = typeof(IEnumerable).IsAssignableFrom(propOfp.PropertyType);

                    if (isCollectionOfChild)
                        ctx.Entry(instance).Collection(nameOfproperty).Load();
                    else
                        ctx.Entry(instance).Reference(nameOfproperty).Load();


                    var instancegetter = PropsExtensions.FindGetter(instance.GetType(), nameOfproperty);

                    if (instancegetter != null)
                    {
                        instance = instancegetter(instance);
                        if (instance != null)
                        {
                            if (isCollectionOfChild)
                            {
                                var c = (instance as IEnumerable).Cast<object>().Count();
                                if (c == 0)
                                    break;
                                foreach (var insItem in instance as IEnumerable)
                                {
                                    instanceBag.Enqueue(insItem);

                                }
                                var listofnav = navpaths.ToList();
                                var nextindex = listofnav.IndexOf(nextnav);
                                var restofnav = listofnav.Skip(int.Parse(p.Substring(0, p.IndexOf("!"))) + 1).ToArray();
                                navStack = ReCreateStack(navStack, c, string.IsNullOrEmpty(nextnav) ? nextnav : $"{p}.{nextnav}", (listofnav.Count > nextindex + 1 && nextindex != -1) ? listofnav[nextindex + 1] : null, restofnav, p);


                                break;
                            }
                            else
                            {
                                instanceBag.Enqueue(instance);
                                var listofnav = navpaths.ToList();
                                var nextindex = listofnav.IndexOf(nextnav);
                                var restofnav = listofnav.Skip(int.Parse(p.Substring(0, p.IndexOf("!"))) + 1).ToArray();
                                navStack = ReCreateStack(navStack, 1, string.IsNullOrEmpty(nextnav) ? nextnav : $"{p}.{nextnav}", (listofnav.Count > nextindex + 1 && nextindex != -1) ? listofnav[nextindex + 1] : null, restofnav, p);

                                break;
                            }
                        }


                    }

                }


            }

        }

        private static Stack<string> ReCreateStack(Stack<string> orgstack, int count, string nextnav, string tailnav, string[] restofnav, string currentnav)
        {
            if (!string.IsNullOrEmpty(nextnav))
            {
                var childnav = nextnav.Split('.').LastOrDefault();
                if (!string.IsNullOrEmpty(childnav))
                {
                    var headarr = orgstack.ToArray().Where(y => y.Contains(".")).ToArray();
                    var tailarr = orgstack.ToArray().Where(y => (!y.Contains(".") && (string.IsNullOrEmpty(tailnav) || y != tailnav))).ToArray();
                    if (!string.IsNullOrEmpty(tailnav))
                    {
                        childnav = string.Join(".", new[] { childnav, tailnav });
                    }

                    orgstack.Clear();
                    var newstack = headarr.Concat(Enumerable.Repeat(childnav, count)).Concat(tailarr).Reverse();
                    return new Stack<string>(newstack);
                }


            }


            if (restofnav.Length == 1)
            {
                orgstack = new Stack<string>(restofnav.Reverse());
                while (--count > 0)
                {
                    orgstack.Push(orgstack.Peek());
                }

            }
            else if (restofnav.Length > 1)
            {
                orgstack.Clear();
                var restnavs = restofnav.Skip(2).Reverse();
                foreach (var n in restnavs)
                {
                    orgstack.Push(n);
                }
                var newnav = string.Join(".", restofnav.Take(2));
                while (count-- > 0)
                {
                    orgstack.Push(newnav);
                }
            }
            return orgstack;
        }
    }
    #endregion


}
