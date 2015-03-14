# memoise.net
Create memoised types of any interface at runtime

Usage:
```
var memoised = MemoiseFactory.Create<TInterface>(instance);

Console.WriteLine(memoised.SomeMethod(5));  // outputs 'x'
Console.WriteLine(memoised.SomeMethod(5));  // outputs 'x' - memoised (cached) from last call
```

The type is generated on the time by emitting IL. Methods that have no output are not memoised, calls are simply delegated to the underlying instance.

The outline for how each memoised method is generated is as follows:
```
TResult SomeMethod(int arg0, string arg1)
{
  var key = new Tuple(arg0, arg1);
  TResult result;
  if (dict.TryGetValue(key, out result))
    return result;

  result = instance.SomeMethod(arg0, arg1);
  dict.Add(key, result);
  return result;
}
```
Each method gets its own dictionary, the key is a tuple made up of all params, the value is the result of the underlying method call.

The reason for using Tuple as a key is that they make good composite key wrappers because of the
implementation of .Equals
