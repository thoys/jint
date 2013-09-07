﻿using System;
using System.Linq;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Number;
using Jint.Native.Object;
using Jint.Parser.Ast;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Environments;
using Jint.Runtime.References;

namespace Jint.Runtime
{
    public class ExpressionInterpreter
    {
        private readonly Engine _engine;

        public ExpressionInterpreter(Engine engine)
        {
            _engine = engine;
        }

        private object EvaluateExpression(Expression expression)
        {
            return _engine.EvaluateExpression(expression);
        }

        public object EvaluateConditionalExpression(ConditionalExpression conditionalExpression)
        {
            var test = _engine.EvaluateExpression(conditionalExpression.Test);
            var evaluate = TypeConverter.ToBoolean(test) ? conditionalExpression.Consequent : conditionalExpression.Alternate;
            
            return _engine.EvaluateExpression(evaluate);
        }

        public object EvaluateAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            object rval = _engine.GetValue(EvaluateExpression(assignmentExpression.Right));

            var lref = EvaluateExpression(assignmentExpression.Left) as Reference;

            if (assignmentExpression.Operator == "=")
            {
                if(lref != null && lref.IsStrict() && lref.GetBase() is EnvironmentRecord && (lref.GetReferencedName() == "eval" || lref.GetReferencedName() == "arguments"))
                {
                    throw new JavaScriptException(_engine.SyntaxError);
                }

                _engine.PutValue(lref, rval);
                return rval;
            }

            object lval = _engine.GetValue(lref);

            switch (assignmentExpression.Operator)
            {
                case "+=":
                    var lprim = TypeConverter.ToPrimitive(lval);
                    var rprim = TypeConverter.ToPrimitive(rval);
                    if (TypeConverter.GetType(lprim) == TypeCode.String ||
                        TypeConverter.GetType(rprim) == TypeCode.String)
                    {
                        lval = TypeConverter.ToString(lprim) + TypeConverter.ToString(rprim);
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;

                case "-=":
                    lval = TypeConverter.ToNumber(lval) - TypeConverter.ToNumber(rval);
                    break;

                case "*=":
                    if (lval == Undefined.Instance || rval == Undefined.Instance)
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) * TypeConverter.ToNumber(rval);
                    }
                    break;

                case "/=":
                    lval = Divide(lval, rval);
                    break;

                case "%=":
                    if (lval == Undefined.Instance || rval == Undefined.Instance)
                    {
                        lval = Undefined.Instance;
                    }
                    else
                    {
                        lval = TypeConverter.ToNumber(lval) % TypeConverter.ToNumber(rval);
                    }
                    break;

                case "&=":
                    lval = TypeConverter.ToInt32(lval) & TypeConverter.ToInt32(rval);
                    break;

                case "|=":
                    lval = TypeConverter.ToInt32(lval) | TypeConverter.ToInt32(rval);
                    break;

                case "^=":
                    lval = TypeConverter.ToInt32(lval) ^ TypeConverter.ToInt32(rval);
                    break;

                case "<<=":
                    lval = TypeConverter.ToInt32(lval) << (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                case ">>=":
                    lval = TypeConverter.ToInt32(lval) >> (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;

                case ">>>=":
                    lval = (uint)TypeConverter.ToInt32(lval) >> (int)(TypeConverter.ToUint32(rval) & 0x1F);
                    break;
                
                default:
                    throw new NotImplementedException();

            }

            _engine.PutValue(lref, lval);

            return lval;
        }

        private object Divide(object lval, object rval)
        {
            if (lval == Undefined.Instance || rval == Undefined.Instance)
            {
                return Undefined.Instance;
            }
            else
            {
                var rN = TypeConverter.ToNumber(rval);
                var lN = TypeConverter.ToNumber(lval);

                if (double.IsNaN(rN) || double.IsNaN(lN))
                {
                    return double.NaN;
                }

                if (double.IsInfinity(lN) && double.IsInfinity(rN))
                {
                    return double.NaN;
                }

                if (double.IsInfinity(lN) && rN == 0)
                {
                    if (NumberInstance.IsNegativeZero(rN))
                    {
                        return -lN;
                    }

                    return lN;
                }

                if (lN == 0 && rN == 0)
                {
                    return double.NaN;
                }

                if (rN == 0)
                {
                    if (NumberInstance.IsNegativeZero(rN))
                    {
                        return lN > 0 ? -double.PositiveInfinity : -double.NegativeInfinity;
                    }

                    return lN > 0 ? double.PositiveInfinity : double.NegativeInfinity;
                }

                return lN/rN;
            }
        }

        public object EvaluateBinaryExpression(BinaryExpression expression)
        {
            object left = _engine.GetValue(EvaluateExpression(expression.Left));
            object right = _engine.GetValue(EvaluateExpression(expression.Right));
            object value;

            switch (expression.Operator)
            {
                case "+":
                    var lprim = TypeConverter.ToPrimitive(left);
                    var rprim = TypeConverter.ToPrimitive(right);
                    if (TypeConverter.GetType(lprim) == TypeCode.String || TypeConverter.GetType(rprim) == TypeCode.String)
                    {
                        value = TypeConverter.ToString(lprim) + TypeConverter.ToString(rprim);
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(lprim) + TypeConverter.ToNumber(rprim);
                    }
                    break;
                
                case "-":
                    value = TypeConverter.ToNumber(left) - TypeConverter.ToNumber(right);
                    break;
                
                case "*":
                    if (left == Undefined.Instance || right == Undefined.Instance)
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) * TypeConverter.ToNumber(right);
                    }
                    break;
                
                case "/":
                    value = Divide(left, right);
                    break;

                case "%":
                    if (left == Undefined.Instance || right == Undefined.Instance)
                    {
                        value = Undefined.Instance;
                    }
                    else
                    {
                        value = TypeConverter.ToNumber(left) % TypeConverter.ToNumber(right);
                    }
                    break;

                case "==":
                    value = Equal(left, right);
                    break;
                
                case "!=":
                    value = !Equal(left, right);
                    break;
                
                case ">":
                    value = Compare(right, left, false);
                    if (value == Undefined.Instance)
                    {
                        value = false;
                    }
                    break;

                case ">=":
                    value = Compare(left, right);
                    if (value == Undefined.Instance || (bool) value == true)
                    {
                        value = false;
                    }
                    else
                    {
                        value = true;
                    }
                    break;
                
                case "<":
                    value = Compare(left, right);
                    if (value == Undefined.Instance)
                    {
                        value = false;
                    }
                    break;
                
                case "<=":
                    value = Compare(right, left, false);
                    if (value == Undefined.Instance || (bool) value == true)
                    {
                        value = false;
                    }
                    else
                    {
                        value = true;
                    }
                    break;
                
                case "===":
                    return StriclyEqual(left, right);
                
                case "!==":
                    return !StriclyEqual(left, right);

                case "&":
                    return TypeConverter.ToInt32(left) & TypeConverter.ToInt32(right);

                case "|":
                    return TypeConverter.ToInt32(left) | TypeConverter.ToInt32(right);

                case "^":
                    return TypeConverter.ToInt32(left) ^ TypeConverter.ToInt32(right);

                case "<<":
                    return TypeConverter.ToInt32(left) << (int)(TypeConverter.ToUint32(right) & 0x1F);

                case ">>":
                    return TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);

                case ">>>":
                    return (uint)TypeConverter.ToInt32(left) >> (int)(TypeConverter.ToUint32(right) & 0x1F);

                case "instanceof":
                    var f = right as FunctionInstance;

                    if (f == null)
                    {
                        throw new JavaScriptException(_engine.TypeError, "instanceof can only be used with a function object");
                    }

                    value = f.HasInstance(left);
                    break;
                
                case "in":
                    var o = right as ObjectInstance;

                    if (o == null)
                    {
                        throw new JavaScriptException(_engine.TypeError, "in can only be used with an object");
                    }

                    value = o.HasProperty(TypeConverter.ToString(left));
                    break;
                
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public object EvaluateLogicalExpression(LogicalExpression logicalExpression)
        {
            var left = _engine.GetValue(EvaluateExpression(logicalExpression.Left));

            switch (logicalExpression.Operator)
            {

                case "&&":
                    if (!TypeConverter.ToBoolean(left))
                    {
                        return left;
                    }

                    return _engine.GetValue(EvaluateExpression(logicalExpression.Right));

                case "||":
                    if (TypeConverter.ToBoolean(left))
                    {
                        return left;
                    }

                    return _engine.GetValue(EvaluateExpression(logicalExpression.Right));

                default:
                    throw new NotImplementedException();
            }
        }

        public static bool Equal(object x, object y)
        {
            var typex = TypeConverter.GetType(x);
            var typey = TypeConverter.GetType(y);

            if (typex == typey)
            {
                if (typex == TypeCode.Empty)
                {
                    return true;
                }

                if (typex == TypeCode.Double)
                {
                    var nx = TypeConverter.ToNumber(x);
                    var ny = TypeConverter.ToNumber(y);

                    if (double.IsNaN(nx) || double.IsNaN(ny))
                    {
                        return false;
                    }

                    if (nx == ny)
                    {
                        return true;
                    }

                    return false;
                }

                if (typex == TypeCode.String)
                {
                    return TypeConverter.ToString(x) == TypeConverter.ToString(y);
                }

                if (typex == TypeCode.Boolean)
                {
                    return (bool) x == (bool) y;
                }

                return x == y;
            }

            if (x == Null.Instance && y == Undefined.Instance)
            {
                return true;
            }

            if (x == Undefined.Instance && y == Null.Instance)
            {
                return true;
            }

            if (typex == TypeCode.Double && typey == TypeCode.String)
            {
                return Equal(x, TypeConverter.ToNumber(y));
            }

            if (typex == TypeCode.String && typey == TypeCode.Double)
            {
                return Equal(TypeConverter.ToNumber(x), y);
            }

            if (typex == TypeCode.Boolean)
            {
                return Equal(TypeConverter.ToNumber(x), y);
            }

            if (typey == TypeCode.Boolean)
            {
                return Equal(x, TypeConverter.ToNumber(y));
            }

            if (typey == TypeCode.Object && (typex == TypeCode.String || typex == TypeCode.Double))
            {
                return Equal(x, TypeConverter.ToPrimitive(y));
            }

            if (typex == TypeCode.Object && (typey == TypeCode.String || typey == TypeCode.Double))
            {
                return Equal(TypeConverter.ToPrimitive(x), y);
            }

            return false;
        }

        public static bool StriclyEqual(object x, object y)
        {
            var typea = TypeConverter.GetType(x);
            var typeb = TypeConverter.GetType(y);

            if (typea != typeb)
            {
                return false;
            }

            if (typea == TypeCode.Empty)
            {
                return true;
            }
            if (typea == TypeCode.Double)
            {
                var nx = TypeConverter.ToNumber(x);
                var ny = TypeConverter.ToNumber(y);
                if (double.IsNaN(nx) || double.IsNaN(ny))
                {
                    return false;
                }

                if (nx == ny)
                {
                    return true;
                }

                return false;
            }
            if (typea == TypeCode.String)
            {
                return TypeConverter.ToString(x) == TypeConverter.ToString(y);
            }
            if (typea == TypeCode.Boolean)
            {
                return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
            }
            return x == y;
        }

        public static bool SameValue(object x, object y)
        {
            var typea = TypeConverter.GetType(x);
            var typeb = TypeConverter.GetType(y);

            if (typea != typeb)
            {
                return false;
            }

            if (typea == TypeCode.Empty)
            {
                return true;
            }
            if (typea == TypeCode.Double)
            {
                var nx = TypeConverter.ToNumber(x);
                var ny = TypeConverter.ToNumber(y);
                if (double.IsNaN(nx) && double.IsNaN(ny))
                {
                    return true;
                }

                if (nx == ny)
                {
                    if (nx == 0)
                    {
                        // +0 !== -0
                        return NumberInstance.IsNegativeZero(nx) == NumberInstance.IsNegativeZero(ny);
                    }

                    return true;
                }

                return false;
            }
            if (typea == TypeCode.String)
            {
                return TypeConverter.ToString(x) == TypeConverter.ToString(y);
            }
            if (typea == TypeCode.Boolean)
            {
                return TypeConverter.ToBoolean(x) == TypeConverter.ToBoolean(y);
            }
            return x == y;
        }

        public static object Compare(object x, object y, bool leftFirst = true)
        {
            object px, py;
            if (leftFirst)
            {
                px = TypeConverter.ToPrimitive(x, TypeCode.Double);
                py = TypeConverter.ToPrimitive(y, TypeCode.Double);
            }
            else
            {
                py = TypeConverter.ToPrimitive(y, TypeCode.Double);
                px = TypeConverter.ToPrimitive(x, TypeCode.Double);
            }
            var typea = TypeConverter.GetType(x);
            var typeb = TypeConverter.GetType(y);

            if (typea != TypeCode.String || typeb != TypeCode.String)
            {
                var nx = TypeConverter.ToNumber(px);
                var ny = TypeConverter.ToNumber(py);

                if (double.IsNaN(nx) || double.IsNaN(ny))
                {
                    return Undefined.Instance;
                }

                if (nx == ny)
                {
                    return false;
                }

                if (nx == double.PositiveInfinity)
                {
                    return false;
                }

                if (ny == double.PositiveInfinity)
                {
                    return true;
                }

                if (ny == double.NegativeInfinity)
                {
                    return false;
                }

                if (nx == double.NegativeInfinity)
                {
                    return true;
                }

                return nx < ny;
            }
            else
            {
                return String.CompareOrdinal(TypeConverter.ToString(x), TypeConverter.ToString(y)) < 0;
            }
        }

        public object EvaluateIdentifier(Identifier identifier)
        {
            return _engine.ExecutionContext.LexicalEnvironment.GetIdentifierReference(identifier.Name, _engine.Options.IsStrict());
        }

        public object EvaluateLiteral(Literal literal)
        {
            return literal.Value ?? Null.Instance;
        }

        public object EvaluateObjectExpression(ObjectExpression objectExpression)
        {
            // http://www.ecma-international.org/ecma-262/5.1/#sec-11.1.5

            var obj = _engine.Object.Construct(Arguments.Empty);
            foreach (var property in objectExpression.Properties)
            {
                var propName = property.Key.GetKey();
                var previous = obj.GetOwnProperty(propName);
                PropertyDescriptor propDesc;

                switch (property.Kind)
                {
                    case PropertyKind.Data:
                        var exprValue = _engine.EvaluateExpression(property.Value);
                        var propValue = _engine.GetValue(exprValue);
                        propDesc = new DataDescriptor(propValue) {Writable=true, Enumerable=true,Configurable = true};
                        break;

                    case PropertyKind.Get:
                        var getter = property.Value as FunctionExpression;

                        if (getter == null)
                        {
                            throw new JavaScriptException(_engine.SyntaxError);
                        }

                        var get = new ScriptFunctionInstance(
                            _engine,
                            getter,
                            _engine.ExecutionContext.LexicalEnvironment,
                            getter.Strict || _engine.Options.IsStrict()
                            );

                        propDesc = new AccessorDescriptor(get) { Enumerable = true, Configurable = true};
                        break;
                    
                    case PropertyKind.Set:
                        var setter = property.Value as FunctionExpression;

                        if (setter == null)
                        {
                            throw new JavaScriptException(_engine.SyntaxError);
                        }

                        var set = new ScriptFunctionInstance(
                            _engine,
                            setter,
                            _engine.ExecutionContext.LexicalEnvironment,
                            setter.Strict || _engine.Options.IsStrict()
                            );

                        propDesc = new AccessorDescriptor(null, set) { Enumerable = true, Configurable = true};
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (previous != Undefined.Instance)
                {
                    var previousIsData = previous.IsDataDescriptor();
                    var previousIsAccessor = previous.IsAccessorDescriptor();
                    var propIsData = propDesc.IsDataDescriptor();
                    var propIsAccessor = propDesc.IsAccessorDescriptor();

                    if (_engine.Options.IsStrict() && previousIsData && propIsData)
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }

                    if (previousIsData && propIsAccessor)
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }

                    if (previousIsAccessor && propIsData)
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }

                    if (previousIsAccessor && propIsAccessor)
                    {
                        var previousAccessor = previous.As<AccessorDescriptor>();
                        var propAccessor = propDesc.As<AccessorDescriptor>();

                        if (propAccessor.Set != null)
                        {
                            if (previousAccessor.Set != null)
                            {
                                throw new JavaScriptException(_engine.SyntaxError);
                            }

                            if (previousAccessor.Get != null)
                            {
                                propAccessor.Get = previousAccessor.Get;
                            }
                        }
                        else if (propAccessor.Get != null)
                        {
                            if (previousAccessor.Get != null)
                            {
                                throw new JavaScriptException(_engine.SyntaxError);
                            }

                            if (previousAccessor.Set != null)
                            {
                                propAccessor.Set = previousAccessor.Set;
                            }
                        }
                    }
                }

                obj.DefineOwnProperty(propName, propDesc, false);
            }

            return obj;
        }

        /// <summary>
        /// http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.1
        /// </summary>
        /// <param name="memberExpression"></param>
        /// <returns></returns>
        public object EvaluateMemberExpression(MemberExpression memberExpression)
        {
            var baseReference = EvaluateExpression(memberExpression.Object);
            var baseValue = _engine.GetValue(baseReference);

            string propertyNameString;
            if (!memberExpression.Computed) // index accessor ?
            {
                propertyNameString = memberExpression.Property.As<Identifier>().Name;
            }
            else
            {
                var propertyNameReference = EvaluateExpression(memberExpression.Property);
                var propertyNameValue = _engine.GetValue(propertyNameReference);
                TypeConverter.CheckObjectCoercible(_engine, baseValue);
                propertyNameString = TypeConverter.ToString(propertyNameValue);
            }

            return new Reference(baseValue, propertyNameString, _engine.Options.IsStrict());
        }

        public object EvaluateFunctionExpression(FunctionExpression functionExpression)
        {
            return new ScriptFunctionInstance(
                _engine,
                functionExpression,
                LexicalEnvironment.NewDeclarativeEnvironment(_engine, _engine.ExecutionContext.LexicalEnvironment),
                functionExpression.Strict
                );
        }

        public object EvaluateCallExpression(CallExpression callExpression)
        {
            var callee = EvaluateExpression(callExpression.Callee);

            var func = _engine.GetValue(callee);

            if (func == Undefined.Instance)
            {
                throw new JavaScriptException(_engine.TypeError);
            }

            object thisObject;

            // todo: implement as in http://www.ecma-international.org/ecma-262/5.1/#sec-11.2.4
            var arguments = callExpression.Arguments.Select(EvaluateExpression).Select(_engine.GetValue).ToArray();

            if (TypeConverter.GetType(func) != TypeCode.Object)
            {
                throw new JavaScriptException(_engine.TypeError);
            }

            var callable = func as ICallable;
            if (callable == null)
            {
                throw new JavaScriptException(_engine.TypeError);
            }

            var r = callee as Reference;
            if (r != null)
            {
                if (r.IsPropertyReference())
                {
                    thisObject = r.GetBase();
                }
                else
                {
                    var env = r.GetBase() as EnvironmentRecord;
                    thisObject = env.ImplicitThisValue();
                }
            }
            else
            {
                thisObject = Undefined.Instance;
            }

            return callable.Call(thisObject, arguments);
        }

        public object EvaluateSequenceExpression(SequenceExpression sequenceExpression)
        {
            foreach (var expression in sequenceExpression.Expressions)
            {
                _engine.EvaluateExpression(expression);
            }

            return Undefined.Instance;
        }

        public object EvaluateUpdateExpression(UpdateExpression updateExpression)
        {
            var value = _engine.EvaluateExpression(updateExpression.Argument);
            Reference r;

            switch (updateExpression.Operator)
            {
                case "++":
                    r = value as Reference;
                    if (r != null
                        && r.IsStrict()
                        && (r.GetBase() is EnvironmentRecord)
                        && (Array.IndexOf(new[] { "eval", "arguments" }, r.GetReferencedName()) != -1))
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }

                    var oldValue = _engine.GetValue(value);
                    var newValue = TypeConverter.ToNumber(oldValue) + 1;
                    _engine.PutValue(r, newValue);

                    return updateExpression.Prefix ? newValue : oldValue;

                case "--":
                    r = value as Reference;
                    if (r != null
                        && r.IsStrict()
                        && (r.GetBase() is EnvironmentRecord)
                        && (Array.IndexOf(new[] { "eval", "arguments" }, r.GetReferencedName()) != -1))
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }

                    oldValue = _engine.GetValue(value);
                    newValue = TypeConverter.ToNumber(oldValue) - 1;
                    _engine.PutValue(r, newValue);

                    return updateExpression.Prefix ? newValue : oldValue;
                default:
                    throw new ArgumentException();
            }

        }

        public object EvaluateThisExpression(ThisExpression thisExpression)
        {
            return _engine.ExecutionContext.ThisBinding;
        }

        public object EvaluateNewExpression(NewExpression newExpression)
        {
            var arguments = newExpression.Arguments.Select(EvaluateExpression).Select(_engine.GetValue).ToArray();
            
            // todo: optimize by defining a common abstract class or interface
            var callee = _engine.GetValue(EvaluateExpression(newExpression.Callee)) as IConstructor;
            
            if (callee == null)
            {
                throw new JavaScriptException(_engine.TypeError, "The object can't be used as constructor.");
            }

            // construct the new instance using the Function's constructor method
            var instance = callee.Construct(arguments);

            return instance;
        }

        public object EvaluateArrayExpression(ArrayExpression arrayExpression)
        {
            var a = _engine.Array.Construct(Arguments.Empty);
            var n = 0;
            foreach (var expr in arrayExpression.Elements)
            {
                var value = expr == null ? Null.Instance : _engine.GetValue(EvaluateExpression(expr));
                a.DefineOwnProperty(n.ToString(), new DataDescriptor(value) { Writable = true, Enumerable = true, Configurable = true }, false);
                n++;
            }
            
            return a;
        }

        public object EvaluateUnaryExpression(UnaryExpression unaryExpression)
        {
            var value = _engine.EvaluateExpression(unaryExpression.Argument);
            Reference r;

            switch (unaryExpression.Operator)
            {
                case "+":
                    return TypeConverter.ToNumber(_engine.GetValue(value));
                    
                case "-":
                    var n = TypeConverter.ToNumber(_engine.GetValue(value));
                    return double.IsNaN(n) ? double.NaN : n*-1;
                
                case "~":
                    return ~TypeConverter.ToInt32(_engine.GetValue(value));
                
                case "!":
                    return !TypeConverter.ToBoolean(_engine.GetValue(value));
                
                case "delete":
                    r = value as Reference;
                    if (r == null)
                    {
                        return true;
                    }
                    if (r.IsUnresolvableReference())
                    {
                        if (r.IsStrict())
                        {
                            throw new JavaScriptException(_engine.SyntaxError);
                        }

                        return true;
                    }
                    if (r.IsPropertyReference())
                    {
                        var o = TypeConverter.ToObject(_engine, r.GetBase());
                        return o.Delete(r.GetReferencedName(), r.IsStrict());
                    }
                    if (r.IsStrict())
                    {
                        throw new JavaScriptException(_engine.SyntaxError);
                    }
                    var bindings = r.GetBase() as EnvironmentRecord;
                    return bindings.DeleteBinding(r.GetReferencedName());
                
                case "void":
                    _engine.GetValue(value);
                    return Undefined.Instance;

                case "typeof":
                    r = value as Reference;
                    if (r != null)
                    {
                        if (r.IsUnresolvableReference())
                        {
                            return "undefined";
                        }
                    }
                    var v = _engine.GetValue(value);
                    if (v == Undefined.Instance)
                    {
                        return "undefined";
                    }
                    if (v == Null.Instance)
                    {
                        return "object";
                    }
                    switch (TypeConverter.GetType(v))
                    {
                        case TypeCode.Boolean: return "boolean";
                        case TypeCode.Double: return "number";
                        case TypeCode.String: return "string";
                    }
                    if (v is ICallable)
                    {
                        return "function";
                    }
                    return "object";

                default:
                    throw new ArgumentException();
            }
        }
    }
}
