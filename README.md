# ConstExpressionForUnity
[日本語](README_ja.md)

## Summary
This library "ConstExpressionForUnity" is a compile-time calculation library.  
It finds the constant computation process at compile time, and only the result of the computation can be embedded in the code.  
By using this library, you can implement high-load processes such as hash calculation and de Bruijn sequence calculation at zero cost.  

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
}
```

```Test.cs
using UnityEngine;

public static class Test
{
    public static void Run()
    {
        Debug.Log(MyConstExpression.Add(100, 200));
    }
}
```

### Post-compilation code(with ILSpy)
```Test.cs
using UnityEngine;

public static class Test
{
	public static void Run()
	{
		Debug.Log((object)300);
	}
}
```
Thus, unnecessary run-time calculations can be omitted and only the results of the calculations can be embedded in the code.  

## Performance
### Measurement code on the editor
[Test Code.](packages/Tests/Runtime/ConstExpressionPerformanceTest.cs)  
Multiple prime number calculations by Sieve of Eratosthenes.

#### Result

|  Process  |  Processing Time  |
| ---- | ---- |
| CalcPrime_ConstExpression | 0.056805 ms |
| CalcPrime_Raw | 78.87844 ms |

Since the calculation itself disappears and only the result of the calculation is returned, the cost is almost negligible.  

## How to install
### Installing ILPostProcessorCommon
Refer to [ILPostProcessorCommon v1.1.0](https://github.com/Katsuya100/ILPostProcessorCommon/tree/v1.1.0) for installation.

### Installing ConstExpressionForUnity
1. Open [Window > Package Manager].
2. click [+ > Add package from git url...].
3. Type `https://github.com/Katsuya100/ConstExpressionForUnity.git?path=packages` and click [Add].

#### If it doesn't work
The above method may not work well in environments where git is not installed.  
Download the appropriate version of `com.katuusagi.constexpressionforunity.tgz` from [Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases), and then [Package Manager > + > Add package from tarball...] Use [Package Manager > + > Add package from tarball...] to install the package.

#### If it still doesn't work
Download the appropriate version of `Katuusagi.ConstExpressionForUnity.unitypackage` from [Releases](https://github.com/Katsuya100/ConstExpressionForUnity/releases) and Import it into your project from [Assets > Import Package > Custom Package].

## How to Use
### Normal usage
#### Creating Assembly for ConstExpression
1. in any folder in the Project Browser, select [right click > Create > Folder].
2. Set a name for the folder (e.g. ConstExpressionEntry). 
3. select [right click > Create > Assembly Definition Reference] on the folder of 2.
4. Set an asset name (e.g. `ConstExpressionEntry.asmref`). 
5. Click on the asset of 4. and set `ConstExpressionEntry` to `Assembly Definition` in Inspector.

#### Implementing Calculation Functions
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
Functions with the `ConstExpression` attribute are subject to compile-time constant calculation.  

### Constraints on compile-time constant calculations
There are several restrictions to performing compile-time constant calculations.

#### ConstExpression Attribute
Only functions with the `ConstExpression` attribute are subject to compile-time constant calculation.

#### Static Function
The `ConstExpression` attribute can only be attached to static functions.

#### Argument
The `ConstExpression` attribute may only be attached to functions with the following argument types  
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
      <td></td>
      <td></td>
    </tr>
 </table>

Also, to perform compile-time constant calculations, the value assigned to the argument must be a constant.  
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

#### Return
The `ConstExpression` attribute may only be attached to functions with the following return types  

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
      <td>struct<br/>- Marshal.SizeOf possible<br/>- No class as a member</td>
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
      <td></td>
      <td></td>
      <td></td>
    </tr>
 </table>

struct and ReadOnlyArray cannot be literalized and are held as static readonly variables.

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
