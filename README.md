# ConstExpressionForUnity
[日本語](README_ja.md)

## Summary
This library "ConstExpressionForUnity" is a compile-time computation library.  
With `ConstExpression`, you can find a constant computation process at compile-time and embed only the result of the computation in your code.  
By using it, you can implement high-load processes such as hash computation and de Brownian sequence computation at zero cost.  
  
Also included in the library is `StaticExpression`, a runtime pre-computation function.  
With `StaticExpression`, the constant computation process is performed statically.  

## System Requirements
|  Environment  |  Version  |
| ---- | ---- |
| Unity | 2021.3.15f1, 2022.3.2f1 |
| .Net | 4.x, Standard 2.1 |

## Example of ConstExpression execution
### Implementation Example
```MyConstExpression.cs
using Katuusagi.ConstExpressionForUnity;

public static class MyConstExpression
{
    [ConstExpression]
    public static int Add(int l, int r)
    {
        return l + r;
    }

    [StaticExpression]
    public static int Add2(int l, int r)
    {
        return l + r;
    }
}
```

```Test.cs
using UnityEngine;

public static class Test
{
    public static void Run()
    {
        Debug.Log(MyConstExpression.Add(100, 200));
        Debug.Log(MyConstExpression.Add2(100, 200));
    }
}
```

### Post-compilation code(with ILSpy)
```Test.cs
using Katuusagi.ConstExpressionForUnity.Generated;
using UnityEngine;

public static class Test
{
	public static void Run()
	{
		Debug.Log((object)300);
		Debug.Log((object)$$StaticTable.$0);
	}
}
```
Thus, unnecessary run-time computation can be omitted and only the results of the computation can be embedded in the code.  

## Performance
### Measurement code on the editor
[Test Code.](packages/Tests/Runtime/ConstExpressionPerformanceTest.cs)  
Multiple prime number calculations by Sieve of Eratosthenes.

#### Result

|  Process  |  Processing Time  |
| ---- | ---- |
| CalcPrime_ConstExpression | 0.0138 ms |
| CalcPrime_StaticExpression | 0.0201 ms |
| CalcPrime_Raw | 17.98335 ms |

Since the computation itself disappears and only the result of the computation is returned, the cost is almost negligible.  

## How to install
### Installing ILPostProcessorCommon
Refer to [ILPostProcessorCommon v2.2.0](https://github.com/Katsuya100/ILPostProcessorCommon/tree/v2.2.0) for installation.

### Installing ConstExpressionForUnity
1. Open [Window > Package Manager].
2. click [+ > Add package from git url...].
3. Type `https://github.com/Katsuya100/ConstExpressionForUnity.git?path=packages` and click [Add].

#### If it doesn't work
The above method may not work well in environments where git is not installed.  
Download the appropriate version of `com.katuusagi.constexpressionforunity.tgz` from [Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases), and then [Package Manager > + > Add package from tarball...] Use [Package Manager > + > Add package from tarball...] to install the package.

#### If it still doesn't work
Download the appropriate version of `Katuusagi.ConstExpressionForUnity.unitypackage` from [Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases) and Import it into your project from [Assets > Import Package > Custom Package].

## How to Use ConstExpression
### Normal usage
#### Creating Assembly for ConstExpression
1. in any folder in the Project Browser, select [right click > Create > Folder].
2. Set a name for the folder (e.g. ConstExpressionEntry). 
3. select [right click > Create > Assembly Definition Reference] on the folder of 2.
4. Set an asset name (e.g. `ConstExpressionEntry.asmref`). 
5. Click on the asset of 4. and set `ConstExpressionEntry` to `Assembly Definition` in Inspector.

#### Implementing Computation Functions
Implement the script as follows under the folder of the assembly for ConstExpression.
```.cs
using Katuusagi.ConstExpressionForUnity;

public static class MyConstExpression
{
    [ConstExpression]
    public static int Add(int l, int r)
    {
        return l + r;
    }
}
```
Functions with the `ConstExpression` attribute are subject to compile-time constant computations.  

### Constraints on compile-time constant computations.
There are several restrictions to performing compile-time constant computations.

#### ConstExpression Attribute
Only functions with the `ConstExpression` attribute are subject to compile-time constant computations.

#### Static Function
The `ConstExpression` attribute can only be attached to static functions.

#### Arguments/Return Values
The `ConstExpression` attribute may only be attached to functions with the following argument/return types.  
<table>
    <tr>
      <td>string</td>
      <td>char</td>
      <td>sbyte</td>
      <td>byte</td>
    </tr>
    <tr>
      <td>short</td>
      <td>ushort</td>
      <td>int</td>
      <td>uint</td>
    </tr>
    <tr>
      <td>long</td>
      <td>ulong</td>
      <td>float</td>
      <td>double</td>
    </tr>
    <tr>
      <td>bool</td>
      <td>enum</td>
      <td>struct<br/>- No reference type instances as members</td>
      <td></td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltstring&gt</td>
      <td>ReadOnlyArray&ltchar&gt</td>
      <td>ReadOnlyArray&ltsbyte&gt</td>
      <td>ReadOnlyArray&ltbyte&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltshort&gt</td>
      <td>ReadOnlyArray&ltushort&gt</td>
      <td>ReadOnlyArray&ltint&gt</td>
      <td>ReadOnlyArray&ltuint&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltlong&gt</td>
      <td>ReadOnlyArray&ltulong&gt</td>
      <td>ReadOnlyArray&ltfloat&gt</td>
      <td>ReadOnlyArray&ltdouble&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltbool&gt</td>
      <td>ReadOnlyArray&ltenum&gt</td>
      <td>ReadOnlyArray&ltstruct&gt<br/>- No reference type instances as members</td>
      <td></td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltReadOnlyArray&lt...&gt&gt</td>
      <td></td>
      <td></td>
      <td></td>
    </tr>
 </table>

Also, to perform compile-time constant computations, the value assigned to the argument must be a constant.  
Failure to assign a constant will result in a warning.  
```.cs
// OK
MyConstExpression.Add(100, 200);

// OK
const int l = 100;
const int r = 200;
MyConstExpression.Add(l, r);

// NG
int l = 100;
int r = 200;
MyConstExpression.Add(l, r);

// NG
readonly int l = 100;
readonly int r = 200;
MyConstExpression.Add(l, r);
```

Because struct and ReadOnlyArray cannot be literalized, the return value is held as a static readonly variable.  
If passed as an argument, it must be taken from the return value of the `ConstExpression` method.  
```.cs
// OK
MyConstExpression.Add(MyConstExpression.MakeVector2(10, 20), MyConstExpression.MakeVector2(30, 40));

// NG
MyConstExpression.Add(new Vector2(10, 20), new Vector2(30, 40));
```

##### CalculationFailedWarning option
Setting the `ConstExpression` attribute to `CalculationFailedWarning=false` will prevent the warning from occurring when assigning a non-constant value to an argument.
```
[ConstExpression(CalculationFailedWarning = false)]
public static int Add(int l, int r)
{
    return l + r;
}

// No warning occurs.
int l = 100;
int r = 200;
MyConstExpression.Add(l, r);
```

## How to Use StaticExpression
### Normal usage
#### Creating Assembly for StaticExpression
Implement the script as follows.
```.cs
using Katuusagi.ConstExpressionForUnity;

public static class MyStaticExpression
{
    [StaticExpression]
    public static int Add(int l, int r)
    {
        return l + r;
    }
}
```
Functions with the `StaticExpression` attribute are subject to runtime constant pre-computations.  

### Constraints on runtime constant pre-computations.
There are several limitations to performing runtime constant pre-computations.

#### StaticExpression属性
Only functions with the `StaticExpression` attribute are subject to runtime constant pre-computations

#### static関数
The `StaticExpression` attribute can only be attached to static functions.

#### 引数/戻り値
The `StaticExpression` attribute can only be attached to functions with the following argument/return types.  
Unlike `ConstExpression`, reflection types can be used.  
 <table>
    <tr>
      <td>string</td>
      <td>char</td>
      <td>sbyte</td>
      <td>byte</td>
    </tr>
    <tr>
      <td>short</td>
      <td>ushort</td>
      <td>int</td>
      <td>uint</td>
    </tr>
    <tr>
      <td>long</td>
      <td>ulong</td>
      <td>float</td>
      <td>double</td>
    </tr>
    <tr>
      <td>bool</td>
      <td>enum</td>
      <td>struct<br/>- No reference type instances as members</td>
      <td>Type</td>
    </tr>
    <tr>
      <td>MemberInfo</td>
      <td>TypeInfo</td>
      <td>ConstructorInfo</td>
      <td>FieldInfo</td>
    </tr>
    <tr>
      <td>PropertyInfo</td>
      <td>MethodInfo</td>
      <td>MethodBase</td>
      <td>EventInfo</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltstring&gt</td>
      <td>ReadOnlyArray&ltchar&gt</td>
      <td>ReadOnlyArray&ltsbyte&gt</td>
      <td>ReadOnlyArray&ltbyte&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltshort&gt</td>
      <td>ReadOnlyArray&ltushort&gt</td>
      <td>ReadOnlyArray&ltint&gt</td>
      <td>ReadOnlyArray&ltuint&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltlong&gt</td>
      <td>ReadOnlyArray&ltulong&gt</td>
      <td>ReadOnlyArray&ltfloat&gt</td>
      <td>ReadOnlyArray&ltdouble&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltbool&gt</td>
      <td>ReadOnlyArray&ltenum&gt</td>
      <td>ReadOnlyArray&ltstruct&gt<br/>- No reference type instances as members</td>
      <td>ReadOnlyArray&ltType&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltMemberInfo&gt</td>
      <td>ReadOnlyArray&ltTypeInfo&gt</td>
      <td>ReadOnlyArray&ltConstructorInfo&gt</td>
      <td>ReadOnlyArray&ltFieldInfo&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltPropertyInfo&gt</td>
      <td>ReadOnlyArray&ltMethodInfo&gt</td>
      <td>ReadOnlyArray&ltMethodBase&gt</td>
      <td>ReadOnlyArray&ltEventInfo&gt</td>
    </tr>
    <tr>
      <td>ReadOnlyArray&ltReadOnlyArray&lt...&gt&gt</td>
      <td></td>
      <td></td>
      <td></td>
    </tr>
 </table>

Also, to perform runtime constant pre-computations, the value assigned to the argument must be a constant.  
Failure to assign a constant will result in a warning.  
```.cs
// OK
MyStaticExpression.Add(100, 200);

// OK
const int l = 100;
const int r = 200;
MyStaticExpression.Add(l, r);

// NG
int l = 100;
int r = 200;
MyStaticExpression.Add(l, r);

// NG
readonly int l = 100;
readonly int r = 200;
MyStaticExpression.Add(l, r);
```

Type arguments can also be used, but the type must be unambiguous.  
Setting a GenericParameter to a type argument will result in a warning.  
```.cs
// OK
MyStaticExpression.Add<int>(100, 200);

// NG
MyStaticExpression.Add<T>(100, 200);
```

Because struct and ReadOnlyArray cannot be literalized, the return value is held as a static readonly variable.
If passed as an argument, it must be taken from the return value of ConstExpression or StaticExpression.
```.cs
// OK
MyStaticExpression.Add(MyStaticExpression.MakeVector2(10, 20), MyStaticExpression.MakeVector2(30, 40));

// NG
MyStaticExpression.Add(new Vector2(10, 20), new Vector2(30, 40));
```

##### CalculationFailedWarningオプション
Setting the `ConstExpression` attribute to `CalculationFailedWarning=false` will prevent the warning from occurring when assigning a non-constant value to an argument.
```
[StaticExpression(CalculationFailedWarning = false)]
public static int Add(int l, int r)
{
    return l + r;
}

// No warning occurs.
MyStaticExpression.Add(l, r);
```

### About ReadOnlyArray
ReadOnlyArray is an immutable array prepared as a return constant for ConstExpression.  
It inherits from IReadOnlyList and can be implicitly cast to ReadOnlySpan.  
Implicit casts from arrays are also possible.  
```.cs
// OK
ReadOnlyArray<byte> binary = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

// OK
ReadOnlySpan<byte> spanBinary = binary;

// OK
IReadOnlyList<byte> listBinary = binary;

// NG
byte[] bytes = binary;
```
