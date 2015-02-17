using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Memoise
{
    public class MemoiseFactory : IMemoiseFactory
    {
        public TMemoised CreateMemoised<TMemoised>(TMemoised instance)
        {
            var type = BuildType<TMemoised>();

            var memoisedInstance = Activator.CreateInstance(type, instance);

            return (TMemoised)memoisedInstance;
        }

        private Type BuildType<TMemoised>()
        {
            var moduleBuilder = ModuleBuilder.Value;

            var typeBuilder = moduleBuilder.DefineType("Memoise_" + Guid.NewGuid().ToString("N"),
                TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.Class);

            var interfaceType = typeof(TMemoised);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var methodsToMemoise = interfaceType.GetMethods()
                .Where(method => method.ReturnType != typeof(void) || MethodHasRefParam(method))
                .ToArray();

            var methodsNotToMemoise = interfaceType.GetMethods()
                .Where(method => !methodsToMemoise.Contains(method))
                .ToArray();

            var instanceField = typeBuilder.DefineField("_instance", interfaceType, FieldAttributes.Private);

            var resultFields = DefineResultFields(methodsToMemoise, typeBuilder);

            DefineConstructor(typeBuilder, interfaceType, instanceField, resultFields);

            GenerateMemoiseMethods(methodsToMemoise, typeBuilder, instanceField, resultFields);
            GenerateDelegates(methodsNotToMemoise, typeBuilder, instanceField);

            var type = typeBuilder.CreateType();

            return type;
        }

        private void GenerateMemoiseMethods(
            MethodInfo[] methods, TypeBuilder typeBuilder, FieldBuilder instanceField, FieldBuilder[] resultFields)
        {
            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];

                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    method.ReturnType,
                    method.GetParameters().Select(x => x.ParameterType).ToArray());

                var ilGenerator = methodBuilder.GetILGenerator();

                // create key
                var parameters = GetTupleParameters(method);
                var keyTupleType = GetTupleTypeForParameters(parameters);
                var resultTupleType = GetResultTupleTypeForMethod(method);

                var keyLocal = ilGenerator.DeclareLocal(keyTupleType);
                var resultLocal = ilGenerator.DeclareLocal(resultTupleType);
                var resultNotFoundLabel = ilGenerator.DefineLabel();

                // load each argument
                for (var j = 0; j < method.GetParameters().Count(); j++)
                {
                    var parameter = method.GetParameters()[j];

                    ilGenerator.Emit(OpCodes.Ldarg, (short)(j + 1));

                    if (parameter.ParameterType.IsByRef)
                        ilGenerator.Emit(OpCodes.Ldind_Ref);
                }

                // new tuple, we now have the key for this dict
                ilGenerator.Emit(OpCodes.Newobj, keyTupleType.GetConstructor(parameters));
                ilGenerator.Emit(OpCodes.Stloc, keyLocal);

                // set result to null for now
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Stloc, resultLocal);

                // perform Dictionary.TryGetValue
                var resultField = resultFields[i];
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, resultField);
                ilGenerator.Emit(OpCodes.Ldloc, keyLocal);
                ilGenerator.Emit(OpCodes.Ldloca_S, resultLocal);

                var dictType = GetDictionaryType(keyTupleType, resultTupleType);
                ilGenerator.Emit(OpCodes.Callvirt, dictType.GetMethod("TryGetValue"));

                // if result of TryGetValue was false, go to rest of method, else return value
                ilGenerator.Emit(OpCodes.Brfalse_S, resultNotFoundLabel);
                ReturnFromMemoisedMethod(method, ilGenerator, resultTupleType);

                // A result has not previously been stored, so get the result and store it...
                ilGenerator.MarkLabel(resultNotFoundLabel);

                CallUnderlyingMethod(instanceField, ilGenerator, method);

                // set values of result tuple
                // result will already be on stack, no need to load it
                // load each ref/out param onto stack
                var methodParameters = method.GetParameters();
                for (int j = 0; j < methodParameters.Length; j++)
                {
                    if (!methodParameters[j].ParameterType.IsByRef)
                        continue;

                    ilGenerator.Emit(OpCodes.Ldarg, (short)(j + 1));
                    ilGenerator.Emit(OpCodes.Ldind_Ref);
                }
                ilGenerator.Emit(OpCodes.Newobj, resultTupleType.GetConstructor(resultTupleType.GetGenericArguments()));
                ilGenerator.Emit(OpCodes.Stloc, resultLocal);

                // store result tuple
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, resultField);
                ilGenerator.Emit(OpCodes.Ldloc, keyLocal);  // key
                ilGenerator.Emit(OpCodes.Ldloc, resultLocal);  //result
                ilGenerator.Emit(OpCodes.Callvirt, dictType.GetMethod("Add"));

                // return result
                ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
                ilGenerator.Emit(OpCodes.Callvirt, resultTupleType.GetProperty("Item1").GetGetMethod());
                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        private static void ReturnFromMemoisedMethod(MethodInfo method, ILGenerator ilGenerator, Type resultTupleType)
        {
            SetOutAndRefParameters(method, ilGenerator, resultTupleType);
            ReturnFromMemoisedMethod(ilGenerator, resultTupleType);
        }

        private static void ReturnFromMemoisedMethod(ILGenerator ilGenerator, Type resultTupleType)
        {
            ilGenerator.Emit(OpCodes.Ldloc_1);
            ilGenerator.Emit(OpCodes.Callvirt, resultTupleType.GetProperty("Item1").GetGetMethod());
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void SetOutAndRefParameters(MethodInfo method, ILGenerator ilGenerator, Type resultTupleType)
        {
            var methodParameters = method.GetParameters();
            for (int i = 0, item = 2; i < methodParameters.Length; i++)
            {
                if (!methodParameters[i].ParameterType.IsByRef)
                    continue;

                ilGenerator.Emit(OpCodes.Ldarg, (short)(i + 1));
                ilGenerator.Emit(OpCodes.Ldloc_1);
                ilGenerator.Emit(OpCodes.Callvirt, resultTupleType.GetProperty(string.Format("Item{0}", item)).GetGetMethod());
                ilGenerator.Emit(OpCodes.Stind_Ref);
                item++;
            }
        }

        private void GenerateDelegates(MethodInfo[] methods, TypeBuilder typeBuilder,
            FieldBuilder instanceField)
        {
            foreach (var method in methods)
            {
                var methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                    CallingConventions.HasThis,
                    method.ReturnType,
                    method.GetParameters().Select(x => x.ParameterType).ToArray());

                var ilGenerator = methodBuilder.GetILGenerator();

                CallUnderlyingMethod(instanceField, ilGenerator, method);

                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        private static void CallUnderlyingMethod(FieldBuilder instanceField, ILGenerator ilGenerator, MethodInfo method)
        {
            // load this
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // load instance field
            ilGenerator.Emit(OpCodes.Ldfld, instanceField);

            // load each argument
            for (var i = 0; i < method.GetParameters().Count(); i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, (short)(i + 1));
            }

            // call instance
            ilGenerator.Emit(OpCodes.Callvirt, method);
        }

        private void DefineConstructor(
            TypeBuilder typeBuilder, Type interfaceType, FieldBuilder instanceField, FieldBuilder[] resultFields)
        {
            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.HasThis, new[] { interfaceType });

            var ctorGenerator = ctorBuilder.GetILGenerator();

            // new up any result field
            foreach (var field in resultFields)
            {
                var fieldCtor = field.FieldType.GetConstructor(Type.EmptyTypes);
                ctorGenerator.Emit(OpCodes.Ldarg_0);
                ctorGenerator.Emit(OpCodes.Newobj, fieldCtor);
                ctorGenerator.Emit(OpCodes.Stfld, field);
            }

            // call base class constructor - in this case the base class is object
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));

            // store a reference to the object being memoised
            ctorGenerator.Emit(OpCodes.Ldarg_0);
            ctorGenerator.Emit(OpCodes.Ldarg_1);
            ctorGenerator.Emit(OpCodes.Stfld, instanceField);

            ctorGenerator.Emit(OpCodes.Ret);
        }

        private FieldBuilder[] DefineResultFields(
            MethodInfo[] methodsToMemoise, TypeBuilder typeBuilder)
        {
            // results will be stored and looked up based on a tuple of parameters passed in to a given method
            var fields = new List<FieldBuilder>();
            for (var i = 0; i < methodsToMemoise.Length; i++)
            {
                var method = methodsToMemoise[i];
                var parameterTypes = GetTupleParameters(method);

                var keyTupleType = GetTupleTypeForParameters(parameterTypes);
                var resultTupleType = GetResultTupleTypeForMethod(method);

                var dictionaryType = GetDictionaryType(keyTupleType, resultTupleType);
                var fieldName = GetResultsFieldForMethodIndex(i);
                var field = typeBuilder.DefineField(fieldName, dictionaryType, FieldAttributes.Private);
                fields.Add(field);
            }
            return fields.ToArray();
        }

        private Type GetResultTupleTypeForMethod(MethodInfo method)
        {
            var resultTypes = new[] { method.ReturnType }.Concat(
                method.GetParameters()
                    .Where(p => p.ParameterType.IsByRef)
                    .Select(p => p.ParameterType.GetElementType()))
                .ToArray();
            var resultTupleType = GetTupleTypeForParameters(resultTypes);
            return resultTupleType;
        }

        private static Type GetDictionaryType(Type keyTupleType, Type valueTupleType)
        {
            var openDictionaryType = typeof(Dictionary<,>);
            var dictionaryType = openDictionaryType.MakeGenericType(keyTupleType, valueTupleType);
            return dictionaryType;
        }

        private static Type[] GetTupleParameters(MethodInfo method)
        {
            var parameterTypes = method.GetParameters()
                .Select(parameter => parameter.ParameterType.IsByRef
                    ? parameter.ParameterType.GetElementType()
                    : parameter.ParameterType)
                .ToArray();

            return parameterTypes;
        }

        private string GetResultsFieldForMethodIndex(int i)
        {
            return string.Format("results_{0}", i);
        }

        private Type GetTupleTypeForParameters(Type[] parameterTypes)
        {
            var openTupleType = Type.GetType(string.Format("System.Tuple`{0}", parameterTypes.Length));
            var tupleType = openTupleType.MakeGenericType(parameterTypes);
            return tupleType;
        }

        private static bool MethodHasRefParam(MethodInfo method)
        {
            return method.GetParameters().Any(param => param.ParameterType.IsByRef);
        }

        private static readonly Lazy<ModuleBuilder> ModuleBuilder = new Lazy<ModuleBuilder>(() =>
        {
            var assemblyName = new AssemblyName("MemoiseAssembly_" + Guid.NewGuid().ToString("N"));

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MemoiseModule_" + Guid.NewGuid().ToString("N"));

            return moduleBuilder;
        });
    }
}
